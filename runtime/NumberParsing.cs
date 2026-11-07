using System;
using System.Globalization;
using System.Numerics;

public static partial class Js
{
    static class NumberParsers
    {
        internal static readonly JsFunction Integer = new((FunctionBody)(static args => ParseInteger(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "parseInt", 2);
        internal static readonly JsFunction Float = new((FunctionBody)(static args => ParseFloat(FunctionArgument(args, 0))), null, "parseFloat", 1);
    }
    static int SkipNumberWhitespace(string text)
    {
        int index = 0; while (index < text.Length && TrimWhitespace(text[index])) index++; return index;
    }
    static int ParseDigit(char value) => value is >= '0' and <= '9' ? value - '0' : value is >= 'a' and <= 'z' ? value - 'a' + 10 : value is >= 'A' and <= 'Z' ? value - 'A' + 10 : -1;
    static double ParseGroupedInteger(string text, int start, int end, int radix)
    {
        while (start < end && text[start] == '0') start++;
        // Match Node's rounding for radices where the standard permits an
        // approximation. Each floating-point step consumes a bounded digit block.
        uint scale = 1; int width = 0;
        while ((ulong)scale * (uint)radix <= uint.MaxValue / 36) { scale *= (uint)radix; width++; }
        double result = 0;
        while (start < end)
        {
            int count = Math.Min(width, end - start); uint block = 0, factor = 1;
            for (int index = 0; index < count; index++) { block = block * (uint)radix + (uint)ParseDigit(text[start++]); factor *= (uint)radix; }
            double shifted = result * factor; result = shifted + block;
        }
        return result;
    }
    static double ParseInteger(object input, object radixValue)
    {
        string text = StringValue(input);
        int radix = ToInt32(Number(radixValue));
        if (radix != 0 && (radix < 2 || radix > 36)) return double.NaN;
        bool stripHex = radix == 0 || radix == 16;
        if (radix == 0) radix = 10;
        int index = SkipNumberWhitespace(text);
        bool negative = index < text.Length && text[index] == '-';
        if (index < text.Length && text[index] is '+' or '-') index++;
        if (stripHex && index + 1 < text.Length && text[index] == '0' && text[index + 1] is 'x' or 'X') { radix = 16; index += 2; }
        if (radix != 10 && (radix & (radix - 1)) != 0)
        {
            int end = index;
            while (end < text.Length) { int digit = ParseDigit(text[end]); if (digit < 0 || digit >= radix) break; end++; }
            if (end == index) return double.NaN;
            double grouped = ParseGroupedInteger(text, index, end, radix);
            return negative ? -grouped : grouped;
        }
        int start = index; ulong small = 0; BigInteger large = default; bool wide = false, infinite = false;
        for (; index < text.Length; index++)
        {
            int digit = ParseDigit(text[index]);
            if (digit < 0 || digit >= radix) break;
            if (infinite) continue;
            if (!wide && small <= (ulong.MaxValue - (uint)digit) / (uint)radix) { small = small * (uint)radix + (uint)digit; continue; }
            if (!wide) { large = small; wide = true; }
            large = large * radix + digit;
            // Once above 1024 bits, every further digit still rounds to infinity.
            if (large.GetBitLength() > 1024) infinite = true;
        }
        if (index == start) return double.NaN;
        double result = infinite ? double.PositiveInfinity : wide
            ? double.Parse(large.ToString(CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture) : small;
        return negative ? -result : result;
    }
    static double ParseFloat(object input)
    {
        string text = StringValue(input);
        int start = SkipNumberWhitespace(text), index = start;
        if (index < text.Length && text[index] is '+' or '-') index++;
        if (text.AsSpan(index).StartsWith("Infinity", StringComparison.Ordinal)) return start < text.Length && text[start] == '-' ? double.NegativeInfinity : double.PositiveInfinity;
        int digits = 0;
        while (index < text.Length && text[index] is >= '0' and <= '9') { index++; digits++; }
        if (index < text.Length && text[index] == '.')
        {
            index++;
            while (index < text.Length && text[index] is >= '0' and <= '9') { index++; digits++; }
        }
        if (digits == 0) return double.NaN;
        int end = index;
        if (index < text.Length && text[index] is 'e' or 'E')
        {
            index++; if (index < text.Length && text[index] is '+' or '-') index++;
            int exponent = index;
            while (index < text.Length && text[index] is >= '0' and <= '9') index++;
            if (index != exponent) end = index;
        }
        return double.Parse(text.AsSpan(start, end - start), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
