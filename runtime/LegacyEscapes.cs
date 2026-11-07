using System;
using System.Text;

public static partial class Js
{
    static class LegacyEscapes
    {
        internal static readonly JsFunction Escape = new((FunctionBody)(static args => EscapeText(StringValue(FunctionArgument(args, 0)))), null, "escape", 1);
        internal static readonly JsFunction Unescape = new((FunctionBody)(static args => UnescapeText(StringValue(FunctionArgument(args, 0)))), null, "unescape", 1);
    }
    static string EscapeText(string text)
    {
        StringBuilder result = null;
        const string hex = "0123456789ABCDEF";
        for (int index = 0; index < text.Length; index++)
        {
            char value = text[index];
            bool unchanged = value is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '@' or '*' or '_' or '+' or '-' or '.' or '/';
            if (unchanged) { result?.Append(value); continue; }
            result ??= new StringBuilder(text.Length).Append(text, 0, index);
            result.Append('%');
            if (value > 255) { result.Append('u'); result.Append(hex[value >> 12]); result.Append(hex[value >> 8 & 15]); }
            result.Append(hex[value >> 4 & 15]); result.Append(hex[value & 15]);
        }
        return result?.ToString() ?? text;
    }
    static string UnescapeText(string text)
    {
        StringBuilder result = null;
        for (int index = 0; index < text.Length; index++)
        {
            if (text[index] == '%')
            {
                int digits = index + 1 < text.Length && text[index + 1] == 'u' ? 4 : 2;
                int start = index + (digits == 4 ? 2 : 1), code = 0, count = 0;
                for (; count < digits && start + count < text.Length; count++)
                {
                    char value = text[start + count];
                    int digit = value is >= '0' and <= '9' ? value - '0' : value is >= 'a' and <= 'f' ? value - 'a' + 10 : value is >= 'A' and <= 'F' ? value - 'A' + 10 : -1;
                    if (digit < 0) break;
                    code = code * 16 + digit;
                }
                if (count == digits)
                {
                    result ??= new StringBuilder(text.Length).Append(text, 0, index);
                    result.Append((char)code); index = start + digits - 1; continue;
                }
            }
            result?.Append(text[index]);
        }
        return result?.ToString() ?? text;
    }
}
