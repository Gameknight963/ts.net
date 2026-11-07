using System;

public static partial class Js
{
    internal static bool TryGetIterableObject(object value, out TsObject iterator)
    {
        iterator = null;
        if (value is not TsObject source) return false;
        string key = PropertyKey(IteratorSymbol);
        object method = source.Get(key);
        if (IsNullish(method) || ReferenceEquals(method, NativeIteratorPrototypes.Self) && IsIteratorObject(source)) return false;
        if (method is not JsFunction function) throw new InvalidOperationException("Symbol.iterator is not callable");
        iterator = function.InvokeWithReceiver(source, Array.Empty<object>()) as TsObject
            ?? throw new InvalidOperationException("Symbol.iterator must return an object");
        return true;
    }

    internal static bool TryStartCustomIterable(object value, out object cursor)
    {
        cursor = null;
        if (!TryGetIterableObject(value, out var iterator)) return false;
        cursor = StartIteratorObject(iterator);
        return true;
    }
}
