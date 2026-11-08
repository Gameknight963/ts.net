using System;

public static partial class Js
{
    static string RelativeTimeLocale(string name)
    {
        var locale = new LocaleValue(name, Undefined);
        if (locale.Language != "en") return null;
        string selected = locale.BaseName == "en-US" ? "en-US" : "en";
        return locale.Keyword("nu") is string digits ? selected + "-u-nu-" + digits : selected;
    }
    static object RelativeTimeSupportedLocales(object locales, object optionValue)
    {
        var requested = (JsArray)CanonicalLocales(locales);
        IntlLocaleMatcher(IsUndefined(optionValue) ? null : ToObject(optionValue));
        var result = new JsArray(0);
        for (uint index = 0; index < requested.Length(); index++) {
            string name = (string)requested.ElementAt(index);
            if (RelativeTimeLocale(name) != null) result.Append(name);
        }
        return result;
    }
    sealed class RelativeTimeFormatValue : JsDescriptorObject
    {
        readonly string locale, style, numeric, numberingSystem, one;
        readonly NumberFormatValue numbers;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlRelativeTimeFormat.Constructor.Get("prototype");
        internal RelativeTimeFormatValue(object locales, object optionValue)
        {
            var requested = (JsArray)CanonicalLocales(locales);
            TsObject options = IsUndefined(optionValue) ? null : ToObject(optionValue);
            IntlLocaleMatcher(options);
            object system = options == null ? Undefined : options.Get("numberingSystem");
            if (!IsUndefined(system)) {system = StringValue(system); ValidateNumberingSystem((string)system);}
            string selected = "en-US";
            for (uint index = 0; index < requested.Length(); index++) {
                string supported = RelativeTimeLocale((string)requested.ElementAt(index));
                if (supported != null) {selected = supported; break;}
            }
            string Option(string name, string fallback) {
                object value = options == null ? Undefined : options.Get(name);
                return IsUndefined(value) ? fallback : StringValue(value);
            }
            style = Option("style", "long");
            if (style is not ("long" or "short" or "narrow")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid relative-time style");
            numeric = Option("numeric", "always");
            if (numeric is not ("always" or "auto")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid relative-time numeric option");
            var numberOptions = new JsDynamicObject();
            if (!IsUndefined(system)) numberOptions.DefineOwn("numberingSystem", system);
            numbers = new NumberFormatValue(selected, numberOptions);
            var resolved = (TsObject)numbers.Options();
            locale = (string)resolved.Get("locale"); numberingSystem = (string)resolved.Get("numberingSystem");
            one = numbers.Text(1d);
        }
        string UnitName(string unit, bool singular, bool automatic = false)
        {
            if (style == "long") return unit + (singular ? "" : "s");
            if (style == "narrow" && !automatic) return unit switch {
                "year" => "y", "quarter" => "q", "month" => "mo", "week" => "w", "day" => "d", "hour" => "h", "minute" => "m", _ => "s"
            };
            return unit switch {
                "year" => "yr.", "quarter" => singular ? "qtr." : "qtrs.", "month" => "mo.", "week" => "wk.", "day" => singular ? "day" : "days",
                "hour" => "hr.", "minute" => "min.", _ => "sec."
            };
        }
        string Automatic(double value, string unit)
        {
            if (numeric != "auto") return null;
            if (unit == "second") return value == 0 ? "now" : null;
            if (unit == "day") return value == -1 ? "yesterday" : value == 0 ? "today" : value == 1 ? "tomorrow" : null;
            if (unit is "hour" or "minute") return value == 0 ? "this " + unit : null;
            if (value is not (-1 or 0 or 1)) return null;
            return (value < 0 ? "last " : value > 0 ? "next " : "this ") + UnitName(unit, true, true);
        }
        internal object Format(object input, object requestedUnit, bool parts)
        {
            double value = Number(input);
            string unit = StringValue(requestedUnit);
            if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(input), "Relative time must be finite");
            if (unit.EndsWith('s')) unit = unit[..^1];
            if (unit is not ("year" or "quarter" or "month" or "week" or "day" or "hour" or "minute" or "second"))
                throw new ArgumentOutOfRangeException(nameof(requestedUnit), "Invalid relative-time unit");
            string automatic = Automatic(value, unit);
            var result = parts ? new JsArray(0) : null;
            void Literal(string text) {
                if (text.Length == 0) return;
                var part = new JsDynamicObject(); part.DefineOwn("type", "literal"); part.DefineOwn("value", text); result.Append(part);
            }
            if (automatic != null) {
                if (!parts) return automatic;
                Literal(automatic); return result;
            }
            var numericParts = parts ? (JsArray)numbers.Parts(Math.Abs(value)) : null;
            string number = parts ? null : numbers.Text(Math.Abs(value));
            bool singular = parts ? numericParts.Length() == 1 && (string)((TsObject)numericParts.ElementAt(0)).Get("value") == one : number == one;
            string prefix = double.IsNegative(value) ? "" : "in ";
            string suffix = (style == "narrow" ? "" : " ") + UnitName(unit, singular) + (double.IsNegative(value) ? " ago" : "");
            if (!parts) return prefix + number + suffix;
            Literal(prefix);
            for (uint index = 0; index < numericParts.Length(); index++) {
                var part = (JsDynamicObject)numericParts.ElementAt(index);
                part.DefineOwn("unit", unit); result.Append(part);
            }
            Literal(suffix); return result;
        }
        internal object Options()
        {
            var result = new JsDynamicObject();
            result.DefineOwn("locale", locale); result.DefineOwn("style", style); result.DefineOwn("numeric", numeric); result.DefineOwn("numberingSystem", numberingSystem);
            return result;
        }
    }
    static RelativeTimeFormatValue RequireRelativeTimeFormat(object receiver) => receiver as RelativeTimeFormatValue ?? throw new InvalidOperationException("RelativeTimeFormat receiver required");
    sealed class RelativeTimeFormatConstructor : NativeNamespaceFunction
    {
        internal RelativeTimeFormatConstructor() : base("RelativeTimeFormat", static _ => throw new InvalidOperationException("RelativeTimeFormat requires new"), 0)
        {
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype}; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.RelativeTimeFormat", false);
            prototype.DefineMethod("format", new CollectionMethod("format", 2, static (receiver, args) => RequireRelativeTimeFormat(receiver).Format(FunctionArgument(args, 0), FunctionArgument(args, 1), false)));
            prototype.DefineMethod("formatToParts", new CollectionMethod("formatToParts", 2, static (receiver, args) => RequireRelativeTimeFormat(receiver).Format(FunctionArgument(args, 0), FunctionArgument(args, 1), true)));
            prototype.DefineMethod("resolvedOptions", new CollectionMethod("resolvedOptions", 0, static (receiver, _) => RequireRelativeTimeFormat(receiver).Options()));
            Member("supportedLocalesOf", new JsFunction((FunctionBody)(static args => RelativeTimeSupportedLocales(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "supportedLocalesOf", 1));
        }
        public override object Construct(object[] args) => new RelativeTimeFormatValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget) {
            var prototype = ConstructionPrototype(newTarget);
            var result = (TsObject)Construct(args); result.SetPrototype(prototype); return result;
        }
    }
    static class IntlRelativeTimeFormat {internal static readonly JsFunction Constructor = new RelativeTimeFormatConstructor();}
}
