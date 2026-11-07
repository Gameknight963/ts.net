using System;
using System.Globalization;

public static partial class Js
{
    public static object ObjectValues(object source) => source is TsObject obj ? obj.OwnValues() : PrimitiveValues(source, false);
    public static object ObjectEntries(object source) => source is TsObject obj ? obj.OwnEntries() : PrimitiveValues(source, true);
    static object PrimitiveValues(object source, bool entries)
    {
        if (IsNullish(source)) throw new InvalidOperationException("Cannot convert null or undefined to object");
        if (source is not string text || text.Length == 0) return new JsArray(0);
        var result = new object[text.Length];
        for (int i = 0; i < result.Length; i++) {
            object value = text[i].ToString();
            result[i] = entries ? new JsArray(new object[] {i.ToString(CultureInfo.InvariantCulture), value}) : value;
        }
        return new JsArray(result);
    }
    internal static object EnumerateProperties(TsObject obj, bool entries)
    {
        var keys = (JsArray)obj.OwnKeys();
        var values = new object[(int)keys.Length()];int count=0;
        for (uint i = 0; i < values.Length; i++) {
            string key = (string)keys.ElementAt(i);
            if (IsSymbolKey(key) || !obj.HasOwn(key)) continue;
            object value = obj.Get(key);
            values[count++] = entries ? new JsArray(new object[] {key, value}) : value;
        }
        if (count != values.Length) Array.Resize(ref values, count);
        return new JsArray(values);
    }
    public static object ObjectNumberValues(object source)
    {
        if (IsNullish(source)) throw new InvalidOperationException("Cannot convert null or undefined to object");
        if (source is TsObject obj) return obj.OwnNumberValues();
        if (source is string text && text.Length != 0) throw new InvalidOperationException("Object value is not a number");
        return new NumberArray(0);
    }
    internal static object EnumerateNumberProperties(TsObject obj)
    {
        var result = new NumberArray(0);
        var keys = (JsArray)obj.OwnKeys();result.ReservePushes(keys.Length());
        for (uint i = 0; i < keys.Length(); i++) {
            string key = (string)keys.ElementAt(i);
            if (!IsSymbolKey(key) && obj.HasOwn(key)) result.PushNumber(Cast<double?>(obj.Get(key), "Object value is not a number"));
        }
        return result;
    }
    public static object NumberValuesFromFields(double[] fields) => NumberArray.FromFields(fields);
    static class ObjectEnumerationCache
    {
        internal static readonly JsFunction Values = new JsFunction((FunctionBody)(arguments => ObjectValues(FunctionArgument(arguments,0))), null, "values", 1);
        internal static readonly JsFunction Entries = new JsFunction((FunctionBody)(arguments => ObjectEntries(FunctionArgument(arguments,0))), null, "entries", 1);
    }
    public static object GetObjectValuesFunction() => ObjectEnumerationCache.Values;
    public static object GetObjectEntriesFunction() => ObjectEnumerationCache.Entries;
}

public abstract partial class TsObject
{
    public virtual object OwnValues() => Js.EnumerateProperties(this, false);
    public virtual object OwnEntries() => Js.EnumerateProperties(this, true);
    public virtual object OwnNumberValues() => Js.EnumerateNumberProperties(this);
}

public sealed partial class NumberArray
{
    internal static NumberArray FromFields(double[] fields)
    {
        uint prefix=0;
        for (int i=0;i<fields.Length;i++) {
            double? value=Js.DecodeNumberField(fields[i]);
            fields[i]=Encode(value);
            if (value.HasValue && prefix==(uint)i) prefix++;
        }
        return new NumberArray(new ArrayStorage<double>(fields, Missing), prefix);
    }
}
