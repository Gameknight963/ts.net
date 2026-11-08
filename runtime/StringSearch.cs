using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    enum StringSearchKind { IndexOf, Includes, LastIndexOf }
    sealed class StringSearchFunction(StringSearchKind kind, string name) : JsFunction(
        (FunctionBody)(static _ => throw new InvalidOperationException("String search method requires a receiver")), null, name, 1)
    {
        internal readonly StringSearchKind Kind = kind;
        public override object InvokeWithReceiver(object receiver, object[] arguments) =>
            StringSearchArguments(this, receiver, FunctionArgument(arguments, 0), FunctionArgument(arguments, 1));
    }
    static class StringSearchFunctions
    {
        internal static readonly StringSearchFunction IndexOf = new(StringSearchKind.IndexOf, "indexOf");
        internal static readonly StringSearchFunction Includes = new(StringSearchKind.Includes, "includes");
        internal static readonly StringSearchFunction LastIndexOf = new(StringSearchKind.LastIndexOf, "lastIndexOf");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string StringValue(object value) => value is string text ? text : StringValueConverted(value);
    static string StringValueConverted(object value)
    {
        object primitive = value is TsObject obj ? ToPrimitive(obj, "string") : value;
        if (primitive is JsSymbol) throw new InvalidOperationException("Cannot convert a symbol to a string");
        return primitive is double number ? NumberKeyString(number) : Text(primitive);
    }
    static int StringSearchCore(string text, string needle, double position, StringSearchKind kind)
    {
        bool backwards = kind == StringSearchKind.LastIndexOf;
        if (double.IsNaN(position)) position = backwards ? text.Length : 0;
        int start = StringRangeBound(position, text.Length, true);
        if (!backwards) return text.IndexOf(needle, start, StringComparison.Ordinal);
        if (needle.Length == 0) return start;
        // LastIndexOf's position limits the match start, not its last code unit.
        int end = needle.Length > text.Length - start ? text.Length : start + needle.Length;
        return text.AsSpan(0, end).LastIndexOf(needle.AsSpan(), StringComparison.Ordinal);
    }
    static int StringSearchPrimitive(StringSearchFunction method, object receiver, object needle, double? position)
    {
        string text = AtString(receiver);
        if (method.Kind == StringSearchKind.Includes && IsRegExpValue(needle)) throw new InvalidOperationException("String.includes does not accept regular expressions");
        string search = StringValue(needle);
        double start = position ?? (method.Kind == StringSearchKind.LastIndexOf ? double.PositiveInfinity : 0);
        return StringSearchCore(text, search, start, method.Kind);
    }
    static int StringSearchConverted(StringSearchKind kind, object receiver, object needle, object position)
    {
        string text = AtString(receiver);
        if (kind == StringSearchKind.Includes && IsRegExpValue(needle)) throw new InvalidOperationException("String.includes does not accept regular expressions");
        string search = StringValue(needle);
        double start = Number(position);
        return StringSearchCore(text, search, start, kind);
    }
    static object StringSearchResult(StringSearchKind kind, int index) => kind == StringSearchKind.Includes ? BoxBoolean(index >= 0) : (double)index;
    static object StringSearchArguments(StringSearchFunction method, object receiver, object needle, object position) =>
        StringSearchResult(method.Kind, StringSearchConverted(method.Kind, receiver, needle, position));
    static object StringSearchFallback(object target, object receiver, object needle, double? position, int count) => count switch {
        0 => CallMethod0(target, receiver),
        1 => CallMethod(target, receiver, new[]{needle}),
        _ => CallMethod(target, receiver, new[]{needle, BoxNumber(position)})
    };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? CallStringSearchNumber(object target, object receiver, object needle, double? position, int count)
    {
        if (target is StringSearchFunction method) {
            int index = StringSearchPrimitive(method, receiver, needle, position);
            return method.Kind != StringSearchKind.Includes ? index : Cast<double?>(BoxBoolean(index >= 0), "Function result violates number type");
        }
        return Cast<double?>(StringSearchFallback(target, receiver, needle, position, count), "Function result violates number type");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool? CallStringSearchBoolean(object target, object receiver, object needle, double? position, int count)
    {
        if (target is StringSearchFunction method) {
            int index = StringSearchPrimitive(method, receiver, needle, position);
            return method.Kind == StringSearchKind.Includes ? index >= 0 : Cast<bool?>((double)index, "Function result violates boolean type");
        }
        return Cast<bool?>(StringSearchFallback(target, receiver, needle, position, count), "Function result violates boolean type");
    }
    public static object CallStringSearch(object target, object receiver, object needle, double? position, int count) => target is StringSearchFunction method
        ? StringSearchResult(method.Kind, StringSearchPrimitive(method, receiver, needle, position))
        : StringSearchFallback(target, receiver, needle, position, count);
}
