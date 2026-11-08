using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    static double RegExpLength(object value)
    {
        double number = Number(value);
        return double.IsNaN(number) || number <= 0 ? 0 : Math.Min(9007199254740991d, Math.Floor(number));
    }
    static object ReplaceCustomRegExp(string text, TsObject regex, JsFunction function, string template, string flags)
    {
        bool global = flags.Contains('g'), unicode = flags.Contains('u') || flags.Contains('v');
        if (global) regex.Set("lastIndex", 0d);
        var matches = new List<TsObject>();
        while (true)
        {
            var match = ExecuteRegExp(regex, text);
            if (match == null) break;
            matches.Add(match);
            if (!global) break;
            if (StringValue(match.Get("0")).Length == 0)
            {
                double index = RegExpLength(regex.Get("lastIndex"));
                int step = unicode && index < text.Length - 1 && char.IsHighSurrogate(text[(int)index]) && char.IsLowSurrogate(text[(int)index + 1]) ? 2 : 1;
                regex.Set("lastIndex", index + step);
            }
        }
        var output = new StringBuilder(text.Length);
        long copied = 0;
        foreach (var match in matches)
        {
            double length = RegExpLength(match.Get("length"));
            if (length > Array.MaxLength - 3) throw new ArgumentOutOfRangeException(null, "Too many replacement captures");
            int count = (int)Math.Max(length - 1, 0);
            string value = StringValue(match.Get("0"));
            double index = Number(match.Get("index"));
            int position = double.IsNaN(index) || index <= 0 ? 0 : (int)Math.Min(text.Length, Math.Truncate(index));
            var captures = new object[count];
            for (int capture = 0; capture < count; capture++)
            {
                object item = match.Get((capture + 1).ToString(CultureInfo.InvariantCulture));
                captures[capture] = IsUndefined(item) ? Undefined : StringValue(item);
            }
            object groups = match.Get("groups");
            string replacement;
            if (function != null)
            {
                var args = new object[count + (IsUndefined(groups) ? 3 : 4)];
                args[0] = value; captures.CopyTo(args, 1);
                args[count + 1] = (double)position; args[count + 2] = text;
                if (!IsUndefined(groups)) args[^1] = groups;
                replacement = StringValue(function.InvokeWithReceiver(Undefined, args));
            }
            else replacement = CustomRegExpSubstitution(template, text, value, position, captures, IsUndefined(groups) ? null : ToObject(groups));
            if (position >= copied)
            {
                output.Append(text, (int)copied, position - (int)copied).Append(replacement);
                copied = (long)position + value.Length;
            }
        }
        if (copied < text.Length) output.Append(text, (int)copied, text.Length - (int)copied);
        return output.ToString();
    }
    static string CustomRegExpSubstitution(string template, string text, string match, int position, object[] captures, TsObject groups)
    {
        var result = new StringBuilder();
        for (int index = 0; index < template.Length; index++)
        {
            char token = template[index];
            if (token != '$' || index + 1 == template.Length) { result.Append(token); continue; }
            token = template[index + 1];
            if (token is '$' or '&' or '`' or '\'')
            {
                if (token == '$') result.Append('$');
                else if (token == '&') result.Append(match);
                else if (token == '`') result.Append(text, 0, position);
                else { int start = (int)Math.Min((long)position + match.Length, text.Length); result.Append(text, start, text.Length - start); }
                index++; continue;
            }
            if (token == '<' && groups != null)
            {
                int end = template.IndexOf('>', index + 2);
                if (end >= 0)
                {
                    object value = groups.Get(template[(index + 2)..end]);
                    if (!IsUndefined(value)) result.Append(StringValue(value));
                    index = end; continue;
                }
            }
            if (token is >= '0' and <= '9')
            {
                int capture = token - '0', digits = 1;
                if (index + 2 < template.Length && char.IsAsciiDigit(template[index + 2]))
                {
                    int pair = capture * 10 + template[index + 2] - '0';
                    if (pair > 0 && pair <= captures.Length) { capture = pair; digits = 2; }
                }
                if (capture > 0 && capture <= captures.Length)
                {
                    if (!IsUndefined(captures[capture - 1])) result.Append((string)captures[capture - 1]);
                    index += digits; continue;
                }
            }
            result.Append('$');
        }
        return result.ToString();
    }
}
