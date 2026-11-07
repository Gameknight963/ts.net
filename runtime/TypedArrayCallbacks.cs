using System;

public static partial class Js
{
    static object TypedArrayCallback(TypedArrayValue array, string method, object[] args)
    {
        int length = array.Count;
        if (FunctionArgument(args, 0) is not JsFunction callback) throw new InvalidOperationException("Typed array callback is not callable");
        bool reduce = method is "reduce" or "reduceRight";
        bool backwards = method is "reduceRight" or "findLast" or "findLastIndex";
        bool initialized = args.Length > 1;
        object accumulator = FunctionArgument(args, 1), receiver = reduce ? Undefined : FunctionArgument(args, 1);
        for (int index = backwards ? length - 1 : 0; index >= 0 && index < length; index += backwards ? -1 : 1)
        {
            object value = array.Read(index);
            if (reduce && !initialized) { accumulator = value; initialized = true; continue; }
            object result = callback.InvokeWithReceiver(receiver, reduce
                ? new object[] { accumulator, value, (double)index, array }
                : new object[] { value, (double)index, array });
            if (reduce) accumulator = result;
            else if (method == "every" && !Truthy(result)) return BoxBoolean(false);
            else if (method == "some" && Truthy(result)) return BoxBoolean(true);
            else if (method is "find" or "findLast" && Truthy(result)) return value;
            else if (method is "findIndex" or "findLastIndex" && Truthy(result)) return (double)index;
        }
        if (reduce) return initialized ? accumulator : throw new InvalidOperationException("Reduce of empty typed array with no initial value");
        return method switch { "every" => BoxBoolean(true), "some" => BoxBoolean(false), "findIndex" or "findLastIndex" => -1d, _ => Undefined };
    }
}
