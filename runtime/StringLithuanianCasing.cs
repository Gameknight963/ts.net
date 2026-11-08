using System;
using System.Text;

public static partial class Js
{
    static bool SoftDotted(Rune rune) => rune.Value is 0x69 or 0x6a or 0x12f or 0x249 or 0x268 or 0x29d or 0x2b2 or 0x3f3 or 0x456 or 0x458 or 0x1d62 or 0x1d96 or 0x1da4 or 0x1da8 or 0x1e2d or 0x1ecb or 0x2071 or 0x2148 or 0x2149 or 0x2c7c
        or >= 0x1d422 and <= 0x1d423 or >= 0x1d456 and <= 0x1d457 or >= 0x1d48a and <= 0x1d48b or >= 0x1d4be and <= 0x1d4bf or >= 0x1d4f2 and <= 0x1d4f3 or >= 0x1d526 and <= 0x1d527 or >= 0x1d55a and <= 0x1d55b or >= 0x1d58e and <= 0x1d58f or >= 0x1d5c2 and <= 0x1d5c3 or >= 0x1d5f6 and <= 0x1d5f7 or >= 0x1d62a and <= 0x1d62b or >= 0x1d65e and <= 0x1d65f or >= 0x1d692 and <= 0x1d693 or 0x1df1a or 0x1e04c or 0x1e04d or 0x1e068;
    static bool LithuanianContext(string text, int index, bool backward)
    {
        while ((uint)index < (uint)text.Length)
        {
            if (backward && char.IsLowSurrogate(text[index]) && index > 0 && char.IsHighSurrogate(text[index - 1])) index--;
            if (!Rune.TryGetRuneAt(text, index, out var rune)) return false;
            if (backward && SoftDotted(rune)) return true;
            int category = CaseCombiningClass(rune);
            if (!backward && category == 230) return true;
            if (category != -1) return false;
            index += backward ? -1 : rune.Utf16SequenceLength;
        }
        return false;
    }
    static string ApplyLithuanianCasing(string text, bool upper)
    {
        StringBuilder result = null;
        int copied = 0;
        for (int index = 0; index < text.Length; index++)
        {
            string replacement = upper
                ? text[index] == '\u0307' && LithuanianContext(text, index - 1, true) ? "" : null
                : text[index] switch
                {
                    'I' when LithuanianContext(text, index + 1, false) => "i\u0307",
                    'J' when LithuanianContext(text, index + 1, false) => "j\u0307",
                    '\u012e' when LithuanianContext(text, index + 1, false) => "\u012f\u0307",
                    '\u00cc' => "i\u0307\u0300", '\u00cd' => "i\u0307\u0301", '\u0128' => "i\u0307\u0303",
                    _ => null
                };
            if (replacement == null) continue;
            result ??= new StringBuilder(text.Length + 8);
            result.Append(text, copied, index - copied).Append(replacement);
            copied = index + 1;
        }
        return result == null ? text : result.Append(text, copied, text.Length - copied).ToString();
    }
}
