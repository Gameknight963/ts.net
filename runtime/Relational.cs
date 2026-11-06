using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StringRelational(object left, object right, int mode)
    {
        if (left is string a && right is string b) {
            int order = string.CompareOrdinal(a, b);
            return mode switch { 0 => order < 0, 1 => order > 0, 2 => order <= 0, 3 => order >= 0,
                _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
        }
        return Relational(left, right, mode);
    }

    public static bool Relational(object left, object right, int mode)
    {
        if (left is TsObject first) left = OrdinaryPrimitive(first, false);
        if (right is TsObject second) right = OrdinaryPrimitive(second, false);
        if (left is string a && right is string b) {
            int order = string.CompareOrdinal(a, b);
            return mode switch { 0 => order < 0, 1 => order > 0, 2 => order <= 0, 3 => order >= 0,
                _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
        }
        double x = Number(left), y = Number(right);
        return mode switch { 0 => x < y, 1 => x > y, 2 => x <= y, 3 => x >= y,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
    }
}
