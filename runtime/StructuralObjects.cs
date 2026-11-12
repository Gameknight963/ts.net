using System.Numerics;

public static partial class Js
{
    public static bool HasStructuralProperty(object value, string key)
    {
        // Keep the existing nullable reference ABI. Checking a shape must not
        // evaluate its getters or replace the original object with a wrapper.
        if (IsNullish(value)) return true;
        if (value is TsObject obj) return obj.HasProperty(key);
        return (value is string or double or bool or JsSymbol or BigInteger)
            && ToObject(value).HasProperty(key);
    }
}
