using System;
using System.Globalization;
using System.Collections.Generic;

public static partial class Js
{
    sealed class DateLocaleSettings
    {
        internal readonly CultureInfo Culture;
        readonly string localeName, numberingSystem, digitCharacters, calendarName;
        internal readonly TimeZoneInfo Zone;
        readonly int? fixedOffset;
        readonly string mode, dateStyle, timeStyle;
        readonly DateLocaleFields dateFields;
        readonly DateLocaleTimeFields timeFields;
        internal DateLocaleSettings(CultureInfo culture, object optionValue, string mode, LocaleValue locale = null)
        {
            Culture = culture; this.mode = mode;
            TsObject options = IsUndefined(optionValue) ? new JsDynamicObject() : ToObject(optionValue);
            string Option(string name)
            {
                object value = options.Get(name);
                return IsUndefined(value) ? null : StringValue(value);
            }
            string matcher = Option("localeMatcher");
            if (matcher != null && matcher is not ("lookup" or "best fit")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale matcher");
            dateStyle = Option("dateStyle"); timeStyle = Option("timeStyle");
            static bool ValidStyle(string style) => style is null or "full" or "long" or "medium" or "short";
            if (!ValidStyle(dateStyle) || !ValidStyle(timeStyle)) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid date or time style");
            if (mode == "d" && timeStyle != null || mode == "T" && dateStyle != null)
                throw new InvalidOperationException("This date method does not accept the requested style");
            string calendarDefault = locale?.Keyword("ca") as string, numberingDefault = locale?.Keyword("nu") as string;
            string cycleDefault = (locale?.Keyword("hc") as string) switch { "h11" => "h11", "h12" => "h12", "h23" => "h23", "h24" => "h24", _ => null };
            string calendar = Option("calendar") ?? calendarDefault;
            string requestedNumbering = Option("numberingSystem");
            numberingSystem = requestedNumbering ?? numberingDefault ?? "latn";
            string nativeCalendar = calendar == "iso8601" ? "gregory" : calendar;
            if (nativeCalendar != null && nativeCalendar != NativeCalendarName(culture))
            {
                Calendar selected = null;
                foreach (var candidate in culture.OptionalCalendars)
                    if (NativeCalendarName(candidate) == nativeCalendar) { selected = candidate; break; }
                if (selected == null) throw new NotSupportedException("Date locale formatting option calendar is not supported for this culture");
                culture = (CultureInfo)culture.Clone();
                culture.DateTimeFormat.Calendar = selected;
                Culture = culture;
            }
            if (calendar == "iso8601") {
                culture = (CultureInfo)culture.Clone();
                var dateInfo = culture.DateTimeFormat;
                dateInfo.ShortDatePattern = "yyyy-MM-dd";
                dateInfo.LongDatePattern = "yyyy MMMM d, dddd";
                dateInfo.DateSeparator = "-";
                dateInfo.FirstDayOfWeek = DayOfWeek.Monday;
                dateInfo.CalendarWeekRule = CalendarWeekRule.FirstFourDayWeek;
                Culture = culture;
            }
            calendarName = calendar == "iso8601" ? "iso8601" : NativeCalendarName(Culture);
            ValidateNumberingSystem(numberingSystem);
            numberingSystem = ResolveNumberingSystem(numberingSystem);
            try { digitCharacters = NumberDigitCharacters(numberingSystem); }
            catch (NotSupportedException) { throw new NotSupportedException("Date locale formatting option numberingSystem is not supported"); }
            dateFields = new DateLocaleFields(options);
            string regionalCycle = locale == null ? null : LocaleDefaultHourCycle(locale, false);
            string nativeCycle = culture.DateTimeFormat.ShortTimePattern.Contains('H') ? "h23" : "h12";
            timeFields = new DateLocaleTimeFields(options, cycleDefault, regionalCycle == nativeCycle ? null : regionalCycle);
            string extensions = "";
            if (calendarDefault != null && calendarDefault == calendarName) extensions += "-ca-" + calendarDefault;
            if (timeFields.KeepCycleExtension) extensions += "-hc-" + cycleDefault;
            if (numberingDefault != null && numberingDefault == numberingSystem && (requestedNumbering == null || requestedNumbering == numberingDefault)) extensions += "-nu-" + numberingDefault;
            localeName = Culture.Name + (extensions.Length == 0 ? "" : "-u" + extensions);
            if ((dateFields.HasFields || timeFields.HasFields) && (dateStyle != null || timeStyle != null))
                throw new InvalidOperationException("Date styles cannot be combined with component options");
            string formatMatcher = Option("formatMatcher");
            if (formatMatcher != null && formatMatcher is not ("basic" or "best fit")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid format matcher");
            string zoneName = Option("timeZone");
            try
            {
                if (zoneName != null && TryDateOffsetZone(zoneName, out int offset, out string canonical))
                {
                    fixedOffset = offset;
                    Zone = TimeZoneInfo.CreateCustomTimeZone(canonical, TimeSpan.Zero, "GMT" + canonical, "GMT" + canonical);
                }
                else Zone = zoneName == null ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(zoneName);
            }
            catch (TimeZoneNotFoundException) { throw new ArgumentOutOfRangeException(nameof(optionValue), "Unknown time zone"); }
            catch (InvalidTimeZoneException) { throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid time zone"); }

            if (mode == "intl" && dateStyle == null && timeStyle == null && !dateFields.HasDateRequest && !timeFields.HasClockFields)
                dateFields.UseDateDefaults(culture);
        }
        static bool TryDateOffsetZone(string name, out int offset, out string canonical)
        {
            offset = 0; canonical = null;
            if (name.Length == 0 || name[0] is not ('+' or '-')) return false;
            string digits = name.Length == 6 && name[3] == ':' ? name.Remove(3, 1) : name;
            if (digits.Length is not (3 or 5)) throw new ArgumentOutOfRangeException(nameof(name), "Invalid time zone offset");
            for (int index = 1; index < digits.Length; index++) if (!char.IsAsciiDigit(digits[index])) throw new ArgumentOutOfRangeException(nameof(name), "Invalid time zone offset");
            int hours = (digits[1] - '0') * 10 + digits[2] - '0';
            int minutes = digits.Length == 3 ? 0 : (digits[3] - '0') * 10 + digits[4] - '0';
            if (hours > 23 || minutes > 59) throw new ArgumentOutOfRangeException(nameof(name), "Invalid time zone offset");
            offset = (hours * 60 + minutes) * (digits[0] == '-' ? -1 : 1);
            canonical = (offset < 0 ? "-" : "+") + hours.ToString("00", CultureInfo.InvariantCulture) + ":" + minutes.ToString("00", CultureInfo.InvariantCulture);
            return true;
        }
        static string NativeCalendarName(CultureInfo culture) => NativeCalendarName(culture.DateTimeFormat.Calendar) ?? throw new NotSupportedException("Native calendar identification is not supported");
        internal static string NativeCalendarName(Calendar calendar) => calendar switch {
            GregorianCalendar => "gregory", JapaneseCalendar => "japanese", ThaiBuddhistCalendar => "buddhist",
            HebrewCalendar => "hebrew", TaiwanCalendar => "roc", PersianCalendar => "persian", UmAlQuraCalendar => "islamic-umalqura", HijriCalendar => "islamic",
            _ => null
        };
        internal TsObject Options()
        {
            var result = new JsDynamicObject();
            result.DefineOwn("locale", localeName);
            result.DefineOwn("calendar", calendarName); result.DefineOwn("numberingSystem", numberingSystem); result.DefineOwn("timeZone", Zone.Id);
            if (dateStyle != null) result.DefineOwn("dateStyle", dateStyle);
            if (timeStyle != null) result.DefineOwn("timeStyle", timeStyle);
            if (dateStyle == null && timeStyle == null) dateFields.WriteOptions(result);
            timeFields.WriteOptions(result, Culture, dateStyle != null || timeStyle != null, timeStyle != null);
            return result;
        }
        internal string Format(double timestamp) => LocalizeNumberDigits(FormatLatin(timestamp), digitCharacters);
        internal bool SameLocalDay(double first, double second)
        {
            DateTime Local(double value) => (fixedOffset.HasValue ? NativeDate(value + fixedOffset.Value * 60000d) : TimeZoneInfo.ConvertTime(NativeDate(value),Zone)).Date;
            return Local(first)==Local(second);
        }
        internal JsArray Parts(double timestamp)
        {
            var parts = new List<(string Type, string Value)>();
            FormatLatin(timestamp, parts);
            var result = new JsArray(0);
            foreach (var part in parts) {
                var entry = new JsDynamicObject();
                entry.DefineOwn("type", part.Type);
                entry.DefineOwn("value", LocalizeNumberDigits(part.Value, digitCharacters));
                result.Append(entry);
            }
            return result;
        }
        string FormatLatin(double timestamp, List<(string Type, string Value)> parts = null)
        {
            var culture = Culture; var zone = Zone;
            var native = NativeDate(timestamp);
            var local = fixedOffset.HasValue ? NativeDate(timestamp + fixedOffset.Value * 60000d) : TimeZoneInfo.ConvertTime(native, zone);
            if (dateFields.HasFields || timeFields.HasFields || (timeFields.HasClockOption && dateStyle == null && timeStyle == null)) {
                bool noComponents = !dateFields.HasFields && !timeFields.HasClockFields;
                string datePart = dateFields.HasFields ? dateFields.Format(local, culture, parts)
                    : mode == "d" || mode == "G" && noComponents ? NativeDateFormat(local, "d", culture, parts) : null;
                bool hasTime = timeFields.HasFields || mode == "T" || mode == "G" && noComponents;
                if (datePart != null && hasTime) parts?.Add(("literal", " "));
                string timePart = timeFields.HasFields ? timeFields.Format(local, culture, zone, !timeFields.HasClockFields && (mode == "G" || mode == "T"), fixedOffset: fixedOffset, formattedParts: parts)
                    : mode == "T" || mode == "G" && noComponents ? timeFields.Format(local, culture, zone, true, fixedOffset: fixedOffset, formattedParts: parts) : null;
                return datePart == null ? timePart : timePart == null ? datePart : datePart + " " + timePart;
            }
            if (dateStyle == null && timeStyle == null) return NativeDateFormat(local, mode, culture, parts);
            // CLR cultures provide short and long date patterns, rather than Intl's four widths.
            string dateText = dateStyle == null ? null : NativeDateFormat(local, dateStyle is "short" or "medium" ? "d" : "D", culture, parts);
            if (dateText != null && timeStyle != null) parts?.Add(("literal", " "));
            string timeText = timeStyle == null ? null : timeFields.HasClockOption ? timeFields.Format(local, culture, zone, true, timeStyle, fixedOffset, parts) : NativeDateFormat(local, timeStyle == "short" ? "t" : "T", culture, parts);
            if (timeStyle is "long" or "full") {
                string zoneText = zone.IsDaylightSavingTime(native) ? zone.DaylightName : zone.StandardName;
                timeText += " " + zoneText;
                parts?.Add(("literal", " ")); parts?.Add(("timeZoneName", zoneText));
            }
            return dateText == null ? timeText : timeText == null ? dateText : dateText + " " + timeText;
        }
    }
}
