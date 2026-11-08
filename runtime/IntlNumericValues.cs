using System;
using System.Globalization;
using System.Numerics;

public static partial class Js
{
    readonly record struct LocaleDecimal(BigInteger Digits, int Exponent, bool Negative) : IFormattable
    {
        public string ToString(string format, IFormatProvider provider) => (Negative ? "-" : "") + Digits.ToString(CultureInfo.InvariantCulture) + "E" + Exponent.ToString(CultureInfo.InvariantCulture);
    }
    static object IntlNumericValue(object value)
    {
        if (value is TsObject obj) value = ToPrimitive(obj, "number");
        if (value is not string source) return value;
        double rounded = StringNumber(source);
        if (!double.IsFinite(rounded) || rounded == 0) return rounded;
        int start = 0, end = source.Length;
        while (start < end && NumberSpace(source[start])) start++;
        while (end > start && NumberSpace(source[end - 1])) end--;
        ReadOnlySpan<char> text = source.AsSpan(start, end - start);
        if (text.Length > 2 && text[0] == '0')
        {
            int width = text[1] switch { 'x' or 'X' => 4, 'o' or 'O' => 3, 'b' or 'B' => 1, _ => 0 };
            if (width != 0)
            {
                BigInteger integer = BigInteger.Zero;
                foreach (char ch in text[2..])
                {
                    int digit = char.IsAsciiDigit(ch) ? ch - '0' : char.ToLowerInvariant(ch) - 'a' + 10;
                    integer = (integer << width) + digit;
                }
                return integer;
            }
        }
        bool negative = text[0] == '-';
        if (text[0] is '-' or '+') text = text[1..];
        int split = text.IndexOfAny('e', 'E');
        int exponent = split < 0 ? 0 : int.Parse(text[(split + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        if (split >= 0) text = text[..split];
        int point = text.IndexOf('.');
        if (point >= 0) exponent -= text.Length - point - 1;
        string digits = text.ToString().Replace(".", "");
        return new LocaleDecimal(BigInteger.Parse(digits, CultureInfo.InvariantCulture), exponent, negative);
    }
}
