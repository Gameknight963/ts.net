using System;
using System.Globalization;
using System.Numerics;

public static partial class Js
{
    static BigInteger FixedInteger(double value, int digits)
    {
        ulong bits = BitConverter.DoubleToUInt64Bits(value);
        int exponent = (int)((bits >> 52) & 0x7ff);
        ulong significand = bits & 0x000fffffffffffffUL;
        if (exponent != 0) significand |= 0x0010000000000000UL;
        int shift = exponent == 0 ? -1074 : exponent - 1075;
        BigInteger numerator = (BigInteger)significand * BigInteger.Pow(10, digits);
        if (shift >= 0) return numerator << shift;
        BigInteger denominator = BigInteger.One << -shift;
        BigInteger rounded = BigInteger.DivRem(numerator, denominator, out var remainder);
        // JavaScript chooses the larger integer at an exact decimal tie.
        if (remainder * 2 >= denominator) rounded++;
        return rounded;
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
            string integral = FixedInteger(Math.Abs(value), digits).ToString(CultureInfo.InvariantCulture);
            string result = digits == 0 ? integral : integral.Length <= digits
                ? "0." + new string('0', digits - integral.Length) + integral
                : integral.Insert(integral.Length - digits, ".");
            return value < 0 ? "-" + result : result;
        }
    }
}
