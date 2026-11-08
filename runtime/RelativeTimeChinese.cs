using System;

public static partial class Js
{
    static bool TraditionalRelativeTime(string locale) => locale.StartsWith("zh-TW", StringComparison.Ordinal) || locale.StartsWith("zh-Hant", StringComparison.Ordinal);
    static (string Prefix, string Suffix) ChineseRelativeTimeAffixes(string locale, string style, string unit, bool past)
    {
        bool traditional = TraditionalRelativeTime(locale);
        string noun = (traditional, unit) switch {
            (false, "year") => "\u5e74",
            (false, "quarter") => "\u4e2a\u5b63\u5ea6",
            (false, "month") => "\u4e2a\u6708",
            (false, "week") => "\u5468",
            (false, "day") => "\u5929",
            (false, "hour") => "\u5c0f\u65f6",
            (false, "minute") => "\u5206\u949f",
            (false, "second") => "\u79d2\u949f",
            (true, "year") => "\u5e74",
            (true, "quarter") => "\u5b63",
            (true, "month") => "\u500b\u6708",
            (true, "week") => "\u9031",
            (true, "day") => "\u5929",
            (true, "hour") => "\u5c0f\u6642",
            (true, "minute") => "\u5206\u9418",
            (true, "second") => "\u79d2",
            _ => throw new InvalidOperationException("Invalid relative-time unit")
        };
        if (!traditional && unit == "second" && style != "long") noun = "\u79d2";
        return ("", (traditional ? " " : "") + noun + (past ? "\u524d" : traditional ? "\u5f8c" : "\u540e"));
    }
    static string ChineseRelativeTimeAutomatic(string locale, double value, string unit)
    {
        if (value != Math.Truncate(value) || value < -2 || value > 2) return null;
        return (TraditionalRelativeTime(locale), unit, (int)value) switch {
            (false, "year", -1) => "\u53bb\u5e74",
            (false, "year", 0) => "\u4eca\u5e74",
            (false, "year", 1) => "\u660e\u5e74",
            (false, "quarter", -1) => "\u4e0a\u5b63\u5ea6",
            (false, "quarter", 0) => "\u672c\u5b63\u5ea6",
            (false, "quarter", 1) => "\u4e0b\u5b63\u5ea6",
            (false, "month", -1) => "\u4e0a\u4e2a\u6708",
            (false, "month", 0) => "\u672c\u6708",
            (false, "month", 1) => "\u4e0b\u4e2a\u6708",
            (false, "week", -1) => "\u4e0a\u5468",
            (false, "week", 0) => "\u672c\u5468",
            (false, "week", 1) => "\u4e0b\u5468",
            (false, "day", -2) => "\u524d\u5929",
            (false, "day", -1) => "\u6628\u5929",
            (false, "day", 0) => "\u4eca\u5929",
            (false, "day", 1) => "\u660e\u5929",
            (false, "day", 2) => "\u540e\u5929",
            (false, "hour", 0) => "\u8fd9\u4e00\u65f6\u95f4 / \u6b64\u65f6",
            (false, "minute", 0) => "\u6b64\u523b",
            (false, "second", 0) => "\u73b0\u5728",
            (true, "year", -1) => "\u53bb\u5e74",
            (true, "year", 0) => "\u4eca\u5e74",
            (true, "year", 1) => "\u660e\u5e74",
            (true, "quarter", -1) => "\u4e0a\u4e00\u5b63",
            (true, "quarter", 0) => "\u9019\u4e00\u5b63",
            (true, "quarter", 1) => "\u4e0b\u4e00\u5b63",
            (true, "month", -1) => "\u4e0a\u500b\u6708",
            (true, "month", 0) => "\u672c\u6708",
            (true, "month", 1) => "\u4e0b\u500b\u6708",
            (true, "week", -1) => "\u4e0a\u9031",
            (true, "week", 0) => "\u672c\u9031",
            (true, "week", 1) => "\u4e0b\u9031",
            (true, "day", -2) => "\u524d\u5929",
            (true, "day", -1) => "\u6628\u5929",
            (true, "day", 0) => "\u4eca\u5929",
            (true, "day", 1) => "\u660e\u5929",
            (true, "day", 2) => "\u5f8c\u5929",
            (true, "hour", 0) => "\u9019\u4e00\u5c0f\u6642",
            (true, "minute", 0) => "\u9019\u4e00\u5206\u9418",
            (true, "second", 0) => "\u73fe\u5728",
            _ => null
        };
    }
}
