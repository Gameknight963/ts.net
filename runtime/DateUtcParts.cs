using System;
using System.Globalization;

public static partial class Js
{
    readonly record struct UtcDateParts(int Year, int Month, int Day, int Weekday, int Hour, int Minute, int Second, int Millisecond);
    static long FloorDateDivision(long value, int divisor) => value / divisor - (value % divisor < 0 ? 1 : 0);
    static long DateYearStart(int year)
    {
        long previous = (long)year - 1;
        return 365 * previous + FloorDateDivision(previous, 4) - FloorDateDivision(previous, 100) + FloorDateDivision(previous, 400) - 719162;
    }
    static UtcDateParts ExtendedUtcDateParts(double timestamp)
    {
        long days = FloorDateDivision((long)timestamp, 86400000);
        int time = (int)((long)timestamp - days * 86400000);
        int low = -271822, high = 275762;
        while (low + 1 < high)
        {
            int middle = low + (high - low) / 2;
            if (DateYearStart(middle) <= days) low = middle; else high = middle;
        }
        int day = (int)(days - DateYearStart(low)), month = 1;
        bool leap = low % 4 == 0 && (low % 100 != 0 || low % 400 == 0);
        while (true)
        {
            int length = month == 2 ? leap ? 29 : 28 : month is 4 or 6 or 9 or 11 ? 30 : 31;
            if (day < length) break;
            day -= length; month++;
        }
        int weekday = (int)((days + 4) % 7); if (weekday < 0) weekday += 7;
        return new(low, month, day + 1, weekday, time / 3600000, time / 60000 % 60, time / 1000 % 60, time % 1000);
    }
    static bool OutsideNativeDate(double timestamp) => timestamp < -62135596800000d || timestamp > 253402300799999d;
    static string ExtendedDateIso(double timestamp)
    {
        var value = ExtendedUtcDateParts(timestamp);
        string year = value.Year is >= 0 and <= 9999 ? value.Year.ToString("D4", CultureInfo.InvariantCulture)
            : (value.Year < 0 ? "-" : "+") + Math.Abs(value.Year).ToString("D6", CultureInfo.InvariantCulture);
        return string.Create(CultureInfo.InvariantCulture, $"{year}-{value.Month:D2}-{value.Day:D2}T{value.Hour:D2}:{value.Minute:D2}:{value.Second:D2}.{value.Millisecond:D3}Z");
    }
    static double ExtendedUtcDateField(double timestamp, string name)
    {
        var value = ExtendedUtcDateParts(timestamp);
        return name switch
        {
            "FullYear" => value.Year,
            "Month" => value.Month - 1,
            "Date" => value.Day,
            "Day" => value.Weekday,
            "Hours" => value.Hour,
            "Minutes" => value.Minute,
            "Seconds" => value.Second,
            _ => value.Millisecond
        };
    }
}
