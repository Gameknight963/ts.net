using System.Collections.Generic;
using System;
using System.Globalization;
using System.Numerics;

public static partial class Js
{
    static bool SanctionedNumberUnit(string unit) => Array.BinarySearch(IntlValueLists.Units, unit, StringComparer.Ordinal) >= 0;
    static bool WellFormedNumberUnit(string unit)
    {
        if (SanctionedNumberUnit(unit)) return true;
        int separator = unit.IndexOf("-per-", StringComparison.Ordinal);
        return separator >= 0 && SanctionedNumberUnit(unit[..separator]) && SanctionedNumberUnit(unit[(separator + 5)..]);
    }

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
        readonly bool minimumGrouping;
        internal readonly bool grouping;
        internal readonly object groupingOption;
        internal readonly string signDisplay;
        internal readonly string roundingMode, trailingZeroDisplay, roundingPriority, notation, compactDisplay;
        internal readonly string style;
        internal readonly string unit, unitDisplay;
        internal readonly string currency, currencyDisplay, currencySign;
        readonly string positivePattern, negativePattern, compactNegativePattern, compactPositivePattern;
        readonly string format, ungroupedFormat;
        readonly double minimumGroupingThreshold;
        internal readonly string numberingSystem, localeName;
        internal readonly string exponentSeparator;
        readonly NativeCompactNumbers nativeCompact;
        readonly NativeCurrencyNames nativeCurrencyNames;
        readonly NativeUnitFormatting nativeUnit;
        internal bool UsesNativeUnit=>nativeUnit!=null;
        internal bool UsesNativeCompact=>nativeCompact!=null;
        readonly string digitCharacters;
        internal NumberLocaleSettings(object locales, object optionValue)
        {
            var requestedLocales = (JsArray)CollatorSupportedLocales(locales, Undefined);
            LocaleValue selectedLocale = IndexedLength(requestedLocales) == 0 ? null : new LocaleValue(IndexedValue(requestedLocales, 0), Undefined);
            culture = selectedLocale == null ? CultureInfo.CurrentCulture : LocaleCulture(CollatorLocale(selectedLocale.BaseName));
            // Windows culture data can retain the older French grouping space.
            if (culture.TwoLetterISOLanguageName == "fr" && culture.NumberFormat.NumberGroupSeparator == "\u00a0") {
                culture = (CultureInfo)culture.Clone();
                culture.NumberFormat.NumberGroupSeparator = "\u202f";
                if (culture.NumberFormat.CurrencyGroupSeparator == "\u00a0") culture.NumberFormat.CurrencyGroupSeparator = "\u202f";
                if (culture.NumberFormat.PercentGroupSeparator == "\u00a0") culture.NumberFormat.PercentGroupSeparator = "\u202f";
            }
            var localeFacts=new LocaleValue(selectedLocale?.BaseName ?? (culture.Name.Length==0 ? "en-US" : culture.Name),Undefined);
            string preferredNumbers=LocaleDefaultNumberingSystem(localeFacts);
            numberingSystem = ResolveNumberingSystem(selectedLocale?.Keyword("nu") as string ?? preferredNumbers,preferredNumbers);
            localeName = culture.Name + (selectedLocale?.Keyword("nu") as string == numberingSystem ? "-u-nu-" + numberingSystem : "");
            exponentSeparator="E";notation = "standard"; roundingPriority = "auto";
            minimumSignificant = maximumSignificant = 0;
            roundingIncrement = 1; roundingMode = "halfExpand"; trailingZeroDisplay = "auto";
            style = "decimal"; positivePattern = negativePattern = null;
            currency = null; currencyDisplay = "symbol"; currencySign = "standard";
            int currencyDigits = 2;
            minimum = 0; maximum = 3; integerDigits = 1; grouping = true; groupingOption = "auto"; signDisplay = "auto";

            if (!IsUndefined(optionValue))
            {
                var options = ToObject(optionValue);
                object requestedMatcher = options.Get("localeMatcher");
                if (!IsUndefined(requestedMatcher) && StringValue(requestedMatcher) is not ("lookup" or "best fit"))
                    throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale matcher");
                object requestedNumberingSystem = options.Get("numberingSystem");
                if (!IsUndefined(requestedNumberingSystem))
                {
                    string system = StringValue(requestedNumberingSystem);
                    ValidateNumberingSystem(system);
                    numberingSystem = ResolveNumberingSystem(system,preferredNumbers);
                    if (selectedLocale?.Keyword("nu") as string != system) localeName = culture.Name;
                }
                culture=NativeNumberSymbols.Configure(culture,numberingSystem,out exponentSeparator);
                object requestedStyle = options.Get("style");
                if (!IsUndefined(requestedStyle)) style = StringValue(requestedStyle);
                if (style is not ("decimal" or "percent" or "currency" or "unit")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid number formatting style");
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
                object requestedUnit = options.Get("unit");
                unit = IsUndefined(requestedUnit) ? null : StringValue(requestedUnit);
                if (style == "unit" && unit == null) throw new InvalidOperationException("Unit is required");
                if (unit != null && !WellFormedNumberUnit(unit))
                    throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid number formatting unit");
                unitDisplay = CurrencyOption("unitDisplay", "short");
                if (unitDisplay is not ("short" or "narrow" or "long")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid unit display");
                if (style == "currency")
                {
                    if (currency == null) throw new InvalidOperationException("Currency code is required");
                    if (currencyDisplay == "name" && culture.Name is not ("en" or "en-US")) nativeCurrencyNames=new NativeCurrencyNames(culture.Name,currency);
                    var metadata = CurrencyFor(currency, culture, currencyDisplay);
                    currencyDigits = metadata.Digits;
                    culture = (CultureInfo)culture.Clone();
                    var info = culture.NumberFormat;
                    info.CurrencySymbol = metadata.Symbol;
                    if(NativeNumberSymbols.Patterns(culture,numberingSystem)) {
                        if(currencyDisplay is "symbol" or "narrowSymbol")info.CurrencySymbol=NativeNumberSymbols.CurrencySymbol(culture.Name,currency,currencyDisplay=="narrowSymbol") ?? info.CurrencySymbol;
                        info.CurrencyGroupSizes=NativeNumberSymbols.Groups(culture.Name,numberingSystem,2) ?? info.CurrencyGroupSizes;
                    }
                    // Keep native symbol placement and spacing inside parentheses.
                    if (currencySign == "accounting") info.CurrencyNegativePattern = info.CurrencyPositivePattern switch {
                        0 => 0, 1 => 4, 2 => 14, _ => 15
                    };
                    if(currencyDisplay!="name") {
                        info.NumberDecimalSeparator = info.CurrencyDecimalSeparator;
                        info.NumberGroupSeparator = info.CurrencyGroupSeparator;
                        info.NumberGroupSizes = info.CurrencyGroupSizes;
                    }
                    info.NumberNegativePattern = 1;
                    positivePattern = SpaceCurrencyTemplate(1d.ToString("C0", culture));
                    negativePattern = SpaceCurrencyTemplate((-1d).ToString("C0", culture));
                    if(NativeNumberSymbols.Patterns(culture,numberingSystem)) {
                        positivePattern=NativeCompactNumbers.Pattern(culture.Name,numberingSystem,info.CurrencySymbol,false,currencySign=="accounting" ? 12 : 2);
                        negativePattern=NativeCompactNumbers.Pattern(culture.Name,numberingSystem,info.CurrencySymbol,true,currencySign=="accounting" ? 12 : 2);
                        if(culture.TwoLetterISOLanguageName=="ar") {
                            var patterns=ArabicCurrencyPatterns(info.CurrencySymbol,numberingSystem,info.NegativeSign,currencySign=="accounting",false);positivePattern=patterns.Positive;negativePattern=patterns.Negative;
                        }
                    }
                }
                object requestedNotation = options.Get("notation");
                if (!IsUndefined(requestedNotation)) notation = StringValue(requestedNotation);
                if (notation is not ("standard" or "scientific" or "engineering" or "compact")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid number notation");
                integerDigits = Digits("minimumIntegerDigits", 1, 1, 21);
                object minimumSetting = options.Get("minimumFractionDigits"), maximumSetting = options.Get("maximumFractionDigits");
                object minimumSignificantSetting = options.Get("minimumSignificantDigits"), maximumSignificantSetting = options.Get("maximumSignificantDigits");
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
                object priority = options.Get("roundingPriority");
                if (!IsUndefined(priority)) roundingPriority = StringValue(priority);
                if (roundingPriority is not ("auto" or "morePrecision" or "lessPrecision"))
                    throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid rounding priority");
                object requestedZeros = options.Get("trailingZeroDisplay");
                if (!IsUndefined(requestedZeros)) trailingZeroDisplay = StringValue(requestedZeros);
                if (trailingZeroDisplay is not ("auto" or "stripIfInteger"))
                    throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid trailing zero display");
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
                if (roundingPriority != "auto" || !IsUndefined(minimumSignificantSetting) || !IsUndefined(maximumSignificantSetting))
                {
                    minimumSignificant = DigitValue(minimumSignificantSetting, 1, 1, 21);
                    maximumSignificant = DigitValue(maximumSignificantSetting, 21, 1, 21);
                    if (minimumSignificant > maximumSignificant) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid significant digit count");
                    if (roundingIncrement != 1) throw new InvalidOperationException("Significant digits cannot be combined with rounding increments");
                }
                if (maximumSignificant == 0 || roundingPriority != "auto")
                {
                    minimum = DigitValue(minimumSetting, style == "currency" && notation == "standard" ? currencyDigits : 0);
                    maximum = DigitValue(maximumSetting, Math.Max(style == "currency" && notation == "standard" ? currencyDigits : style == "percent" ? 0 : 3, minimum));
                    if (minimum > maximum && IsUndefined(minimumSetting)) minimum = maximum;
                    if (minimum > maximum) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid digit count");
                    if (roundingIncrement != 1 && minimum != maximum)
                        throw new ArgumentOutOfRangeException(nameof(optionValue), "Rounding increments require equal minimum and maximum fraction digits");
                }
                if (notation == "compact" && roundingPriority == "auto" && IsUndefined(minimumSetting) && IsUndefined(maximumSetting) && IsUndefined(minimumSignificantSetting) && IsUndefined(maximumSignificantSetting)) {
                    minimum = maximum = 0; minimumSignificant = 1; maximumSignificant = 2; roundingPriority = "morePrecision";
                    if (roundingIncrement != 1) throw new InvalidOperationException("Compact significant digits cannot be combined with rounding increments");
                }
                compactDisplay = CurrencyOption("compactDisplay", "short");
                if (compactDisplay is not ("short" or "long")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid compact display");
                if (notation == "compact") groupingOption = "min2";
                object group = options.Get("useGrouping");
                if (!IsUndefined(group))
                {
                    grouping = Truthy(group);
                    groupingOption = !grouping ? BoxBoolean(false) : group is string text ? text switch
                    {
                        "auto" or "always" => text,
                        "true" or "false" => notation == "compact" ? "min2" : "auto",
                        "min2" => text,
                        _ => throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid grouping option")
                    } : "always";
                }
                object sign = options.Get("signDisplay");
                if (!IsUndefined(sign))
                {
                    signDisplay = StringValue(sign);
                    if (signDisplay is not ("auto" or "always" or "exceptZero" or "never" or "negative"))
                        throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid sign display");
                }
            }
            if(IsUndefined(optionValue))culture=NativeNumberSymbols.Configure(culture,numberingSystem,out exponentSeparator);
            if (style == "percent")
            {
                culture = (CultureInfo)culture.Clone();
                var info = culture.NumberFormat;
                if(NativeNumberSymbols.Patterns(culture,numberingSystem))info.PercentGroupSizes=NativeNumberSymbols.Groups(culture.Name,numberingSystem,3) ?? info.PercentGroupSizes;
                info.NumberDecimalSeparator = info.PercentDecimalSeparator;
                info.NumberGroupSeparator = info.PercentGroupSeparator;
                info.NumberGroupSizes = info.PercentGroupSizes;
                positivePattern = (0.01d).ToString("P0", culture);
                negativePattern = (-0.01d).ToString("P0", culture);
                if(NativeNumberSymbols.Patterns(culture,numberingSystem)) {
                    positivePattern=NativeCompactNumbers.Pattern(culture.Name,numberingSystem,null,false,3);
                    negativePattern=NativeCompactNumbers.Pattern(culture.Name,numberingSystem,null,true,3);
                }
            }
            if (notation == "compact" && culture.Name is not ("en" or "en-US")) nativeCompact=new NativeCompactNumbers(culture.Name,LongCompact);
            if(nativeCompact!=null && style=="currency") {
                positivePattern=NativeCompactNumbers.Pattern(culture.Name,numberingSystem,culture.NumberFormat.CurrencySymbol,false,currencySign=="accounting" ? 12 : 2);
                compactPositivePattern=NativeCompactNumbers.Pattern(culture.Name,numberingSystem,culture.NumberFormat.CurrencySymbol,false,2);
                negativePattern=NativeCompactNumbers.Pattern(culture.Name,numberingSystem,culture.NumberFormat.CurrencySymbol,true,currencySign=="accounting" ? 12 : 2);
                compactNegativePattern=NativeCompactNumbers.Pattern(culture.Name,numberingSystem,culture.NumberFormat.CurrencySymbol,true,2);
                if(culture.TwoLetterISOLanguageName=="ar") {
                    var ordinary=ArabicCurrencyPatterns(culture.NumberFormat.CurrencySymbol,numberingSystem,culture.NumberFormat.NegativeSign,currencySign=="accounting",false);
                    var compact=ArabicCurrencyPatterns(culture.NumberFormat.CurrencySymbol,numberingSystem,culture.NumberFormat.NegativeSign,false,true);
                    positivePattern=ordinary.Positive;negativePattern=ordinary.Negative;compactPositivePattern=compact.Positive;compactNegativePattern=compact.Negative;
                }
            }
            if(nativeCompact!=null && style=="percent" && culture.TwoLetterISOLanguageName=="es") {positivePattern=positivePattern.Replace(" ","\u00a0");negativePattern=negativePattern.Replace(" ","\u00a0");}
            if(nativeCompact!=null && numberingSystem=="latn" && culture.NumberFormat.NegativeSign.Contains('\u061c')) {
                culture=(CultureInfo)culture.Clone();culture.NumberFormat.NegativeSign=culture.NumberFormat.NegativeSign.Replace('\u061c','\u200e');culture.NumberFormat.PositiveSign=culture.NumberFormat.PositiveSign.Replace('\u061c','\u200e');
            }
            if(nativeCompact!=null && numberingSystem=="latn" && culture.NumberFormat.NumberDecimalSeparator=="\u066b") {
                culture=(CultureInfo)culture.Clone();culture.NumberFormat.NumberDecimalSeparator=".";culture.NumberFormat.NumberGroupSeparator=",";
            }
            if(nativeCompact!=null && culture.TwoLetterISOLanguageName=="ar" && culture.NumberFormat.NaNSymbol=="\u0644\u064a\u0633\u00a0\u0631\u0642\u0645") {
                culture=(CultureInfo)culture.Clone();culture.NumberFormat.NaNSymbol="\u0644\u064a\u0633\u00a0\u0631\u0642\u0645\u064b\u0627";
            }
            if (style == "unit" && culture.Name is not ("en" or "en-US")) nativeUnit=new NativeUnitFormatting(culture.Name,unit,unitDisplay,numberingSystem);
            minimumGrouping = groupingOption is "min2" || groupingOption is "auto" && culture.Name is "es" or "es-ES" or "pl" or "pl-PL";
            minimumGroupingWidth = int.MaxValue;
            if (minimumGrouping)
            {
                int[] sizes = culture.NumberFormat.NumberGroupSizes;
                if (sizes.Length > 0 && sizes[0] > 0) minimumGroupingWidth = sizes[0] + 2;
            }
            minimumGroupingThreshold=Math.Pow(10,minimumGroupingWidth-1);
            ungroupedFormat="F"+maximum.ToString(CultureInfo.InvariantCulture);
            format = (grouping ? "N" : "F") + maximum.ToString(CultureInfo.InvariantCulture);
            digitCharacters = NumberDigitCharacters(numberingSystem);
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
        string FiniteNumber(double value, out int fractionMinimum, out int fractionMaximum, out int notationExponent)
        {
            notationExponent = 0; fractionMinimum = minimum; fractionMaximum = maximum;
            // Safe integers need no decimal rounding and retain native formatting.
            if (notation == "standard" && maximumSignificant == 0 && roundingIncrement == 1 && style is ("decimal" or "unit") && double.IsInteger(value) && Math.Abs(value) <= 9007199254740991d) {
                bool groups=!minimumGrouping || integerDigits>=minimumGroupingWidth || Math.Abs(value)>=minimumGroupingThreshold;
                return value.ToString(groups ? format : ungroupedFormat,culture);
            }
            string raw = Math.Abs(value).ToString("R", CultureInfo.InvariantCulture);
            int split = raw.IndexOf('E');
            int exponent = split < 0 ? 0 : int.Parse(raw.AsSpan(split + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            string coefficient = split < 0 ? raw : raw[..split];
            int point = coefficient.IndexOf('.');
            int scale = point < 0 ? 0 : coefficient.Length - point - 1;
            BigInteger digits = BigInteger.Parse(coefficient.Replace(".", ""), CultureInfo.InvariantCulture);
            return NumericText(digits, exponent - scale + (style == "percent" ? 2 : 0), double.IsNegative(value), out fractionMinimum, out fractionMaximum, out notationExponent);
        }
        int ExponentFor(int magnitude) => notation == "compact" && nativeCompact!=null ? nativeCompact.Exponent(magnitude) : notation == "compact" ? Math.Clamp((int)Math.Floor(magnitude / 3d) * 3, 0, 12) : notation == "engineering" ? (int)Math.Floor(magnitude / 3d) * 3 : magnitude;
        string NumericText(BigInteger digits, int exponent, bool negative, out int fractionMinimum, out int fractionMaximum, out int notationExponent)
        {
            if (notation == "standard")
            {
                notationExponent = 0;
                return ScaledText(digits, exponent, negative, out fractionMinimum, out fractionMaximum);
            }
            int magnitude = digits.IsZero ? 0 : digits.ToString(CultureInfo.InvariantCulture).Length + exponent - 1;
            notationExponent = digits.IsZero ? 0 : ExponentFor(magnitude);
            string result = ScaledText(digits, exponent - notationExponent, negative, out fractionMinimum, out fractionMaximum);
            if (notation != "standard" && !digits.IsZero)
            {
                int point = result.IndexOf(culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
                int width = 0;
                for (int i = 0; i < (point < 0 ? result.Length : point); i++) if (char.IsAsciiDigit(result[i])) width++;
                if (width > magnitude - notationExponent + 1)
                {
                    int adjusted = ExponentFor(magnitude + 1);
                    if (adjusted != notationExponent)
                    {
                        notationExponent = adjusted;
                        result = ScaledText(digits, exponent - notationExponent, negative, out fractionMinimum, out fractionMaximum);
                    }
                }
            }
            return result;
        }
        string ScaledText(BigInteger digits, int exponent, bool negative, out int fractionMinimum, out int fractionMaximum)
        {
            if (maximumSignificant != 0) return PrecisionText(digits, exponent, negative, out fractionMinimum, out fractionMaximum);
            fractionMinimum = minimum; fractionMaximum = maximum;
            return QuantizedText(Quantize(digits, exponent + maximum, negative), negative);
        }
        string PrecisionText(BigInteger digits, int exponent, bool negative, out int fractionMinimum, out int fractionMaximum)
        {
            int width = digits.IsZero ? 1 : digits.ToString(CultureInfo.InvariantCulture).Length;
            int places = digits.IsZero ? maximumSignificant - 1 : maximumSignificant - width - exponent;
            BigInteger rounded = Quantize(digits, exponent + places, negative);
            int magnitude = rounded.IsZero ? 0 : rounded.ToString(CultureInfo.InvariantCulture).Length - places - 1;
            int roundingMagnitude = magnitude - maximumSignificant + 1;
            bool useSignificant = roundingPriority == "auto" || (roundingPriority == "morePrecision" ? roundingMagnitude <= -maximum : roundingMagnitude > -maximum);
            if (!useSignificant)
            {
                fractionMinimum = minimum; fractionMaximum = maximum;
                return QuantizedText(Quantize(digits, exponent + maximum, negative), negative);
            }
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
            if (minimumGrouping)
                useGroups = UseMinimumGrouping(whole.ToString(CultureInfo.InvariantCulture).Length);
            string result = whole.ToString(useGroups ? "N0" : "F0", culture);
            if (fractionDigits != 0) result += culture.NumberFormat.NumberDecimalSeparator + fraction.ToString(CultureInfo.InvariantCulture).PadLeft(fractionDigits, '0');
            return negative ? culture.NumberFormat.NegativeSign + result : result;
        }
        internal bool LongCompact => compactDisplay == "long" && (style != "currency" || CurrencyName);
        internal string CompactLabel(int exponent) => (LongCompact, exponent) switch {
            (false, 3) => "K", (false, 6) => "M", (false, 9) => "B", (false, 12) => "T",
            (true, 3) => "thousand", (true, 6) => "million", (true, 9) => "billion", (true, 12) => "trillion", _ => ""
        };
        bool EnglishSingularNumber(string number)
        {
            var info = culture.NumberFormat;
            string magnitude = number;
            if (magnitude.StartsWith(info.NegativeSign, StringComparison.Ordinal)) magnitude = magnitude[info.NegativeSign.Length..];
            else if (magnitude.StartsWith(info.PositiveSign, StringComparison.Ordinal)) magnitude = magnitude[info.PositiveSign.Length..];
            if (magnitude.EndsWith("E0", StringComparison.Ordinal)) magnitude = magnitude[..^2];
            return magnitude.TrimStart('0') == "1";
        }
        internal bool CurrencyName => style == "currency" && currencyDisplay == "name";
        internal string CurrencyNameAffix(string number) => EnglishCurrencyName(currency, EnglishSingularNumber(number));
        internal (string Gap, string Name) UnitAffix(string number)
        {
            bool singular = EnglishSingularNumber(number);
            int separator = unit.IndexOf("-per-", StringComparison.Ordinal);
            if (separator < 0) return EnglishNumberUnit(unit, unitDisplay, singular);
            var numerator = EnglishNumberUnit(unit[..separator], unitDisplay, singular);
            string denominatorUnit = unit[(separator + 5)..];
            var denominator = EnglishNumberUnit(denominatorUnit, unitDisplay, true);
            string denominatorName = unitDisplay == "long" ? denominator.Name : denominatorUnit switch {"second" => "s", "minute" => "min", "hour" => "h", "day" => "d", "week" => "w", "month" => "m", "year" => "y", _ => denominator.Name};
            if (unitDisplay != "long" && unit == "mile-per-hour") return (numerator.Gap, "mph");
            return (numerator.Gap, numerator.Name + (unitDisplay == "long" ? " per " : "/") + denominatorName);
        }
        internal string LocalizeDigits(string text) => LocalizeNumberDigits(text, digitCharacters);
        internal string Format<T>(T value, bool finite) where T : struct, IFormattable
        {
            if(!CurrencyName && !UsesNativeUnit)return LocalizeDigits(FormatLatin(value,finite));
            var details=new NumberFormatAnnotations();string number=FormatLatin(value,finite,false,null,details);return details.Apply(LocalizeDigits(number));
        }
        internal string FormatLatin<T>(T value, bool finite, bool includeAffix = true, List<string> compactLabels = null, NumberFormatAnnotations details = null) where T : struct, IFormattable
        {
            int fractionMinimum = minimum, fractionMaximum = maximum, notationExponent = 0;
            string result = value is LocaleDecimal exact ? NumericText(exact.Digits, exact.Exponent + (style == "percent" ? 2 : 0), exact.Negative, out fractionMinimum, out fractionMaximum, out notationExponent)
                : finite && value is double numberValue ? FiniteNumber(numberValue, out fractionMinimum, out fractionMaximum, out notationExponent)
                : (maximumSignificant != 0 || notation != "standard") && value is BigInteger significantInteger
                    ? NumericText(BigInteger.Abs(significantInteger), style == "percent" ? 2 : 0, significantInteger.Sign < 0, out fractionMinimum, out fractionMaximum, out notationExponent)
                : (roundingIncrement != 1 || minimumGrouping) && value is BigInteger quantizedInteger
                    ? QuantizedText(Quantize(BigInteger.Abs(quantizedInteger), maximum + (style == "percent" ? 2 : 0), quantizedInteger.Sign < 0), quantizedInteger.Sign < 0)
                : style == "percent" && value is BigInteger integerValue ? (integerValue * 100).ToString(format, culture) : value.ToString(format, culture);
            if (finite && integerDigits > 1)
            {
                bool useGroups = grouping;
                if (minimumGrouping)
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
                bool negative = value is LocaleDecimal decimalValue ? decimalValue.Negative : value is double number ? !double.IsNaN(number) && double.IsNegative(number) : value is System.Numerics.BigInteger integer && integer.Sign < 0;
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
            var namePattern=nativeCurrencyNames?.Pattern(result,notationExponent,culture.NumberFormat,finite);
            var unitPattern=nativeUnit?.Pattern(result,notationExponent,culture.NumberFormat,finite);
            if(details!=null)details.Unit=unitPattern;
            if(finite && nativeCompact!=null)result=nativeCompact.Apply(result,notationExponent,culture.NumberFormat,compactLabels);
            else if (finite && notation == "compact") { string compact = CompactLabel(notationExponent); if (compact.Length != 0) result += (LongCompact ? " " : "") + compact; }
            else if (finite && notation != "standard")
                result += exponentSeparator + (notationExponent < 0 ? culture.NumberFormat.NegativeSign : "") + Math.Abs(notationExponent).ToString(CultureInfo.InvariantCulture);
            if (CurrencyName) {
                namePattern??=CurrencyNamePattern.English(CurrencyNameAffix(result));if(details!=null)details.Currency=namePattern;
                return includeAffix ? namePattern.Apply(result) : result;
            }
            if(unitPattern!=null)return includeAffix ? unitPattern.Apply(result) : result;
            if (style == "unit") { if (!includeAffix) return result; var affix = UnitAffix(result); return result + affix.Gap + affix.Name; }
            if (style == "decimal") return result;
            var info = culture.NumberFormat;
            bool negativeSign = result.StartsWith(info.NegativeSign, StringComparison.Ordinal);
            bool positiveSign = !negativeSign && result.StartsWith(info.PositiveSign, StringComparison.Ordinal);
            string template = negativeSign || positiveSign ? nativeCompact!=null && notationExponent!=0 && compactNegativePattern!=null ? compactNegativePattern : negativePattern : nativeCompact!=null && notationExponent!=0 && compactPositivePattern!=null ? compactPositivePattern : positivePattern;
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
