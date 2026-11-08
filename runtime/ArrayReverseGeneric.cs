using System;
using System.Globalization;

public static partial class Js
{
    internal static object ReverseArrayLike(object receiver)
    {
        if (receiver is JsArray array && array.CanUseElementStorage) return array.Reverse();
        var source = ToObject(receiver);
        long length = IndexedLength(source);
        void Set(long index, object value) => source.SetWithReceiver(index.ToString(CultureInfo.InvariantCulture), value, source);
        void Delete(long index)
        {
            if (!source.DeleteOwn(index.ToString(CultureInfo.InvariantCulture))) throw new InvalidOperationException("Cannot delete array-like property");
        }
        for (long lower = 0; lower < length / 2; lower++)
        {
            long upper = length - lower - 1;
            bool hasLower = TryIndexedValue(source, lower, out object low);
            bool hasUpper = TryIndexedValue(source, upper, out object high);
            if (hasLower && hasUpper) { Set(lower, high); Set(upper, low); }
            else if (hasUpper) { Set(lower, high); Delete(upper); }
            else if (hasLower) { Delete(lower); Set(upper, low); }
        }
        return source;
    }
}
