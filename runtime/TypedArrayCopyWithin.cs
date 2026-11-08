using System;

public static partial class Js
{
    static object CopyWithinTypedArray(TypedArrayValue array, object[] args)
    {
        int length = array.Count;
        int target = BufferSliceIndex(FunctionArgument(args, 0), length);
        int start = BufferSliceIndex(FunctionArgument(args, 1), length);
        object requestedEnd = FunctionArgument(args, 2);
        int end = IsUndefined(requestedEnd) ? length : BufferSliceIndex(requestedEnd, length);
        int count = Math.Min(end - start, length - target);
        if (count <= 0) return array;
        RequireTypedArrayMethod(array);
        int currentLength = array.Count;
        count = Math.Min(count, Math.Min(currentLength - start, currentLength - target));
        if (count > 0) {
            byte[] bytes = array.Buffer.Bytes;
            bytes.AsSpan(array.Offset + start * array.Size, count * array.Size)
                .CopyTo(bytes.AsSpan(array.Offset + target * array.Size));
        }
        return array;
    }
}
