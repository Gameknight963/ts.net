using System.Runtime.CompilerServices;

public static partial class Js
{
    // Keep the hash loop out of the early-field path in generated readers.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int PropertyKeyHash(string key)
    {
        uint hash = 2166136261;
        if (key != null)
            foreach (char value in key) hash = unchecked((hash ^ value) * 16777619);
        return unchecked((int)hash);
    }
}
