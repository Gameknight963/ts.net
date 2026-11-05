using System;
using System.Globalization;

public static partial class Js
{
    static bool NumberSpace(char value) => value is >= '\u0009' and <= '\u000D' or
        '\u0020' or '\u00A0' or '\u1680' or >= '\u2000' and <= '\u200A' or
        '\u2028' or '\u2029' or '\u202F' or '\u205F' or '\u3000' or '\uFEFF';

    static double StringNumber(string source)
    {
        ReadOnlySpan<char> text = source.AsSpan();
        int start = 0, end = text.Length;
        while (start < end && NumberSpace(text[start])) start++;
        while (end > start && NumberSpace(text[end - 1])) end--;
        text = text[start..end];
        if (text.Length == 0) return 0;
        if (text.SequenceEqual("Infinity") || text.SequenceEqual("+Infinity")) return double.PositiveInfinity;
        if (text.SequenceEqual("-Infinity")) return double.NegativeInfinity;
        if (text.Length >= 2 && text[0] == '0') {
            int width = text[1] switch { 'x' or 'X' => 4, 'o' or 'O' => 3, 'b' or 'B' => 1, _ => 0 };
            if (width != 0) return RadixNumber(text[2..], width);
        }
        int index = text[0] is '+' or '-' ? 1 : 0;
        int digits = 0; ulong integer = 0;
        while (index < text.Length && text[index] is >= '0' and <= '9') {
            if (digits < 15) integer = integer * 10 + (uint)(text[index] - '0');
            digits++; index++;
        }
        if (index == text.Length && digits is > 0 and <= 15)
            return text[0] == '-' ? -(double)integer : integer;
        if (index < text.Length && text[index] == '.') {
            index++;
            while (index < text.Length && text[index] is >= '0' and <= '9') { digits++; index++; }
        }
        if (digits == 0) return double.NaN;
        if (index < text.Length && text[index] is 'e' or 'E') {
            index++;
            if (index < text.Length && text[index] is '+' or '-') index++;
            int first = index;
            while (index < text.Length && text[index] is >= '0' and <= '9') index++;
            if (index == first) return double.NaN;
        }
        if (index != text.Length) return double.NaN;
        return double.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint |
            NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out double number) ? number : double.NaN;
    }

    static double RadixNumber(ReadOnlySpan<char> text, int width)
    {
        if (text.Length == 0) return double.NaN;
        ulong leading = 0; long bits = 0; bool sticky = false;
        foreach (char ch in text) {
            int digit = ch is >= '0' and <= '9' ? ch - '0' : ch is >= 'a' and <= 'f' ? ch - 'a' + 10 :
                ch is >= 'A' and <= 'F' ? ch - 'A' + 10 : -1;
            if ((uint)digit >= (1u << width)) return double.NaN;
            for (int shift = width - 1; shift >= 0; shift--) {
                uint bit = (uint)(digit >> shift) & 1;
                if (bits == 0 && bit == 0) continue;
                bits++;
                if (bits <= 54) leading = (leading << 1) | bit;
                else sticky |= bit != 0;
            }
        }
        if (bits <= 53) return leading;
        if (bits > 1024) return double.PositiveInfinity;
        // Retain 53 bits and round ties to even using the discarded bits.
        ulong rounded = leading >> 1;
        if ((leading & 1) != 0 && (sticky || (rounded & 1) != 0)) rounded++;
        return Math.ScaleB(rounded, (int)bits - 53);
    }
}
