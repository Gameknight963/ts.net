using System;
using System.Globalization;
using System.Numerics;

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
        static string FormatLocaleNumber<T>(T value, bool finite, object[] arguments) where T : struct, IFormattable =>
            new NumberLocaleSettings(FunctionArgument(arguments, 0), FunctionArgument(arguments, 1)).Format(value, finite);
    }
    readonly struct NumberLocaleSettings
    {
        internal readonly CultureInfo culture;
        internal readonly int minimum, maximum, integerDigits;
        internal readonly bool grouping;
        internal readonly object groupingOption;
        internal readonly string signDisplay;
        internal readonly string style;
        internal readonly string currency, currencyDisplay, currencySign;
        readonly string positivePattern, negativePattern;
        readonly string format;
        internal NumberLocaleSettings(object locales, object optionValue)
        {
            culture = CaseCulture(locales);
            style = "decimal"; positivePattern = negativePattern = null;
            currency = null; currencyDisplay = "symbol"; currencySign = "standard";
            int currencyDigits = 2;
            minimum = 0; maximum = 3; integerDigits = 1; grouping = true; groupingOption = "auto"; signDisplay = "auto";

            if (!IsUndefined(optionValue))
            {
                var options = ToObject(optionValue);
                object requestedStyle = options.Get("style");
                if (!IsUndefined(requestedStyle)) style = StringValue(requestedStyle);
                if (style == "unit") throw new NotSupportedException("Number locale formatting option style is not supported");
                if (style is not ("decimal" or "percent" or "currency")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid number formatting style");
                object requestedCurrency = options.Get("currency");
                if (!IsUndefined(requestedCurrency))
                {
                    currency = StringValue(requestedCurrency);
                    if (currency.Length != 3 || !char.IsAsciiLetter(currency[0]) || !char.IsAsciiLetter(currency[1]) || !char.IsAsciiLetter(currency[2]))
                        throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid currency code");
                    currency = currency.ToUpperInvariant();
                }
                string CurrencyOption(string key, string fallback)
                {
                    object setting = options.Get(key); return IsUndefined(setting) ? fallback : StringValue(setting);
                }
                currencyDisplay = CurrencyOption("currencyDisplay", "symbol");
                currencySign = CurrencyOption("currencySign", "standard");
                if (currencyDisplay is not ("symbol" or "narrowSymbol" or "code" or "name")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid currency display");
                if (currencySign is not ("standard" or "accounting")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid currency sign");
                if (style == "currency")
                {
                    if (currency == null) throw new InvalidOperationException("Currency code is required");
                    if (currencyDisplay == "name") throw new NotSupportedException("Currency display names are not supported");
                    if (currencySign == "accounting") throw new NotSupportedException("Accounting currency patterns are not supported");
                    var metadata = CurrencyFor(currency, culture, currencyDisplay);
                    currencyDigits = metadata.Digits;
                    culture = (CultureInfo)culture.Clone();
                    var info = culture.NumberFormat;
                    info.CurrencySymbol = metadata.Symbol;
                    info.NumberDecimalSeparator = info.CurrencyDecimalSeparator;
                    info.NumberGroupSeparator = info.CurrencyGroupSeparator;
                    info.NumberGroupSizes = info.CurrencyGroupSizes;
                    info.NumberNegativePattern = 1;
                    positivePattern = SpaceCurrencyTemplate(1d.ToString("C0", culture));
                    negativePattern = SpaceCurrencyTemplate((-1d).ToString("C0", culture));
                }
                foreach (string key in new[] { "minimumSignificantDigits", "maximumSignificantDigits", "compactDisplay", "unit", "numberingSystem" })
                    if (!IsUndefined(options.Get(key))) throw new NotSupportedException("Number locale formatting option " + key + " is not supported");
                foreach (var item in new[] { ("notation", "standard"), ("roundingMode", "halfExpand"), ("roundingPriority", "auto"), ("roundingIncrement", "1"), ("trailingZeroDisplay", "auto") })
                {
                    object setting = options.Get(item.Item1);
                    if (!IsUndefined(setting) && StringValue(setting) != item.Item2)
                        throw new NotSupportedException("Number locale formatting option " + item.Item1 + " is not supported");
                }
                object matcher = options.Get("localeMatcher");
                if (!IsUndefined(matcher) && StringValue(matcher) is not ("lookup" or "best fit"))
                    throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale matcher");
                int Digits(string key, int fallback, int lower = 0, int upper = 100)
                {
                    return DigitValue(options.Get(key), fallback, lower, upper);
                }
                int DigitValue(object setting, int fallback, int lower = 0, int upper = 100)
                {
                    if (IsUndefined(setting)) return fallback;
                    double count = Number(setting);
                    if (!double.IsFinite(count) || count < lower || count > upper) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid digit count");
                    return (int)Math.Floor(count);
                }
                object sign = options.Get("signDisplay");
                if (!IsUndefined(sign))
                {
                    signDisplay = StringValue(sign);
                    if (signDisplay is not ("auto" or "always" or "exceptZero" or "never" or "negative"))
                        throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid sign display");
                }
                integerDigits = Digits("minimumIntegerDigits", 1, 1, 21);
                object minimumSetting = options.Get("minimumFractionDigits"), maximumSetting = options.Get("maximumFractionDigits");
                minimum = DigitValue(minimumSetting, style == "currency" ? currencyDigits : 0);
                maximum = DigitValue(maximumSetting, Math.Max(style == "currency" ? currencyDigits : style == "percent" ? 0 : 3, minimum));
                if (minimum > maximum && IsUndefined(minimumSetting)) minimum = maximum;
                if (minimum > maximum) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid digit count");
                object group = options.Get("useGrouping");
                if (!IsUndefined(group))
                {
                    grouping = Truthy(group);
                    groupingOption = !grouping ? BoxBoolean(false) : group is string text ? text switch
                    {
                        "auto" or "always" => text,
                        "true" or "false" => "auto",
                        "min2" => throw new NotSupportedException("Minimum grouping threshold is not supported"),
                        _ => throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid grouping option")
                    } : "always";
                }
            }
            if (style == "percent")
            {
                culture = (CultureInfo)culture.Clone();
                var info = culture.NumberFormat;
                info.NumberDecimalSeparator = info.PercentDecimalSeparator;
                info.NumberGroupSeparator = info.PercentGroupSeparator;
                info.NumberGroupSizes = info.PercentGroupSizes;
                positivePattern = (0.01d).ToString("P0", culture);
                negativePattern = (-0.01d).ToString("P0", culture);
            }
            format = (grouping ? "N" : "F") + maximum.ToString(CultureInfo.InvariantCulture);
        }
        string FiniteNumber(double value)
        {
            // Safe integers need no decimal rounding and retain native formatting.
            if (style == "decimal" && double.IsInteger(value) && Math.Abs(value) <= 9007199254740991d) return value.ToString(format, culture);
            string raw = Math.Abs(value).ToString("R", CultureInfo.InvariantCulture);
            int split = raw.IndexOf('E');
            int exponent = split < 0 ? 0 : int.Parse(raw.AsSpan(split + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            string coefficient = split < 0 ? raw : raw[..split];
            int point = coefficient.IndexOf('.');
            int scale = point < 0 ? 0 : coefficient.Length - point - 1;
            BigInteger digits = BigInteger.Parse(coefficient.Replace(".", ""), CultureInfo.InvariantCulture);
            int shift = exponent - scale + maximum + (style == "percent" ? 2 : 0);
            if (shift >= 0) digits *= BigInteger.Pow(10, shift);
            else
            {
                BigInteger denominator = BigInteger.Pow(10, -shift);
                digits = BigInteger.DivRem(digits, denominator, out var remainder);
                if (remainder * 2 >= denominator) digits++;
            }
            BigInteger whole = BigInteger.DivRem(digits, BigInteger.Pow(10, maximum), out var fraction);
            string result = whole.ToString(grouping ? "N0" : "F0", culture);
            if (maximum != 0) result += culture.NumberFormat.NumberDecimalSeparator + fraction.ToString(CultureInfo.InvariantCulture).PadLeft(maximum, '0');
            return double.IsNegative(value) ? culture.NumberFormat.NegativeSign + result : result;
        }
        internal string Format<T>(T value, bool finite) where T : struct, IFormattable
        {
            string result = finite && value is double numberValue ? FiniteNumber(numberValue)
                : style == "percent" && value is BigInteger integerValue ? (integerValue * 100).ToString(format, culture) : value.ToString(format, culture);
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
            if (finite && maximum > 0)
            {
                int trim = 0;
                while (trim < maximum - minimum && result[result.Length - trim - 1] == '0') trim++;
                if (trim == maximum) trim += culture.NumberFormat.NumberDecimalSeparator.Length;
                if (trim != 0) result = result[..^trim];
            }
            if (style == "decimal") return result;
            var info = culture.NumberFormat;
            bool negativeSign = result.StartsWith(info.NegativeSign, StringComparison.Ordinal);
            bool positiveSign = !negativeSign && result.StartsWith(info.PositiveSign, StringComparison.Ordinal);
            string template = negativeSign || positiveSign ? negativePattern : positivePattern;
            if (negativeSign) result = result[info.NegativeSign.Length..];
            if (positiveSign)
            {
                result = result[info.PositiveSign.Length..];
                template = template.Contains(info.NegativeSign, StringComparison.Ordinal)
                    ? template.Replace(info.NegativeSign, info.PositiveSign, StringComparison.Ordinal) : info.PositiveSign + positivePattern;
            }
            int placeholder = template.IndexOf('1');
            return template[..placeholder] + result + template[(placeholder + 1)..];
        }
    }
}
