using System;
using System.Numerics;
using System.Text;

public static partial class Js
{
    sealed class NumberRadixFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Number method requires a receiver")), null, "toString", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            object primitive = receiver is JsBoxedPrimitive boxed ? boxed.Value : receiver;
            if (primitive is not double value) throw new InvalidOperationException("Number method has an incompatible receiver");
            object argument = FunctionArgument(arguments, 0);
            double converted = IsUndefined(argument) ? 10 : Math.Truncate(Number(argument));
            if (!(converted >= 2 && converted <= 36)) throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid radix");
            int radix = (int)converted;
            if (radix == 10 || !double.IsFinite(value) || value == 0) return StringValue(value);
            bool binary = (radix & (radix - 1)) == 0;
            double magnitude = Math.Abs(value), integral = Math.Truncate(magnitude), fraction = magnitude - integral;
            if (!binary && (fraction != 0 || magnitude > 9007199254740991d)) return RoundedNumberRadix(value,radix);
            const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
            var integer = new BigInteger(integral); var reversed = new StringBuilder();
            do { integer = BigInteger.DivRem(integer, radix, out var digit); reversed.Append(digits[(int)digit]); } while (!integer.IsZero);
            var result = new StringBuilder(reversed.Length + 16);
            if (value < 0) result.Append('-');
            for (int index = reversed.Length - 1; index >= 0; index--) result.Append(reversed[index]);
            if (fraction != 0)
            {
                result.Append('.');
                while (fraction != 0) { fraction *= radix; int digit = (int)fraction; result.Append(digits[digit]); fraction -= digit; }
            }
            return result.ToString();
        }
    }
}
