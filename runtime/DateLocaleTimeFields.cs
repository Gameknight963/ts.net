using System;
using System.Globalization;
using System.Text;

public static partial class Js
{
    sealed class DateLocaleTimeFields
    {
        readonly string hour, minute, second, cycle, zoneName, dayPeriod;
        readonly bool? hour12;
        readonly int fractionDigits;
        internal bool HasClockFields => hour != null || minute != null || second != null || fractionDigits != 0 || dayPeriod != null;
        internal bool HasFields => HasClockFields || zoneName != null;
        internal bool HasClockOption => hour12.HasValue || cycle != null;
        internal DateLocaleTimeFields(TsObject options)
        {
            string Read(string name, params string[] choices)
            {
                object value = options.Get(name);
                if (IsUndefined(value)) return null;
                string text = StringValue(value);
                if (Array.IndexOf(choices, text) < 0) throw new ArgumentOutOfRangeException(name, "Invalid time formatting option");
                return text;
            }
            hour = Read("hour", "numeric", "2-digit");
            minute = Read("minute", "numeric", "2-digit");
            second = Read("second", "numeric", "2-digit");
            dayPeriod = Read("dayPeriod", "long", "short", "narrow");
            cycle = Read("hourCycle", "h11", "h12", "h23", "h24");
            zoneName = Read("timeZoneName", "short", "long", "shortOffset", "longOffset", "shortGeneric", "longGeneric");
            object use12 = options.Get("hour12");
            hour12 = IsUndefined(use12) ? null : Truthy(use12);
            object digits = options.Get("fractionalSecondDigits");
            if (!IsUndefined(digits)) {
                double number = Math.Floor(Number(digits));
                if (!double.IsFinite(number) || number < 1 || number > 3) throw new ArgumentOutOfRangeException("fractionalSecondDigits");
                fractionDigits = (int)number;
            }
        }
        string Clock(CultureInfo culture) => hour12.HasValue ? hour12.Value ? "h12" : "h23"
            : cycle ?? (culture.DateTimeFormat.ShortTimePattern.Contains('H') ? "h23" : "h12");
        internal void WriteOptions(TsObject target, CultureInfo culture, bool styled, bool timeStyle)
        {
            void Field(string name, string value) { if (value != null) target.DefineOwn(name, value); }
            if (hour != null || timeStyle) {
                string clock = Clock(culture);
                target.DefineOwn("hourCycle", clock); target.DefineOwn("hour12", BoxBoolean(clock is "h11" or "h12"));
            }
            if (styled) return;
            Field("hour", hour); Field("minute", minute == null ? null : hour != null ? "2-digit" : minute);
            Field("second", second == null ? null : hour != null || minute != null ? "2-digit" : second);
            Field("dayPeriod", dayPeriod); Field("timeZoneName", zoneName);
            if (fractionDigits != 0) target.DefineOwn("fractionalSecondDigits", (double)fractionDigits);
        }
        internal string Format(DateTimeOffset value, CultureInfo culture, TimeZoneInfo zone, bool defaults = false, string style = null)
        {
            string clock = Clock(culture);
            bool use12 = clock is "h11" or "h12";
            bool paddedStyle = style != null && (culture.DateTimeFormat.ShortTimePattern.Contains("HH", StringComparison.Ordinal) || culture.DateTimeFormat.ShortTimePattern.Contains("hh", StringComparison.Ordinal));
            string h = defaults ? paddedStyle || !use12 ? "2-digit" : "numeric" : hour;
            string m = defaults ? "2-digit" : minute;
            string s = defaults ? style == "short" ? null : "2-digit" : second;
            var result = new StringBuilder();
            void Component(int number, string width)
            {
                if (width == null) return;
                if (result.Length != 0) result.Append(culture.DateTimeFormat.TimeSeparator);
                result.Append(number.ToString(width == "2-digit" ? "00" : "0", culture));
            }
            int hours = clock switch {
                "h11" => value.Hour % 12,
                "h12" => value.Hour % 12 == 0 ? 12 : value.Hour % 12,
                "h24" => value.Hour == 0 ? 24 : value.Hour,
                _ => value.Hour
            };
            Component(hours, h);
            // Native multi-component clocks conventionally pad minutes and seconds.
            Component(value.Minute, m == null ? null : h != null ? "2-digit" : m);
            Component(value.Second, s == null ? null : h != null || m != null ? "2-digit" : s);
            if (fractionDigits != 0) {
                if (result.Length != 0) result.Append(culture.NumberFormat.NumberDecimalSeparator);
                result.Append(value.Millisecond.ToString("000", CultureInfo.InvariantCulture).AsSpan(0, fractionDigits));
            }
            if ((use12 && h != null) || (h == null && dayPeriod != null)) {
                string period = value.Hour < 12 ? culture.DateTimeFormat.AMDesignator : culture.DateTimeFormat.PMDesignator;
                if (dayPeriod == "narrow" && period.Length != 0) period = StringInfo.GetNextTextElement(period);
                if (period.Length != 0) {
                    string pattern = culture.DateTimeFormat.ShortTimePattern;
                    int position = pattern.IndexOf("tt", StringComparison.Ordinal);
                    if (result.Length == 0) result.Append(period);
                    else if (position >= 0 && position < pattern.IndexOf('h')) result.Insert(0, period + " ");
                    else { if (result.Length != 0) result.Append(' '); result.Append(period); }
                }
            }
            if (zoneName != null) {
                string text;
                if (zoneName is "shortOffset" or "longOffset") {
                    int offset = (int)value.Offset.TotalMinutes, absolute = Math.Abs(offset);
                    text = "GMT";
                    text += offset < 0 ? "-" : "+";
                    text += (absolute / 60).ToString(zoneName == "longOffset" ? "00" : "0", CultureInfo.InvariantCulture);
                    if (absolute % 60 != 0 || zoneName == "longOffset") text += ":" + (absolute % 60).ToString("00", CultureInfo.InvariantCulture);
                } else text = zoneName is "shortGeneric" or "longGeneric" ? zone.StandardName : zone.IsDaylightSavingTime(value) ? zone.DaylightName : zone.StandardName;
                if (result.Length != 0) result.Append(' ');
                result.Append(text);
            }
            return result.ToString();
        }
    }
}
