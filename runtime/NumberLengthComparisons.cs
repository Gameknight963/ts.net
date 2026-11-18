using System;

public static partial class Js
{
    public static bool NumberLengthRelational(double? left, object receiver, int mode)
    {
        double length;
        if (receiver is string text) length = text.Length;
        else if (receiver is JsArray array && (array.GetType() == typeof(JsArray) ||
            array is NumberArray or TaggedArray or ReversibleArray or JsTemplateArray)) length = array.Length();
        else return Relational(BoxNumber(left), Get(receiver, "length"), mode);
        double number = NumberValue(left);
        return mode switch { 0 => number < length, 1 => number > length, 2 => number <= length, 3 => number >= length,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
    }
}
