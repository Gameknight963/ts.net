using System;

public static partial class Js
{
    static double LocalDateTimestamp(DateTime local)
    {
        var zone = TimeZoneInfo.Local;
        TimeSpan offset = zone.GetUtcOffset(local);
        if (zone.IsAmbiguousTime(local))
        {
            var choices = zone.GetAmbiguousTimeOffsets(local);
            offset = choices[0] > choices[1] ? choices[0] : choices[1];
        }
        return new DateTimeOffset(local, offset).ToUnixTimeMilliseconds();
    }
    static double DateFromParts(object[] arguments, bool utc)
    {
        double[] parts = { double.NaN, 0, 1, 0, 0, 0, 0 };
        for (int index = 0; index < Math.Min(arguments.Length, parts.Length); index++) parts[index] = Math.Truncate(Number(arguments[index]));
        return DateFromComponents(parts, utc, true);
    }
    static double DateFromComponents(double[] parts, bool utc, bool adjustYear)
    {
        foreach (double part in parts) if (!double.IsFinite(part)) return double.NaN;
        double year = parts[0]; if (adjustYear && year >= 0 && year <= 99) year += 1900;
        year += Math.Floor(parts[1] / 12);
        int month = (int)(parts[1] - 12 * Math.Floor(parts[1] / 12)) + 1;
        if (utc)
        {
            double previous = year - 1;
            double days = year >= int.MinValue && year <= int.MaxValue ? DateYearStart((int)year)
                : 365 * previous + Math.Floor(previous / 4) - Math.Floor(previous / 100) + Math.Floor(previous / 400) - 719162;
            bool leap = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
            for (int index = 1; index < month; index++)
                days += index == 2 ? leap ? 29 : 28 : index is 4 or 6 or 9 or 11 ? 30 : 31;
            days += parts[2] - 1;
            double time = parts[3] * 3600000 + parts[4] * 60000 + parts[5] * 1000 + parts[6];
            return ClipDate(days * 86400000 + time);
        }
        if (year < 1 || year > 9999) throw new NotSupportedException("Date calendar construction outside .NET's year range 1 through 9999 is not supported");
        try
        {
            var date = new DateTime((int)year, month, 1, 0, 0, 0, DateTimeKind.Unspecified)
                .AddDays(parts[2] - 1).AddHours(parts[3]).AddMinutes(parts[4]).AddSeconds(parts[5]).AddMilliseconds(parts[6]);
            return ClipDate(utc ? new DateTimeOffset(date, TimeSpan.Zero).ToUnixTimeMilliseconds() : LocalDateTimestamp(date));
        }
        catch (ArgumentOutOfRangeException) { throw new NotSupportedException("Date calendar construction outside .NET's year range 1 through 9999 is not supported"); }
    }
}
