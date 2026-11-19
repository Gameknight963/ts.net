using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    public static byte[] TryTypedNumberBuffer(object value, string kind)
    {
        if (value is not TypedArrayValue array || array.Kind != kind || TypedArrayNumberKindFrom(kind) == TypedArrayNumberKind.None ||
            array.Offset != 0 || array.Buffer.Detached || array.Buffer.Resizable ||
            (long)array.FixedCount * array.Size != array.Buffer.Bytes.Length || array.HasOwn("length")) return null;
        return TryTypedArrayGetter(array.PropertyPrototype, "length", out var getter) && getter.IsLength ? array.Buffer.Bytes : null;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadInt16Buffer(byte[] bytes, int index) => BitConverter.ToInt16(bytes.AsSpan(index * 2, 2));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadUint16Buffer(byte[] bytes, int index) => BitConverter.ToUInt16(bytes.AsSpan(index * 2, 2));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadInt32Buffer(byte[] bytes, int index) => BitConverter.ToInt32(bytes.AsSpan(index * 4, 4));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadUint32Buffer(byte[] bytes, int index) => BitConverter.ToUInt32(bytes.AsSpan(index * 4, 4));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadFloat32Buffer(byte[] bytes, int index) => BitConverter.ToSingle(bytes.AsSpan(index * 4, 4));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadFloat16Buffer(byte[] bytes, int index) => (double)BitConverter.UInt16BitsToHalf(BitConverter.ToUInt16(bytes.AsSpan(index * 2, 2)));
}
