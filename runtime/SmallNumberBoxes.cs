using System.Runtime.CompilerServices;

public static partial class Js
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object BoxInteger(int value) => (uint)value < 256 ? SmallNumberBoxes.Values[value] : (object)(double)value;

    static class SmallNumberBoxes
    {
        internal static readonly object[] Values = Create();
        static object[] Create()
        {
            var values = new object[256];
            for (int i = 0; i < values.Length; i++) values[i] = (double)i;
            return values;
        }
    }
}
