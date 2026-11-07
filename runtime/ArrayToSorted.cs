using System;
using System.Globalization;

public static partial class Js
{
    static double CompareSortedElements(object left, object right, JsFunction comparator)
    {
        if (IsUndefined(left)) return IsUndefined(right) ? 0 : 1;
        if (IsUndefined(right)) return -1;
        if (comparator != null)
        {
            double result = Number(comparator.InvokeWithReceiver(Undefined, new[] { left, right }));
            return double.IsNaN(result) ? 0 : result;
        }
        string first = StringValue(left), second = StringValue(right);
        return string.CompareOrdinal(first, second);
    }
    // Merging keeps equal elements in their original order and lets callback
    // exceptions propagate without the exception wrapping of Array.Sort.
    static void StableSort(object[] values, object[] scratch, int first, int end, JsFunction comparator)
    {
        if (end - first < 2) return;
        int middle = first + (end - first) / 2;
        StableSort(values, scratch, first, middle, comparator);
        StableSort(values, scratch, middle, end, comparator);
        int left = first, right = middle, output = first;
        while (left < middle && right < end)
            scratch[output++] = CompareSortedElements(values[left], values[right], comparator) <= 0 ? values[left++] : values[right++];
        while (left < middle) scratch[output++] = values[left++];
        while (right < end) scratch[output++] = values[right++];
        Array.Copy(scratch, first, values, first, end - first);
    }
    sealed class ArrayToSortedFunction(bool mutate = false) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, mutate ? "sort" : "toSorted", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            object argument = FunctionArgument(arguments, 0);
            if (!IsUndefined(argument) && argument is not JsFunction) throw new InvalidOperationException("Array comparator is not callable");
            var source = ToObject(receiver);
            long length = IndexedLength(source);
            if (length > Array.MaxLength) throw new ArgumentOutOfRangeException(nameof(receiver), "Array is too large to sort");
            var values = new object[(int)length];
            int count = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (!mutate) values[count++] = IndexedValue(source, i);
                else if (TryIndexedValue(source, i, out object value)) values[count++] = value;
            }
            if (count > 1) StableSort(values, new object[count], 0, count, argument as JsFunction);
            if (!mutate) return new JsArray(values);
            for (int i = 0; i < count; i++) source.SetWithReceiver(i.ToString(CultureInfo.InvariantCulture), values[i], source);
            for (long i = count; i < length; i++)
                if (!source.DeleteOwn(i.ToString(CultureInfo.InvariantCulture))) throw new InvalidOperationException("Cannot delete array-like property");
            return source;
        }
    }
}
