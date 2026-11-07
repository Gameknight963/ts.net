using System;
using System.Globalization;
using System.Numerics;

public static partial class Js
{
    static void NumberFraction(double value, out BigInteger numerator, out BigInteger denominator)
    {
        ulong bits = BitConverter.DoubleToUInt64Bits(value);
        int exponent = (int)((bits >> 52) & 0x7ff);
        ulong significand = bits & 0x000fffffffffffffUL;
        if (exponent != 0) significand |= 0x0010000000000000UL;
        int shift = exponent == 0 ? -1074 : exponent - 1075;
        numerator = significand; denominator = BigInteger.One;
        if (shift >= 0) numerator <<= shift; else denominator <<= -shift;
    }
    static BigInteger RoundDecimal(double value, int scale)
    {
        NumberFraction(value, out var numerator, out var denominator);
        if (scale >= 0) numerator *= BigInteger.Pow(10, scale);
        else denominator *= BigInteger.Pow(10, -scale);
        BigInteger rounded = BigInteger.DivRem(numerator, denominator, out var remainder);
        if (remainder * 2 >= denominator) rounded++;
        return rounded;
    }
    static int CompareDecimalPower(BigInteger numerator, BigInteger denominator, int power) => power >= 0
        ? numerator.CompareTo(denominator * BigInteger.Pow(10, power))
        : (numerator * BigInteger.Pow(10, -power)).CompareTo(denominator);
    static int DecimalExponent(double value)
    {
        int exponent = (int)Math.Floor(Math.Log10(value));
        NumberFraction(value, out var numerator, out var denominator);
        // Log10 can round across a power-of-ten boundary. Check the exact ratio.
        while (CompareDecimalPower(numerator, denominator, exponent) < 0) exponent--;
        while (CompareDecimalPower(numerator, denominator, exponent + 1) >= 0) exponent++;
        return exponent;
    }
    static string SignificantDigits(double value, int precision, out int exponent)
    {
        if (value == 0) { exponent = 0; return new string('0', precision); }
        exponent = DecimalExponent(value);
        BigInteger rounded = RoundDecimal(value, precision - 1 - exponent);
        if (rounded >= BigInteger.Pow(10, precision)) { rounded /= 10; exponent++; }
        return rounded.ToString(CultureInfo.InvariantCulture).PadLeft(precision, '0');
    }
    static string ExponentialText(string digits, int exponent) =>
        (digits.Length == 1 ? digits : digits.Insert(1, ".")) + "e" + (exponent >= 0 ? "+" : "") + exponent.ToString(CultureInfo.InvariantCulture);
    static string ShortestExponential(double value)
    {
        if (value == 0) return "0e+0";
        string text = StringValue(value);
        int split = text.IndexOf('e'); if (split < 0) split = text.IndexOf('E');
        int power = split < 0 ? 0 : int.Parse(text.AsSpan(split + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        string coefficient = split < 0 ? text : text[..split];
        int dot = coefficient.IndexOf('.'), before = dot < 0 ? coefficient.Length : dot;
        string digits = coefficient.Replace(".", ""); int leading = 0;
        while (digits[leading] == '0') leading++;
        return ExponentialText(digits[leading..].TrimEnd('0'), before - leading - 1 + power);
    }
    sealed class NumberPrecisionFunction(bool precision) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Number method requires a receiver")), null, precision ? "toPrecision" : "toExponential", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            object primitive = receiver is JsBoxedPrimitive boxed ? boxed.Value : receiver;
            if (primitive is not double value) throw new InvalidOperationException("Number method has an incompatible receiver");
            object argument = FunctionArgument(arguments, 0);
            if (precision && IsUndefined(argument)) return StringValue(value);
            double count = Number(argument); count = double.IsNaN(count) ? 0 : Math.Truncate(count);
            if (!double.IsFinite(value)) return StringValue(value);
            if (count < (precision ? 1 : 0) || count > 100) throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid precision");
            double magnitude = Math.Abs(value); string result;
            if (!precision && IsUndefined(argument)) result = ShortestExponential(magnitude);
            else
            {
                int length = (int)count + (precision ? 0 : 1);
                string digits = SignificantDigits(magnitude, length, out int exponent);
                if (!precision || exponent < -6 || exponent >= length) result = ExponentialText(digits, exponent);
                else
                {
                    int point = exponent + 1;
                    result = point <= 0 ? "0." + new string('0', -point) + digits
                        : point >= length ? digits + new string('0', point - length) : digits.Insert(point, ".");
                }
            }
            return value < 0 ? "-" + result : result;
        }
    }
    sealed class NumberFixedFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Number method requires a receiver")), null, "toFixed", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            object primitive = receiver is JsBoxedPrimitive boxed ? boxed.Value : receiver;
            if (primitive is not double value) throw new InvalidOperationException("Number method has an incompatible receiver");
            double count = Number(FunctionArgument(arguments, 0));
            count = double.IsNaN(count) ? 0 : Math.Truncate(count);
            if (count < 0 || count > 100) throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid fraction digits");
            if (!double.IsFinite(value) || Math.Abs(value) >= 1e21) return StringValue(value);
            int digits = (int)count;
            string integral = RoundDecimal(Math.Abs(value), digits).ToString(CultureInfo.InvariantCulture);
            string result = digits == 0 ? integral : integral.Length <= digits
                ? "0." + new string('0', digits - integral.Length) + integral
                : integral.Insert(integral.Length - digits, ".");
            return value < 0 ? "-" + result : result;
        }
    }
}
