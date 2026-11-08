using System;

public static partial class Js
{
    static bool TryGetAsyncIterator(object value, out TsObject iterator, out object next)
    {
        iterator = null; next = null;
        if (value is not TsObject source) return false;
        object method = source.Get(PropertyKey(AsyncIteratorSymbol));
        if (IsNullish(method)) return false;
        if (method is not JsFunction function) throw new InvalidOperationException("Symbol.asyncIterator is not callable");
        iterator = function.InvokeWithReceiver(source, Array.Empty<object>()) as TsObject
            ?? throw new InvalidOperationException("Symbol.asyncIterator must return an object");
        next = iterator.Get("next");
        return true;
    }

    static JsFunction CustomIterableMethod(object value) => CustomIterableMethod(value, out _);
    static JsFunction CustomIterableMethod(object value, out bool disabled)
    {
        disabled = false;
        var source = value as TsObject;
        if (source == null && value is not string) return null;
        string key = PropertyKey(IteratorSymbol);
        object method = source == null ? Get(value, key) : source.Get(key);
        if (IsNullish(method)) { disabled = value is string or JsBoxedPrimitive { Value: string } || source.HasInProperty(key); return null; }
        if (ReferenceEquals(method, NativeIteratorPrototypes.Self) && IsIteratorObject(source)) return null;
        return method as JsFunction ?? throw new InvalidOperationException("Symbol.iterator is not callable");
    }
    static TsObject InvokeIterableMethod(JsFunction method, object source) =>
        method.InvokeWithReceiver(source, Array.Empty<object>()) as TsObject
            ?? throw new InvalidOperationException("Symbol.iterator must return an object");
    internal static bool TryGetIterableObject(object value, out TsObject iterator)
    {
        var method = CustomIterableMethod(value);
        iterator = method == null ? null : InvokeIterableMethod(method, value);
        return iterator != null;
    }

    internal static bool TryStartCustomIterable(object value, out object cursor, bool required = false)
    {
        cursor = null;
        var method = CustomIterableMethod(value, out bool disabled);
        if (required && disabled) throw new InvalidOperationException("Value is not iterable");
        if (value is string && ReferenceEquals(method, StringIteratorMethods.Iterator)) return false;
        if (method == null) return false;
        cursor = StartIteratorObject(InvokeIterableMethod(method, value));
        return true;
    }
}
