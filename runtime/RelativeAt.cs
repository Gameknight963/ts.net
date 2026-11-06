using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    sealed class RelativeAtFunction(bool text) : JsFunction((FunctionBody)(static _ =>
        throw new InvalidOperationException("at requires a receiver")), null, "at", 1)
    {
        internal readonly bool Text = text;
    }
    static class RelativeAtFunctions
    {
        internal static readonly RelativeAtFunction Array = new(false);
        internal static readonly RelativeAtFunction String = new(true);
    }
    public static object ArrayAtFunction() => RelativeAtFunctions.Array;
    public static object StringAtFunction() => RelativeAtFunctions.String;
    public static object RelativeAtTarget(object receiver) => Get(receiver, "at");

    static void RequireAtReceiver(object receiver)
    {
        if (IsNullish(receiver)) throw new InvalidOperationException("at requires a receiver");
    }
    static double AtLength(object receiver)
    {
        RequireAtReceiver(receiver);
        if (receiver is JsArray array) return array.Length();
        if (receiver is string text) return text.Length;
        double value = Number(Get(receiver, "length"));
        return value > 0 ? Math.Min(Math.Truncate(value), 9007199254740991.0) : 0;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static string AtString(object receiver)
    {
        if (receiver is string text) return text;
        return AtStringConverted(receiver);
    }
    static string AtStringConverted(object receiver)
    {
        RequireAtReceiver(receiver);
        return StringValue(receiver);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double AtIndex(double index, double length)
    {
        double integer = double.IsNaN(index) ? 0 : Math.Truncate(index);
        return integer < 0 ? length + integer : integer;
    }
    static object AtPrimitive(object receiver, double index, bool text)
    {
        if (text) {
            string value = AtString(receiver);
            double position = AtIndex(index, value.Length);
            return position >= 0 && position < value.Length ? Iteration.CodeUnit(value[(int)position]) : Undefined;
        }
        double length = AtLength(receiver), selected = AtIndex(index, length);
        return selected >= 0 && selected < length ? IndexGet(receiver, selected) : Undefined;
    }
    static object AtConverted(object receiver, object index, bool text)
    {
        // The receiver/length snapshot precedes observable index conversion.
        if (text) {
            string value = AtString(receiver);
            double position = AtIndex(Number(index), value.Length);
            return position >= 0 && position < value.Length ? Iteration.CodeUnit(value[(int)position]) : Undefined;
        }
        double length = AtLength(receiver), selected = AtIndex(Number(index), length);
        return selected >= 0 && selected < length ? IndexGet(receiver, selected) : Undefined;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object CallMethodIndex(object target, object receiver, double? index) => target is StringRangeFunction range
        ? StringRangePrimitive(range, receiver, index, null)
        : target is CharacterFunction character
        ? CharacterValue(AtString(receiver), index.GetValueOrDefault(), character.Kind)
        : target is RelativeAtFunction method
        ? AtPrimitive(receiver, index.GetValueOrDefault(), method.Text) : target is StringSearchFunction search
        ? StringSearchResult(search.Kind, StringSearchPrimitive(search, receiver, BoxNumber(index), null)) : CallFunction(target, new[] { BoxNumber(index) });
    public static object CallMethod0(object target, object receiver) => target is StringRangeFunction range
        ? StringRangePrimitive(range, receiver, null, null)
        : target is CharacterFunction character
        ? CharacterValue(AtString(receiver), 0, character.Kind)
        : target is RelativeAtFunction method
        ? AtPrimitive(receiver, 0, method.Text) : target is StringSearchFunction search
        ? StringSearchResult(search.Kind, StringSearchPrimitive(search, receiver, Undefined, null)) : CallFunction0(target);
    public static object CallMethod(object target, object receiver, object[] arguments) => target is StringRangeFunction range
        ? StringRangeConverted(range, receiver, FunctionArgument(arguments, 0), FunctionArgument(arguments, 1))
        : target is CharacterFunction character
        ? CharacterConverted(character, receiver, FunctionArgument(arguments, 0))
        : target is RelativeAtFunction method
        ? AtConverted(receiver, FunctionArgument(arguments, 0), method.Text) : target is StringSearchFunction search
        ? StringSearchArguments(search, receiver, FunctionArgument(arguments, 0), FunctionArgument(arguments, 1)) : CallFunction(target, arguments);
    public static object CallMethodArguments(object target, object receiver, object arguments) => target is StringRangeFunction range
        ? StringRangeConverted(range, receiver, Iteration.Value(arguments, 0), Iteration.Value(arguments, 1))
        : target is CharacterFunction character
        ? CharacterConverted(character, receiver, Iteration.Value(arguments, 0))
        : target is RelativeAtFunction method
        ? AtConverted(receiver, Iteration.Value(arguments, 0), method.Text) : target is StringSearchFunction search
        ? StringSearchArguments(search, receiver, Iteration.Value(arguments, 0), Iteration.Value(arguments, 1)) : CallFunctionArguments(target, arguments);
    public static double? CallMethodNumber0(object target, object receiver) => target is StringRangeFunction range
        ? Cast<double?>(StringRangePrimitive(range, receiver, null, null), "Function result violates number type")
        : target is CharacterFunction character
        ? CharacterNumericResult(character, receiver, 0)
        : target is RelativeAtFunction method
        ? Cast<double?>(AtPrimitive(receiver, 0, method.Text), "Function result violates number type") : target is StringSearchFunction
        ? CallStringSearchNumber(target, receiver, Undefined, null, 0) : CallFunctionNumber0(target);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? CallMethodNumber1(object target, object receiver, double? first)
    {
        if (target is CharacterFunction character)
            return CharacterNumericResult(character, receiver, first.GetValueOrDefault());
        if (target is RelativeAtFunction method) {
            if (!method.Text && receiver is NumberArray numbers) {
                double length = numbers.Length(), index = AtIndex(first.GetValueOrDefault(), length);
                return index >= 0 && index < length ? numbers.GetNumberAt(ToInt32(index)) : null;
            }
            return Cast<double?>(AtPrimitive(receiver, first.GetValueOrDefault(), method.Text), "Function result violates number type");
        }
        if (target is StringRangeFunction range)
            return Cast<double?>(StringRangePrimitive(range, receiver, first, null), "Function result violates number type");
        if (target is StringSearchFunction) return CallStringSearchNumber(target, receiver, BoxNumber(first), null, 1);
        return CallFunctionNumber1(target, first);
    }
    public static double? CallMethodNumber2(object target, object receiver, double? first, double? second) => target is StringRangeFunction range
        ? Cast<double?>(StringRangePrimitive(range, receiver, first, second), "Function result violates number type")
        : target is RelativeAtFunction or CharacterFunction
        ? CallMethodNumber1(target, receiver, first) : target is StringSearchFunction
        ? CallStringSearchNumber(target, receiver, BoxNumber(first), second, 2) : CallFunctionNumber2(target, first, second);
}
