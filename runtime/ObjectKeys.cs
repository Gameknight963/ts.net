using System;
using System.Collections.Generic;
using System.Globalization;

public abstract partial class TsObject
{
    public virtual object OwnKeys() => IsModuleNamespace ? ModuleNamespaceKeys() : KeysFromFixed(Array.Empty<object>());
    protected double ExtraKeyCount => extra?.StringCount ?? 0;
    // Unknown subclasses retain their enumeration behavior; generated shapes
    // and array representations override this with allocation-free counts.
    public virtual double OwnKeyCount() => Js.StringPropertyKeyCount(OwnKeys());
    public double KeyCountWithFixed(int count) => count + ExtraKeyCount;
    // Generated callers prove that none of the fixed names are array indices.
    // Extras may introduce indices, so they still need the general ordering path.
    public object KeysFromTextFields(object[] names) => extra == null || extra.Count == 0
        ? new JsArray(names) : KeysFromFixed(names);
    public object KeysFromFixed(object[] names) => new JsArray(KeyNamesFromFixed(names));
    object[] KeyNamesFromFixed(object[] names)
    {
        if (extra == null || extra.Count == 0) return Js.OrderOwnKeyNames(names);
        var all = new object[names.Length + extra.Count];
        Array.Copy(names, all, names.Length);
        // With no fixed fields, every key comes from the unique property store.
        var seen = names.Length == 0 ? null : new HashSet<string>();
        foreach (string name in names) seen.Add(name);
        int count = names.Length;
        foreach (string name in extra.Keys) if (seen == null || seen.Add(name)) all[count++] = name;
        if (count != all.Length) Array.Resize(ref all, count);
        return Js.OrderOwnKeyNames(all);
    }
    internal object[] JsonPropertyNames(object[] previous)
    {
        if (GetType() != typeof(JsDynamicObject) || IsModuleNamespace || ((JsDynamicObject)this).HasPropertyDescriptors) return null;
        if (previous != null && (extra?.Count ?? 0) == previous.Length)
        {
            // Compare every key before sharing an immutable snapshot. A nested
            // toJSON or replacer may have changed this object's properties.
            int index = 0;
            bool matches = true;
            if (extra != null)
                foreach (string name in extra.Keys)
                    if (name != (string)previous[index++]) { matches = false; break; }
            if (matches) return previous;
        }
        var names = KeyNamesFromFixed(Array.Empty<object>());
        int count = 0;
        foreach (string name in names) if (!Js.IsSymbolKey(name)) names[count++] = name;
        if (count != names.Length) Array.Resize(ref names, count);
        return names;
    }
}

partial struct ArrayStorage<T>
{
    internal double CountPresent(Func<T, bool> present)
    {
        double count = 0;uint end = Math.Min(Length, (uint)dense.Length);
        for (uint i = 0; i < end; i++) if (present(dense[i])) count++;
        if (sparse != null) foreach (var item in sparse) if (item.Key < Length && present(item.Value)) count++;
        return count;
    }
    internal List<uint> PresentIndices(Func<T, bool> present)
    {
        var result = new List<uint>();
        uint end = Math.Min(Length, (uint)dense.Length);
        for (uint i = 0; i < end; i++) if (present(dense[i])) result.Add(i);
        if (sparse != null)
            foreach (var item in sparse) if (item.Key < Length && present(item.Value)) result.Add(item.Key);
        result.Sort();
        return result;
    }
}

public partial class JsArray
{
    internal virtual double ElementKeyCount() => values.CountPresent(static value => !ReferenceEquals(value, Hole));
    public override double OwnKeyCount() => HasOwnAccessors ? Js.StringPropertyKeyCount(OwnKeys()) : ElementKeyCount() + ExtraKeyCount - (LengthWritable ? 0 : 1);
    internal virtual uint? LastStoredIndex() => values.ExtremePresent(static value => !ReferenceEquals(value, Hole), true);
    internal virtual List<uint> PresentIndices() => values.PresentIndices(static value => !ReferenceEquals(value, Hole));
    internal object ArrayPropertyNames()
    {
        var indices = PresentIndices();
        var names = new object[indices.Count];
        for (int i = 0; i < names.Length; i++) names[i] = indices[i].ToString(CultureInfo.InvariantCulture);
        return KeysFromFixed(names);
    }
    public override object OwnKeys()
    {
        var keys = (JsArray)ArrayPropertyNames();
        if (LengthWritable && !HasOwnAccessors) return keys;
        var result = new JsArray(0);
        for (uint i = 0; i < keys.Length(); i++) if (ForInEnumerable((string)keys.ElementAt(i))) result.Append(keys.ElementAt(i));
        return result;
    }
}

