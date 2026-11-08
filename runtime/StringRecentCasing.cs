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
    static int RecentSupplementaryPoint(int value, bool upper) => upper ? value switch
    {
        >= 0x10597 and <= 0x105a1 or >= 0x105a3 and <= 0x105b1 or >= 0x105b3 and <= 0x105b9 or >= 0x105bb and <= 0x105bc => value - 0x27,
        >= 0x10d70 and <= 0x10d85 => value - 0x20,
        >= 0x16ebb and <= 0x16ed3 => value - 0x1b,
        _ => value
    } : value switch
    {
        >= 0x10570 and <= 0x1057a or >= 0x1057c and <= 0x1058a or >= 0x1058c and <= 0x10592 or >= 0x10594 and <= 0x10595 => value + 0x27,
        >= 0x10d50 and <= 0x10d65 => value + 0x20,
        >= 0x16ea0 and <= 0x16eb8 => value + 0x1b,
        _ => value
    };
    static string RecentSupplementaryCase(string text, bool upper)
    {
        int first = text.AsSpan().IndexOfAnyInRange('\ud800', '\udbff');
        if (first < 0) return text;
        char[] result = null;
        for (int index = first; index < text.Length; index++)
        {
            if (!char.IsHighSurrogate(text[index]) || index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1])) continue;
            int value = char.ConvertToUtf32(text[index], text[index + 1]);
            int mapped = RecentSupplementaryPoint(value, upper);
            if (mapped != value)
            {
                result ??= text.ToCharArray();
                int supplementary = mapped - 0x10000;
                result[index] = (char)(0xd800 + (supplementary >> 10));
                result[index + 1] = (char)(0xdc00 + (supplementary & 1023));
            }
            index++;
        }
        return result == null ? text : new string(result);
    }
}
