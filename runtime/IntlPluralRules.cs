using System;

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
    static object PluralSupportedLocales(object locales, object optionValue)
    {
        var requested = (JsArray)CanonicalLocales(locales);
        IntlLocaleMatcher(IsUndefined(optionValue) ? null : ToObject(optionValue));
        var result = new JsArray(0);
        for (uint index = 0; index < requested.Length(); index++) {
            string name = (string)requested.ElementAt(index);
            if (new LocaleValue(name, Undefined).Language == "en") result.Append(name);
        }
        return result;
    }
    sealed class PluralRulesValue : JsDescriptorObject
    {
        readonly string type;
        readonly NumberLocaleSettings digits;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlPluralRules.Constructor.Get("prototype");
        internal PluralRulesValue(object locales, object optionValue)
        {
            _ = CanonicalLocales(locales);
            TsObject options = IsUndefined(optionValue) ? null : ToObject(optionValue);
            IntlLocaleMatcher(options);
            object requested = options == null ? Undefined : options.Get("type");
            type = IsUndefined(requested) ? "cardinal" : StringValue(requested);
            if (type is not ("cardinal" or "ordinal")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid plural rule type");
            digits = new NumberLocaleSettings("en-US", new PluralNumberOptions(options));
        }
        string Rounded(double value)
        {
            string text = digits.FormatLatin(value, true);
            return text.StartsWith("-", StringComparison.Ordinal) ? text[1..] : text;
        }
        string Category(string text)
        {
            int point = text.IndexOf('.');
            ReadOnlySpan<char> integer = point < 0 ? text.AsSpan() : text.AsSpan(0, point);
            while (integer.Length > 1 && integer[0] == '0') integer = integer[1..];
            if (type == "cardinal") return point < 0 && integer.SequenceEqual("1") ? "one" : "other";
            if (point >= 0)
                foreach (char digit in text.AsSpan(point + 1)) if (digit != '0') return "other";
            int last = integer[^1] - '0';
            int pair = integer.Length > 1 ? (integer[^2] - '0') * 10 + last : last;
            return last == 1 && pair != 11 ? "one" : last == 2 && pair != 12 ? "two" : last == 3 && pair != 13 ? "few" : "other";
        }
        internal object Select(object input)
        {
            double value = Number(input);
            return double.IsFinite(value) ? Category(Rounded(value)) : "other";
        }
        internal object Range(object start, object end)
        {
            if (IsUndefined(start) || IsUndefined(end)) throw new InvalidOperationException("Plural range requires two values");
            double first = Number(start), last = Number(end);
            if (double.IsNaN(first) || double.IsNaN(last)) throw new ArgumentOutOfRangeException(nameof(start), "Invalid plural range");
            if (!double.IsFinite(first) || !double.IsFinite(last)) return "other";
            string left = Rounded(first), right = Rounded(last);
            return left == right ? Category(left) : "other";
        }
        internal object Options()
        {
            var result = new JsDynamicObject(); result.DefineOwn("locale", "en"); result.DefineOwn("type", type);
            result.DefineOwn("minimumIntegerDigits", (double)digits.integerDigits);
            if (digits.maximumSignificant == 0 || digits.roundingPriority != "auto") {
                result.DefineOwn("minimumFractionDigits", (double)digits.minimum); result.DefineOwn("maximumFractionDigits", (double)digits.maximum);
            }
            if (digits.maximumSignificant != 0) {
                result.DefineOwn("minimumSignificantDigits", (double)digits.minimumSignificant); result.DefineOwn("maximumSignificantDigits", (double)digits.maximumSignificant);
            }
            result.DefineOwn("pluralCategories", type == "cardinal" ? new JsArray(new object[] {"one", "other"}) : new JsArray(new object[] {"one", "two", "few", "other"}));
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
