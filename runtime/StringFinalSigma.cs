using System;
using System.Globalization;
using System.Text;

public static partial class Js
{
    static bool SigmaCaseIgnorable(Rune rune) => Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format or UnicodeCategory.ModifierLetter or UnicodeCategory.ModifierSymbol
        || rune.Value is 0x27 or 0x2e or 0x3a or 0xb7 or 0x387 or 0x55f or 0x5f4 or 0x2018 or 0x2019 or 0x2024 or 0x2027 or 0xfe13 or 0xfe52 or 0xfe55 or 0xff07 or 0xff0e or 0xff1a;
    static bool SigmaCased(Rune rune) => Rune.GetUnicodeCategory(rune) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter
        || rune.IsBmp && (RecentCase((char)rune.Value, true) != rune.Value || RecentCase((char)rune.Value, false) != rune.Value)
        || rune.Value is 0xaa or 0xba or >= 0x2160 and <= 0x217f or >= 0x24b6 and <= 0x24e9 or >= 0x1f130 and <= 0x1f149 or >= 0x1f150 and <= 0x1f169 or >= 0x1f170 and <= 0x1f189;
    static bool SigmaCasedNeighbor(string text, int index, bool backward)
    {
        while ((uint)index < (uint)text.Length)
        {
            if (backward && char.IsLowSurrogate(text[index]) && index > 0 && char.IsHighSurrogate(text[index - 1])) index--;
            if (!Rune.TryGetRuneAt(text, index, out var rune)) return false;
            if (!SigmaCaseIgnorable(rune)) return SigmaCased(rune);
            index += backward ? -1 : rune.Utf16SequenceLength;
        }
        return false;
    }
    static string ApplyFinalSigma(string text)
    {
        char[] result = null;
        for (int index = text.IndexOf('\u03a3'); index >= 0; index = text.IndexOf('\u03a3', index + 1))
        {
            if (!SigmaCasedNeighbor(text, index - 1, true) || SigmaCasedNeighbor(text, index + 1, false)) continue;
            result ??= text.ToCharArray();
            result[index] = '\u03c2';
        }
        return result == null ? text : new string(result);
    }
}
