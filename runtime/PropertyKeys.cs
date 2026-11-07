using System;
using System.Globalization;

public static partial class Js
{
    static bool KeyPrimitive(object value) => value == null || ReferenceEquals(value, Undefined) ||
        value is string || value is double || value is bool;

    public static string PropertyKey(object value)
    {
        if (value is TsObject obj) value = OrdinaryPrimitive(obj, true);
        return value is double number ? NumberKeyString(number) : Text(value);
    }

    static object OrdinaryPrimitive(TsObject obj, bool stringHint)
    {
        object primitive = obj;
        for (int step = 0; step < 2; step++) {
            string name = (step == 0) == stringHint ? "toString" : "valueOf";
            if (obj.HasProperty(name)) {
                object method = obj.Get(name);
                if (method is JsFunction) {
                    primitive = CallMethod0(method, obj);
                    if (KeyPrimitive(primitive)) break;
                }
            } else if (name == "toString" && obj.HasInProperty(name)) {
                if (obj is JsArray array) {
                    if (array.HasOwn("join")) {
                        object join = array.Get("join");
                        primitive = join is JsFunction ? CallMethod0(join, array) : "[object Array]";
                    } else primitive = ArrayJoin(array, Undefined);
                } else if (obj is JsCaughtError error) {
                    primitive = ErrorText(error);
                } else if (obj is JsFunction function) {
                    primitive = FunctionDisplayText(function);
                }
                else primitive = obj is JsArguments ? "[object Arguments]" : "[object Object]";
                if (KeyPrimitive(primitive)) break;
            }
        }
        if (!KeyPrimitive(primitive)) throw new InvalidOperationException("Cannot convert object to primitive value");
        return primitive;
    }

    static string NumberKeyString(double value)
    {
        if (!double.IsFinite(value) || value == 0) return Text(value);
        string raw = value.ToString("R", CultureInfo.InvariantCulture);
        int e = raw.IndexOf('E');
        if (e < 0) return raw;
        int exponent = int.Parse(raw.AsSpan(e + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        if (exponent < -6 || exponent >= 21)
            return raw[..e] + "e" + (exponent >= 0 ? "+" : "") + exponent.ToString(CultureInfo.InvariantCulture);
        bool negative = raw[0] == '-';
        string digits = raw[(negative ? 1 : 0)..e].Replace(".", "");
        string sign = negative ? "-" : "";
        int point = exponent + 1;
        if (point <= 0) return sign + "0." + new string('0', -point) + digits;
        if (point >= digits.Length) return sign + digits + new string('0', point - digits.Length);
        return sign + digits[..point] + "." + digits[point..];
    }
}
