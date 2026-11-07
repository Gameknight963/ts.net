using System;
using System.Collections.Generic;
using System.Text;

public static partial class Js
{
    [ThreadStatic] static HashSet<object> localeJoining;
    sealed class ArrayLocaleFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, "toLocaleString", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var source = ToObject(receiver); long length = IndexedLength(source);
            var active = localeJoining ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
            if (!active.Add(source)) return "";
            try
            {
                object locales = FunctionArgument(arguments, 0), options = FunctionArgument(arguments, 1);
                var result = new StringBuilder();
                for (long index = 0; index < length; index++)
                {
                    if (index != 0) result.Append(',');
                    object value = IndexedValue(source, index);
                    if (IsNullish(value)) continue;
                    result.Append(StringValue(CallMethod(Js.Get(value, "toLocaleString"), value, new[] { locales, options })));
                }
                return result.ToString();
            }
            finally { active.Remove(source); }
        }
    }
}
