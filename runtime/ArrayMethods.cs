using System;

public static partial class Js
{
    sealed class ArrayMethod(string name, int mode, int length) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, name, length)
    {
        object Execute(object receiver, object[] args)
        {
            if (receiver is not JsArray) throw new InvalidOperationException("Array method requires an array");
            return mode switch
            {
                0 => ArrayPush(receiver, args),
                1 => ArrayPop(receiver),
                2 => Slice(receiver, FunctionArgument(args, 0), FunctionArgument(args, 1)),
                3 => ArrayReverse(receiver, true),
                4 => ArraySearch(receiver, FunctionArgument(args, 0), FunctionArgument(args, 1), false),
                _ => BoxBoolean(ArraySearch(receiver, FunctionArgument(args, 0), FunctionArgument(args, 1), true) >= 0)
            };
        }
        public override object InvokeWithReceiver(object receiver, object[] args) => Execute(receiver, args);
        public override double? InvokeMethodNumber0(object receiver) => Cast<double?>(Execute(receiver, Array.Empty<object>()), "Function result violates number type");
        public override double? InvokeMethodNumber1(object receiver, double? first) => Cast<double?>(Execute(receiver, new[] { BoxNumber(first) }), "Function result violates number type");
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Cast<double?>(Execute(receiver, new[] { BoxNumber(first), BoxNumber(second) }), "Function result violates number type");
    }
    static class ArrayMethods
    {
        internal static readonly JsFunction Join = new ArrayJoinFunction(), Push = new ArrayMethod("push", 0, 1), Pop = new ArrayMethod("pop", 1, 0), Slice = new ArrayMethod("slice", 2, 2), Reverse = new ArrayMethod("reverse", 3, 0), IndexOf = new ArrayMethod("indexOf", 4, 1), Includes = new ArrayMethod("includes", 5, 1);
    }
    internal static bool IsArrayMethod(string key) => key is "join" or "at" or "push" or "pop" or "slice" or "reverse" or "indexOf" or "includes";
    internal static object ArrayMethodValue(string key) => key switch { "join" => ArrayMethods.Join, "at" => ArrayAtFunction(), "push" => ArrayMethods.Push, "pop" => ArrayMethods.Pop, "slice" => ArrayMethods.Slice, "reverse" => ArrayMethods.Reverse, "indexOf" => ArrayMethods.IndexOf, "includes" => ArrayMethods.Includes, _ => Undefined };
}
