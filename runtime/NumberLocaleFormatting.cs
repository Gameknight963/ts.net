using System;
using System.Globalization;

public static partial class Js
{
    static string PadLocaleInteger(string text, int minimum, bool grouping, NumberFormatInfo format)
    {
        int start = 0;
        while (start < text.Length && !char.IsAsciiDigit(text[start])) start++;
        int end = text.IndexOf(format.NumberDecimalSeparator, start, StringComparison.Ordinal);
        if (end < 0)
        {
            end = text.Length;
            while (end > start && !char.IsAsciiDigit(text[end - 1])) end--;
        }
        string digits = text[start..end];
        if (grouping && format.NumberGroupSeparator.Length != 0) digits = digits.Replace(format.NumberGroupSeparator, "", StringComparison.Ordinal);
        if (digits.Length >= minimum) return text;
        digits = digits.PadLeft(minimum, '0');
        if (grouping)
        {
            int[] sizes = format.NumberGroupSizes;
            int position = digits.Length, index = 0;
            while (sizes.Length != 0 && sizes[index] != 0 && position > sizes[index])
            {
                position -= sizes[index];
                digits = digits.Insert(position, format.NumberGroupSeparator);
                if (index + 1 < sizes.Length) index++;
            }
        }
        return text[..start] + digits + text[end..];
    }
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
            int minimum = 0, maximum = 3, integerDigits = 1; bool grouping = true; string signDisplay = "auto";
            object optionValue = FunctionArgument(arguments, 1);
            if (!IsUndefined(optionValue))
            {
                var options = ToObject(optionValue);
                foreach (string key in new[] { "style", "currency", "notation", "minimumSignificantDigits", "maximumSignificantDigits", "roundingMode", "roundingPriority", "roundingIncrement", "trailingZeroDisplay", "compactDisplay", "unit", "numberingSystem", "localeMatcher" })
                    if (!IsUndefined(options.Get(key))) throw new NotSupportedException("Number locale formatting option " + key + " is not supported");
                int Digits(string key, int fallback, int lower = 0, int upper = 100)
                {
                    object setting = options.Get(key); if (IsUndefined(setting)) return fallback;
                    double count = Number(setting);
                    if (!double.IsFinite(count) || count < lower || count > upper) throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid digit count");
                    return (int)Math.Floor(count);
                }
                object sign = options.Get("signDisplay");
                if (!IsUndefined(sign))
                {
                    signDisplay = StringValue(sign);
                    if (signDisplay is not ("auto" or "always" or "exceptZero" or "never" or "negative"))
                        throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid sign display");
                }
                integerDigits = Digits("minimumIntegerDigits", 1, 1, 21);
                minimum = Digits("minimumFractionDigits", 0);
                maximum = Digits("maximumFractionDigits", Math.Max(3, minimum));
                if (minimum > maximum) throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid digit count");
                object group = options.Get("useGrouping"); if (!IsUndefined(group)) grouping = Truthy(group);
            }
            string result = value.ToString((grouping ? "N" : "F") + maximum.ToString(CultureInfo.InvariantCulture), culture);
            if (finite && integerDigits > 1) result = PadLocaleInteger(result, integerDigits, grouping, culture.NumberFormat);
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
