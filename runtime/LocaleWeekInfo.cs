using System;
using System.Globalization;

public static partial class Js
{
    static (int FirstDay, int WeekendStart, int WeekendEnd) LocaleWeekRules(string region) => region switch {
        "AF" => (6, 4, 5),
        "AG" or "AS" or "BD" or "BR" or "BS" or "BT" or "BU" or "BW" or "BZ" or "CA" or "CO" or "DM" or "DO" or "ET" or "GT" or "GU" or "HK" or "HN" or "ID" or "IS" or "JM" or "JP" or "JT" or "KE" or "KH" or "KR" or "LA" or "MH" or "MI" or "MM" or "MO" or "MT" or "MX" or "MZ" or "NI" or "NP" or "PA" or "PE" or "PH" or "PK" or "PR" or "PT" or "PU" or "PY" or "PZ" or "RH" or "SG" or "SV" or "TH" or "TT" or "TW" or "UM" or "US" or "VE" or "VI" or "WK" or "WS" or "ZA" or "ZW" => (7, 6, 7),
        "BH" or "DZ" or "EG" or "IQ" or "JO" or "KW" or "LY" or "OM" or "QA" or "SD" or "SY" => (6, 5, 6),
        "DJ" => (6, 6, 7),
        "IL" or "NT" or "SA" or "YD" or "YE" => (7, 5, 6),
        "IN" => (7, 7, 7),
        "IR" => (6, 5, 5),
        "MV" => (5, 6, 7),
        "UG" => (1, 7, 7),
        _ => (1, 6, 7)
    };
    static string LocaleRegionOverride(LocaleValue locale, string key)
    {
        if (locale.Keyword(key) is not string value) return null;
        int width = value.Length > 0 && char.IsAsciiDigit(value[0]) ? 3 : 2;
        if (value.Length <= width || key == "rg" && value[width..] != "zzzz") return null;
        for (int i = 0; i < value.Length; i++) if (!char.IsAsciiLetterOrDigit(value[i])) return null;
        for (int i = 0; i < width; i++) if (width == 3 ? !char.IsAsciiDigit(value[i]) : !char.IsAsciiLetter(value[i])) return null;
        try {
            string region = new LocaleValue("und-" + value[..width], Undefined).Region;
            if (LocaleSubtagRegionAliases(region) is string aliases) region = aliases.Split(' ')[0];
            if (key == "rg" && region != "001") _ = new RegionInfo(region);
            return region;
        }
        catch (ArgumentException) { return null; }
    }
    static object LocaleWeekInfo(LocaleValue locale)
    {
        string region = LocaleRegionOverride(locale, "rg") ?? locale.Region ?? LocaleRegionOverride(locale, "sd");
        region ??= ExpandLocaleSubtags(locale.Language, locale.Script, null).Region ?? "001";
        var rules = LocaleWeekRules(region);
        int firstDay = locale.Keyword("fw") switch {"mon" => 1, "tue" => 2, "wed" => 3, "thu" => 4, "fri" => 5, "sat" => 6, "sun" => 7, _ => rules.FirstDay};
        var weekend = new JsArray(0);
        for (int day = rules.WeekendStart; day <= rules.WeekendEnd; day++) weekend.Append((double)day);
        var result = new JsDynamicObject(); result.DefineOwn("firstDay", (double)firstDay); result.DefineOwn("weekend", weekend);
        return result;
    }
}
