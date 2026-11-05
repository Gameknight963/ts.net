using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NumberPredicate(object value, int mode) => value is double number && NumberPredicateNumber(number, mode);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NumberPredicateNumber(double? value, int mode)
    {
        if (!value.HasValue) return false;
        double number = value.Value;
        return mode switch
        {
            0 => double.IsNaN(number),
            1 => double.IsFinite(number),
            2 => double.IsFinite(number) && Math.Truncate(number) == number,
            3 => double.IsFinite(number) && Math.Truncate(number) == number && Math.Abs(number) <= 9007199254740991.0,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }
    static class NumberPredicateValues
    {
        internal static readonly JsFunction[] Functions = {
            new JsFunction((FunctionBody)(static args => BoxBoolean(NumberPredicate(FunctionArgument(args, 0), 0))), null, "isNaN", 1),
            new JsFunction((FunctionBody)(static args => BoxBoolean(NumberPredicate(FunctionArgument(args, 0), 1))), null, "isFinite", 1),
            new JsFunction((FunctionBody)(static args => BoxBoolean(NumberPredicate(FunctionArgument(args, 0), 2))), null, "isInteger", 1),
            new JsFunction((FunctionBody)(static args => BoxBoolean(NumberPredicate(FunctionArgument(args, 0), 3))), null, "isSafeInteger", 1)
        };
    }
    public static object NumberPredicateFunction(int mode) => NumberPredicateValues.Functions[mode];
}
