using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    // Scalar NaNs are canonicalized before storage, leaving one payload for
    // undefined. Public getters and setters retain the nullable scalar ABI.
    const long UndefinedFieldBits = 0x7ff8000000000001;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double EncodeNumberField(double? value) =>
        !value.HasValue ? BitConverter.Int64BitsToDouble(UndefinedFieldBits) :
        double.IsNaN(value.Value) ? double.NaN : value.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? DecodeNumberField(double value) =>
        BitConverter.DoubleToInt64Bits(value) == UndefinedFieldBits ? null : value;
}
