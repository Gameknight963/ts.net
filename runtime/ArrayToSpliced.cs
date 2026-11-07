using System;
using System.Globalization;

public static partial class Js
{
    sealed class ArrayToSplicedFunction(bool mutate = false) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, mutate ? "splice" : "toSpliced", 2)
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
            if (mutate)
            {
                if (removed > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(receiver), "Invalid array length");
                var deleted = ArraySpeciesCreate(source, removed);
                for (long i = 0; i < removed; i++)
                    if (TryIndexedValue(source, start + i, out object value)) ArraySpeciesSet(deleted, i, value);
                deleted.Set("length", (double)removed);
                void Delete(long index)
                {
                    if (!source.DeleteOwn(index.ToString(CultureInfo.InvariantCulture))) throw new InvalidOperationException("Cannot delete array-like property");
                }
                void Move(long from, long to)
                {
                    if (TryIndexedValue(source, from, out object value)) source.SetWithReceiver(to.ToString(CultureInfo.InvariantCulture), value, source);
                    else Delete(to);
                }
                if (inserted < removed)
                {
                    for (long i = start; i < length - removed; i++) Move(i + removed, i + inserted);
                    for (long i = length; i > resultLength; i--) Delete(i - 1);
                }
                else if (inserted > removed)
                    for (long i = length - removed; i > start; i--) Move(i + removed - 1, i + inserted - 1);
                for (int i = 0; i < inserted; i++) source.SetWithReceiver((start + i).ToString(CultureInfo.InvariantCulture), arguments[i + 2], source);
                source.SetWithReceiver("length", (double)resultLength, source);
                return deleted;
            }
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
