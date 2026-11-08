using System;

public static partial class Js
{
    static object SetDateComponents(object receiver, object[] arguments, int component, int count, bool utc, bool legacy = false)
    {
        var date = RequireDate(receiver);
        bool invalid = double.IsNaN(date.Timestamp);
        DateTime native = new(1970, 1, 1);
        if (!invalid && !(utc && OutsideNativeDate(date.Timestamp)))
        {
            var current = NativeDate(date);
            native = utc ? current.DateTime : TimeZoneInfo.ConvertTime(current, TimeZoneInfo.Local).DateTime;
        }
        double[] parts = { native.Year, native.Month - 1, native.Day, native.Hour, native.Minute, native.Second, native.Millisecond };
        if (!invalid && utc && OutsideNativeDate(date.Timestamp))
        {
            var extended = ExtendedUtcDateParts(date.Timestamp);
            parts = new double[] { extended.Year, extended.Month - 1, extended.Day, extended.Hour, extended.Minute, extended.Second, extended.Millisecond };
        }
        int supplied = Math.Max(1, Math.Min(count, arguments.Length));
        for (int index = 0; index < supplied; index++) parts[component + index] = Math.Truncate(Number(FunctionArgument(arguments, index)));
        if (invalid && component != 0) return date.Timestamp = double.NaN;
        return date.Timestamp = DateFromComponents(parts, utc, legacy);
    }
    static void DefineDateSetters(JsCreatedObject prototype)
    {
        string[] names = { "FullYear", "Month", "Date", "Hours", "Minutes", "Seconds", "Milliseconds" };
        int[] counts = { 3, 2, 1, 4, 3, 2, 1 };
        foreach (bool utc in new[] { false, true })
            for (int index = 0; index < names.Length; index++)
            {
                int component = index, count = counts[index];
                string name = "set" + (utc ? "UTC" : "") + names[index];
                prototype.DefineMethod(name, new CollectionMethod(name, count, (receiver, arguments) => SetDateComponents(receiver, arguments, component, count, utc)));
            }
        prototype.DefineMethod("setYear", new CollectionMethod("setYear", 1, (receiver, arguments) => SetDateComponents(receiver, arguments, 0, 1, false, true)));
    }
}
