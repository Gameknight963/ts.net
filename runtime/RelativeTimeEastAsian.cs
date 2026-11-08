using System;

public static partial class Js
{
    static bool EastAsianRelativeTime(string locale) => locale.StartsWith("ja", StringComparison.Ordinal) || locale.StartsWith("ko", StringComparison.Ordinal);
    static (string Prefix, string Suffix) EastAsianRelativeTimeAffixes(string locale, string style, string unit, bool past)
    {
        bool japanese = locale.StartsWith("ja", StringComparison.Ordinal);
        string noun = (japanese, unit) switch {
            (true, "year") => "\u5e74",
            (true, "quarter") => "\u56db\u534a\u671f",
            (true, "month") => "\u304b\u6708",
            (true, "week") => "\u9031\u9593",
            (true, "day") => "\u65e5",
            (true, "hour") => "\u6642\u9593",
            (true, "minute") => "\u5206",
            (true, "second") => "\u79d2",
            (false, "year") => "\ub144",
            (false, "quarter") => "\ubd84\uae30",
            (false, "month") => "\uac1c\uc6d4",
            (false, "week") => "\uc8fc",
            (false, "day") => "\uc77c",
            (false, "hour") => "\uc2dc\uac04",
            (false, "minute") => "\ubd84",
            (false, "second") => "\ucd08",
            _ => throw new InvalidOperationException("Invalid relative-time unit")
        };
        string gap = japanese && style != "narrow" ? " " : "";
        string direction = japanese ? past ? "\u524d" : "\u5f8c" : past ? " \uc804" : " \ud6c4";
        return ("", gap + noun + direction);
    }
    static string EastAsianRelativeTimeAutomatic(string locale, double value, string unit)
    {
        if (value != Math.Truncate(value) || value < -2 || value > 2) return null;
        return (locale.StartsWith("ja", StringComparison.Ordinal), unit, (int)value) switch {
            (true, "year", -1) => "\u6628\u5e74",
            (true, "year", 0) => "\u4eca\u5e74",
            (true, "year", 1) => "\u6765\u5e74",
            (true, "quarter", -1) => "\u524d\u56db\u534a\u671f",
            (true, "quarter", 0) => "\u4eca\u56db\u534a\u671f",
            (true, "quarter", 1) => "\u7fcc\u56db\u534a\u671f",
            (true, "month", -1) => "\u5148\u6708",
            (true, "month", 0) => "\u4eca\u6708",
            (true, "month", 1) => "\u6765\u6708",
            (true, "week", -1) => "\u5148\u9031",
            (true, "week", 0) => "\u4eca\u9031",
            (true, "week", 1) => "\u6765\u9031",
            (true, "day", -2) => "\u4e00\u6628\u65e5",
            (true, "day", -1) => "\u6628\u65e5",
            (true, "day", 0) => "\u4eca\u65e5",
            (true, "day", 1) => "\u660e\u65e5",
            (true, "day", 2) => "\u660e\u5f8c\u65e5",
            (true, "hour", 0) => "1 \u6642\u9593\u4ee5\u5185",
            (true, "minute", 0) => "1 \u5206\u4ee5\u5185",
            (true, "second", 0) => "\u4eca",
            (false, "year", -1) => "\uc791\ub144",
            (false, "year", 0) => "\uc62c\ud574",
            (false, "year", 1) => "\ub0b4\ub144",
            (false, "quarter", -1) => "\uc9c0\ub09c \ubd84\uae30",
            (false, "quarter", 0) => "\uc774\ubc88 \ubd84\uae30",
            (false, "quarter", 1) => "\ub2e4\uc74c \ubd84\uae30",
            (false, "month", -1) => "\uc9c0\ub09c\ub2ec",
            (false, "month", 0) => "\uc774\ubc88 \ub2ec",
            (false, "month", 1) => "\ub2e4\uc74c \ub2ec",
            (false, "week", -1) => "\uc9c0\ub09c\uc8fc",
            (false, "week", 0) => "\uc774\ubc88 \uc8fc",
            (false, "week", 1) => "\ub2e4\uc74c \uc8fc",
            (false, "day", -2) => "\uadf8\uc800\uaed8",
            (false, "day", -1) => "\uc5b4\uc81c",
            (false, "day", 0) => "\uc624\ub298",
            (false, "day", 1) => "\ub0b4\uc77c",
            (false, "day", 2) => "\ubaa8\ub808",
            (false, "hour", 0) => "\ud604\uc7ac \uc2dc\uac04",
            (false, "minute", 0) => "\ud604\uc7ac \ubd84",
            (false, "second", 0) => "\uc9c0\uae08",
            _ => null
        };
    }
}
