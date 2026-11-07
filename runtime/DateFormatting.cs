using System;
using System.Globalization;

public static partial class Js
{
    static void DefineDateLocaleFormatting(JsClassInstance prototype)
    {
        foreach (var item in new[] { ("toLocaleString", "G"), ("toLocaleDateString", "d"), ("toLocaleTimeString", "T") })
            prototype.DefineMethod(item.Item1, new CollectionMethod(item.Item1, 0, (receiver, arguments) =>
            {
                var date = RequireDate(receiver);
                if (double.IsNaN(date.Timestamp)) return "Invalid Date";
                var culture = CaseCulture(FunctionArgument(arguments, 0));
                return FormatLocaleDate(date, culture, FunctionArgument(arguments, 1), item.Item2);
            }));
    }
    static readonly string[] UnsupportedDateLocaleOptions = { "calendar", "numberingSystem" };
    static string FormatLocaleDate(JsDate date, CultureInfo culture, object optionValue, string mode)
    {
        if (IsUndefined(optionValue)) return TimeZoneInfo.ConvertTime(NativeDate(date), TimeZoneInfo.Local).ToString(mode, culture);
        TsObject options = ToObject(optionValue);
        string Option(string name)
        {
            object value = options.Get(name);
            return IsUndefined(value) ? null : StringValue(value);
        }
        string dateStyle = Option("dateStyle"), timeStyle = Option("timeStyle");
        static bool ValidStyle(string style) => style is null or "full" or "long" or "medium" or "short";
        if (!ValidStyle(dateStyle) || !ValidStyle(timeStyle)) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid date or time style");
        if (mode == "d" && timeStyle != null || mode == "T" && dateStyle != null)
            throw new InvalidOperationException("This date method does not accept the requested style");
        foreach (string component in UnsupportedDateLocaleOptions)
            if (!IsUndefined(options.Get(component)))
                throw new NotSupportedException("Date locale formatting option " + component + " is not supported");
        var dateFields = new DateLocaleFields(options);
        var timeFields = new DateLocaleTimeFields(options);
        if ((dateFields.HasFields || timeFields.HasFields) && (dateStyle != null || timeStyle != null))
            throw new InvalidOperationException("Date styles cannot be combined with component options");
        string matcher = Option("localeMatcher");
        if (matcher != null && matcher is not ("lookup" or "best fit")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale matcher");
        string formatMatcher = Option("formatMatcher");
        if (formatMatcher != null && formatMatcher is not ("basic" or "best fit")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid format matcher");
        string zoneName = Option("timeZone");
        TimeZoneInfo zone;
        try { zone = zoneName == null ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(zoneName); }
        catch (TimeZoneNotFoundException) { throw new ArgumentOutOfRangeException(nameof(optionValue), "Unknown time zone"); }
        catch (InvalidTimeZoneException) { throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid time zone"); }
        var native = NativeDate(date);
        var local = TimeZoneInfo.ConvertTime(native, zone);
        if (dateFields.HasFields || timeFields.HasFields || (timeFields.HasClockOption && dateStyle == null && timeStyle == null)) {
            bool noComponents = !dateFields.HasFields && !timeFields.HasClockFields;
            string datePart = dateFields.HasFields ? dateFields.Format(local, culture)
                : mode == "d" || mode == "G" && noComponents ? local.ToString("d", culture) : null;
            string timePart = timeFields.HasFields ? timeFields.Format(local, culture, zone, !timeFields.HasClockFields && mode != "d")
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
    static string LocalDateText(object receiver, int mode)
    {
        var date = RequireDate(receiver);
        if (double.IsNaN(date.Timestamp)) return "Invalid Date";
        var native = NativeDate(date); var zone = TimeZoneInfo.Local;
        var local = TimeZoneInfo.ConvertTime(native, zone);
        string day = mode == 1 ? null : local.ToString("ddd MMM dd yyyy", CultureInfo.InvariantCulture);
        if (mode == 0) return day;
        int offset = (int)local.Offset.TotalMinutes;
        int magnitude = Math.Abs(offset);
        string name = zone.IsDaylightSavingTime(native) ? zone.DaylightName : zone.StandardName;
        string time = local.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " GMT" + (offset < 0 ? "-" : "+")
            + (magnitude / 60).ToString("00", CultureInfo.InvariantCulture) + (magnitude % 60).ToString("00", CultureInfo.InvariantCulture)
            + " (" + name + ")";
        return mode == 1 ? time : day + " " + time;
    }
}
