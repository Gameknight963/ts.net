using System;
using System.Globalization;

public static partial class Js
{
    sealed class ArrayFillFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("fill requires a receiver")), null, "fill", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var source = ToObject(receiver);
            long length = IndexedLength(source);
            long Position(object argument)
            {
                double value = Number(argument);
                value = double.IsNaN(value) ? 0 : Math.Truncate(value);
                return (long)(value < 0 ? Math.Max(length + value, 0) : Math.Min(value, length));
            }
            object value = FunctionArgument(arguments, 0);
            long start = Position(FunctionArgument(arguments, 1));
            object endArgument = FunctionArgument(arguments, 2);
            long end = IsUndefined(endArgument) ? length : Position(endArgument);
            // Existing packed elements are own data properties. This path cannot
            // invoke inherited setters, and both range conversions already ran.
            if (source is NumberArray numbers && value is double number && end > start && end <= int.MaxValue)
            {
                double[] packed = numbers.TryPacked(end);
                if (packed != null)
                {
                    Array.Fill(packed, double.IsNaN(number) ? double.NaN : number, (int)start, (int)(end - start));
                    return source;
                }
            }
            for (long index = start; index < end; index++)
                source.SetWithReceiver(index.ToString(CultureInfo.InvariantCulture), value, source);
            return source;
        }
    }
}
