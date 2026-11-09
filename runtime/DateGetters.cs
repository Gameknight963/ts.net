using System;

public static partial class Js
{
    static void DefineDateGetters(JsCreatedObject prototype)
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
                    if(!utc)return LocalDateField(date.Timestamp,item.Name,item.Read);
                    if (OutsideNativeDate(date.Timestamp)) return ExtendedUtcDateField(date.Timestamp, item.Name);
                    return item.Read(NativeDate(date));
                }));
            }
        }
        prototype.DefineMethod("getTimezoneOffset", new CollectionMethod("getTimezoneOffset", 0, (receiver, _) =>
        {
            var date = RequireDate(receiver);
            return double.IsNaN(date.Timestamp) ? double.NaN : ExtendedLocalDate(date.Timestamp) ? Math.Truncate(-LocalDateFields(date.Timestamp).Offset/60000d) : -NativeLocalDate(date.Timestamp).Offset.TotalMinutes;
        }));
        prototype.DefineMethod("getYear", new CollectionMethod("getYear", 0, (receiver, _) =>
        {
            var date = RequireDate(receiver);
            return double.IsNaN(date.Timestamp) ? double.NaN : ExtendedLocalDate(date.Timestamp) ? LocalDateFields(date.Timestamp).Fields.Year-1900d : NativeLocalDate(date.Timestamp).Year-1900d;
        }));
    }
}
