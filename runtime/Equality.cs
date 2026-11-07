using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NumberAtIsNullish(object receiver, int index) => receiver is NumberArray numbers
        ? numbers.IsUndefinedAt(index) : !ArrayGetNumberAt(receiver, index).HasValue;

    public static bool SameValue(object left, object right)
    {
        if (left is double a && right is double b)
            return double.IsNaN(a) && double.IsNaN(b) || a == b && (a != 0 || BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b));
        return Equal(left, right);
    }

    public static bool LooseEqual(object left, object right)
    {
        if (left is double a) return LooseEqualNumberValue(a, right);
        if (right is double b) return LooseEqualNumberValue(b, left);
        if (left is bool x) return LooseEqualNumberValue(x ? 1 : 0, right);
        if (right is bool y) return LooseEqualNumberValue(y ? 1 : 0, left);
        if (IsNullish(left)) return IsNullish(right);
        if (IsNullish(right)) return false;
        if (left is JsSymbol) return right is TsObject symbolObject ? ReferenceEquals(left, ToPrimitive(symbolObject, "default")) : ReferenceEquals(left, right);
        if (right is JsSymbol && left is TsObject boxedSymbol) return ReferenceEquals(ToPrimitive(boxedSymbol, "default"), right);
        if (left is string text) {
            if (right is string other) return text == other;
            return right is TsObject obj && LooseEqual(left, ToPrimitive(obj, "default"));
        }
        if (right is string && left is TsObject value)
            return LooseEqual(ToPrimitive(value, "default"), right);
        return ReferenceEquals(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LooseEqualNumber(double? number, object value) => number.HasValue
        ? LooseEqualNumberValue(number.GetValueOrDefault(), value) : IsNullish(value);

    static bool LooseEqualNumberValue(double number, object value) => value switch {
        double other => number == other,
        string text => number == StringNumber(text),
        bool boolean => number == (boolean ? 1 : 0),
        TsObject obj => LooseEqualNumberValue(number, ToPrimitive(obj, "default")),
        _ => false
    };
}

public sealed partial class NumberArray
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsUndefinedAt(int index) => unchecked((ulong)(BitConverter.DoubleToInt64Bits(numbers.Get((uint)index)) - MissingBits)) <= 1;
}
