using System;
using System.Globalization;

public static partial class Js
{
    sealed class DateLocaleSettings
    {
        internal readonly CultureInfo Culture;
        readonly string localeName;
        internal readonly TimeZoneInfo Zone;
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
            dateStyle = Option("dateStyle"); timeStyle = Option("timeStyle");
            static bool ValidStyle(string style) => style is null or "full" or "long" or "medium" or "short";
            if (!ValidStyle(dateStyle) || !ValidStyle(timeStyle)) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid date or time style");
            if (mode == "d" && timeStyle != null || mode == "T" && dateStyle != null)
                throw new InvalidOperationException("This date method does not accept the requested style");
            string calendarDefault = locale?.Keyword("ca") as string, numberingDefault = locale?.Keyword("nu") as string;
            string cycleDefault = (locale?.Keyword("hc") as string) switch { "h11" => "h11", "h12" => "h12", "h23" => "h23", "h24" => "h24", _ => null };
            string calendar = Option("calendar") ?? calendarDefault, numberingSystem = Option("numberingSystem") ?? numberingDefault;
            if (calendar != null && calendar != NativeCalendarName(culture))
            {
                Calendar selected = null;
                foreach (var candidate in culture.OptionalCalendars)
                    if (NativeCalendarName(candidate) == calendar) { selected = candidate; break; }
                if (selected == null) throw new NotSupportedException("Date locale formatting option calendar is not supported for this culture");
                culture = (CultureInfo)culture.Clone();
                culture.DateTimeFormat.Calendar = selected;
                Culture = culture;
            }
            if (numberingSystem != null && numberingSystem != "latn") throw new NotSupportedException("Date locale formatting option numberingSystem is not supported");
            dateFields = new DateLocaleFields(options);
            timeFields = new DateLocaleTimeFields(options, cycleDefault);
            string extensions = "";
            if (calendarDefault != null && calendarDefault == NativeCalendarName(Culture)) extensions += "-ca-" + calendarDefault;
            if (timeFields.KeepCycleExtension) extensions += "-hc-" + cycleDefault;
            if (numberingDefault == "latn") extensions += "-nu-latn";
            localeName = Culture.Name + (extensions.Length == 0 ? "" : "-u" + extensions);
            if ((dateFields.HasFields || timeFields.HasFields) && (dateStyle != null || timeStyle != null))
                throw new InvalidOperationException("Date styles cannot be combined with component options");
            string matcher = Option("localeMatcher");
            if (matcher != null && matcher is not ("lookup" or "best fit")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale matcher");
            string formatMatcher = Option("formatMatcher");
            if (formatMatcher != null && formatMatcher is not ("basic" or "best fit")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid format matcher");
            string zoneName = Option("timeZone");
            try { Zone = zoneName == null ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(zoneName); }
            catch (TimeZoneNotFoundException) { throw new ArgumentOutOfRangeException(nameof(optionValue), "Unknown time zone"); }
            catch (InvalidTimeZoneException) { throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid time zone"); }

            if (mode == "intl" && dateStyle == null && timeStyle == null && !dateFields.HasDateRequest && !timeFields.HasClockFields)
                dateFields.UseDateDefaults(culture);
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
            result.DefineOwn("calendar", NativeCalendarName(Culture)); result.DefineOwn("numberingSystem", "latn"); result.DefineOwn("timeZone", Zone.Id);
            if (dateStyle != null) result.DefineOwn("dateStyle", dateStyle);
            if (timeStyle != null) result.DefineOwn("timeStyle", timeStyle);
            if (dateStyle == null && timeStyle == null) dateFields.WriteOptions(result);
            timeFields.WriteOptions(result, Culture, dateStyle != null || timeStyle != null, timeStyle != null);
            return result;
        }
        internal string Format(double timestamp)
        {
            var culture = Culture; var zone = Zone;
            var native = NativeDate(timestamp);
            var local = TimeZoneInfo.ConvertTime(native, zone);
            if (dateFields.HasFields || timeFields.HasFields || (timeFields.HasClockOption && dateStyle == null && timeStyle == null)) {
                bool noComponents = !dateFields.HasFields && !timeFields.HasClockFields;
                string datePart = dateFields.HasFields ? dateFields.Format(local, culture)
                    : mode == "d" || mode == "G" && noComponents ? local.ToString("d", culture) : null;
                string timePart = timeFields.HasFields ? timeFields.Format(local, culture, zone, !timeFields.HasClockFields && (mode == "G" || mode == "T"))
                    : mode == "T" || mode == "G" && noComponents ? timeFields.Format(local, culture, zone, true) : null;
                return datePart == null ? timePart : timePart == null ? datePart : datePart + " " + timePart;
            }
            if (dateStyle == null && timeStyle == null) return local.ToString(mode, culture);
            // CLR cultures provide short and long date patterns, rather than Intl's four widths.
            string dateText = dateStyle == null ? null : local.ToString(dateStyle is "short" or "medium" ? "d" : "D", culture);
            string timeText = timeStyle == null ? null : timeFields.HasClockOption ? timeFields.Format(local, culture, zone, true, timeStyle) : local.ToString(timeStyle == "short" ? "t" : "T", culture);
            if (timeStyle is "long" or "full") timeText += " " + (zone.IsDaylightSavingTime(native) ? zone.DaylightName : zone.StandardName);
            return dateText == null ? timeText : timeText == null ? dateText : dateText + " " + timeText;
        }
    }
}
