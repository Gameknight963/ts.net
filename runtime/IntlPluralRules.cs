using System;
using System.Globalization;

public static partial class Js
{
    sealed class PluralNumberOptions(TsObject source) : JsDynamicObject
    {
        public override object Get(string key) => key switch {
            "useGrouping" => BoxBoolean(false),
            "minimumIntegerDigits" or "minimumFractionDigits" or "maximumFractionDigits" or "minimumSignificantDigits" or "maximumSignificantDigits"
                or "roundingIncrement" or "roundingMode" or "roundingPriority" or "trailingZeroDisplay" => source == null ? Undefined : source.Get(key),
            _ => Undefined
        };
    }
    static bool SupportsPluralLanguage(string language) => language is "en" or "de" or "fr" or "ru" or "ja" or "ko" or "zh" || AdditionalPluralLanguage(language);
    static object PluralSupportedLocales(object locales, object optionValue)
    {
        var requested = (JsArray)CanonicalLocales(locales);
        IntlLocaleMatcher(IsUndefined(optionValue) ? null : ToObject(optionValue));
        var result = new JsArray(0);
        for (uint index = 0; index < requested.Length(); index++) {
            string name = (string)requested.ElementAt(index);
            if (SupportsPluralLanguage(new LocaleValue(name, Undefined).Language)) result.Append(name);
        }
        return result;
    }
    sealed class PluralRulesValue : JsDescriptorObject
    {
        readonly string locale, type;
        readonly bool allOther;
        readonly NumberLocaleSettings digits;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlPluralRules.Constructor.Get("prototype");
        internal PluralRulesValue(object locales, object optionValue)
        {
            var requestedLocales = (JsArray)CanonicalLocales(locales);
            TsObject options = IsUndefined(optionValue) ? null : ToObject(optionValue);
            IntlLocaleMatcher(options);
            string current = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            locale = SupportsPluralLanguage(current) ? current : "en";
            for (uint index = 0; index < requestedLocales.Length(); index++) {
                string language = new LocaleValue(requestedLocales.ElementAt(index), Undefined).Language;
                if (SupportsPluralLanguage(language)) {locale = language; break;}
            }
            object requested = options == null ? Undefined : options.Get("type");
            type = IsUndefined(requested) ? "cardinal" : StringValue(requested);
            if (type is not ("cardinal" or "ordinal")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid plural rule type");
            allOther = locale is "ja" or "ko" or "zh" || type == "ordinal" && locale is "de" or "ru" or "ar" or "pl" or "cs" or "sk" or "sl" or "lt" or "lv" or "he";
            digits = new NumberLocaleSettings("en-US", new PluralNumberOptions(options));
        }
        string Rounded(double value)
        {
            string text = digits.FormatLatin(value, true);
            return text.StartsWith("-", StringComparison.Ordinal) ? text[1..] : text;
        }
        string Category(string text)
        {
            if(AdditionalPluralLanguage(locale))return AdditionalPluralCategory(locale,type,text);
            int point = text.IndexOf('.');
            ReadOnlySpan<char> integer = point < 0 ? text.AsSpan() : text.AsSpan(0, point);
            while (integer.Length > 1 && integer[0] == '0') integer = integer[1..];
            bool isOne = integer.SequenceEqual("1");
            bool integral = true;
            if (point >= 0)
                foreach (char digit in text.AsSpan(point + 1)) if (digit != '0') {integral = false; break;}
            if (locale == "fr") {
                if (type == "ordinal") return integral && isOne ? "one" : "other";
                if (isOne || integer.SequenceEqual("0")) return "one";
                return point < 0 && integer.Length > 6 && integer.EndsWith("000000") ? "many" : "other";
            }
            int last = integer[^1] - '0';
            int pair = integer.Length > 1 ? (integer[^2] - '0') * 10 + last : last;
            if (locale == "ru") {
                if (point >= 0) return "other";
                if (last == 1 && pair != 11) return "one";
                if (last is >= 2 and <= 4 && pair is not (>= 12 and <= 14)) return "few";
                return "many";
            }
            if (type == "cardinal") return point < 0 && isOne ? "one" : "other";
            if (!integral) return "other";
            return last == 1 && pair != 11 ? "one" : last == 2 && pair != 12 ? "two" : last == 3 && pair != 13 ? "few" : "other";
        }
        internal object Select(object input)
        {
            double value = Number(input);
            return !allOther && double.IsFinite(value) ? Category(Rounded(value)) : "other";
        }
        internal object Range(object start, object end)
        {
            if (IsUndefined(start) || IsUndefined(end)) throw new InvalidOperationException("Plural range requires two values");
            double first = Number(start), last = Number(end);
            if (double.IsNaN(first) || double.IsNaN(last)) throw new ArgumentOutOfRangeException(nameof(start), "Invalid plural range");
            if (allOther) return "other";
            string left = double.IsFinite(first) ? Rounded(first) : null, right = double.IsFinite(last) ? Rounded(last) : null;
            if (left != null && left == right) return Category(left);
            string firstCategory = left == null ? "other" : Category(left), lastCategory = right == null ? "other" : Category(right);
            if(AdditionalPluralLanguage(locale))return AdditionalPluralRange(locale,firstCategory,lastCategory);
            if (locale == "ru") return lastCategory;
            if (locale == "de" && firstCategory == "other" && lastCategory == "one") return "one";
            if (locale == "fr" && firstCategory == "one" && lastCategory == "one") return "one";
            return "other";
        }
        internal object Options()
        {
            var result = new JsDynamicObject(); result.DefineOwn("locale", locale); result.DefineOwn("type", type);
            result.DefineOwn("minimumIntegerDigits", (double)digits.integerDigits);
            if (digits.maximumSignificant == 0 || digits.roundingPriority != "auto") {
                result.DefineOwn("minimumFractionDigits", (double)digits.minimum); result.DefineOwn("maximumFractionDigits", (double)digits.maximum);
            }
            if (digits.maximumSignificant != 0) {
                result.DefineOwn("minimumSignificantDigits", (double)digits.minimumSignificant); result.DefineOwn("maximumSignificantDigits", (double)digits.maximumSignificant);
            }
            object[] categories = allOther ? new object[] {"other"}
                : AdditionalPluralLanguage(locale) ? AdditionalPluralCategories(locale,type)
                : type == "ordinal" && locale == "en" ? new object[] {"one", "two", "few", "other"}
                : locale == "ru" ? new object[] {"one", "few", "many", "other"}
                : locale == "fr" && type == "cardinal" ? new object[] {"one", "many", "other"} : new object[] {"one", "other"};
            result.DefineOwn("pluralCategories", new JsArray(categories));
            result.DefineOwn("roundingIncrement", (double)digits.roundingIncrement); result.DefineOwn("roundingMode", digits.roundingMode);
            result.DefineOwn("roundingPriority", digits.roundingPriority); result.DefineOwn("trailingZeroDisplay", digits.trailingZeroDisplay);
            return result;
        }
    }
    static PluralRulesValue RequirePluralRules(object receiver) => receiver as PluralRulesValue ?? throw new InvalidOperationException("PluralRules receiver required");
    sealed class PluralRulesConstructor : NativeNamespaceFunction
    {
        internal PluralRulesConstructor() : base("PluralRules", static _ => throw new InvalidOperationException("PluralRules requires new"), 0)
        {
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype}; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.PluralRules", false);
            prototype.DefineMethod("select", new CollectionMethod("select", 1, static (receiver, args) => RequirePluralRules(receiver).Select(FunctionArgument(args, 0))));
            prototype.DefineMethod("selectRange", new CollectionMethod("selectRange", 2, static (receiver, args) => RequirePluralRules(receiver).Range(FunctionArgument(args, 0), FunctionArgument(args, 1))));
            prototype.DefineMethod("resolvedOptions", new CollectionMethod("resolvedOptions", 0, static (receiver, _) => RequirePluralRules(receiver).Options()));
            Member("supportedLocalesOf", new JsFunction((FunctionBody)(static args => PluralSupportedLocales(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "supportedLocalesOf", 1));
        }
        public override object Construct(object[] args) => new PluralRulesValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget) {
            var prototype = ConstructionPrototype(newTarget);
            var result = (TsObject)Construct(args); result.SetPrototype(prototype); return result;
        }
    }
    static class IntlPluralRules {internal static readonly JsFunction Constructor = new PluralRulesConstructor();}
}
