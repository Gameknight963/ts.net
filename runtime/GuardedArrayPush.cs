using System;
using System.Runtime.CompilerServices;

public abstract partial class TsObject
{
    public static bool IsPlainDataRecord<T>(object value) => value is TsObject record &&
        value.GetType() == typeof(T) && !record.HasPropertyOverrides;
}

public partial class JsArray
{
    internal void AppendGuarded(object value) => values.Set(values.Length, value);
}

public static partial class Js
{
    public static bool GuardPrivateArrayPush(object receiver, double limit)
    {
        if (receiver is not JsArray array || receiver.GetType() != typeof(JsArray) ||
            !(limit > 0 && limit <= 1048576) || array.Length() != 0 || !array.LengthWritable || array.HasOwn("push") || !array.CanAppendDirectly) return false;
        var prototype = ArrayPrototype;
        if (!prototype.HasOwn("push") || prototype.TryOwnAccessor("push", out _) ||
            !ReferenceEquals(prototype.Get("push"), ArrayMethods.Push)) return false;
        array.ReserveCapacity((int)Math.Ceiling(limit));
        return true;
    }

    // Emitted only inside a loop whose guard and body proof keep these checks valid.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ArrayPushGuarded(object receiver, object value)
    {
        var array = (JsArray)receiver;
        array.AppendGuarded(value);
        return array.Length();
    }
}
