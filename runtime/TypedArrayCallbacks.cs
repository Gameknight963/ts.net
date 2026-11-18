using System;

public static partial class Js
{
    readonly struct ReduceCallbackKind : IArrayCallbackKind { public static ArrayCallbackKind Value => ArrayCallbackKind.Reduce; }
    readonly struct ReduceRightCallbackKind : IArrayCallbackKind { public static ArrayCallbackKind Value => ArrayCallbackKind.ReduceRight; }
    static object TypedArrayCallback(TypedArrayValue array, string method, object[] args) => method switch
    {
        "forEach" => TypedArrayCallback<ForEachCallbackKind>(array, args),
        "every" => TypedArrayCallback<EveryCallbackKind>(array, args),
        "some" => TypedArrayCallback<SomeCallbackKind>(array, args),
        "find" => TypedArrayCallback<FindCallbackKind>(array, args),
        "findIndex" => TypedArrayCallback<FindIndexCallbackKind>(array, args),
        "findLast" => TypedArrayCallback<FindLastCallbackKind>(array, args),
        "findLastIndex" => TypedArrayCallback<FindLastIndexCallbackKind>(array, args),
        "reduce" => TypedArrayCallback<ReduceCallbackKind>(array, args),
        "reduceRight" => TypedArrayCallback<ReduceRightCallbackKind>(array, args),
        _ => throw new InvalidOperationException("Unknown typed array callback method")
    };
    static object TypedArrayCallback<TKind>(TypedArrayValue array, object[] args) where TKind : struct, IArrayCallbackKind
    {
        ArrayCallbackKind kind = TKind.Value;
        int length = array.Count;
        if (FunctionArgument(args, 0) is not JsFunction callback) throw new InvalidOperationException("Typed array callback is not callable");
        bool reduce = kind is ArrayCallbackKind.Reduce or ArrayCallbackKind.ReduceRight;
        bool backwards = kind is ArrayCallbackKind.ReduceRight or ArrayCallbackKind.FindLast or ArrayCallbackKind.FindLastIndex;
        bool initialized = args.Length > 1;
        object accumulator = FunctionArgument(args, 1), receiver = reduce ? Undefined : FunctionArgument(args, 1);
        bool numericElements = !array.Kind.StartsWith("Big", StringComparison.Ordinal);
        if (reduce && length > 0 && numericElements && callback.SupportsArrayNumber() && callback.SupportsNumberResult() &&
            (!initialized || accumulator is double || IsUndefined(accumulator)))
            return TypedArrayReduceNumbers<TKind>(array, callback, length, initialized, accumulator);
        bool numericCallback = !reduce && numericElements && (callback.SupportsArrayNumber() || callback.SupportsArrayNumberSource());
        bool numericResult = numericCallback && callback.SupportsNumberResult();
        bool sourceNumberResult = numericCallback && callback.SupportsArrayNumberResult();
        for (int index = backwards ? length - 1 : 0; index >= 0 && index < length; index += backwards ? -1 : 1)
        {
            if (numericCallback && array.ReadCallbackNumber(index) is double number)
            {
                if (kind == ArrayCallbackKind.ForEach) {
                    if (numericResult) callback.InvokeNumber2(number, index);
                    else if (sourceNumberResult) callback.InvokeArrayNumberResult(receiver, number, index, array);
                    else callback.InvokeArrayNumber(receiver, number, index, array);
                    continue;
                }
                bool truth = numericResult ? NumberTruthy(callback.InvokeNumber2(number, index))
                    : sourceNumberResult ? NumberTruthy(callback.InvokeArrayNumberResult(receiver, number, index, array))
                    : callback.InvokeArrayNumberTruth(receiver, number, index, array);
                if (kind == ArrayCallbackKind.Every && !truth) return BoxBoolean(false);
                if (kind == ArrayCallbackKind.Some && truth) return BoxBoolean(true);
                if (kind is ArrayCallbackKind.Find or ArrayCallbackKind.FindLast && truth) return number;
                if (kind is ArrayCallbackKind.FindIndex or ArrayCallbackKind.FindLastIndex && truth) return (double)index;
                continue;
            }
            object value = array.Read(index);
            if (reduce && !initialized) { accumulator = value; initialized = true; continue; }
            object result = callback.InvokeWithReceiver(receiver, reduce
                ? new object[] { accumulator, value, (double)index, array }
                : new object[] { value, (double)index, array });
            if (reduce) accumulator = result;
            else if (kind == ArrayCallbackKind.Every && !Truthy(result)) return BoxBoolean(false);
            else if (kind == ArrayCallbackKind.Some && Truthy(result)) return BoxBoolean(true);
            else if (kind is ArrayCallbackKind.Find or ArrayCallbackKind.FindLast && Truthy(result)) return value;
            else if (kind is ArrayCallbackKind.FindIndex or ArrayCallbackKind.FindLastIndex && Truthy(result)) return (double)index;
        }
        if (reduce) return initialized ? accumulator : throw new InvalidOperationException("Reduce of empty typed array with no initial value");
        return kind switch { ArrayCallbackKind.Every => BoxBoolean(true), ArrayCallbackKind.Some => BoxBoolean(false), ArrayCallbackKind.FindIndex or ArrayCallbackKind.FindLastIndex => -1d, _ => Undefined };
    }
    static object TypedArrayReduceNumbers<TKind>(TypedArrayValue array, JsFunction callback, int length, bool initialized, object initial) where TKind : struct, IArrayCallbackKind
    {
        bool backwards = TKind.Value == ArrayCallbackKind.ReduceRight;
        double? accumulator = initial as double?;
        for (int index = backwards ? length - 1 : 0; index >= 0 && index < length; index += backwards ? -1 : 1)
        {
            double? value = array.ReadCallbackNumber(index);
            if (!initialized) { accumulator = value; initialized = true; }
            else accumulator = callback.InvokeNumber2(accumulator, value);
        }
        return BoxNumber(accumulator);
    }
}
