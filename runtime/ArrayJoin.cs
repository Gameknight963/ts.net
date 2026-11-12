using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    [ThreadStatic] static HashSet<object> joining;
    sealed class ArrayJoinFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("join requires a receiver")), null, "join", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => ArrayJoin(receiver, FunctionArgument(arguments, 0));
    }
    static string ArrayJoin(object receiver, object separator)
    {
        RequireObject(receiver);
        double size = Number(Get(receiver, "length"));
        long length = double.IsNaN(size) || size <= 0 ? 0 : (long)Math.Min(9007199254740991d, Math.Floor(size));
        string text = IsUndefined(separator) ? "," : StringValue(separator);
        if (length == 0) return "";
        var active = joining ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (!active.Add(receiver)) return "";
        try
        {
            var result = new StringBuilder();
            // Separator conversion has already run. A fully present numeric
            // range with no accessors cannot invoke user code while joining.
            double[] packed = receiver is NumberArray numeric && !numeric.HasIndexedAccessors ? numeric.TryPacked(length)
                : receiver is TaggedArray tagged ? tagged.TryPackedNumbers(length) : null;
            if (packed != null)
            {
                for (int i = 0; i < (int)length; i++)
                {
                    if (i != 0) result.Append(text);
                    result.Append(NumberKeyString(packed[i]));
                }
                return result.ToString();
            }
            for (long i = 0; i < length; i++)
            {
                if (i != 0) result.Append(text);
                object value;
                if (receiver is JsArray array)
                {
                    uint index = (uint)i;
                    value = array.HasElement(index) ? array.ElementAt(index) : array.PropertyPrototype?.GetWithReceiver(i.ToString(CultureInfo.InvariantCulture), array) ?? Undefined;
                }
                else value = IndexGet(receiver, (double)i);
                if (!IsNullish(value)) result.Append(StringValue(value));
            }
            return result.ToString();
        }
        finally { active.Remove(receiver); }
    }
}
