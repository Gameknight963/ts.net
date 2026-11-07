using System;
using System.Globalization;

public static partial class Js
{
    static object SliceArrayLike(object receiver, object start, object end)
    {
        if (receiver is JsArray array) return array.Slice(start, end);
        var source = ToObject(receiver);
        long length = IndexedLength(source);
        long Bound(object argument)
        {
            double value = Number(argument);
            value = double.IsNaN(value) ? 0 : Math.Truncate(value);
            return (long)(value < 0 ? Math.Max(length + value, 0) : Math.Min(value, length));
        }
        long first = Bound(start), last = IsUndefined(end) ? length : Bound(end);
        long count = Math.Max(last - first, 0);
        if (count > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(receiver), "Invalid array length");
        var result = new JsArray(0); result.SetLength(count);
        for (long index = 0; index < count; index++)
            if (TryIndexedValue(source, first + index, out object value)) result.Set(index.ToString(CultureInfo.InvariantCulture), value);
        return result;
    }
}
