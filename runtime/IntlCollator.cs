using System;
using System.Collections.Generic;
using System.Globalization;

public static partial class Js
{
    sealed class CollatorValue : TsObject
    {
        internal readonly CultureInfo Culture;
        internal readonly string Usage, Sensitivity;
        internal readonly bool Numeric, IgnorePunctuation;
        readonly CompareOptions flags;
        readonly CompareInfo comparer;
        internal readonly JsFunction Compare;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlCollator.Constructor.Get("prototype");
        internal CollatorValue(object locales, object optionValue)
        {
            var supported = (JsArray)CollatorSupportedLocales(locales, Undefined);
            string selected = supported.Length() == 0 ? null : (string)supported.ElementAt(0);
            if (selected != null && selected.Contains("-u-", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Collator Unicode locale extensions are not supported");
            Culture = selected == null ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(CollatorLocale(selected));
            comparer = Culture.CompareInfo;
            TsObject options = IsUndefined(optionValue) ? null : ToObject(optionValue);
            string Option(string name, string fallback)
            {
                object value = options == null ? Undefined : options.Get(name);
                return IsUndefined(value) ? fallback : StringValue(value);
            }
            string matcher = Option("localeMatcher", "best fit");
            if (matcher is not ("lookup" or "best fit")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale matcher");
            Usage = Option("usage", "sort");
            if (Usage is not ("sort" or "search")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid collator usage");
            string collation = Option("collation", "default");
            if (collation != "default") throw new NotSupportedException("Custom collations are not supported");
            string caseFirst = Option("caseFirst", "false");
            if (caseFirst is not ("false" or "upper" or "lower")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid case order");
            if (caseFirst != "false") throw new NotSupportedException("Custom case order is not supported");
            Sensitivity = Option("sensitivity", "variant");
            flags = Sensitivity switch
            {
                "base" => CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace,
                "accent" => CompareOptions.IgnoreCase,
                "case" => CompareOptions.IgnoreNonSpace,
                "variant" => CompareOptions.None,
                _ => throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid comparison sensitivity")
            };
            Numeric = options != null && Truthy(options.Get("numeric"));
            IgnorePunctuation = options != null && Truthy(options.Get("ignorePunctuation"));
            if (Numeric) flags |= CompareOptions.NumericOrdering;
            Compare = new JsFunction((FunctionBody)(args => Execute(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "", 2);
        }
        object Execute(object first, object second)
        {
            string left = StringValue(first), right = StringValue(second);
            if (IgnorePunctuation) { left = WithoutComparisonPunctuation(left); right = WithoutComparisonPunctuation(right); }
            return (double)Math.Sign(comparer.Compare(left, right, flags));
        }
        internal TsObject Options()
        {
            var result = new JsDynamicObject();
            result.DefineOwn("locale", Culture.Name);
            result.DefineOwn("usage", Usage); result.DefineOwn("sensitivity", Sensitivity);
            result.DefineOwn("ignorePunctuation", BoxBoolean(IgnorePunctuation));
            result.DefineOwn("collation", "default"); result.DefineOwn("numeric", BoxBoolean(Numeric)); result.DefineOwn("caseFirst", "false");
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
        result.DefineMethod("Collator", IntlCollator.Constructor);
        result.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl");
        return result;
    }
}
