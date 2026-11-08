using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class LocaleValue : JsDescriptorObject
    {
        internal readonly string Name, BaseName, Language, Script, Region;
        readonly SortedDictionary<string, string> keywords;
        internal object Keyword(string key) => keywords.TryGetValue(key, out var value) ? value.Length == 0 ? "true" : value : Undefined;
        internal bool Numeric => keywords.TryGetValue("kn", out var value) && value.Length == 0;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlLocale.Constructor.Get("prototype");
        internal LocaleValue(object tag, object optionValue)
        {
            if (tag is not string && tag is not TsObject) throw new InvalidOperationException("Locale must be a string or object");
            string name = tag is LocaleValue locale ? locale.Name : StringValue(tag);
            var parts = name.Split('-');
            int baseEnd = 1;
            while (baseEnd < parts.Length && parts[baseEnd].Length != 1) baseEnd++;
            _ = LocaleIdentifier(string.Join("-", parts, 0, baseEnd));
            var extensions = new LocaleExtensions(parts, baseEnd);
            int next = 1;
            string language = parts[0], script = null, region = null;
            if (next < baseEnd && parts[next].Length == 4) script = parts[next++];
            if (next < baseEnd && parts[next].Length is 2 or 3) region = parts[next++];
            if (!IsUndefined(optionValue))
            {
                var options = ToObject(optionValue);
                string Option(string key) { object value = options.Get(key); return IsUndefined(value) ? null : StringValue(value); }
                string requested = Option("language");
                if (requested != null) {
                    if (!(requested.Length is 2 or 3 || requested.Length is >= 5 and <= 8) || !AsciiLetters(requested)) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale language");
                    language = requested;
                }
                requested = Option("script");
                if (requested != null) {
                    if (requested.Length != 4 || !AsciiLetters(requested)) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale script");
                    script = requested;
                }
                requested = Option("region");
                if (requested != null) {
                    if (!(requested.Length == 2 && AsciiLetters(requested) || requested.Length == 3 && AsciiDigits(requested))) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale region");
                    region = requested;
                }
                extensions.Option(options, "calendar", "ca");
                extensions.Option(options, "collation", "co");
                extensions.Option(options, "firstDayOfWeek", "fw", weekday: true);
                extensions.Option(options, "hourCycle", "hc", new[] { "h11", "h12", "h23", "h24" });
                extensions.Option(options, "caseFirst", "kf", new[] { "upper", "lower", "false" });
                object numeric = options.Get("numeric");
                if (!IsUndefined(numeric)) extensions.Keywords["kn"] = Truthy(numeric) ? "" : "false";
                extensions.Option(options, "numberingSystem", "nu");
            }
            var selected = new List<string> { language };
            if (script != null) selected.Add(script);
            if (region != null) selected.Add(region);
            for (int i = next; i < baseEnd; i++) selected.Add(parts[i]);
            BaseName = LocaleIdentifier(string.Join('-', selected));
            Name = BaseName + extensions.Suffix();
            keywords = extensions.Keywords;
            parts = BaseName.Split('-'); next = 1; baseEnd = parts.Length;
            Language = parts[0];
            if (next < baseEnd && parts[next].Length == 4) Script = parts[next++];
            if (next < baseEnd && parts[next].Length is 2 or 3) Region = parts[next];
        }
    }
    static bool AsciiLetters(string text) { foreach (char value in text) if (!char.IsAsciiLetter(value)) return false; return true; }
    static bool AsciiDigits(string text) { foreach (char value in text) if (!char.IsAsciiDigit(value)) return false; return true; }
    static LocaleValue RequireLocale(object value) => value as LocaleValue ?? throw new InvalidOperationException("Locale receiver required");
    sealed class LocaleConstructor : NativeNamespaceFunction
    {
        internal LocaleConstructor() : base("Locale", static _ => throw new InvalidOperationException("Locale requires new"), 1)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.Locale", false);
            prototype.DefineMethod("toString", new CollectionMethod("toString", 0, static (receiver, _) => RequireLocale(receiver).Name));
            void Getter(string key, Func<LocaleValue, object> read) => prototype.DefineClassAccessor(key, new CollectionMethod("get " + key, 0, (receiver, _) => read(RequireLocale(receiver))), 1);
            Getter("baseName", value => value.BaseName); Getter("language", value => value.Language);
            Getter("script", value => value.Script ?? Undefined); Getter("region", value => value.Region ?? Undefined);
            foreach (var pair in new[] { ("calendar", "ca"), ("collation", "co"), ("firstDayOfWeek", "fw"), ("hourCycle", "hc"), ("caseFirst", "kf"), ("numberingSystem", "nu") }) Getter(pair.Item1, value => value.Keyword(pair.Item2));
            Getter("numeric", static value => BoxBoolean(value.Numeric));
            prototype.DefineMethod("getWeekInfo", new CollectionMethod("getWeekInfo", 0, static (receiver, _) => LocaleWeekInfo(RequireLocale(receiver))));
            prototype.DefineMethod("getNumberingSystems", new CollectionMethod("getNumberingSystems", 0, static (receiver, _) => LocaleNumberingSystems(RequireLocale(receiver))));
            prototype.DefineMethod("getCollations", new CollectionMethod("getCollations", 0, static (receiver, _) => LocaleCollations(RequireLocale(receiver))));
            prototype.DefineMethod("getTimeZones", new CollectionMethod("getTimeZones", 0, static (receiver, _) => LocaleTimeZones(RequireLocale(receiver))));
            prototype.DefineMethod("maximize", new CollectionMethod("maximize", 0, static (receiver, _) => LocaleSubtags(RequireLocale(receiver), false)));
            prototype.DefineMethod("minimize", new CollectionMethod("minimize", 0, static (receiver, _) => LocaleSubtags(RequireLocale(receiver), true)));
            prototype.DefineMethod("getCalendars", new CollectionMethod("getCalendars", 0, static (receiver, _) =>
            {
                var locale = RequireLocale(receiver);
                object calendar = locale.Keyword("ca");
                if (!IsUndefined(calendar)) return new JsArray(new[] { calendar });
                var result = new JsArray(0);
                var names = new HashSet<string>(StringComparer.Ordinal);
                var culture = LocaleCulture(locale.BaseName);
                void Add(System.Globalization.Calendar value)
                {
                    string name = DateLocaleSettings.NativeCalendarName(value);
                    if (name != null && names.Add(name)) result.Append(name);
                }
                Add(culture.DateTimeFormat.Calendar);
                foreach (var available in culture.OptionalCalendars) Add(available);
                return result;
            }));
            prototype.DefineMethod("getHourCycles", new CollectionMethod("getHourCycles", 0, static (receiver, _) =>
            {
                var locale = RequireLocale(receiver);
                object cycle = locale.Keyword("hc");
                if (IsUndefined(cycle))
                {
                    // The likely region for an undetermined language is US.
                    string pattern = LocaleCulture(locale.BaseName == "und" ? "en-US" : locale.BaseName).DateTimeFormat.ShortTimePattern;
                    bool quoted = false;
                    cycle = "h23";
                    for (int index = 0; index < pattern.Length; index++)
                    {
                        char character = pattern[index];
                        if (character == '\\') { index++; continue; }
                        if (character == '\'') { quoted = !quoted; continue; }
                        if (!quoted && character is 'H' or 'h') { cycle = character == 'h' ? "h12" : "h23"; break; }
                    }
                }
                return new JsArray(new[] { cycle });
            }));
            prototype.DefineMethod("getTextInfo", new CollectionMethod("getTextInfo", 0, static (receiver, _) =>
            {
                var locale = RequireLocale(receiver);
                var result = new JsDynamicObject();
                string script = locale.Script ?? ExpandLocaleSubtags(locale.Language, null, locale.Region).Script;
                bool rtl = script is
                    "Adlm" or "Arab" or "Armi" or "Avst" or "Cprt" or "Chrs" or "Elym" or "Gara" or "Hatr" or "Hebr" or "Hung" or
                    "Khar" or "Lydi" or "Mand" or "Mani" or "Mend" or "Merc" or "Mero" or "Narb" or "Nbat" or "Nkoo" or
                    "Orkh" or "Ougr" or "Palm" or "Phli" or "Phlp" or "Phnx" or "Prti" or "Rohg" or "Samr" or "Sarb" or
                    "Sidt" or "Sogd" or "Sogo" or "Syrc" or "Thaa" or "Yezi";
                result.DefineOwn("direction", rtl ? "rtl" : "ltr");
                return result;
            }));
        }
        public override object Construct(object[] args) => new LocaleValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);
            var result = (TsObject)Construct(args); result.SetPrototype(prototype); return result;
        }
    }
    static class IntlLocale { internal static readonly JsFunction Constructor = new LocaleConstructor(); }
}
