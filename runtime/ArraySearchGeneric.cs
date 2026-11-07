using System;

public static partial class Js
{
    static double SearchArrayLike(object receiver, object search, object fromIndex, bool includes)
    {
        if (receiver is JsArray array) return array.Search(search, fromIndex, includes);
        var source = ToObject(receiver);
        long length = IndexedLength(source);
        if (length == 0) return -1;
        double start = Number(fromIndex);
        start = double.IsNaN(start) ? 0 : Math.Truncate(start);
        if (start >= length) return -1;
        long first = (long)(start < 0 ? Math.Max(length + start, 0) : start);
        for (long index = first; index < length; index++)
        {
            object value;
            if (includes) value = IndexedValue(source, index);
            else if (!TryIndexedValue(source, index, out value)) continue;
            if (Equal(value, search) || includes && value is double left && search is double right && double.IsNaN(left) && double.IsNaN(right)) return index;
        }
        return -1;
    }
}
