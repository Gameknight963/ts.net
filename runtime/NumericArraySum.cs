public sealed partial class TaggedArray
{
    internal double? TrySumNumbers(double? initial)
    {
        if (TryTags(length) == null || length > numbers.Length) return null;
        double sum = Js.NumberValue(initial);
        bool found = false;
        for (int i = 0; i < (int)length; i++)
            if (tags[i] == 1) {sum += numbers[i]; found = true;}
        // With no additions, preserve undefined and negative zero as well.
        return found ? sum : initial;
    }
}

public static partial class Js
{
    // Called after intrinsic iterator selection, for a body with no user calls.
    public static double? TrySumNumericArray(object receiver, double? initial) =>
        receiver is TaggedArray array ? array.TrySumNumbers(initial) : null;
}
