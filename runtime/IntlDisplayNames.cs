using System;
using System.Collections.Generic;
using System.Globalization;

public static partial class Js
{
    static string DisplayLanguageCode(string code)
    {
        string[] fields = code.Split('-');
        string language = fields[0].ToLowerInvariant(), script = null, region = null;
        if (!(language.Length is 2 or 3 || language.Length is >= 5 and <= 8) || !AsciiLetters(language))
            throw new ArgumentOutOfRangeException(nameof(code), "Invalid display-name language");
        int next = 1;
        if (next < fields.Length && fields[next].Length == 4 && AsciiLetters(fields[next])) {
            script = char.ToUpperInvariant(fields[next][0]) + fields[next][1..].ToLowerInvariant(); next++;
        }
        if (next < fields.Length && (fields[next].Length == 2 && AsciiLetters(fields[next]) || fields[next].Length == 3 && AsciiDigits(fields[next])))
            region = fields[next++].ToUpperInvariant();
        var variants = new SortedSet<string>(StringComparer.Ordinal);
        for (; next < fields.Length; next++) {
            string variant = fields[next].ToLowerInvariant();
            if (!(variant.Length is >= 5 and <= 8 || variant.Length == 4 && char.IsAsciiDigit(variant[0])))
                throw new ArgumentOutOfRangeException(nameof(code), "Invalid display-name language variant");
            foreach (char ch in variant) if (!char.IsAsciiLetterOrDigit(ch)) throw new ArgumentOutOfRangeException(nameof(code), "Invalid display-name language variant");
            if (!variants.Add(variant)) throw new ArgumentOutOfRangeException(nameof(code), "Duplicate display-name language variant");
        }
        string[] alias = LocaleSubtagLanguageAlias(language).Split('-'); language = alias[0];
        for (int index = 1; index < alias.Length; index++) {if (alias[index].Length == 4) script ??= alias[index]; else region ??= alias[index];}
        if (script == "Qaai") script = "Zinh";
        if (region != null && LocaleSubtagRegionAliases(region) != null) region = ExpandLocaleSubtags(language, script, region).Region;
        var result = new List<string>{language};
        if (script != null) result.Add(script);
        if (region != null) result.Add(region);
        result.AddRange(variants);
        return string.Join('-', result);
    }
    sealed class DisplayNamesValue : JsDescriptorObject
    {
        readonly string locale, style, type, fallback, languageDisplay;
        readonly NativeDisplayNames names;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlDisplayNames.Constructor.Get("prototype");
        internal DisplayNamesValue(object locales, object optionValue)
        {
            var requested = (JsArray)CanonicalLocales(locales);
            if (IsUndefined(optionValue)) throw new InvalidOperationException("DisplayNames options are required");
            var options = ToObject(optionValue);
            IntlLocaleMatcher(options);
            var supported = (JsArray)CollatorSupportedLocales(requested, Undefined);
            locale = supported.Length() == 0 ? CultureInfo.CurrentCulture.Name : CollatorLocale((string)supported.ElementAt(0));
            if (locale.Length == 0) locale = "en-US";
            string Option(string key, string defaultValue) {
                object value = options.Get(key);
                return IsUndefined(value) ? defaultValue : StringValue(value);
            }
            style = Option("style", "long");
            if (style is not ("long" or "short" or "narrow")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid display-name style");
            type = Option("type", null);
            if (type == null) throw new InvalidOperationException("DisplayNames type is required");
            if (type is not ("language" or "region" or "script" or "currency" or "calendar" or "dateTimeField"))
                throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid display-name type");
            fallback = Option("fallback", "code");
            if (fallback is not ("code" or "none")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid display-name fallback");
            languageDisplay = Option("languageDisplay", "dialect");
            if (languageDisplay is not ("dialect" or "standard")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid language display");
            names = new(locale, type, style, fallback, languageDisplay);
        }
        internal object Of(object value)
        {
            string code = StringValue(value), native = code;
            int field = 0;
            bool valid;
            switch (type) {
                case "language":
                    code = DisplayLanguageCode(code);
                    native = code.Replace('-', '_'); valid = true; break;
                case "region":
                    valid = code.Length == 2 && AsciiLetters(code) || code.Length == 3 && AsciiDigits(code);
                    code = native = code.ToUpperInvariant(); break;
                case "script":
                    valid = code.Length == 4 && AsciiLetters(code);
                    if (valid) code = native = char.ToUpperInvariant(code[0]) + code[1..].ToLowerInvariant();
                    break;
                case "currency":
                    valid = code.Length == 3 && AsciiLetters(code);
                    code = native = code.ToUpperInvariant(); break;
                case "calendar":
                    valid = true;
                    foreach (string part in code.Split('-')) {
                        if (part.Length is < 3 or > 8) {valid = false; break;}
                        foreach (char ch in part) if (!char.IsAsciiLetterOrDigit(ch)) valid = false;
                    }
                    code = code.ToLowerInvariant();
                    native = code switch {"gregory" => "gregorian", "ethioaa" => "ethiopic-amete-alem", _ => code};
                    break;
                default:
                    field = code switch {"era" => 0, "year" => 1, "quarter" => 2, "month" => 3, "weekOfYear" => 4, "weekday" => 6,
                        "day" => 9, "dayPeriod" => 10, "hour" => 11, "minute" => 12, "second" => 13, "timeZoneName" => 15, _ => -1};
                    valid = field >= 0; break;
            }
            if (!valid) throw new ArgumentOutOfRangeException(nameof(value), "Invalid display-name code");
            string result = names.Name(native, field);
            return result != null ? result : fallback == "code" ? code : Undefined;
        }
        internal object Options()
        {
            var result = new JsDynamicObject();
            result.DefineOwn("locale", locale); result.DefineOwn("style", style); result.DefineOwn("type", type); result.DefineOwn("fallback", fallback);
            if (type == "language") result.DefineOwn("languageDisplay", languageDisplay);
            return result;
        }
    }
    sealed class DisplayNamesConstructor : NativeNamespaceFunction
    {
        internal DisplayNamesConstructor() : base("DisplayNames", static _ => throw new InvalidOperationException("DisplayNames requires new"), 2)
        {
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype}; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.DisplayNames", false);
            prototype.DefineMethod("of", new CollectionMethod("of", 1, static (receiver, args) => receiver is DisplayNamesValue value ? value.Of(FunctionArgument(args, 0)) : throw new InvalidOperationException("DisplayNames receiver required")));
            prototype.DefineMethod("resolvedOptions", new CollectionMethod("resolvedOptions", 0, static (receiver, _) => receiver is DisplayNamesValue value ? value.Options() : throw new InvalidOperationException("DisplayNames receiver required")));
            Member("supportedLocalesOf", new JsFunction((FunctionBody)(static args => CollatorSupportedLocales(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "supportedLocalesOf", 1));
        }
        public override object Construct(object[] args) => new DisplayNamesValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);
            var result = (TsObject)Construct(args); result.SetPrototype(prototype); return result;
        }
    }
    static class IntlDisplayNames {internal static readonly JsFunction Constructor = new DisplayNamesConstructor();}
}
