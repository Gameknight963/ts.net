using System;

public static partial class Js
{
    static void EnsureAttached(BinaryBuffer buffer)
    {
        if (buffer.Detached) throw new InvalidOperationException("ArrayBuffer is detached");
    }
    static object TransferBuffer(object receiver, object requestedLength, bool fixedLength)
    {
        var source = RequireBuffer(receiver);
        int length = IsUndefined(requestedLength) ? source.Bytes.Length : BinaryIndex(requestedLength);
        EnsureAttached(source);
        int maximum = fixedLength ? -1 : source.Maximum;
        if (maximum >= 0 && length > maximum) throw new ArgumentOutOfRangeException(null, "Transfer exceeds maximum buffer length");
        byte[] bytes = source.Bytes;
        Array.Resize(ref bytes, length);
        var result = new BinaryBuffer(0, maximum) { Bytes = bytes };
        source.Detach();
        return result;
    }
}
