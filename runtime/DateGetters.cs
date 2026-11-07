using System;

public static partial class Js
{
    static void DefineDateGetters(JsClassInstance prototype)
    {
        foreach (bool utc in new[] { false, true })
        {
            foreach (var item in new (string Name, Func<DateTimeOffset, double> Read)[]
            {
                ("FullYear", date => date.Year), ("Month", date => date.Month - 1),
                ("Date", date => date.Day), ("Day", date => (int)date.DayOfWeek),
                ("Hours", date => date.Hour), ("Minutes", date => date.Minute),
                ("Seconds", date => date.Second), ("Milliseconds", date => date.Millisecond)
            })
            {
                string name = "get" + (utc ? "UTC" : "") + item.Name;
                prototype.DefineMethod(name, new CollectionMethod(name, 0, (receiver, _) =>
                {
                    var date = RequireDate(receiver);
                    if (double.IsNaN(date.Timestamp)) return double.NaN;
                    var native = NativeDate(date);
                    if (!utc) native = TimeZoneInfo.ConvertTime(native, TimeZoneInfo.Local);
                    return item.Read(native);
                }));
            }
        }
        prototype.DefineMethod("getTimezoneOffset", new CollectionMethod("getTimezoneOffset", 0, (receiver, _) =>
        {
            var date = RequireDate(receiver);
            return double.IsNaN(date.Timestamp) ? double.NaN : -TimeZoneInfo.Local.GetUtcOffset(NativeDate(date)).TotalMinutes;
        }));
        prototype.DefineMethod("getYear", new CollectionMethod("getYear", 0, (receiver, _) =>
        {
            var date = RequireDate(receiver);
            return double.IsNaN(date.Timestamp) ? double.NaN : TimeZoneInfo.ConvertTime(NativeDate(date), TimeZoneInfo.Local).Year - 1900d;
        }));
    }
}
