using System;
using System.Runtime.InteropServices;

public static partial class Js
{
    static bool TrySortIntegerTypedArray(TypedArrayValue source, bool copy, out TypedArrayValue result)
    {
        result = null;
        // Shared storage can change during sorting. Integer duplicates have identical
        // bits, so an unstable platform sort cannot change their observable order.
        if (source.Buffer is SharedBuffer || source.OutOfBounds || source.Kind is not
            ("Int8" or "Uint8" or "Int16" or "Uint16" or "Int32" or "Uint32" or "BigInt64" or "BigUint64")) return false;
        int count = source.Count;
        result = copy ? AllocateTypedArray(source.Name, count) : source;
        var bytes = result.Buffer.Bytes.AsSpan(result.Offset, count * result.Size);
        if (copy) source.Buffer.Bytes.AsSpan(source.Offset, bytes.Length).CopyTo(bytes);
        switch (source.Kind)
        {
            case "Int8": MemoryMarshal.Cast<byte, sbyte>(bytes).Sort(); break;
            case "Uint8": bytes.Sort(); break;
            case "Int16": MemoryMarshal.Cast<byte, short>(bytes).Sort(); break;
            case "Uint16": MemoryMarshal.Cast<byte, ushort>(bytes).Sort(); break;
            case "Int32": MemoryMarshal.Cast<byte, int>(bytes).Sort(); break;
            case "Uint32": MemoryMarshal.Cast<byte, uint>(bytes).Sort(); break;
            case "BigInt64": MemoryMarshal.Cast<byte, long>(bytes).Sort(); break;
            case "BigUint64": MemoryMarshal.Cast<byte, ulong>(bytes).Sort(); break;
        }
        return true;
    }
}
