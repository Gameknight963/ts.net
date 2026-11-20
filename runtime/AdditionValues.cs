using System.Runtime.CompilerServices;

// Temporary addition results only; user-visible values keep their existing ABI.
public readonly struct JsAdditionValue
{
    // A null reference marks a number; JavaScript null uses additionNull instead.
    internal readonly object Reference;
    internal readonly double Number;
    internal JsAdditionValue(double value) { Number = value; Reference = null; }
    internal JsAdditionValue(object value) { Number = 0; Reference = value; }
}

public static partial class Js
{
    static readonly object additionNull = new();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsAdditionValue AdditionFromValue(object value) => value is double number
        ? new(number) : new(value ?? additionNull);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsAdditionValue AdditionFromNumber(double? value) => value.HasValue
        ? new(value.GetValueOrDefault()) : new(Undefined);
    public static JsAdditionValue ReadAdditionProperty(object receiver, string key) =>
        receiver is TsObject obj && obj.TryReadDynamicNumber(key, out double? number)
            ? AdditionFromNumber(number) : AdditionFromValue(Get(receiver, key));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsAdditionValue AddAdditionValues(JsAdditionValue left, JsAdditionValue right) =>
        left.Reference == null && right.Reference == null
            ? new(left.Number + right.Number) : AddAdditionValuesSlow(left, right);
    [MethodImpl(MethodImplOptions.NoInlining)]
    static JsAdditionValue AddAdditionValuesSlow(JsAdditionValue left, JsAdditionValue right) =>
        AdditionFromValue(Add(BoxAdditionValue(left), BoxAdditionValue(right)));
    public static JsAdditionValue AddAdditionProperty(JsAdditionValue left, object receiver, string key)
    {
        if (receiver is TsObject obj && obj.TryReadDynamicNumber(key, out double? number)) {
            if (left.Reference == null && number.HasValue) return new(left.Number + number.GetValueOrDefault());
            return AddAdditionValues(left, AdditionFromNumber(number));
        }
        return AddAdditionValues(left, AdditionFromValue(Get(receiver, key)));
    }
    static object BoxAdditionValue(JsAdditionValue value) => value.Reference == null
        ? BoxNumber(value.Number) : ReferenceEquals(value.Reference, additionNull) ? null : value.Reference;
    public static double? CheckedAdditionNumber(JsAdditionValue value) => value.Reference == null
        ? value.Number : Cast<double?>(BoxAdditionValue(value), "checked conversion failed");
    public static double? AddAdditionToNumber(double? left, JsAdditionValue right) => right.Reference == null
        ? NumberValue(left) + right.Number : AddToNumber(left, BoxAdditionValue(right));
}