public sealed partial class NumberArray
{
    internal override uint? LastStoredIndex() => numbers.ExtremePresent(static value => BitConverter.DoubleToInt64Bits(value) != MissingBits, true);
    internal override double ElementKeyCount() => numbers.CountPresent(static value => BitConverter.DoubleToInt64Bits(value) != MissingBits);
    internal override List<uint> PresentIndices() => numbers.PresentIndices(static value => BitConverter.DoubleToInt64Bits(value) != MissingBits);
}

public sealed partial class TaggedArray
{
    internal override uint? LastStoredIndex()
    {
        if (materialized != null) return materialized.LastStoredIndex();
        uint end = Math.Min(length, (uint)tags.Length);
        for (uint i = end; i > 0; i--) if (tags[i - 1] != 0) return i - 1;
        return null;
    }
    internal override double ElementKeyCount()
    {
        if (materialized != null) return materialized.ElementKeyCount();
        double count = 0;uint end = Math.Min(length, (uint)tags.Length);
        for (uint i = 0; i < end; i++) if (tags[i] != 0) count++;
        return count;
    }
    internal override List<uint> PresentIndices()
    {
        if (materialized != null) return materialized.PresentIndices();
        var result = new List<uint>();
        uint end = Math.Min(length, (uint)tags.Length);
        for (uint i = 0; i < end; i++) if (tags[i] != 0) result.Add(i);
        return result;
    }
}

public sealed partial class ReversibleArray
{
    internal override uint? LastStoredIndex()
    {
        uint? index = values.ExtremePresent(static value => !ReferenceEquals(value, HoleValue()), !reversed);
        return reversed && index.HasValue ? Count - 1 - index.Value : index;
    }
    internal override List<uint> PresentIndices()
    {
        var result = base.PresentIndices();
        if (reversed) {
            for (int i = 0; i < result.Count; i++) result[i] = Count - 1 - result[i];
            result.Sort();
        }
        return result;
    }
}

public static partial class Js
{
    internal static object OrderOwnKeys(object[] names) => new JsArray(OrderOwnKeyNames(names));
    internal static object[] OrderOwnKeyNames(object[] names)
    {
        bool text = false, unordered = false, haveIndex = false, symbol = false;
        uint previous = 0;
        foreach (string name in names) {
            if (IsSymbolKey(name)) { symbol = true; continue; }
            if (symbol) unordered = true;
            if (JsArray.Index(name, out uint index)) {
                if (text || haveIndex && index < previous) unordered = true;
                haveIndex = true; previous = index;
            } else text = true;
        }
        if (!unordered) return names;
        var indices = new List<KeyValuePair<uint, object>>();
        var strings = new List<object>();
        var symbols = new List<object>();
        foreach (string name in names)
            if (IsSymbolKey(name)) symbols.Add(name);
            else if (JsArray.Index(name, out uint index)) indices.Add(new KeyValuePair<uint, object>(index, name));
            else strings.Add(name);
        indices.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        int position = 0;
        foreach (var item in indices) names[position++] = item.Value;
        foreach (var name in strings) names[position++] = name;
        foreach (var name in symbols) names[position++] = name;
        return names;
    }
    public static object ObjectKeys(object source)
    {
        if (source == null || ReferenceEquals(source, Undefined)) throw new InvalidOperationException("Cannot convert null or undefined to object");
        if (source is TsObject value) return StringPropertyKeys(value.OwnKeys());
        if (source is string text) {
            var names = new object[text.Length];
            for (int i = 0; i < names.Length; i++) names[i] = i.ToString(CultureInfo.InvariantCulture);
            return new JsArray(names);
        }
        return new JsArray(0);
    }
    public static double ObjectKeyCount(object source)
    {
        if (source == null || ReferenceEquals(source, Undefined)) throw new InvalidOperationException("Cannot convert null or undefined to object");
        if (source is TsObject value) return value.OwnKeyCount();
        return source is string text ? text.Length : 0;
    }
    static class ObjectKeysCache
    {
        internal static readonly JsFunction Function = new JsFunction((FunctionBody)(arguments => ObjectKeys(FunctionArgument(arguments, 0))), null, "keys", 1);
    }
    public static object GetObjectKeysFunction() => ObjectKeysCache.Function;
}
