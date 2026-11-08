using System;

public static partial class Js
{
    readonly record struct ListPattern(string Pair, string Middle, string End);
    static ListPattern ListPatternFor(string locale, string type, string style)
    {
        static ListPattern Uniform(string separator) => new(separator, separator, separator);
        static ListPattern Ending(string separator) => new(separator, ", ", separator);
        if (locale.StartsWith("de", StringComparison.Ordinal))
            return type == "unit" ? new(", ", ", ", " und ") : Ending(type == "disjunction" ? " oder " : " und ");
        if (locale.StartsWith("fr", StringComparison.Ordinal)) {
            if (type == "unit" && style == "narrow") return Uniform(" ");
            if (type == "conjunction" && style == "narrow") return Uniform(", ");
            return Ending(type == "disjunction" ? " ou " : " et ");
        }
        if (locale.StartsWith("ru", StringComparison.Ordinal)) {
            if (type == "unit") return Uniform(" ");
            if (type == "conjunction" && style == "narrow") return Uniform(", ");
            return Ending(type == "disjunction" ? " \u0438\u043b\u0438 " : " \u0438 ");
        }
        if (locale.StartsWith("ja", StringComparison.Ordinal)) {
            if (type == "unit") return Uniform(style == "narrow" ? "" : " ");
            return type == "disjunction" ? new("\u307e\u305f\u306f", "\u3001", "\u3001\u307e\u305f\u306f") : Uniform("\u3001");
        }
        if (locale.StartsWith("ko", StringComparison.Ordinal)) {
            if (type == "unit") return Uniform(" ");
            return Ending(type == "disjunction" ? " \ub610\ub294 " : " \ubc0f ");
        }
        if (locale.StartsWith("zh", StringComparison.Ordinal)) {
            bool traditional = locale is "zh-TW" or "zh-Hant";
            if (type == "unit") return Uniform(traditional && style != "narrow" ? " " : "");
            if (type == "disjunction") return new("\u6216", "\u3001", "\u6216");
            if (style == "narrow") return traditional ? new("\u3001", "\u3001", "\u548c") : Uniform("\u3001");
            return new("\u548c", "\u3001", "\u548c");
        }
        if (type == "unit") return Uniform(style == "narrow" ? " " : ", ");
        if (type == "conjunction" && style == "narrow") return Uniform(", ");
        string word = type == "disjunction" ? "or" : style == "short" && locale != "en-GB" ? "&" : "and";
        return new(" " + word + " ", ", ", (locale == "en-GB" ? " " : ", ") + word + " ");
    }
}
