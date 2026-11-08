using System;

public static partial class Js
{
    static string RelativeTimeUnitName(string locale, string style, string unit, bool singular)
    {
        if (locale.StartsWith("de", StringComparison.Ordinal)) {
            if (style != "long") {
                if (unit == "quarter") return style == "short" ? "Quart." : "Q";
                if (unit == "week" && style == "narrow") return "Wo.";
                if (unit == "hour") return "Std.";
                if (unit == "minute") return style == "short" ? "Min." : "m";
                if (unit == "second") return style == "short" ? "Sek." : "s";
            }
            return unit switch {
                "year" => singular ? "Jahr" : "Jahren", "quarter" => singular ? "Quartal" : "Quartalen", "month" => singular ? "Monat" : "Monaten",
                "week" => singular ? "Woche" : "Wochen", "day" => singular ? "Tag" : "Tagen", "hour" => singular ? "Stunde" : "Stunden",
                "minute" => singular ? "Minute" : "Minuten", _ => singular ? "Sekunde" : "Sekunden"
            };
        }
        if (locale.StartsWith("fr", StringComparison.Ordinal)) {
            if (style != "long") return unit switch {"year" => "a", "quarter" => "trim.", "month" => "m.", "week" => "sem.", "day" => "j", "hour" => "h", "minute" => "min", _ => "s"};
            string noun = unit switch {"year" => "an", "quarter" => "trimestre", "month" => "mois", "week" => "semaine", "day" => "jour", "hour" => "heure", "minute" => "minute", _ => "seconde"};
            return singular || unit == "month" ? noun : noun + "s";
        }
        if (style == "long") return unit + (singular ? "" : "s");
        if (style == "narrow") return unit switch {"year" => "y", "quarter" => "q", "month" => "mo", "week" => "w", "day" => "d", "hour" => "h", "minute" => "m", _ => "s"};
        return unit switch {"year" => "yr.", "quarter" => singular ? "qtr." : "qtrs.", "month" => "mo.", "week" => "wk.", "day" => singular ? "day" : "days", "hour" => "hr.", "minute" => "min.", _ => "sec."};
    }
    static (string Prefix, string Suffix) RelativeTimeAffixes(string locale, string style, string unit, bool singular, bool past)
    {
        if (EastAsianRelativeTime(locale)) return EastAsianRelativeTimeAffixes(locale, style, unit, past);
        string noun = RelativeTimeUnitName(locale, style, unit, singular);
        if (locale.StartsWith("de", StringComparison.Ordinal)) {
            string gap = past && unit == "month" && (style == "short" && !singular || style == "narrow" && singular) ? "\u00a0" : " ";
            return (past ? "vor " : "in ", gap + noun);
        }
        if (locale.StartsWith("fr", StringComparison.Ordinal)) {
            string gap = style == "short" && unit is "day" or "hour" or "minute" or "second" ? "\u00a0" : " ";
            return (style == "narrow" ? past ? "-" : "+" : past ? "il y a " : "dans ", gap + noun);
        }
        return (past ? "" : "in ", (style == "narrow" ? "" : " ") + noun + (past ? " ago" : ""));
    }
    static string RelativeTimeChoice(double value, string past, string current, string future) => value == -1 ? past : value == 0 ? current : value == 1 ? future : null;
    static string RelativeTimeAutomatic(string locale, string style, double value, string unit)
    {
        if (EastAsianRelativeTime(locale)) return EastAsianRelativeTimeAutomatic(locale, value, unit);
        if (locale.StartsWith("de", StringComparison.Ordinal)) return unit switch {
            "year" => RelativeTimeChoice(value, "letztes Jahr", "dieses Jahr", "n\u00e4chstes Jahr"),
            "quarter" => RelativeTimeChoice(value, "letztes Quartal", "dieses Quartal", "n\u00e4chstes Quartal"),
            "month" => RelativeTimeChoice(value, "letzten Monat", "diesen Monat", "n\u00e4chsten Monat"),
            "week" => RelativeTimeChoice(value, "letzte Woche", "diese Woche", "n\u00e4chste Woche"),
            "day" => value switch {-2 => "vorgestern", -1 => "gestern", 0 => "heute", 1 => "morgen", 2 => "\u00fcbermorgen", _ => null},
            "hour" => value == 0 ? "in dieser Stunde" : null, "minute" => value == 0 ? "in dieser Minute" : null, _ => value == 0 ? "jetzt" : null
        };
        if (locale.StartsWith("fr", StringComparison.Ordinal)) return unit switch {
            "year" => RelativeTimeChoice(value, style == "long" ? "l\u2019ann\u00e9e derni\u00e8re" : "l\u2019an dernier", "cette ann\u00e9e", style == "long" ? "l\u2019ann\u00e9e prochaine" : "l\u2019an prochain"),
            "quarter" => RelativeTimeChoice(value, "le trimestre dernier", "ce trimestre", "le trimestre prochain"),
            "month" => RelativeTimeChoice(value, "le mois dernier", "ce mois-ci", "le mois prochain"),
            "week" => RelativeTimeChoice(value, "la semaine derni\u00e8re", "cette semaine", "la semaine prochaine"),
            "day" => value switch {-2 => "avant-hier", -1 => "hier", 0 => "aujourd\u2019hui", 1 => "demain", 2 => "apr\u00e8s-demain", _ => null},
            "hour" => value == 0 ? "cette heure-ci" : null, "minute" => value == 0 ? "cette minute-ci" : null, _ => value == 0 ? "maintenant" : null
        };
        if (unit == "second") return value == 0 ? "now" : null;
        if (unit == "day") return RelativeTimeChoice(value, "yesterday", "today", "tomorrow");
        if (unit is "hour" or "minute") return value == 0 ? "this " + unit : null;
        string name = RelativeTimeUnitName(locale, style == "long" ? "long" : "short", unit, true);
        return RelativeTimeChoice(value, "last " + name, "this " + name, "next " + name);
    }
}
