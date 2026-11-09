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
        readonly double nativeStart,nativeEnd;
        NativeDateFormatting extended;
        readonly bool nativeCalendarPath;
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
            var localeFacts=new LocaleValue(locale?.BaseName ?? (culture.Name.Length==0 ? "en-US" : culture.Name),Undefined);
            string preferredCalendar=(string)((JsArray)LocaleCalendars(localeFacts)).ElementAt(0);
            string preferredNumbers=LocaleDefaultNumberingSystem(localeFacts);
            string calendarDefault = locale?.Keyword("ca") as string, numberingDefault = locale?.Keyword("nu") as string;
            string cycleDefault = (locale?.Keyword("hc") as string) switch { "h11" => "h11", "h12" => "h12", "h23" => "h23", "h24" => "h24", _ => null };
            string CanonicalCalendar(string value) {
                if(value==null)return null;
                ValidateNumberingSystem(value);
                return value.ToLowerInvariant() switch {"islamicc"=>"islamic-civil","ethiopic-amete-alem"=>"ethioaa",var other=>other};
            }
            calendarDefault=CanonicalCalendar(calendarDefault);
            string calendar = CanonicalCalendar(Option("calendar") ?? calendarDefault) ?? preferredCalendar;
            if(calendar!=null && Array.IndexOf(IntlValueLists.Calendars.Value,calendar)<0)calendar=preferredCalendar;
            string requestedNumbering = Option("numberingSystem");
            numberingSystem = requestedNumbering ?? numberingDefault ?? preferredNumbers;
            if(preferredCalendar!=NativeCalendarName(culture))nativeCalendarPath=true;
            string nativeCalendar = calendar == "iso8601" ? "gregory" : calendar;
            if (nativeCalendar != null && nativeCalendar != NativeCalendarName(culture))
            {
                Calendar selected = null;
                foreach (var candidate in culture.OptionalCalendars)
                    if (NativeCalendarName(candidate) == nativeCalendar) { selected = candidate; break; }
                if (selected == null) nativeCalendarPath=true;
                else {
                    culture = (CultureInfo)culture.Clone();
                    culture.DateTimeFormat.Calendar = selected;
                    Culture = culture;
                }
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
            calendarName = calendar ?? NativeCalendarName(Culture);
            if(calendarName is not ("gregory" or "iso8601") && NativeDateFormatting.Calendars!=null)nativeCalendarPath=true;
            ValidateNumberingSystem(numberingSystem);
            numberingSystem = ResolveNumberingSystem(numberingSystem,preferredNumbers);
            try { digitCharacters = numberingSystem switch {"arab"=>"\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669","arabext"=>"\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9",_=>NumberDigitCharacters(numberingSystem)}; }
            catch (NotSupportedException) { throw new NotSupportedException("Date locale formatting option numberingSystem is not supported"); }
            string decimalSeparator=numberingSystem is "arab" or "arabext" ? "\u066b" : culture.NumberFormat.NumberDecimalSeparator=="\u066b" ? "." : culture.NumberFormat.NumberDecimalSeparator;
            if(decimalSeparator!=culture.NumberFormat.NumberDecimalSeparator) {
                culture=(CultureInfo)culture.Clone();culture.NumberFormat.NumberDecimalSeparator=decimalSeparator;Culture=culture;
            }
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
                dateFields.UseDateDefaults(culture,!nativeCalendarPath);
            var supported=Culture.DateTimeFormat.Calendar;
            nativeStart=(supported.MinSupportedDateTime.Ticks-DateTime.UnixEpoch.Ticks)/(double)TimeSpan.TicksPerMillisecond+86400000;
            nativeEnd=(supported.MaxSupportedDateTime.Ticks-DateTime.UnixEpoch.Ticks)/(double)TimeSpan.TicksPerMillisecond-86400000;
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
            var result=RawOptions();
            if(nativeCalendarPath && dateStyle==null && timeStyle==null) {
                if(result.HasOwn("month") && ExtendedFormatter().MonthWidth is string width)result.Set("month",width);
                if(calendarName is "chinese" or "dangi")result.DeleteOwn("year");
                if(calendarName=="japanese" && IsUndefined(result.Get("era")))result.DefineOwn("era","narrow");
            }
            return result;
        }
        TsObject RawOptions()
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
        internal TsObject RequestedOptions()=>RawOptions();
        internal void ValidateRangeEndpoint(double timestamp)
        {
            if(timestamp<0 && calendarName is "indian" or "islamic" or "islamic-rgsa")ExtendedFormatter().CheckCalendar(timestamp);
        }
        bool NeedsExtended(double timestamp)=>nativeCalendarPath || timestamp<nativeStart || timestamp>nativeEnd;
        NativeDateFormatting ExtendedFormatter()
        {
            if(extended!=null)return extended;
            var options=RawOptions();
            if(dateStyle==null && timeStyle==null) {
                bool defaults=!dateFields.HasDateRequest && !timeFields.HasClockFields;
                if(mode=="d" && !dateFields.HasDateRequest || mode=="G" && defaults) {options.DefineOwn("year","numeric");options.DefineOwn("month","numeric");options.DefineOwn("day","numeric");}
                if(mode=="T" && !timeFields.HasClockFields || mode=="G" && defaults) {
                    options.DefineOwn("hour","numeric");options.DefineOwn("minute","2-digit");options.DefineOwn("second","2-digit");
                    timeFields.WriteOptions(options,Culture,false,true);
                }
            }
            return extended=new NativeDateFormatting(options,Culture.Name);
        }
        internal string Format(double timestamp) => NeedsExtended(timestamp) ? ExtendedFormatter().Text(timestamp) : LocalizeNumberDigits(FormatLatin(timestamp), digitCharacters);
        internal bool SameLocalDay(double first, double second)
        {
            DateTime Local(double value) => (fixedOffset.HasValue ? NativeDate(value + fixedOffset.Value * 60000d) : TimeZoneInfo.ConvertTime(NativeDate(value),Zone)).Date;
            return Local(first)==Local(second);
        }
        internal JsArray Parts(double timestamp)
        {
            if(NeedsExtended(timestamp))return ExtendedFormatter().Parts(timestamp);
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
