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
        internal readonly int roundingIncrement;
        internal readonly int minimumSignificant, maximumSignificant;
        readonly int minimumGroupingWidth;
        internal readonly bool grouping;
        internal readonly object groupingOption;
        internal readonly string signDisplay;
        internal readonly string roundingMode, trailingZeroDisplay;
        internal readonly string style;
        internal readonly string currency, currencyDisplay, currencySign;
        readonly string positivePattern, negativePattern;
        readonly string format;
        internal NumberLocaleSettings(object locales, object optionValue)
        {
            culture = CaseCulture(locales);
            minimumSignificant = maximumSignificant = 0;
            roundingIncrement = 1; roundingMode = "halfExpand"; trailingZeroDisplay = "auto";
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
                foreach (string key in new[] { "compactDisplay", "unit", "numberingSystem" })
                    if (!IsUndefined(options.Get(key))) throw new NotSupportedException("Number locale formatting option " + key + " is not supported");
                foreach (var item in new[] { ("notation", "standard"), ("roundingPriority", "auto") })
                {
                    object setting = options.Get(item.Item1);
                    if (!IsUndefined(setting) && StringValue(setting) != item.Item2)
                        throw new NotSupportedException("Number locale formatting option " + item.Item1 + " is not supported");
                }
                object increment = options.Get("roundingIncrement");
                if (!IsUndefined(increment))
                {
                    double count = Number(increment);
                    if (!double.IsFinite(count) || count < 1 || count > 5000) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid rounding increment");
                    roundingIncrement = (int)Math.Floor(count);
                    if (roundingIncrement is not (1 or 2 or 5 or 10 or 20 or 25 or 50 or 100 or 200 or 250 or 500 or 1000 or 2000 or 2500 or 5000))
                        throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid rounding increment");
                }
                object requestedRounding = options.Get("roundingMode");
                if (!IsUndefined(requestedRounding)) roundingMode = StringValue(requestedRounding);
                if (roundingMode is not ("ceil" or "floor" or "expand" or "trunc" or "halfCeil" or "halfFloor" or "halfExpand" or "halfTrunc" or "halfEven"))
                    throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid rounding mode");
                object requestedZeros = options.Get("trailingZeroDisplay");
                if (!IsUndefined(requestedZeros)) trailingZeroDisplay = StringValue(requestedZeros);
                if (trailingZeroDisplay is not ("auto" or "stripIfInteger"))
                    throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid trailing zero display");
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
                object minimumSignificantSetting = options.Get("minimumSignificantDigits"), maximumSignificantSetting = options.Get("maximumSignificantDigits");
                if (!IsUndefined(minimumSignificantSetting) || !IsUndefined(maximumSignificantSetting))
                {
                    minimumSignificant = DigitValue(minimumSignificantSetting, 1, 1, 21);
                    maximumSignificant = DigitValue(maximumSignificantSetting, 21, 1, 21);
                    if (minimumSignificant > maximumSignificant) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid significant digit count");
                    if (roundingIncrement != 1) throw new InvalidOperationException("Significant digits cannot be combined with rounding increments");
                }
                else
                {
                    minimum = DigitValue(minimumSetting, style == "currency" ? currencyDigits : 0);
                    maximum = DigitValue(maximumSetting, Math.Max(style == "currency" ? currencyDigits : style == "percent" ? 0 : 3, minimum));
                    if (minimum > maximum && IsUndefined(minimumSetting)) minimum = maximum;
                    if (minimum > maximum) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid digit count");
                    if (roundingIncrement != 1 && minimum != maximum)
                        throw new ArgumentOutOfRangeException(nameof(optionValue), "Rounding increments require equal minimum and maximum fraction digits");
                }
                object group = options.Get("useGrouping");
                if (!IsUndefined(group))
                {
                    grouping = Truthy(group);
                    groupingOption = !grouping ? BoxBoolean(false) : group is string text ? text switch
                    {
                        "auto" or "always" => text,
                        "true" or "false" => "auto",
                        "min2" => text,
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
            minimumGroupingWidth = int.MaxValue;
            if (groupingOption is "min2")
            {
                int[] sizes = culture.NumberFormat.NumberGroupSizes;
                if (sizes.Length > 0 && sizes[0] > 0) minimumGroupingWidth = sizes[0] + 2;
            }
            format = (grouping ? "N" : "F") + maximum.ToString(CultureInfo.InvariantCulture);
        }
        bool RoundUp(BigInteger quotient, BigInteger remainder, BigInteger denominator, bool negative)
        {
            if (remainder.IsZero) return false;
            switch (roundingMode)
            {
                case "ceil": return !negative;
                case "floor": return negative;
                case "expand": return true;
                case "trunc": return false;
            }
            int half = (remainder * 2).CompareTo(denominator);
            if (half != 0) return half > 0;
            return roundingMode switch
            {
                "halfCeil" => !negative,
                "halfFloor" => negative,
                "halfTrunc" => false,
                "halfEven" => !quotient.IsEven,
                _ => true
            };
        }
        string FiniteNumber(double value, out int fractionMinimum, out int fractionMaximum)
        {
            fractionMinimum = minimum; fractionMaximum = maximum;
            // Safe integers need no decimal rounding and retain native formatting.
            if (maximumSignificant == 0 && groupingOption is not "min2" && roundingIncrement == 1 && style == "decimal" && double.IsInteger(value) && Math.Abs(value) <= 9007199254740991d) return value.ToString(format, culture);
            string raw = Math.Abs(value).ToString("R", CultureInfo.InvariantCulture);
            int split = raw.IndexOf('E');
            int exponent = split < 0 ? 0 : int.Parse(raw.AsSpan(split + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            string coefficient = split < 0 ? raw : raw[..split];
            int point = coefficient.IndexOf('.');
            int scale = point < 0 ? 0 : coefficient.Length - point - 1;
            BigInteger digits = BigInteger.Parse(coefficient.Replace(".", ""), CultureInfo.InvariantCulture);
            if (maximumSignificant != 0)
                return SignificantText(digits, exponent - scale + (style == "percent" ? 2 : 0), double.IsNegative(value), out fractionMinimum, out fractionMaximum);
            int shift = exponent - scale + maximum + (style == "percent" ? 2 : 0);
            digits = Quantize(digits, shift, double.IsNegative(value));
            return QuantizedText(digits, double.IsNegative(value));
        }
        string SignificantText(BigInteger digits, int exponent, bool negative, out int fractionMinimum, out int fractionMaximum)
        {
            int width = digits.IsZero ? 1 : digits.ToString(CultureInfo.InvariantCulture).Length;
            int places = digits.IsZero ? maximumSignificant - 1 : maximumSignificant - width - exponent;
            BigInteger rounded = Quantize(digits, exponent + places, negative);
            int magnitude = rounded.IsZero ? 0 : rounded.ToString(CultureInfo.InvariantCulture).Length - places - 1;
            fractionMinimum = Math.Max(0, minimumSignificant - magnitude - 1);
            fractionMaximum = Math.Max(0, places);
            if (places < 0) rounded *= BigInteger.Pow(10, -places);
            return QuantizedText(rounded, negative, fractionMaximum);
        }
        BigInteger Quantize(BigInteger digits, int shift, bool negative)
        {
            BigInteger denominator = roundingIncrement;
            if (shift >= 0) digits *= BigInteger.Pow(10, shift);
            else denominator *= BigInteger.Pow(10, -shift);
            if (denominator.IsOne) return digits;
            BigInteger quotient = BigInteger.DivRem(digits, denominator, out var remainder);
            if (RoundUp(quotient, remainder, denominator, negative)) quotient++;
            return quotient * roundingIncrement;
        }
        bool UseMinimumGrouping(int width)
        {
            return Math.Max(width, integerDigits) >= minimumGroupingWidth;
        }
        string QuantizedText(BigInteger digits, bool negative) => QuantizedText(digits, negative, maximum);
        string QuantizedText(BigInteger digits, bool negative, int fractionDigits)
        {
            BigInteger whole = BigInteger.DivRem(digits, BigInteger.Pow(10, fractionDigits), out var fraction);
            bool useGroups = grouping;
            if (groupingOption is "min2")
                useGroups = UseMinimumGrouping(whole.ToString(CultureInfo.InvariantCulture).Length);
            string result = whole.ToString(useGroups ? "N0" : "F0", culture);
            if (fractionDigits != 0) result += culture.NumberFormat.NumberDecimalSeparator + fraction.ToString(CultureInfo.InvariantCulture).PadLeft(fractionDigits, '0');
            return negative ? culture.NumberFormat.NegativeSign + result : result;
        }
        internal string Format<T>(T value, bool finite) where T : struct, IFormattable
        {
            int fractionMinimum = minimum, fractionMaximum = maximum;
            string result = finite && value is double numberValue ? FiniteNumber(numberValue, out fractionMinimum, out fractionMaximum)
                : maximumSignificant != 0 && value is BigInteger significantInteger
                    ? SignificantText(BigInteger.Abs(significantInteger), style == "percent" ? 2 : 0, significantInteger.Sign < 0, out fractionMinimum, out fractionMaximum)
                : (roundingIncrement != 1 || groupingOption is "min2") && value is BigInteger quantizedInteger
                    ? QuantizedText(Quantize(BigInteger.Abs(quantizedInteger), maximum + (style == "percent" ? 2 : 0), quantizedInteger.Sign < 0), quantizedInteger.Sign < 0)
                : style == "percent" && value is BigInteger integerValue ? (integerValue * 100).ToString(format, culture) : value.ToString(format, culture);
            if (finite && integerDigits > 1)
            {
                bool useGroups = grouping;
                if (groupingOption is "min2")
                {
                    int point = result.IndexOf(culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
                    int width = 0;
                    for (int i = 0; i < (point < 0 ? result.Length : point); i++) if (char.IsAsciiDigit(result[i])) width++;
                    useGroups = UseMinimumGrouping(width);
                }
                result = PadLocaleInteger(result, integerDigits, useGroups, culture.NumberFormat);
            }
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
            if (finite && fractionMaximum > 0)
            {
                int trim = 0;
                while (trim < fractionMaximum - fractionMinimum && result[result.Length - trim - 1] == '0') trim++;
                if (trim == fractionMaximum) trim += culture.NumberFormat.NumberDecimalSeparator.Length;
                if (trim != 0) result = result[..^trim];
            }
            if (finite && trailingZeroDisplay == "stripIfInteger")
            {
                string separator = culture.NumberFormat.NumberDecimalSeparator;
                int point = result.LastIndexOf(separator, StringComparison.Ordinal);
                if (point >= 0)
                {
                    bool integer = true;
                    for (int i = point + separator.Length; i < result.Length; i++) if (result[i] != '0') { integer = false; break; }
                    if (integer) result = result[..point];
                }
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
