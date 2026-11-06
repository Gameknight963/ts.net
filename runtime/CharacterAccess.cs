using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    enum CharacterKind { CharAt, CharCodeAt, CodePointAt }
    sealed class CharacterFunction(CharacterKind kind, string name) : JsFunction(
        (FunctionBody)(static _ => throw new InvalidOperationException("String character method requires a receiver")), null, name, 1)
    {
        internal readonly CharacterKind Kind = kind;
    }
    static class CharacterFunctions
    {
        internal static readonly CharacterFunction CharAt = new(CharacterKind.CharAt, "charAt");
        internal static readonly CharacterFunction CharCodeAt = new(CharacterKind.CharCodeAt, "charCodeAt");
        internal static readonly CharacterFunction CodePointAt = new(CharacterKind.CodePointAt, "codePointAt");
    }
    static object CharacterTarget(string name) => name switch {
        "charAt" => CharacterFunctions.CharAt,
        "charCodeAt" => CharacterFunctions.CharCodeAt,
        "codePointAt" => CharacterFunctions.CodePointAt,
        _ => Undefined
    };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double CharacterPosition(double position) => double.IsNaN(position) ? 0 : Math.Truncate(position);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double? CharacterNumber(string text, double position, CharacterKind kind)
    {
        position = CharacterPosition(position);
        if (!(position >= 0 && position < text.Length)) return kind == CharacterKind.CharCodeAt ? double.NaN : null;
        int index = ToInt32(position);
        char first = text[index];
        if (kind == CharacterKind.CodePointAt && char.IsHighSurrogate(first) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            return char.ConvertToUtf32(first, text[index + 1]);
        return first;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double? CharacterNumericResult(CharacterFunction method, object receiver, double position)
    {
        string text = AtString(receiver);
        return method.Kind == CharacterKind.CharAt
            ? Cast<double?>(CharacterValue(text, position, method.Kind), "Function result violates number type")
            : CharacterNumber(text, position, method.Kind);
    }
    static object CharacterValue(string text, double position, CharacterKind kind)
    {
        if (kind != CharacterKind.CharAt) return BoxNumber(CharacterNumber(text, position, kind));
        position = CharacterPosition(position);
        return position >= 0 && position < text.Length ? Iteration.CodeUnit(text[ToInt32(position)]) : "";
    }
    static object CharacterConverted(CharacterFunction method, object receiver, object index)
    {
        // ToString(this) must precede observable conversion of the index.
        string text = AtString(receiver);
        return CharacterValue(text, Number(index), method.Kind);
    }
}
