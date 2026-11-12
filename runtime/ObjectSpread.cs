using System;
using System.Collections.Generic;
using System.Globalization;

public abstract partial class TsObject
{
    internal static object CreateIteratorResult(object value, object done)
    {
        // Both entries are ordinary own data properties on a fresh object.
        // Initializing their store directly avoids general property-write checks.
        return new JsDynamicObject { extra = new OwnProperties(value, done) };
    }
    public void DefineOwn(string key, object value) {if (!IsExtensible && !HasOwn(key)) throw new InvalidOperationException("Object is not extensible");accessors?.Remove(key);(extra ??= new OwnProperties())[key] = value;}
    public virtual void MarkPresent(string key) { }
    public void CopySpreadExtras(object destination)
    {
        if (extra != null) ((TsObject)destination).extra = new OwnProperties(extra);
    }
    public virtual object CloneForSpread()
    {
        var result = (TsObject)MemberwiseClone();
        if (extra != null) result.extra = new OwnProperties(extra);
        return result;
    }
}

public static partial class Js
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static bool IsExactSpread<T>(object source) => source != null && source.GetType() == typeof(T);

    // Only generated data classes with the exact expected layout may be cloned.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static object TryCloneSpread<T>(object source) =>
        source is TsObject value && source.GetType() == typeof(T) ? value.CloneForSpread() : null;

    public static void ObjectSpread(object destination, object source)
    {
        var target = (TsObject)destination;
        if (source == null || ReferenceEquals(source, Undefined)) return;
        if (source is TsObject value) {
            var keys = (JsArray)value.OwnPropertyNames();
            for (int i = 0; i < keys.Length(); i++) {
                string key = (string)keys.Get(i.ToString(CultureInfo.InvariantCulture));
                if (value.HasOwn(key) && value.ForInEnumerable(key)) target.DefineOwn(key, value.Get(key));
            }
        } else if (source is string text) {
            for (int i = 0; i < text.Length; i++) target.DefineOwn(i.ToString(CultureInfo.InvariantCulture), text[i].ToString());
        }
    }
}
