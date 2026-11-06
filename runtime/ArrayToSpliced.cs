using System;

public static partial class Js
{
    sealed class ArrayToSplicedFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, "toSpliced", 2)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var source = ToObject(receiver);
            long length = IndexedLength(source);
            double relative = Number(FunctionArgument(arguments, 0));
            relative = double.IsNaN(relative) ? 0 : Math.Truncate(relative);
            long start = (long)(relative < 0 ? Math.Max(length + relative, 0) : Math.Min(relative, length));
            long removed = arguments.Length == 0 ? 0 : length - start;
            if (arguments.Length > 1)
            {
                double count = Number(arguments[1]);
                removed = double.IsNaN(count) || count <= 0 ? 0 : (long)Math.Min(Math.Truncate(count), length - start);
            }
            int inserted = Math.Max(0, arguments.Length - 2);
            long resultLength = length - removed + inserted;
            if (resultLength > 9007199254740991L) throw new InvalidOperationException("Array-like length exceeds the safe integer limit");
            if (resultLength > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(receiver), "Invalid array length");
            var result = new JsArray(0);
            result.ReserveCapacity((int)Math.Min(resultLength, 4096));
            for (long i = 0; i < start; i++) result.Append(IndexedValue(source, i));
            for (int i = 2; i < arguments.Length; i++) result.Append(arguments[i]);
            for (long i = start + removed; i < length; i++) result.Append(IndexedValue(source, i));
            return result;
        }
    }
}
