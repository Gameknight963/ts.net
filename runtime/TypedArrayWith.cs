using System;

public static partial class Js
{
    static object WithTypedArray(TypedArrayValue array, object[] args)
    {
        int length = array.Count;
        double index = Math.Truncate(Number(FunctionArgument(args, 0)));
        if (double.IsNaN(index)) index = 0;
        if (index < 0) index += length;
        object input = FunctionArgument(args, 1);
        object value = array.Kind.StartsWith("Big", StringComparison.Ordinal) ? BigIntValue(input, false) : (object)Number(input);
        if (!array.Valid(index)) throw new ArgumentOutOfRangeException(null, "Typed array index is out of range");
        var result = AllocateTypedArray(array.Name, length);
        for (int position = 0; position < length; position++)
            result.Write(position, position == index ? value : array.Read(position));
        return result;
    }
}
