using System;
using System.Globalization;

public static partial class Js
{
    sealed class ArrayCopyWithinFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("copyWithin requires a receiver")), null, "copyWithin", 2)
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
            long target = Position(FunctionArgument(arguments, 0));
            long start = Position(FunctionArgument(arguments, 1));
            object endArgument = FunctionArgument(arguments, 2);
            long end = IsUndefined(endArgument) ? length : Position(endArgument);
            long count = Math.Min(end - start, length - target);
            if (count <= 0) return source;
            // All elements in this prefix are own numeric data properties. Range
            // conversion already ran, and Array.Copy handles overlapping regions.
            if (source is NumberArray numbers && length <= int.MaxValue)
            {
                double[] packed = numbers.TryPacked(length);
                if (packed != null) { Array.Copy(packed, (int)start, packed, (int)target, (int)count); return source; }
            }
            long direction = 1;
            if (start < target && target < start + count) { direction = -1; start += count - 1; target += count - 1; }
            for (; count > 0; count--, start += direction, target += direction)
            {
                string key = target.ToString(CultureInfo.InvariantCulture);
                if (TryIndexedValue(source, start, out object value)) source.SetWithReceiver(key, value, source);
                else if (!source.DeleteOwn(key)) throw new InvalidOperationException("Cannot delete array-like property");
            }
            return source;
        }
    }
}
