using System;

public static partial class Js
{
    sealed class StringSplitFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, "split", 2)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            object separator = FunctionArgument(arguments, 0), limitValue = FunctionArgument(arguments, 1);
            if (separator is JsRegExp) throw new NotSupportedException("Regular-expression string splitting is not supported");
            uint limit = IsUndefined(limitValue) ? uint.MaxValue : unchecked((uint)ToInt32(Number(limitValue)));
            string pattern = StringValue(separator);
            var result = new JsArray(0);
            if (limit == 0) return result;
            if (IsUndefined(separator)) { result.Append(text); return result; }
            if (pattern.Length == 0)
            {
                uint length = Math.Min(limit, (uint)text.Length);
                for (int index = 0; index < length; index++) result.Append(Iteration.CodeUnit(text[index]));
                return result;
            }
            int start = 0;
            while (true)
            {
                int match = text.IndexOf(pattern, start, StringComparison.Ordinal);
                if (match < 0) break;
                result.Append(text.Substring(start, match - start));
                if (result.Length() == limit) return result;
                start = match + pattern.Length;
            }
            result.Append(text.Substring(start)); return result;
        }
    }
}
