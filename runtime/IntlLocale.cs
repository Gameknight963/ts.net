using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class LocaleValue : TsObject
    {
        internal readonly string Name, Language, Script, Region;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlLocale.Constructor.Get("prototype");
        internal LocaleValue(object tag, object optionValue)
        {
            string name = LocaleIdentifier(tag);
            var parts = name.Split('-');
            int next = 1;
            string language = parts[0], script = null, region = null;
            if (next < parts.Length && parts[next].Length == 4) script = parts[next++];
            if (next < parts.Length && parts[next].Length is 2 or 3) region = parts[next++];
            for (int i = next; i < parts.Length; i++)
                if (parts[i].Length == 1) throw new NotSupportedException("Locale extensions are not supported");
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
                foreach (string key in new[] { "calendar", "collation", "hourCycle", "caseFirst", "numeric", "numberingSystem" })
                    if (!IsUndefined(options.Get(key))) throw new NotSupportedException("Locale option " + key + " is not supported");
            }
            var selected = new List<string> { language };
            if (script != null) selected.Add(script);
            if (region != null) selected.Add(region);
            for (int i = next; i < parts.Length; i++) selected.Add(parts[i]);
            Name = LocaleIdentifier(string.Join('-', selected));
            parts = Name.Split('-'); next = 1;
            Language = parts[0];
            if (next < parts.Length && parts[next].Length == 4) Script = parts[next++];
            if (next < parts.Length && parts[next].Length is 2 or 3) Region = parts[next];
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
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.Locale");
            prototype.DefineMethod("toString", new CollectionMethod("toString", 0, static (receiver, _) => RequireLocale(receiver).Name));
            void Getter(string key, Func<LocaleValue, object> read) => prototype.DefineClassAccessor(key, new CollectionMethod("get " + key, 0, (receiver, _) => read(RequireLocale(receiver))), 1);
            Getter("baseName", value => value.Name); Getter("language", value => value.Language);
            Getter("script", value => value.Script ?? Undefined); Getter("region", value => value.Region ?? Undefined);
            foreach (string key in new[] { "calendar", "collation", "hourCycle", "caseFirst", "numberingSystem" }) Getter(key, static _ => Undefined);
            Getter("numeric", static _ => BoxBoolean(false));
        }
        public override object Construct(object[] args) => new LocaleValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var result = (TsObject)Construct(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result;
        }
    }
    static class IntlLocale { internal static readonly JsFunction Constructor = new LocaleConstructor(); }
}
