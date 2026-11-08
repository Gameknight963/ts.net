using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    static string SupportedListLocale(string name)
    {
        string candidate = new LocaleValue(name, Undefined).BaseName;
        while (true) {
            if (candidate is "en" or "en-US" or "en-GB" or "de" or "de-DE" or "fr" or "fr-FR" or "ru" or "ru-RU"
                or "ja" or "ja-JP" or "ko" or "ko-KR" or "zh" or "zh-CN" or "zh-TW" or "zh-Hans" or "zh-Hant") return candidate;
            int separator = candidate.LastIndexOf('-');
            if (separator < 0) return null;
            candidate = candidate[..separator];
        }
    }
    static object ListSupportedLocales(object locales, object optionValue)
    {
        var requested = (JsArray)CanonicalLocales(locales);
        IntlLocaleMatcher(IsUndefined(optionValue) ? null : ToObject(optionValue));
        var result = new JsArray(0);
        for (uint index = 0; index < requested.Length(); index++) {
            string name = (string)requested.ElementAt(index);
            if (SupportedListLocale(name) != null) result.Append(name);
        }
        return result;
    }
    sealed class ListFormatValue : JsDescriptorObject
    {
        readonly string locale, type, style;
        readonly ListPattern pattern;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlListFormat.Constructor.Get("prototype");
        internal ListFormatValue(object locales, object optionValue)
        {
            var requested = (JsArray)CanonicalLocales(locales);
            TsObject options = IsUndefined(optionValue) ? null : ToObject(optionValue);
            IntlLocaleMatcher(options);
            string current = CultureInfo.CurrentCulture.Name;
            locale = (current.Length == 0 ? null : SupportedListLocale(current)) ?? "en-US";
            for (uint index = 0; index < requested.Length(); index++) {
                string selected = SupportedListLocale((string)requested.ElementAt(index));
                if (selected != null) {locale = selected; break;}
            }
            string Option(string name, string fallback) {
                object value = options == null ? Undefined : options.Get(name);
                return IsUndefined(value) ? fallback : StringValue(value);
            }
            type = Option("type", "conjunction");
            if (type is not ("conjunction" or "disjunction" or "unit")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid list type");
            style = Option("style", "long");
            if (style is not ("long" or "short" or "narrow")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid list style");
            pattern = ListPatternFor(locale, type, style);
        }
        static List<string> Items(object input)
        {
            var result = new List<string>();
            if (IsUndefined(input)) return result;
            object source = Iteration.Start(input);
            int position = 0; bool closeable = false;
            try {
                while (true) {
                    closeable = false;
                    if (position >= Iteration.Length(source)) break;
                    object value = Iteration.Value(source, position);
                    position += Iteration.Step(source, position); closeable = true;
                    if (value is not string text) throw new InvalidOperationException("List elements must be strings");
                    result.Add(text);
                }
            } catch {if (closeable) CloseIterationAt(source, position, true); throw;}
            return result;
        }
        internal object Format(object input, bool parts)
        {
            var items = Items(input);
            var result = parts ? new JsArray(0) : null;
            var text = parts ? null : new StringBuilder();
            JsDynamicObject previousLiteral = null;
            void Add(string type, string value) {
                if (value.Length == 0) return;
                if (!parts) {text.Append(value); return;}
                if (type == "literal" && previousLiteral != null) {
                    previousLiteral.DefineOwn("value", (string)previousLiteral.Get("value") + value); return;
                }
                var part = new JsDynamicObject(); part.DefineOwn("type", type); part.DefineOwn("value", value); result.Append(part);
                previousLiteral = type == "literal" ? part : null;
            }
            for (int index = 0; index < items.Count; index++) {
                if (index != 0) Add("literal", items.Count == 2 ? pattern.Pair : index == items.Count - 1 ? pattern.End : pattern.Middle);
                Add("element", items[index]);
            }
            return parts ? result : text.ToString();
        }
        internal object Options()
        {
            var result = new JsDynamicObject();
            result.DefineOwn("locale", locale); result.DefineOwn("type", type); result.DefineOwn("style", style);
            return result;
        }
    }
    static ListFormatValue RequireListFormat(object receiver) => receiver as ListFormatValue ?? throw new InvalidOperationException("ListFormat receiver required");
    sealed class ListFormatConstructor : NativeNamespaceFunction
    {
        internal ListFormatConstructor() : base("ListFormat", static _ => throw new InvalidOperationException("ListFormat requires new"), 0)
        {
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype}; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.ListFormat", false);
            prototype.DefineMethod("format", new CollectionMethod("format", 1, static (receiver, args) => RequireListFormat(receiver).Format(FunctionArgument(args, 0), false)));
            prototype.DefineMethod("formatToParts", new CollectionMethod("formatToParts", 1, static (receiver, args) => RequireListFormat(receiver).Format(FunctionArgument(args, 0), true)));
            prototype.DefineMethod("resolvedOptions", new CollectionMethod("resolvedOptions", 0, static (receiver, _) => RequireListFormat(receiver).Options()));
            Member("supportedLocalesOf", new JsFunction((FunctionBody)(static args => ListSupportedLocales(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "supportedLocalesOf", 1));
        }
        public override object Construct(object[] args) => new ListFormatValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget) {
            var prototype = ConstructionPrototype(newTarget);
            var result = (TsObject)Construct(args); result.SetPrototype(prototype); return result;
        }
    }
    static class IntlListFormat {internal static readonly JsFunction Constructor = new ListFormatConstructor();}
}
