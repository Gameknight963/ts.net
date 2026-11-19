using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Data stores can serve as the state header, avoiding a separate container.
class ObjectPropertyState
{
    internal JsAccessors Accessors;
    internal ObjectMetadata Metadata;
}

// Most dynamic records have only a few keys. Keep those entries together and
// use a dictionary for the tail, preserving insertion order across both.
sealed partial class OwnProperties : ObjectPropertyState
{
    // Low bits remember first writes; this bit remains set after a cell is created.
    const int HasNumberCells = 16;
    const int PossibleIndexKeys = 32;
    internal bool MayHaveIndexKeys => (numericWrites & PossibleIndexKeys) != 0;
    internal bool HasSymbolKeys => symbolCount != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddedKey(string key)
    {
        if (Js.IsSymbolKey(key)) symbolCount++;
        // Keep this conservative after deletion: a false flag proves that
        // insertion order is already the required order for string keys.
        if (key.Length != 0 && key[0] is >= '0' and <= '9') numericWrites |= PossibleIndexKeys;
        version++;
    }
    // Insertion order only; callers guard numeric-key ordering.
    internal object[] CopyStringKeyNames()
    {
        if (StringCount == 0) return Array.Empty<object>();
        var names = new object[StringCount];
        int index = 0;
        if (symbolCount != 0) {
            foreach (string key in Keys) if (!Js.IsSymbolKey(key)) names[index++] = key;
            return names;
        }
        if (inlineCount > 0) names[index++] = key0;
        if (inlineCount > 1) names[index++] = key1;
        if (inlineCount > 2) names[index++] = key2;
        if (inlineCount > 3) names[index++] = key3;
        if (tailOrder is SingleProperty single) names[index] = single.Key;
        else if (tailOrder is List<string> order) foreach (string key in order) names[index++] = key;
        else if (tail != null) ((System.Collections.ICollection)tail.Keys).CopyTo(names, index);
        return names;
    }
    sealed class NumberCell(double value)
    {
        internal double Value = value;
        internal object Boxed;
        internal object Box() => Boxed ??= Value;
    }
    static object Read(object value) => value is NumberCell number ? number.Box() : value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Store(ref object slot, object value)
    {
        if (slot is NumberCell cell && value is double number) {cell.Value = number; cell.Boxed = value;}
        else slot = value;
    }
    void StoreNumber(ref object slot, double? value, int flag)
    {
        if (!value.HasValue) {slot = Js.Undefined; numericWrites &= ~flag; return;}
        if (slot is NumberCell cell) {cell.Value = value.Value;cell.Boxed = null;}
        // One-off writes keep an ordinary boxed value. Repeated writes reuse a cell.
        else if ((numericWrites & flag) != 0) {slot = new NumberCell(value.Value);numericWrites |= HasNumberCells;}
        else {numericWrites |= flag;slot = Js.BoxNumber(value);}
    }
    internal bool TrySetNumber(string key, double? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key == key0) {StoreNumber(ref value0, value, 1);return true;}
        if (key == key1) {StoreNumber(ref value1, value, 2);return true;}
        if (key == key2) {StoreNumber(ref value2, value, 4);return true;}
        if (key == key3) {StoreNumber(ref value3, value, 8);return true;}
        return TrySetTailNumber(key, value);
    }
    bool TrySetTailNumber(string key, double? value)
    {
        // Tail slots have no history bits; create a cell on the first numeric rewrite.
        if (tail != null) {
            if (!tail.TryGetValue(key, out object stored)) return false;
            if (stored is NumberCell cell && value.HasValue) { cell.Value = value.Value; cell.Boxed = null; }
            else tail[key] = value.HasValue ? new NumberCell(value.Value) : Js.Undefined;
            return true;
        }
        if (tailOrder is not SingleProperty single || key != single.Key) return false;
        if (single.Value is NumberCell number && value.HasValue) { number.Value = value.Value; number.Boxed = null; }
        else single.Value = value.HasValue ? new NumberCell(value.Value) : Js.Undefined;
        return true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetNumber(string key, out double? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        object stored;
        if (key == key0) stored = value0;
        else if (key == key1) stored = value1;
        else if (key == key2) stored = value2;
        else if (key == key3) stored = value3;
        else if (!TryGetTail(key, out stored)) {value = null;return false;}
        value = stored is double primitive ? primitive : stored is NumberCell number ? number.Value : Js.Cast<double?>(stored, "checked conversion failed");
        return true;
    }
    internal bool TryReadNumber(string key, out double? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        object stored;
        if (key == key0) stored = value0;
        else if (key == key1) stored = value1;
        else if (key == key2) stored = value2;
        else if (key == key3) stored = value3;
        else if (!TryGetTail(key, out stored)) {value = null;return false;}
        if (stored is double number) {value = number;return true;}
        if (stored is NumberCell cell) {value = cell.Value;return true;}
        value = null;
        return ReferenceEquals(stored, Js.Undefined);
    }
    internal bool HasNumberValue(string key)
    {
        object stored;
        if (key == key0) stored = value0;
        else if (key == key1) stored = value1;
        else if (key == key2) stored = value2;
        else if (key == key3) stored = value3;
        else if (!TryGetTail(key, out stored)) return false;
        return stored is double or NumberCell || ReferenceEquals(stored, Js.Undefined);
    }
    string key0, key1, key2, key3;
    object value0, value1, value2, value3;
    int inlineCount, version, symbolCount, numericWrites;
    Dictionary<string, object> tail;
    // The order slot holds one compact entry until a sixth property needs a
    // dictionary. With a dictionary it holds the optional deletion order.
    object tailOrder;
    sealed class SingleProperty(string key, object value)
    {
        internal readonly string Key = key;
        internal object Value = value;
    }
    bool TryGetTail(string key, out object value)
    {
        if (tail != null) return tail.TryGetValue(key, out value);
        if (tailOrder is SingleProperty single && key == single.Key) { value = single.Value; return true; }
        value = null; return false;
    }
    bool TailContainsKey(string key) => tail != null ? tail.ContainsKey(key)
        : tailOrder is SingleProperty single && key == single.Key;
    [MethodImpl(MethodImplOptions.NoInlining)]
    bool TryGetTailValue(string key, out object value)
    {
        if (!TryGetTail(key, out value)) return false;
        value = Read(value); return true;
    }
    public OwnProperties() { }
    internal OwnProperties(object iteratorValue, object iteratorDone)
    {
        key0 = "value"; value0 = iteratorValue;
        key1 = "done"; value1 = iteratorDone;
        inlineCount = version = 2;
    }
    public OwnProperties(OwnProperties source)
    {
        key0=source.key0;key1=source.key1;key2=source.key2;key3=source.key3;
        // Copies share immutable boxed values, never mutable numeric cells.
        value0=Read(source.value0);value1=Read(source.value1);value2=Read(source.value2);value3=Read(source.value3);
        inlineCount=source.inlineCount;version=source.version;symbolCount=source.symbolCount;
        numericWrites = source.numericWrites & PossibleIndexKeys;
        if (source.tail != null) {
            tail = new Dictionary<string, object>(source.tail.Count);
            foreach (var entry in source.tail) tail.Add(entry.Key, Read(entry.Value));
        }
        if (source.tailOrder is List<string> order) tailOrder = new List<string>(order);
        else if (source.tailOrder is SingleProperty single) tailOrder = new SingleProperty(single.Key, Read(single.Value));
    }
    public int Version => version;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryReadInlineSlot(string key, int slot, out object value)
    {
        // Initial built-in keys are interned literals. Equal strings with a
        // different identity keep the ordinary lookup path.
        if (!ReferenceEquals(InlineKey(slot), key)) { value = null; return false; }
        value = (numericWrites & HasNumberCells) == 0 ? InlineValue(slot) : Read(InlineValue(slot));
        return true;
    }
    public int Count => inlineCount + (tail?.Count ?? (tailOrder is SingleProperty ? 1 : 0));
    public int StringCount => Count - symbolCount;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(string key, out object value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key==key0) { value=(numericWrites & HasNumberCells)==0 ? value0 : Read(value0);return true; }
        if (key==key1) { value=(numericWrites & HasNumberCells)==0 ? value1 : Read(value1);return true; }
        if (key==key2) { value=(numericWrites & HasNumberCells)==0 ? value2 : Read(value2);return true; }
        if (key==key3) { value=(numericWrites & HasNumberCells)==0 ? value3 : Read(value3);return true; }
        value=null;
        return TryGetTailValue(key, out value);
    }
    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key == key0 || inlineCount > 1 && (key == key1 || key == key2 || key == key3 || TailContainsKey(key));
    }
    public object this[string key]
    {
        set {
            ArgumentNullException.ThrowIfNull(key);
            if (key==key0) { Store(ref value0, value);return; }
            if (key==key1) { Store(ref value1, value);return; }
            if (key==key2) { Store(ref value2, value);return; }
            if (key==key3) { Store(ref value3, value);return; }
            if (inlineCount < 4) {
                switch (inlineCount++) {
                    case 0: key0=key;value0=value;break;
                    case 1: key1=key;value1=value;break;
                    case 2: key2=key;value2=value;break;
                    default: key3=key;value3=value;break;
                }
                AddedKey(key);
            } else {
                if (tail == null) {
                    if (tailOrder is SingleProperty single) {
                        if (key == single.Key) { single.Value = value; return; }
                        tail = new Dictionary<string, object> { [single.Key] = single.Value };
                        tailOrder = null;
                    } else {
                        tailOrder = new SingleProperty(key, value);
                        AddedKey(key); return;
                    }
                }
                ref object slot = ref CollectionsMarshal.GetValueRefOrAddDefault(tail, key, out bool present);
                slot=value;
                if (!present) { AddedKey(key); (tailOrder as List<string>)?.Add(key); }
            }
        }
    }
    void InlineSet(int index, string key, object value)
    {
        numericWrites &= ~(1 << index);
        if (value is NumberCell) numericWrites |= HasNumberCells;
        switch (index) {
            case 0: key0=key;value0=value;break;
            case 1: key1=key;value1=value;break;
            case 2: key2=key;value2=value;break;
            default: key3=key;value3=value;break;
        }
    }
    internal bool HasOnlyJsonScalars()
    {
        if (Count > 16 || !IsJsonScalar(value0) || !IsJsonScalar(value1) || !IsJsonScalar(value2) || !IsJsonScalar(value3)) return false;
        if (tail != null) {
            foreach (object value in tail.Values) if (!IsJsonScalar(value)) return false;
        }
        else if (tailOrder is SingleProperty single && !IsJsonScalar(single.Value)) return false;
        return true;
    }
    static bool IsJsonScalar(object value) => value == null || value is string or double or bool or NumberCell || Js.IsUndefined(value);
    object InlineValue(int index) => index switch {0=>value0,1=>value1,2=>value2,_=>value3};
    public bool Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        for (int i=0;i<inlineCount;i++) {
            if (InlineKey(i)!=key) continue;
            for(int j=i;j+1<inlineCount;j++) InlineSet(j,InlineKey(j+1),InlineValue(j+1));
            InlineSet(--inlineCount,null,null);
            if(tail is not null && tail.Count!=0) {
                var order = tailOrder as List<string>;
                if (order == null) tailOrder = order = new List<string>(tail.Keys);
                string first=order[0];InlineSet(inlineCount++,first,tail[first]);
                tail.Remove(first);order.RemoveAt(0);
            } else if (tailOrder is SingleProperty single) {
                InlineSet(inlineCount++, single.Key, single.Value);
                tailOrder = null;
            }
            if (Js.IsSymbolKey(key)) symbolCount--;
            version++;return true;
        }
        if (tail == null) {
            if (tailOrder is not SingleProperty single || key != single.Key) return false;
            tailOrder = null;
        } else {
            if (!tail.ContainsKey(key)) return false;
            // Dictionary slots can be reused after removal. Keep explicit order
            // once deletion makes its enumeration order unsuitable.
            var order = tailOrder as List<string>;
            if (order == null) tailOrder = order = new List<string>(tail.Keys);
            tail.Remove(key);order.Remove(key);
        }
        if (Js.IsSymbolKey(key)) symbolCount--;version++;return true;
    }
    string InlineKey(int index) => index switch {0=>key0,1=>key1,2=>key2,_=>key3};
    public KeyCollection Keys => new KeyCollection(this);
    public readonly struct KeyCollection
    {
        readonly OwnProperties source;
        public KeyCollection(OwnProperties source) { this.source=source; }
        public Enumerator GetEnumerator() => new Enumerator(source);
    }
    public struct Enumerator : IDisposable
    {
        readonly OwnProperties source;
        readonly int version;
        int index;
        bool tailStarted;
        Dictionary<string, object>.KeyCollection.Enumerator tail;
        List<string>.Enumerator orderedTail;
        public string Current { get; private set; }
        public Enumerator(OwnProperties source)
        {
            this.source=source;version=source.version;index=-1;
            tailStarted=false;tail=default;orderedTail=default;Current=null;
        }
        public bool MoveNext()
        {
            if (version != source.version) throw new InvalidOperationException("Properties changed during enumeration");
            if (index+1 < source.inlineCount) { Current=source.InlineKey(++index);return true; }
            if (!tailStarted) {
                tailStarted=true;
                if (source.tailOrder is SingleProperty single) { Current=single.Key; return true; }
                if(source.tailOrder is List<string> order)orderedTail=order.GetEnumerator();
                else if (source.tail != null) tail=source.tail.Keys.GetEnumerator();
            }
            if(source.tailOrder is List<string>) {if(orderedTail.MoveNext()){Current=orderedTail.Current;return true;}}
            else if (source.tail != null && tail.MoveNext()) { Current=tail.Current;return true; }
            return false;
        }
        public void Dispose() {tail.Dispose();orderedTail.Dispose();}
    }
}
