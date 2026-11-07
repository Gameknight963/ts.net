using System;

public static partial class Js
{
    static object SplitRegExp(string text, JsRegExp regex, uint limit)
    {
        var result = new JsArray(0);
        if (limit == 0) return result;
        if (text.Length == 0)
        {
            if (!regex.Engine.IsMatch(text)) result.Append(text);
            return result;
        }
        int copied = 0, search = 0;
        while (search < text.Length)
        {
            var match = regex.Engine.Match(text, search);
            if (!match.Success || match.Index == text.Length) break;
            int end = match.Index + match.Length;
            if (end == copied) { search = match.Index + 1; continue; }
            result.Append(text.Substring(copied, match.Index - copied));
            if (result.Length() == limit) return result;
            for (int group = 1; group < match.Groups.Count; group++)
            {
                result.Append(match.Groups[group].Success ? match.Groups[group].Value : Undefined);
                if (result.Length() == limit) return result;
            }
            copied = end; search = end;
        }
        result.Append(text.Substring(copied)); return result;
    }
    sealed class StringSplitFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, "split", 2)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            object separator = FunctionArgument(arguments, 0), limitValue = FunctionArgument(arguments, 1);
            uint limit = IsUndefined(limitValue) ? uint.MaxValue : unchecked((uint)ToInt32(Number(limitValue)));
            if (separator is JsRegExp regex) return SplitRegExp(text, regex, limit);
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
