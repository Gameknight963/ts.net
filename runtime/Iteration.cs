using System;
using System.Runtime.CompilerServices;

public static class Iteration
{
    static class Characters
    {
        public static readonly string[] Latin = Build();
        static string[] Build()
        {
            var result = new string[256];
            for (int i = 0; i < result.Length; i++) result[i] = new string((char)i, 1);
            return result;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string CodeUnit(char value) => value < 256 ? Characters.Latin[value] : new string(value, 1);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Length(object value) => value switch
    {
        JsArray array => array.Length(),
        JsArguments arguments => arguments.IterationLength(),
        string text => text.Length,
        _ => throw new InvalidOperationException("Value is not iterable")
    };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Step(object value, int position)
    {
        if (value is string text && char.IsHighSurrogate(text[position]) && position + 1 < text.Length && char.IsLowSurrogate(text[position + 1])) return 2;
        return 1;
    }
    public static object Value(object value, int position)
    {
        if (value is JsArray array) return array.ElementAt((uint)position);
        if (value is JsArguments arguments) return arguments.Get(position.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (value is string text)
        {
            char first = text[position];
            if (first < 256) return Characters.Latin[first];
            return text.Substring(position, Step(value, position));
        }
        throw new InvalidOperationException("Value is not iterable");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? Number(object value, int position) => value is NumberArray array ? array.GetNumberAt(position) :
        Js.Cast<double?>(Value(value, position), "Iteration value violates number binding type");
}
