using System;
using System.Globalization;

public static partial class Js
{
    sealed class CollationSettings
    {
        internal readonly CultureInfo Culture;
        internal readonly string Usage, Sensitivity, CaseFirst, Collation, Locale;
        internal readonly bool Numeric, IgnorePunctuation;
        readonly CompareInfo comparer;
        readonly CompareOptions flags;
        readonly bool reverseCaseTies;
        internal CollationSettings(CultureInfo culture, object optionValue, bool? numericDefault = null)
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
            string collation = Option("collation", "default");
            if (!ValidCollation(collation)) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid collation identifier");
            collation = collation.ToLowerInvariant();
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
            object numericOption = options == null ? Undefined : options.Get("numeric");
            Numeric = IsUndefined(numericOption) ? numericDefault ?? false : Truthy(numericOption);
            IgnorePunctuation = options != null && Truthy(options.Get("ignorePunctuation"));
            if (Numeric) flags |= CompareOptions.NumericOrdering;
            bool configure = caseFirst != null || Usage == "search" || collation != "default";
            Culture = configure && culture.Name.Length == 0 ? CultureInfo.GetCultureInfo("en-US") : culture;
            Collation = Usage == "search" ? "default" : SelectCollation(Culture.TwoLetterISOLanguageName, collation);
            Locale = Culture.Name + (numericDefault.HasValue ? numericDefault.Value ? "-u-kn" : "-u-kn-false" : "");
            configure = caseFirst != null || Usage == "search" || Collation != "default";
            comparer = configure ? NativeComparer(Culture, Usage, caseFirst, Collation) : Culture.CompareInfo;
            CaseFirst = caseFirst ?? (comparer.Compare("A", "a") < 0 ? "upper" : "false");
            // IgnoreNonSpace can reset ICU's case ordering. Correct only ties at
            // the case-insensitive comparison level, preserving other weights.
            int casing = Math.Sign(comparer.Compare("A", "a", flags));
            reverseCaseTies = casing != 0 && (casing < 0) != (CaseFirst == "upper");
        }
        static bool ValidCollation(string value)
        {
            int length = 0;
            foreach (char ch in value) {
                if (ch == '-') { if (length < 3 || length > 8) return false; length = 0; }
                else { if (!char.IsAsciiLetterOrDigit(ch)) return false; length++; }
            }
            return length >= 3 && length <= 8;
        }
        static string SelectCollation(string language, string requested) => requested switch {
            "default" or "standard" or "search" => "default",
            "phonebk" => language == "de" ? requested : "default",
            "trad" => language == "es" ? requested : "default",
            "stroke" or "zhuyin" => language == "zh" ? requested : "default",
            _ => throw new NotSupportedException("Custom collations are not supported for this locale")
        };
        static CompareInfo NativeComparer(CultureInfo culture, string usage, string caseFirst, string collation)
        {
            if (culture.Name.Contains("-u-", StringComparison.OrdinalIgnoreCase) || culture.CompareInfo.Name.Contains('_'))
                throw new NotSupportedException("Combining collation options with locale extensions is not supported");
            string sortLocale = collation != "default" && culture.IsNeutralCulture ? CultureInfo.CreateSpecificCulture(culture.Name).Name : culture.Name;
            string name = sortLocale + "-u";
            if (usage == "search") name += "-co-search";
            else if (collation != "default") name += "-co-" + collation;
            if (caseFirst != null) name += "-kf-" + caseFirst;
            CompareInfo result;
            try { result = CultureInfo.GetCultureInfo(name).CompareInfo; }
            catch (CultureNotFoundException) { throw new NotSupportedException("Native globalization does not support the requested collation"); }
            if (usage == "search" && !result.Name.Contains("search", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Native globalization does not support search collations");
            string marker = collation switch { "phonebk" => "_phoneb", "trad" => "_trad", "stroke" => "_stroke", "zhuyin" => "_zhuyin", _ => null };
            if (marker != null && !result.Name.Contains(marker, StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Native globalization does not support the requested named collation");
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
