using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    public static string PropertyKey(object value) => PropertyKey(value, null);

    static bool KeyPrimitive(object value) => value == null || ReferenceEquals(value, Undefined) ||
        value is string || value is double || value is bool;

    static string PropertyKey(object value, HashSet<JsArray> active)
    {
        if (value is TsObject obj) {
            object primitive = obj;
            for (int step = 0; step < 2; step++) {
                string name = step == 0 ? "toString" : "valueOf";
                if (obj.HasOwn(name)) {
                    object method = obj.Get(name);
                    if (method is JsFunction) {
                        primitive = CallFunction0(method);
                        if (KeyPrimitive(primitive)) break;
                    }
                } else if (step == 0) {
                    if (obj is JsArray array) {
                        if (array.HasOwn("join")) {
                            object join = array.Get("join");
                            primitive = join is JsFunction ? CallFunction0(join) : "[object Array]";
                        } else primitive = ArrayKeyString(array, active);
                    } else if (obj is JsFunction) {
                        string functionName = obj.Get("name") as string ?? "";
                        bool identifier = functionName.Length != 0;
                        for (int i = 0; i < functionName.Length; i++) {
                            char ch = functionName[i];
                            if (!(ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_' or '$' || i != 0 && ch is >= '0' and <= '9')) identifier = false;
                        }
                        primitive = "function " + (identifier ? functionName : "") + "() { [native code] }";
                    }
                    else primitive = "[object Object]";
                    if (KeyPrimitive(primitive)) break;
                }
            }
            if (!KeyPrimitive(primitive)) throw new InvalidOperationException("Cannot convert object to primitive value");
            value = primitive;
        }
        return value is double number ? NumberKeyString(number) : Text(value);
    }

    static string ArrayKeyString(JsArray array, HashSet<JsArray> active)
    {
        uint length = (uint)array.Length();
        if (length == 0) return "";
        active ??= new HashSet<JsArray>();
        if (!active.Add(array)) return "";
        try {
            var result = new StringBuilder();
            for (uint i = 0; i < length; i++) {
                if (i != 0) result.Append(',');
                object item = array.ElementAt(i);
                if (!IsNullish(item)) result.Append(PropertyKey(item, active));
            }
            return result.ToString();
        } finally { active.Remove(array); }
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
