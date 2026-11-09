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
        // The local calendar value can be representable even when applying
        // its offset crosses DateTimeOffset's year 1/9999 boundary.
        return (local.Ticks - DateTime.UnixEpoch.Ticks) / TimeSpan.TicksPerMillisecond - offset.TotalMilliseconds;
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
        if (utc) return ClipDate(UtcDateTimestamp(year, month, parts));
        // A local offset cannot bring a date more than one day outside TimeClip
        // back into the valid range. Avoid passing these inputs to DateTime.
        double localTimestamp = UtcDateTimestamp(year, month, parts);
        if (!double.IsFinite(localTimestamp) || Math.Abs(localTimestamp) > 8640000000000000d + 86400000d) return double.NaN;
        if (ExtendedLocalDate(localTimestamp)) return ClipDate(NativeExtendedDates.FromWall(localTimestamp,LocalDateZone()));
        // Normalize all components before entering DateTime's restricted range.
        // Large day and time offsets may cancel to an ordinary local date.
        var normalized = DateTimeOffset.FromUnixTimeMilliseconds((long)localTimestamp).UtcDateTime;
        var local = DateTime.SpecifyKind(normalized, DateTimeKind.Unspecified);
        return ClipDate(LocalDateTimestamp(local));
    }
    static double UtcDateTimestamp(double year, int month, ReadOnlySpan<double> parts)
    {
        double previous = year - 1;
        double days = year >= int.MinValue && year <= int.MaxValue ? DateYearStart((int)year)
            : 365 * previous + Math.Floor(previous / 4) - Math.Floor(previous / 100) + Math.Floor(previous / 400) - 719162;
        bool leap = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        for (int index = 1; index < month; index++)
            days += index == 2 ? leap ? 29 : 28 : index is 4 or 6 or 9 or 11 ? 30 : 31;
        days += parts[2] - 1;
        double time = parts[3] * 3600000 + parts[4] * 60000 + parts[5] * 1000 + parts[6];
        return days * 86400000 + time;
    }

}
