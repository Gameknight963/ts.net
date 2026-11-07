using System;
using System.Globalization;

public static partial class Js
{
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
