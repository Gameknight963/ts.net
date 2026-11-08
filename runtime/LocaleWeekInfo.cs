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
    static string LocaleLikelyRegion(string language) => language switch {
        "aa" or "am" or "om" or "ti" => "ET",
        "ab" or "ka" or "os" => "GE",
        "ae" or "fa" => "IR",
        "af" or "nr" or "ss" or "st" or "tn" or "ts" or "ve" or "xh" or "zu" => "ZA",
        "ak" or "ee" or "tw" => "GH",
        "an" or "ca" or "es" or "eu" or "gl" => "ES",
        "ar" => "EG",
        "as" or "bh" or "gu" or "hi" or "kn" or "ks" or "ml" or "mr" or "or" or "pa" or "sa" or "ta" or "te" => "IN",
        "av" or "ba" or "ce" or "cu" or "cv" or "kv" or "ru" or "tt" => "RU",
        "ay" => "BO",
        "az" => "AZ",
        "be" => "BY",
        "bg" => "BG",
        "bi" => "VU",
        "bm" => "ML",
        "bn" => "BD",
        "bo" or "ii" or "ug" or "za" or "zh" => "CN",
        "br" or "co" or "fr" or "oc" => "FR",
        "bs" => "BA",
        "ch" => "GU",
        "cr" or "iu" or "oj" => "CA",
        "cs" => "CZ",
        "cy" or "gd" or "kw" or "pi" => "GB",
        "da" => "DK",
        "de" => "DE",
        "dv" => "MV",
        "dz" => "BT",
        "el" => "GR",
        "en" or "ik" or "nv" => "US",
        "eo" or "ia" or "io" or "vo" => "001",
        "et" or "ie" => "EE",
        "ff" or "wo" => "SN",
        "fi" => "FI",
        "fj" => "FJ",
        "fo" => "FO",
        "fy" or "li" or "nl" => "NL",
        "ga" => "IE",
        "gn" => "PY",
        "gv" => "IM",
        "ha" or "ig" or "kr" or "yo" => "NG",
        "he" or "iw" => "IL",
        "ho" => "PG",
        "hr" => "HR",
        "ht" => "HT",
        "hu" => "HU",
        "hy" => "AM",
        "hz" or "kj" or "ng" => "NA",
        "id" or "in" or "jv" or "jw" or "su" => "ID",
        "is" => "IS",
        "it" or "sc" => "IT",
        "ja" => "JP",
        "ji" or "uk" or "yi" => "UA",
        "kg" or "ln" or "lu" => "CD",
        "ki" => "KE",
        "kk" => "KZ",
        "kl" => "GL",
        "km" => "KH",
        "ko" => "KR",
        "ku" or "tr" => "TR",
        "ky" => "KG",
        "la" => "VA",
        "lb" => "LU",
        "lg" => "UG",
        "lo" => "LA",
        "lt" => "LT",
        "lv" => "LV",
        "mg" => "MG",
        "mh" => "MH",
        "mi" => "NZ",
        "mk" => "MK",
        "mn" => "MN",
        "mo" or "ro" => "RO",
        "ms" => "MY",
        "mt" => "MT",
        "my" => "MM",
        "na" => "NR",
        "nb" or "nn" or "no" or "se" => "NO",
        "nd" or "sn" => "ZW",
        "ne" => "NP",
        "ny" => "MW",
        "pl" => "PL",
        "ps" => "AF",
        "pt" => "BR",
        "qu" => "PE",
        "rm" => "CH",
        "rn" => "BI",
        "rw" => "RW",
        "sd" or "ur" => "PK",
        "sg" => "CF",
        "sh" or "sr" => "RS",
        "si" => "LK",
        "sk" => "SK",
        "sl" => "SI",
        "sm" => "WS",
        "so" => "SO",
        "sq" => "AL",
        "sv" => "SE",
        "sw" => "TZ",
        "tg" => "TJ",
        "th" => "TH",
        "tk" => "TM",
        "tl" => "PH",
        "to" => "TO",
        "ty" => "PF",
        "uz" => "UZ",
        "vi" => "VN",
        "wa" => "BE",
        "und" => "US", _ => "001"
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
            if (key == "rg" && region != "001") _ = new RegionInfo(region);
            return region;
        }
        catch (ArgumentException) { return null; }
    }
    static object LocaleWeekInfo(LocaleValue locale)
    {
        string region = LocaleRegionOverride(locale, "rg") ?? locale.Region ?? LocaleRegionOverride(locale, "sd");
        if (region == null) {
            region = LocaleLikelyRegion(locale.Language);
            if (locale.Script != null) {
                try {region = new RegionInfo(CultureInfo.CreateSpecificCulture(locale.BaseName).Name).TwoLetterISORegionName;}
                catch (ArgumentException) { }
            }
        }
        var rules = LocaleWeekRules(region);
        int firstDay = locale.Keyword("fw") switch {"mon" => 1, "tue" => 2, "wed" => 3, "thu" => 4, "fri" => 5, "sat" => 6, "sun" => 7, _ => rules.FirstDay};
        var weekend = new JsArray(0);
        for (int day = rules.WeekendStart; day <= rules.WeekendEnd; day++) weekend.Append((double)day);
        var result = new JsDynamicObject(); result.DefineOwn("firstDay", (double)firstDay); result.DefineOwn("weekend", weekend);
        return result;
    }
}
