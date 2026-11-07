using System;
using System.Text;

public static partial class Js
{
    static void AppendReplacement(StringBuilder result, string template, string text, string match, int position)
    {
        int literal = 0;
        for (int index = 0; index + 1 < template.Length; index++)
        {
            if (template[index] != '$') continue;
            char token = template[index + 1];
            if (token != '$' && token != '&' && token != '`' && token != '\'') continue;
            result.Append(template, literal, index - literal);
            switch (token)
            {
                case '$': result.Append('$'); break;
                case '&': result.Append(match); break;
                case '`': result.Append(text, 0, position); break;
                case '\'': result.Append(text, position + match.Length, text.Length - position - match.Length); break;
            }
            index++; literal = index + 1;
        }
        result.Append(template, literal, template.Length - literal);
    }
    sealed class StringReplacementFunction(bool all) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, all ? "replaceAll" : "replace", 2)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            object searchValue = FunctionArgument(arguments, 0);
            if (searchValue is JsRegExp) throw new NotSupportedException("Regular-expression string replacement is not supported");
            string search = StringValue(searchValue);
            object replacement = FunctionArgument(arguments, 1);
            var function = replacement as JsFunction;
            // Replacement conversion is observable even when no match exists.
            string template = function == null ? StringValue(replacement) : null;
            int position = text.IndexOf(search, StringComparison.Ordinal);
            if (position < 0) return text;
            var result = new StringBuilder(text.Length);
            int copied = 0;
            while (true)
            {
                result.Append(text, copied, position - copied);
                if (function != null)
                    result.Append(StringValue(function.InvokeWithReceiver(Undefined, new object[] { search, (double)position, text })));
                else AppendReplacement(result, template, text, search, position);
                copied = position + search.Length;
                if (!all || search.Length == 0 && position == text.Length) break;
                position = text.IndexOf(search, position + Math.Max(search.Length, 1), StringComparison.Ordinal);
                if (position < 0) break;
            }
            result.Append(text, copied, text.Length - copied); return result.ToString();
        }
    }
}
