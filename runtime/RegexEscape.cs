using System;
using System.Globalization;
using System.Text;

public static partial class Js
{
    static object EscapeRegExp(object value)
    {
        if (value is not string text) throw new InvalidOperationException("RegExp.escape requires a string");
        var result = new StringBuilder(text.Length);
        for (int index = 0; index < text.Length; index++)
        {
            char ch = text[index];
            if (index == 0 && char.IsAsciiLetterOrDigit(ch))
                result.Append("\\x").Append(((int)ch).ToString("x2", CultureInfo.InvariantCulture));
            else if ("^$\\.*+?()[]{}|/".Contains(ch)) result.Append('\\').Append(ch);
            else if (ch is '\t' or '\n' or '\v' or '\f' or '\r')
                result.Append('\\').Append(ch switch { '\t' => 't', '\n' => 'n', '\v' => 'v', '\f' => 'f', _ => 'r' });
            else if (char.IsHighSurrogate(ch) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                result.Append(ch).Append(text[++index]);
            else if (",-=<>#&!%:;@~'`\"".Contains(ch) || ch is '\uFEFF' or '\u2028' or '\u2029' || char.GetUnicodeCategory(ch) == UnicodeCategory.SpaceSeparator || char.IsSurrogate(ch))
                result.Append(ch <= 255 ? "\\x" : "\\u").Append(((int)ch).ToString(ch <= 255 ? "x2" : "x4", CultureInfo.InvariantCulture));
            else result.Append(ch);
        }
        return result.ToString();
    }
}
