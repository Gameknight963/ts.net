using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object StringIndexAt(object receiver, int index)
    {
        if (receiver is not string text) throw new InvalidOperationException("Cannot read property of undefined");
        return (uint)index < (uint)text.Length ? Iteration.CodeUnit(text[index]) : Undefined;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object StringIndexNumber(object receiver, double? index)
    {
        if (receiver is not string text) throw new InvalidOperationException("Cannot read property of undefined");
        double value=index.GetValueOrDefault();
        return index.HasValue && value >= 0 && value < text.Length && value == Math.Truncate(value)
            ? Iteration.CodeUnit(text[ToInt32(value)]) : Undefined;
    }
}
