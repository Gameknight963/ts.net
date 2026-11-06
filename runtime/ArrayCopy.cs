using System;

public static partial class Js
{
    sealed class ArrayCopyFunction(string name, bool replace) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, name, replace ? 2 : 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var source = ToObject(receiver);
            long length = IndexedLength(source);
            double index = -1;
            if (replace)
            {
                index = Number(FunctionArgument(arguments, 0));
                index = double.IsNaN(index) ? 0 : Math.Truncate(index);
                if (index < 0) index += length;
                if (index < 0 || index >= length) throw new ArgumentOutOfRangeException(nameof(index), "Array index is out of range");
            }
            if (length > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(receiver), "Invalid array length");
            object replacement = FunctionArgument(arguments, 1);
            var result = new JsArray(0);
            result.ReserveCapacity((int)Math.Min(length, 4096));
            for (long i = 0; i < length; i++)
                result.Append(replace && i == index ? replacement : IndexedValue(source, replace ? i : length - i - 1));
            return result;
        }
    }
}
