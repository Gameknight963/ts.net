using System;
using System.Globalization;

public static partial class Js
{
    sealed class ArrayConcatFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, "concat", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var source = ToObject(receiver);
            var result = new JsArray(0);
            long position = 0;
            void Append(object item)
            {
                if (item is JsArray array)
                {
                    long length = IndexedLength(array);
                    if (position + length > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(receiver), "Invalid array length");
                    for (long index = 0; index < length; index++, position++)
                        if (TryIndexedValue(array, index, out object value)) result.Set(position.ToString(CultureInfo.InvariantCulture), value);
                }
                else
                {
                    if (position == uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(receiver), "Invalid array length");
                    result.Set((position++).ToString(CultureInfo.InvariantCulture), item);
                }
            }
            Append(source);
            foreach (object item in arguments) Append(item);
            result.SetLength(position); return result;
        }
    }
}
