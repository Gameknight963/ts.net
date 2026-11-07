using System;
using System.Globalization;

public static partial class Js
{
    sealed class NumberLocaleFunction(bool bigint = false) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Number method requires a receiver")), null, "toLocaleString", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            object primitive = receiver is JsBoxedPrimitive boxed ? boxed.Value : receiver;
            if (bigint) return FormatLocaleNumber(RequireBigInt(receiver), true, arguments);
            if (primitive is not double value) throw new InvalidOperationException("Number method has an incompatible receiver");
            return FormatLocaleNumber(value, double.IsFinite(value), arguments);
        }
        static string FormatLocaleNumber<T>(T value, bool finite, object[] arguments) where T : struct, IFormattable
        {
            var culture = CaseCulture(FunctionArgument(arguments, 0));
            int minimum = 0, maximum = 3; bool grouping = true; string signDisplay = "auto";
            object optionValue = FunctionArgument(arguments, 1);
            if (!IsUndefined(optionValue))
            {
                var options = ToObject(optionValue);
                foreach (string key in new[] { "style", "currency", "notation", "minimumIntegerDigits", "minimumSignificantDigits", "maximumSignificantDigits", "roundingMode", "roundingPriority", "roundingIncrement", "trailingZeroDisplay", "compactDisplay", "unit", "numberingSystem", "localeMatcher" })
                    if (!IsUndefined(options.Get(key))) throw new NotSupportedException("Number locale formatting option " + key + " is not supported");
                int Digits(string key, int fallback)
                {
                    object setting = options.Get(key); if (IsUndefined(setting)) return fallback;
                    double count = Number(setting);
                    if (!double.IsFinite(count) || count < 0 || count > 100) throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid fraction digits");
                    return (int)Math.Floor(count);
                }
                object sign = options.Get("signDisplay");
                if (!IsUndefined(sign))
                {
                    signDisplay = StringValue(sign);
                    if (signDisplay is not ("auto" or "always" or "exceptZero" or "never" or "negative"))
                        throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid sign display");
                }
                minimum = Digits("minimumFractionDigits", 0);
                maximum = Digits("maximumFractionDigits", Math.Max(3, minimum));
                if (minimum > maximum) throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid fraction digits");
                object group = options.Get("useGrouping"); if (!IsUndefined(group)) grouping = Truthy(group);
            }
            string result = value.ToString((grouping ? "N" : "F") + maximum.ToString(CultureInfo.InvariantCulture), culture);
            if (signDisplay != "auto")
            {
                bool negative = value is double number ? !double.IsNaN(number) && double.IsNegative(number) : value is System.Numerics.BigInteger integer && integer.Sign < 0;
                // Sign selection uses the rounded value; exceptZero also leaves NaN unsigned.
                bool zero = value is double numeric && double.IsNaN(numeric);
                if (finite)
                {
                    zero = true;
                    foreach (char digit in result) if (digit >= '1' && digit <= '9') { zero = false; break; }
                }
                bool show = signDisplay == "always" || signDisplay == "exceptZero" && !zero || signDisplay == "negative" && negative && !zero;
                string minus = culture.NumberFormat.NegativeSign;
                if (negative && !show && result.StartsWith(minus, StringComparison.Ordinal)) result = result[minus.Length..];
                else if (!negative && show) result = culture.NumberFormat.PositiveSign + result;
            }
            if (!finite || maximum == 0) return result;
            int trim = 0;
            while (trim < maximum - minimum && result[result.Length - trim - 1] == '0') trim++;
            if (trim == maximum) trim += culture.NumberFormat.NumberDecimalSeparator.Length;
            return trim == 0 ? result : result[..^trim];
        }
    }
}
