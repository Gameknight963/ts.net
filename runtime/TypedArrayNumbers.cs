using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    enum TypedArrayNumberKind : byte { None, Float64, Float32, Float16, Int8, Uint8, Int16, Uint16, Int32, Uint32 }
    static TypedArrayNumberKind TypedArrayNumberKindFrom(string kind) => kind switch
    {
        "Float64" => TypedArrayNumberKind.Float64,
        "Float32" => TypedArrayNumberKind.Float32,
        "Float16" => TypedArrayNumberKind.Float16,
        "Int8" => TypedArrayNumberKind.Int8,
        "Uint8" => TypedArrayNumberKind.Uint8,
        "Int16" => TypedArrayNumberKind.Int16,
        "Uint16" => TypedArrayNumberKind.Uint16,
        "Int32" => TypedArrayNumberKind.Int32,
        "Uint32" => TypedArrayNumberKind.Uint32,
        _ => TypedArrayNumberKind.None
    };
    // Typed arrays use native byte order, unlike DataView's explicit endianness.
    static double ReadTypedArrayNumber(ReadOnlySpan<byte> bytes, TypedArrayNumberKind kind) => kind switch
    {
        TypedArrayNumberKind.Float64 => BitConverter.ToDouble(bytes),
        TypedArrayNumberKind.Float32 => BitConverter.ToSingle(bytes),
        TypedArrayNumberKind.Float16 => (double)BitConverter.UInt16BitsToHalf(BitConverter.ToUInt16(bytes)),
        TypedArrayNumberKind.Int8 => unchecked((sbyte)bytes[0]),
        TypedArrayNumberKind.Uint8 => bytes[0],
        TypedArrayNumberKind.Int16 => BitConverter.ToInt16(bytes),
        TypedArrayNumberKind.Uint16 => BitConverter.ToUInt16(bytes),
        TypedArrayNumberKind.Int32 => BitConverter.ToInt32(bytes),
        TypedArrayNumberKind.Uint32 => BitConverter.ToUInt32(bytes),
        _ => throw new InvalidOperationException("Typed array element is not a number")
    };
    // One dispatch keeps boxed float reads on the same direct path as integers.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static object ReadBoxedTypedArrayNumber(ReadOnlySpan<byte> bytes, TypedArrayNumberKind kind) => kind switch
    {
        TypedArrayNumberKind.Float64 => BitConverter.ToDouble(bytes),
        TypedArrayNumberKind.Float32 => (double)BitConverter.ToSingle(bytes),
        TypedArrayNumberKind.Float16 => (double)BitConverter.UInt16BitsToHalf(BitConverter.ToUInt16(bytes)),
        TypedArrayNumberKind.Int8 => BoxInteger(unchecked((sbyte)bytes[0])),
        TypedArrayNumberKind.Uint8 => BoxInteger(bytes[0]),
        TypedArrayNumberKind.Int16 => BoxInteger(BitConverter.ToInt16(bytes)),
        TypedArrayNumberKind.Uint16 => BoxInteger(BitConverter.ToUInt16(bytes)),
        TypedArrayNumberKind.Int32 => BoxInteger(BitConverter.ToInt32(bytes)),
        TypedArrayNumberKind.Uint32 => BoxUnsignedInteger(BitConverter.ToUInt32(bytes)),
        _ => throw new InvalidOperationException("Typed array element is not a number")
    };
}
