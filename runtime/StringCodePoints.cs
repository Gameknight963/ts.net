using System;
using System.Text;

public static partial class Js
{
    static object StringFromCharCode(object[] arguments)
    {
        if (arguments.Length == 0) return "";
        if (arguments.Length == 1) return Iteration.CodeUnit(unchecked((char)ToInt32(Number(arguments[0]))));
        var units = new char[arguments.Length];
        for (int index = 0; index < units.Length; index++) units[index] = unchecked((char)ToInt32(Number(arguments[index])));
        return new string(units);
    }
    static object StringFromCodePoint(object[] arguments)
    {
        if (arguments.Length == 0) return "";
        var result = new StringBuilder(arguments.Length);
        foreach (object argument in arguments)
        {
            double number = Number(argument);
            if (!double.IsFinite(number) || number < 0 || number > 0x10ffff || Math.Truncate(number) != number)
                throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid code point");
            int point = (int)number;
            // Surrogate values themselves are accepted, unlike ConvertFromUtf32.
            if (point <= 0xffff) result.Append((char)point);
            else
            {
                point -= 0x10000;
                result.Append((char)(0xd800 + (point >> 10)));
                result.Append((char)(0xdc00 + (point & 0x3ff)));
            }
        }
        return result.ToString();
    }
}
