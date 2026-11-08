using System;
using System.Collections.Generic;
using System.Globalization;

public static partial class Js
{
    sealed class CollatorValue : JsDescriptorObject
    {
        readonly CollationSettings settings;
        internal readonly JsFunction Compare;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlCollator.Constructor.Get("prototype");
        internal CollatorValue(object locales, object optionValue)
        {
            settings = SelectCollationSettings(locales, optionValue);
            Compare = new JsFunction((FunctionBody)(args => Execute(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "", 2);
        }
        object Execute(object first, object second)
        {
            return settings.Compare(StringValue(first), StringValue(second));
        }
        internal TsObject Options()
        {
            var result = new JsDynamicObject();
            result.DefineOwn("locale", settings.Locale);
            result.DefineOwn("usage", settings.Usage); result.DefineOwn("sensitivity", settings.Sensitivity);
            result.DefineOwn("ignorePunctuation", BoxBoolean(settings.IgnorePunctuation));
            result.DefineOwn("collation", settings.Collation); result.DefineOwn("numeric", BoxBoolean(settings.Numeric)); result.DefineOwn("caseFirst", settings.CaseFirst);
            return result;
        }
    }
    static CollationSettings SelectCollationSettings(object locales, object optionValue)
    {
        var supported = (JsArray)CollatorSupportedLocales(locales, Undefined);
        string selected = supported.Length() == 0 ? null : (string)supported.ElementAt(0);
        bool? numeric = null;
        string collation = null, caseFirst = null;
        if (selected != null)
        {
            var locale = new LocaleValue(selected, Undefined);
            collation = locale.Keyword("co") as string;
            caseFirst = (locale.Keyword("kf") as string) switch { "upper" => "upper", "lower" => "lower", "false" => "false", _ => null };
            numeric = (locale.Keyword("kn") as string) switch { "true" => true, "false" => false, _ => null };
        }
        var culture = selected == null ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(CollatorLocale(selected));
        return new CollationSettings(culture, optionValue, numeric, collation, caseFirst);
    }
    static CollatorValue RequireCollator(object receiver) => receiver as CollatorValue ?? throw new InvalidOperationException("Collator receiver required");
    sealed class CollatorConstructor : NativeNamespaceFunction
    {
        internal CollatorConstructor() : base("Collator", static args => new CollatorValue(FunctionArgument(args, 0), FunctionArgument(args, 1)), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.Collator", false);
            prototype.DefineClassAccessor("compare", new CollectionMethod("get compare", 0, static (receiver, _) => RequireCollator(receiver).Compare), 1);
            prototype.DefineMethod("resolvedOptions", new CollectionMethod("resolvedOptions", 0, static (receiver, _) => RequireCollator(receiver).Options()));
            Member("supportedLocalesOf", new JsFunction((FunctionBody)(static args => CollatorSupportedLocales(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "supportedLocalesOf", 1));
        }
        public override object Construct(object[] args) => new CollatorValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);
            var result = (TsObject)Construct(args); result.SetPrototype(prototype); return result;
        }
    }
    static class IntlCollator
    {
        internal static readonly JsFunction Constructor = new CollatorConstructor();
    }
    static class CollatorLocales
    {
        static CollatorLocales() { }
        internal static readonly HashSet<string> Values = BuildLocales();
        static HashSet<string> BuildLocales()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures)) result.Add(culture.Name);
            return result;
        }
    }
    static string CollatorLocale(string name)
    {
        int extension = name.IndexOf("-u-", StringComparison.OrdinalIgnoreCase);
        if (extension >= 0) name = name[..extension];
        while (!CollatorLocales.Values.Contains(name))
        {
            // Known aliases such as zh-CN need not appear in GetCultures.
            // Custom (0x1000) and invariant (0x7f) IDs do not prove support.
            try {
                var alias = CultureInfo.GetCultureInfo(name);
                if (alias.LCID is not (0x1000 or 0x7f)) return alias.Name;
            } catch (CultureNotFoundException) { }
            int dash = name.LastIndexOf('-'); if (dash < 0) return null;
            name = name[..dash];
        }
        return name;
    }
    static object CollatorSupportedLocales(object locales, object optionValue)
    {
        var requested = (JsArray)CanonicalLocales(locales);
        if (!IsUndefined(optionValue))
        {
            object matcher = ToObject(optionValue).Get("localeMatcher");
            if (!IsUndefined(matcher) && StringValue(matcher) is not ("lookup" or "best fit"))
                throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale matcher");
        }
        var result = new JsArray(0);
        for (uint index = 0; index < requested.Length(); index++)
        {
            string name = (string)requested.ElementAt(index);
            if (CollatorLocale(name) != null) result.Append(name);
        }
        return result;
    }
    static TsObject CreateIntlNamespace()
    {
        var result = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype };
        result.DefineMethod("getCanonicalLocales", new JsFunction((FunctionBody)(static args => CanonicalLocales(FunctionArgument(args, 0))), null, "getCanonicalLocales", 1));
        result.DefineMethod("Locale", IntlLocale.Constructor);
        result.DefineMethod("Collator", IntlCollator.Constructor);
        result.DefineMethod("NumberFormat", IntlNumberFormat.Constructor);
        result.DefineMethod("DateTimeFormat", IntlDateTimeFormat.Constructor);
        result.DefineMethod("Segmenter", IntlSegmenter.Constructor);
        result.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl", false);
        return result;
    }
}
