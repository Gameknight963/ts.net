using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;

// Support for operations that cannot be resolved statically. Typed numeric code
// does not pass through this representation.
public abstract class TsObject
{
    private Dictionary<string, object> extra;

    public virtual object Get(string key) =>
        extra != null && extra.TryGetValue(key, out var value) ? value : Js.Undefined;

    public virtual object Set(string key, object value)
    {
        (extra ??= new Dictionary<string, object>())[key] = value;
        return value;
    }
}

public static class Js
{
    public static int Run(Action entry)
    {
        try { entry(); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private sealed class UndefinedValue { }
    public static readonly object Undefined = new UndefinedValue();
    public static object GetUndefined() => Undefined;
    public static string TypeOf(object value) => value switch
    {
        null => "object",
        bool => "boolean",
        double => "number",
        string => "string",
        Delegate => "function",
        _ => ReferenceEquals(value, Undefined) ? "undefined" : "object"
    };
    public static double Now() => Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency);

    public static T Cast<T>(object value, string message) =>
        value is T result ? result : throw new InvalidCastException(message);

    public static double Number(object value) => value switch
    {
        double n => n,
        null => 0,
        bool b => b ? 1 : 0,
        string s => s.Trim().Length == 0 ? 0 : double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) ? n : double.NaN,
        _ => double.NaN
    };
    public static bool Boolean(object value) => Cast<bool>(value, "Expected boolean");
    public static bool Truthy(object value) => value switch
    {
        null => false,
        bool b => b,
        double n => n != 0 && !double.IsNaN(n),
        string s => s.Length != 0,
        _ => !ReferenceEquals(value, Undefined)
    };

    public static object Get(object receiver, string key) => receiver switch
    {
        TsObject obj => obj.Get(key),
        string text when key == "length" => (double)text.Length,
        string or double or bool => Undefined,
        _ => throw new InvalidOperationException("Cannot read property of undefined")
    };

    public static object Set(object receiver, string key, object value) =>
        receiver is TsObject obj ? obj.Set(key, value) :
        throw new InvalidOperationException("Property write requires an object");

    public static object Add(object left, object right)
    {
        if (left is string || right is string) return Text(left) + Text(right);
        return Number(left) + Number(right);
    }

    public static bool Equal(object left, object right)
    {
        if (left is double a && right is double b) return a == b;
        if (left is string x && right is string y) return x == y;
        if (left is bool p && right is bool q) return p == q;
        return ReferenceEquals(left, right);
    }

    public static string Text(object value) => value switch
    {
        double n when double.IsNaN(n) => "NaN",
        double n when double.IsPositiveInfinity(n) => "Infinity",
        double n when double.IsNegativeInfinity(n) => "-Infinity",
        double n when n == 0 => "0",
        double n => n.ToString("G", CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        string s => s,
        TsObject => "[object Object]",
        _ when ReferenceEquals(value, Undefined) => "undefined",
        null => "null",
        _ => throw new InvalidOperationException("Unsupported runtime value")
    };

    private static string Inspect(object value) =>
        value is double n && n == 0 && BitConverter.DoubleToInt64Bits(n) < 0 ? "-0" : Text(value);

    public static void Log(params object[] values) =>
        Console.WriteLine(string.Join(" ", Array.ConvertAll(values, Inspect)));

    public static void Discard(object value) { }
}
