using System;

public static partial class Js
{
    static (string Prefix, string Suffix) SpanishRelativeTimeAffixes(string style, string unit, bool singular, bool past)
    {
        string noun = (style == "long", unit) switch {
            (true, "year") => singular ? "a\u00f1o" : "a\u00f1os",
            (true, "quarter") => singular ? "trimestre" : "trimestres",
            (true, "month") => singular ? "mes" : "meses",
            (true, "week") => singular ? "semana" : "semanas",
            (true, "day") => singular ? "d\u00eda" : "d\u00edas",
            (true, "hour") => singular ? "hora" : "horas",
            (true, "minute") => singular ? "minuto" : "minutos",
            (true, "second") => singular ? "segundo" : "segundos",
            (false, "year") => "a",
            (false, "quarter") => "trim.",
            (false, "month") => "m",
            (false, "week") => "sem.",
            (false, "day") => "d",
            (false, "hour") => "h",
            (false, "minute") => "min",
            (false, "second") => "s",
            _ => throw new InvalidOperationException("Invalid relative-time unit")
        };
        return (past ? "hace " : "dentro de ", " " + noun);
    }
    static string SpanishRelativeTimeAutomatic(string style, double value, string unit)
    {
        if (unit == "week" && style != "long") return RelativeTimeChoice(value, "sem. ant.", "esta sem.", "pr\u00f3x. sem.");
        if (value != Math.Truncate(value) || value < -2 || value > 2) return null;
        return (unit, (int)value) switch {
            ("year", -1) => "el a\u00f1o pasado",
            ("year", 0) => "este a\u00f1o",
            ("year", 1) => "el pr\u00f3ximo a\u00f1o",
            ("quarter", -1) => "el trimestre pasado",
            ("quarter", 0) => "este trimestre",
            ("quarter", 1) => "el pr\u00f3ximo trimestre",
            ("month", -1) => "el mes pasado",
            ("month", 0) => "este mes",
            ("month", 1) => "el pr\u00f3ximo mes",
            ("week", -1) => "la semana pasada",
            ("week", 0) => "esta semana",
            ("week", 1) => "la pr\u00f3xima semana",
            ("day", -2) => "anteayer",
            ("day", -1) => "ayer",
            ("day", 0) => "hoy",
            ("day", 1) => "ma\u00f1ana",
            ("day", 2) => "pasado ma\u00f1ana",
            ("hour", 0) => "esta hora",
            ("minute", 0) => "este minuto",
            ("second", 0) => "ahora",
            _ => null
        };
    }
}
