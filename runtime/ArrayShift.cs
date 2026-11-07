using System;
using System.Globalization;

public static partial class Js
{
    sealed class ArrayShiftFunction(bool prepend) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, prepend ? "unshift" : "shift", prepend ? 1 : 0)
    {
        public override double? InvokeMethodNumber0(object receiver) => Cast<double?>(InvokeWithReceiver(receiver, Array.Empty<object>()), "Function result violates number type");
        public override double? InvokeMethodNumber1(object receiver, double? first) => Cast<double?>(InvokeWithReceiver(receiver, new[] { BoxNumber(first) }), "Function result violates number type");
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Cast<double?>(InvokeWithReceiver(receiver, new[] { BoxNumber(first), BoxNumber(second) }), "Function result violates number type");
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var source = ToObject(receiver);
            long length = IndexedLength(source);
            void Move(long from, long to)
            {
                string key = to.ToString(CultureInfo.InvariantCulture);
                if (TryIndexedValue(source, from, out object value)) source.SetWithReceiver(key, value, source);
                else if (!source.DeleteOwn(key)) throw new InvalidOperationException("Cannot delete array-like property");
            }
            if (prepend)
            {
                int count = arguments.Length;
                if (length > 9007199254740991L - count) throw new InvalidOperationException("Array-like length exceeds the safe integer limit");
                if (count > 0)
                {
                    for (long index = length; index > 0; index--) Move(index - 1, index + count - 1);
                    for (int index = 0; index < count; index++) source.SetWithReceiver(index.ToString(CultureInfo.InvariantCulture), arguments[index], source);
                }
                source.SetWithReceiver("length", (double)(length + count), source);
                return (double)(length + count);
            }
            object first = length == 0 ? Undefined : IndexedValue(source, 0);
            if (length > 0)
            {
                for (long index = 1; index < length; index++) Move(index, index - 1);
                if (!source.DeleteOwn((length - 1).ToString(CultureInfo.InvariantCulture))) throw new InvalidOperationException("Cannot delete array-like property");
                length--;
            }
            source.SetWithReceiver("length", (double)length, source);
            return first;
        }
    }
}
