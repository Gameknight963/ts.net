using System;

public static partial class Js
{
    static char RecentCase(char value, bool upper) => upper ? value switch
    {
        '\u019b' => '\ua7dc', '\u0264' => '\ua7cb', '\u1c8a' => '\u1c89', '\u2c5f' => '\u2c2f',
        '\ua7c1' => '\ua7c0', '\ua7c8' => '\ua7c7', '\ua7ca' => '\ua7c9',
        >= '\ua7cd' and <= '\ua7db' when (value & 1) != 0 => (char)(value - 1),
        '\ua7f6' => '\ua7f5', _ => value
    } : value switch
    {
        '\u1c89' => '\u1c8a', '\u2c2f' => '\u2c5f', '\ua7c0' => '\ua7c1', '\ua7c7' => '\ua7c8', '\ua7c9' => '\ua7ca',
        '\ua7cb' => '\u0264', '\ua7dc' => '\u019b',
        >= '\ua7cc' and <= '\ua7da' when (value & 1) == 0 => (char)(value + 1),
        '\ua7f5' => '\ua7f6', _ => value
    };
    static string RecentLowercase(string text)
    {
        int first = text.AsSpan().IndexOfAnyInRange('\u1c89', '\ua7f5');
        if (first < 0) return text;
        char[] result = null;
        for (int index = first; index < text.Length; index++)
        {
            char mapped = RecentCase(text[index], false);
            if (mapped == text[index]) continue;
            result ??= text.ToCharArray();
            result[index] = mapped;
        }
        return result == null ? text : new string(result);
    }
}
