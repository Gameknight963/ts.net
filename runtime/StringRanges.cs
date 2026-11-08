using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    sealed class StringRangeFunction(bool substring, string name) : JsFunction(
        (FunctionBody)(static _ => throw new InvalidOperationException("String range method requires a receiver")), null, name, 2)
    {
        internal readonly bool Substring = substring;
        public override object InvokeWithReceiver(object receiver, object[] arguments) => StringRangeConverted(this, receiver, FunctionArgument(arguments, 0), FunctionArgument(arguments, 1));
    }
    static class StringRangeFunctions
    {
        internal static readonly StringRangeFunction Slice = new(false, "slice");
        internal static readonly StringRangeFunction Substring = new(true, "substring");
    }
    static object StringRangeTarget(string name) => name == "slice" ? StringRangeFunctions.Slice : StringRangeFunctions.Substring;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int StringRangeBound(double value, int length, bool substring)
    {
        value = CharacterPosition(value);
        if (!substring && value < 0) value += length;
        return value <= 0 ? 0 : value >= length ? length : ToInt32(value);
    }
    static string StringRange(string text, double first, double last, bool substring)
    {
        int start = StringRangeBound(first, text.Length, substring);
        int end = StringRangeBound(last, text.Length, substring);
        if (substring && start > end) (start, end) = (end, start);
        int count = end > start ? end - start : 0;
        return count == 1 ? Iteration.CodeUnit(text[start]) : text.Substring(start, count);
    }
    static string StringRangePrimitive(StringRangeFunction method, object receiver, double? first, double? last)
    {
        string text = AtString(receiver);
        return StringRange(text, first.GetValueOrDefault(), last ?? text.Length, method.Substring);
    }
    static string StringRangeConverted(StringRangeFunction method, object receiver, object first, object last)
    {
        string text = AtString(receiver);
        double start = Number(first);
        double end = IsUndefined(last) ? text.Length : Number(last);
        return StringRange(text, start, end, method.Substring);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object CallMethodIndices(object target, object receiver, double? first, double? last) => target is StringRangeFunction range
        ? StringRangePrimitive(range, receiver, first, last)
        : CallMethod(target, receiver, new[]{BoxNumber(first), BoxNumber(last)});
}
