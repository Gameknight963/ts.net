using System;

public static partial class Js
{
    readonly record struct SetRecord(TsObject Object, double Size, JsFunction Has, JsFunction Keys);
    static SetRecord ReadSetRecord(object value)
    {
        if (value is not TsObject source) throw new InvalidOperationException("Set-like argument must be an object");
        double size = Number(source.Get("size"));
        if (double.IsNaN(size)) throw new InvalidOperationException("Set-like size is NaN");
        size = Math.Truncate(size);
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(value), "Set-like size is negative");
        if (source.Get("has") is not JsFunction has) throw new InvalidOperationException("Set-like has is not callable");
        if (source.Get("keys") is not JsFunction keys) throw new InvalidOperationException("Set-like keys is not callable");
        return new(source, size, has, keys);
    }
    static object SetKeysIterator(SetRecord record)
    {
        object value = record.Keys.InvokeWithReceiver(record.Object, Array.Empty<object>());
        if (value is not TsObject iterator) throw new InvalidOperationException("Set-like keys did not return an object");
        if (iterator is JsCollectionIterator native) return StartCollectionIterator(native);
        if (iterator.Get("next") is not JsFunction next) throw new InvalidOperationException("Iterator next is not callable");
        return new JsProtocolIterator(iterator, next);
    }
    static bool SetKeysNext(object iterator, out object value)
    {
        if (iterator is JsCollectionIterator native) return native.Take(out value, out _);
        var protocol = (JsProtocolIterator)iterator;
        if (!protocol.Ensure()) { value = Undefined; return false; }
        value = protocol.TakeValue(); return true;
    }
    static object SetUnion(JsKeyedCollection source, object other)
    {
        SetRecord record = ReadSetRecord(other);
        object iterator = SetKeysIterator(record);
        var result = new JsKeyedCollection(true);
        // keys() and the lookup of next run before the receiver is copied.
        foreach (var entry in source.Entries) if (entry.Active) result.Write(entry.Key, entry.Key);
        while (SetKeysNext(iterator, out object key)) result.Write(key, key);
        return result;
    }
}
