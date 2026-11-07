using System;
using System.Globalization;

public static partial class Js
{
    sealed class ArrayConcatFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, "concat", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var source = ToObject(receiver);
            var result = ArraySpeciesCreate(source, 0);
            long position = 0;
            void Append(object item)
            {
                bool spreadable = item is JsArray;
                if (item is TsObject obj) {
                    object flag = obj.Get(PropertyKey(ConcatSpreadableSymbol));
                    if (!IsUndefined(flag)) spreadable = Truthy(flag);
                }
                if (spreadable)
                {
                    var indexed = (TsObject)item;
                    long length = IndexedLength(indexed);
                    if (position + length > 9007199254740991L) throw new ArgumentOutOfRangeException(nameof(receiver), "Invalid array length");
                    for (long index = 0; index < length; index++, position++)
                        if (TryIndexedValue(indexed, index, out object value)) ArraySpeciesSet(result, position, value);
                }
                else
                {
                    if (position == 9007199254740991L) throw new ArgumentOutOfRangeException(nameof(receiver), "Invalid array length");
                    ArraySpeciesSet(result, position++, item);
                }
            }
            Append(source);
            foreach (object item in arguments) Append(item);
            result.Set("length", (double)position); return result;
        }
    }
}
