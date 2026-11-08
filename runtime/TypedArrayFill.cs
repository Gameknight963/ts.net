using System;

public static partial class Js
{
    static object FillTypedArray(TypedArrayValue array, object[] args)
    {
        int length = array.Count;
        object input = FunctionArgument(args, 0);
        object value = array.Kind.StartsWith("Big", StringComparison.Ordinal) ? BigIntValue(input, false) : (object)Number(input);
        int start = BufferSliceIndex(FunctionArgument(args, 1), length);
        object requestedEnd = FunctionArgument(args, 2);
        int end = IsUndefined(requestedEnd) ? length : BufferSliceIndex(requestedEnd, length);
        RequireTypedArrayMethod(array);
        // A length-tracking view may have shrunk during argument conversion.
        end = Math.Min(end, array.Count);
        for (int index = start; index < end; index++) array.Write(index, value);
        return array;
    }
}
