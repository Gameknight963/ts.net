using System;
using System.Collections.Generic;
using System.Globalization;

public static partial class Js
{
    sealed class CollatorValue : TsObject
    {
        readonly CollationSettings settings;
        internal readonly JsFunction Compare;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlCollator.Constructor.Get("prototype");
        internal CollatorValue(object locales, object optionValue)
        {
            var supported = (JsArray)CollatorSupportedLocales(locales, Undefined);
            string selected = supported.Length() == 0 ? null : (string)supported.ElementAt(0);
            if (selected != null && selected.Contains("-u-", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Collator Unicode locale extensions are not supported");
            var culture = selected == null ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(CollatorLocale(selected));
            settings = new CollationSettings(culture, optionValue);
            Compare = new JsFunction((FunctionBody)(args => Execute(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "", 2);
        }
        object Execute(object first, object second)
        {
            return settings.Compare(StringValue(first), StringValue(second));
        }
        internal TsObject Options()
        {
            var result = new JsDynamicObject();
            result.DefineOwn("locale", settings.Culture.Name);
            result.DefineOwn("usage", settings.Usage); result.DefineOwn("sensitivity", settings.Sensitivity);
            result.DefineOwn("ignorePunctuation", BoxBoolean(settings.IgnorePunctuation));
            result.DefineOwn("collation", settings.Collation); result.DefineOwn("numeric", BoxBoolean(settings.Numeric)); result.DefineOwn("caseFirst", settings.CaseFirst);
            return result;
        }
    }
    static CollatorValue RequireCollator(object receiver) => receiver as CollatorValue ?? throw new InvalidOperationException("Collator receiver required");
    sealed class CollatorConstructor : NativeNamespaceFunction
    {
        internal CollatorConstructor() : base("Collator", static args => new CollatorValue(FunctionArgument(args, 0), FunctionArgument(args, 1)), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.Collator");
            prototype.DefineClassAccessor("compare", new CollectionMethod("get compare", 0, static (receiver, _) => RequireCollator(receiver).Compare), 1);
            prototype.DefineMethod("resolvedOptions", new CollectionMethod("resolvedOptions", 0, static (receiver, _) => RequireCollator(receiver).Options()));
            Member("supportedLocalesOf", new JsFunction((FunctionBody)(static args => CollatorSupportedLocales(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "supportedLocalesOf", 1));
        }
        public override object Construct(object[] args) => new CollatorValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var result = (TsObject)Construct(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result;
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
        if (!IsUndefined(optionValue))
        {
            object matcher = ToObject(optionValue).Get("localeMatcher");
            if (!IsUndefined(matcher) && StringValue(matcher) is not ("lookup" or "best fit"))
                throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale matcher");
        }
        var result = new JsArray(0); var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(object value)
        {
            if (value is not string && value is not TsObject) throw new InvalidOperationException("Locale must be a string or object");
            string name = CaseCulture(StringValue(value)).Name;
            if (CollatorLocale(name) != null && seen.Add(name)) result.Append(name);
        }
        if (IsUndefined(locales)) return result;
        if (locales is string) Add(locales);
        else
        {
            var list = ToObject(locales); long length = IndexedLength(list);
            for (long i = 0; i < length; i++) if (TryIndexedValue(list, i, out object value)) Add(value);
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
        result.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl");
        return result;
    }
}
