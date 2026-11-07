using System;
using System.Globalization;

public static partial class Js
{
    sealed class CollationSettings
    {
        internal readonly CultureInfo Culture;
        internal readonly string Usage, Sensitivity, CaseFirst;
        internal readonly bool Numeric, IgnorePunctuation;
        readonly CompareInfo comparer;
        readonly CompareOptions flags;
        readonly bool reverseCaseTies;
        internal CollationSettings(CultureInfo culture, object optionValue)
        {
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
            if (Option("collation", "default") != "default") throw new NotSupportedException("Custom collations are not supported");
            string caseFirst = Option("caseFirst", null);
            if (caseFirst is not (null or "false" or "upper" or "lower")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid case order");
            Sensitivity = Option("sensitivity", "variant");
            flags = Sensitivity switch {
                "base" => CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace,
                "accent" => CompareOptions.IgnoreCase,
                "case" => CompareOptions.IgnoreNonSpace,
                "variant" => CompareOptions.None,
                _ => throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid comparison sensitivity")
            };
            Numeric = options != null && Truthy(options.Get("numeric"));
            IgnorePunctuation = options != null && Truthy(options.Get("ignorePunctuation"));
            if (Numeric) flags |= CompareOptions.NumericOrdering;
            bool configure = caseFirst != null || Usage == "search";
            Culture = configure && culture.Name.Length == 0 ? CultureInfo.GetCultureInfo("en-US") : culture;
            comparer = configure ? NativeComparer(Culture, Usage, caseFirst) : Culture.CompareInfo;
            CaseFirst = caseFirst ?? (comparer.Compare("A", "a") < 0 ? "upper" : "false");
            // IgnoreNonSpace can reset ICU's case ordering. Correct only ties at
            // the case-insensitive comparison level, preserving other weights.
            int casing = Math.Sign(comparer.Compare("A", "a", flags));
            reverseCaseTies = casing != 0 && (casing < 0) != (CaseFirst == "upper");
        }
        static CompareInfo NativeComparer(CultureInfo culture, string usage, string caseFirst)
        {
            if (culture.Name.Contains("-u-", StringComparison.OrdinalIgnoreCase) || culture.CompareInfo.Name.Contains('_'))
                throw new NotSupportedException("Combining collation options with locale extensions is not supported");
            string name = culture.Name + "-u";
            if (usage == "search") name += "-co-search";
            if (caseFirst != null) name += "-kf-" + caseFirst;
            CompareInfo result;
            try { result = CultureInfo.GetCultureInfo(name).CompareInfo; }
            catch (CultureNotFoundException) { throw new NotSupportedException("Native globalization does not support the requested collation"); }
            if (usage == "search" && !result.Name.Contains("search", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Native globalization does not support search collations");
            if (caseFirst != null && (result.Compare("A", "a") < 0) != (caseFirst == "upper"))
                throw new NotSupportedException("Native globalization does not support the requested case order");
            return result;
        }
        internal double Compare(string left, string right)
        {
            if (IgnorePunctuation) { left = WithoutComparisonPunctuation(left); right = WithoutComparisonPunctuation(right); }
            int result = Math.Sign(comparer.Compare(left, right, flags));
            return reverseCaseTies && result != 0 && comparer.Compare(left, right, flags | CompareOptions.IgnoreCase) == 0 ? -result : result;
        }
    }
}
