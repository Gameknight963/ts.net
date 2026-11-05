using System;

public static partial class Js
{
    public static bool GuardIntegerLoop(double? value, double bound, int start, int step)
    {
        if (!value.HasValue) return false;
        double first = value.Value;
        if (!(first >= int.MinValue && first <= int.MaxValue) || Math.Truncate(first) != first) return false;
        double last = first + (Math.Ceiling(bound) - start) * step;
        return last >= int.MinValue && last <= int.MaxValue;
    }
}
