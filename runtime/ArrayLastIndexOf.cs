using System;

public static partial class Js
{
    sealed class ArrayLastIndexOfFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, "lastIndexOf", 1)
    {
        double Execute(object receiver, object[] arguments)
        {
            var source = ToObject(receiver);
            long length = IndexedLength(source);
            if (length == 0) return -1;
            double start = length - 1;
            if (arguments.Length > 1)
            {
                start = Number(arguments[1]);
                start = double.IsNaN(start) ? 0 : Math.Truncate(start);
                start = start >= 0 ? Math.Min(start, length - 1) : length + start;
            }
            object search = FunctionArgument(arguments, 0);
            // A fully present numeric range has no getters or inherited holes.
            // Check after fromIndex conversion, which may mutate the array.
            if (source is NumberArray numbers && numbers.TryPacked(length) is double[] packed)
            {
                if (search is not double target) return -1;
                for (int index = start < 0 ? -1 : (int)start; index >= 0; index--)
                    if (packed[index] == target) return index;
                return -1;
            }
            for (long index = start < 0 ? -1 : (long)start; index >= 0; index--)
                if (TryIndexedValue(source, index, out object value) && Equal(value, search)) return index;
            return -1;
        }
        public override object InvokeWithReceiver(object receiver, object[] arguments) => Execute(receiver, arguments);
        public override double? InvokeMethodNumber0(object receiver) => Execute(receiver, Array.Empty<object>());
        public override double? InvokeMethodNumber1(object receiver, double? first) => Execute(receiver, new[] { BoxNumber(first) });
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Execute(receiver, new[] { BoxNumber(first), BoxNumber(second) });
    }
}
