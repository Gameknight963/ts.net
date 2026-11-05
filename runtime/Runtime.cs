using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    public static bool IsUndefined(object value) => ReferenceEquals(value, Undefined);
    public static bool IsNullish(object value) => value == null || ReferenceEquals(value, Undefined);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt32(double value)
    {
        if (value >= int.MinValue && value <= int.MaxValue) return (int)value;
        return ToInt32Slow(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ToInt32Slow(double value)
    {
        if (!double.IsFinite(value)) return 0;
        double reduced = Math.Truncate(value) % 4294967296.0;
        if (reduced < 0) reduced += 4294967296.0;
        return unchecked((int)(uint)reduced);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Cast<T>(object value, string message)
    {
        if (ReferenceEquals(value, Undefined) && Nullable.GetUnderlyingType(typeof(T)) != null) return default;
        return value is T result ? result : throw new InvalidCastException(message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Check<T>(object value, string message) where T : class =>
        ReferenceEquals(value, Undefined) || value is T ? value : throw new InvalidCastException(message);
    public static int StringLength(object value) =>
        value is string text ? text.Length : throw new InvalidOperationException("Expected string");

    // Empty nullable values mean JS undefined; CLR null stays a separate JS value.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? WrapNumber(double value) => value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool? WrapBoolean(bool value) => value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? EmptyNumber() => null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool? EmptyBoolean() => null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object BoxNumber(double? value) => value.HasValue ? (object)value.Value : Undefined;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object BoxBoolean(bool? value) => value.HasValue ? (object)value.Value : Undefined;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NumberValue(double? value) => value.GetValueOrDefault(double.NaN);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BooleanValue(bool? value) => value.GetValueOrDefault();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NumberMissing(double? value) => !value.HasValue;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BooleanMissing(bool? value) => !value.HasValue;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NumberTruthy(double? value) => value.HasValue && value.Value != 0 && !double.IsNaN(value.Value);

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

    public static object IndexGet(object receiver, object key)
    {
        if (receiver is JsArray array) return array.GetIndex(key);
        string text = Text(key);
        if (receiver is string value && uint.TryParse(text, out uint index) && index < value.Length && text == index.ToString(CultureInfo.InvariantCulture))
            return value.Substring((int)index, 1);
        return Get(receiver, text);
    }
    public static void IndexSet(object receiver, object key, object value)
    {
        if (receiver is JsArray array) array.SetIndex(key, value);
        else Set(receiver, Text(key), value);
    }
    public static double ArrayPush(object receiver, object[] values) =>
        receiver is JsArray array ? array.PushMany(values) : throw new InvalidOperationException("push requires an array");
    public static object ArrayPop(object receiver) =>
        receiver is JsArray array ? array.Pop() : throw new InvalidOperationException("pop requires an array");
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? ArrayGetNumber(object receiver, double index) => receiver is NumberArray numbers ? numbers.GetNumber(index) :
        Cast<double?>(IndexGet(receiver, index), "Array element is not a number");
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ArraySetNumber(object receiver, double index, double? value)
    {
        if (receiver is NumberArray numbers) numbers.SetNumber(index, value);
        else IndexSet(receiver, index, BoxNumber(value));
    }
    public static double ArrayLength(object receiver) =>
        receiver is JsArray array ? array.Length() : throw new InvalidOperationException("Expected array");
    public static void ArraySetLength(object receiver, double length)
    {
        if (receiver is not JsArray array) throw new InvalidOperationException("Expected array");
        array.SetLength(length);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? ArrayGetNumberAt(object receiver, int index) => receiver is NumberArray numbers ? numbers.GetNumberAt(index) : ArrayGetNumber(receiver, index);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ArraySetNumberAt(object receiver, int index, double? value)
    {
        if (receiver is NumberArray numbers) numbers.SetNumberAt(index, value);
        else ArraySetNumber(receiver, index, value);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ArrayPushNumber(object receiver, double? value) => receiver is NumberArray numbers ? numbers.PushNumber(value) :
        ArrayPush(receiver, new object[] { BoxNumber(value) });
    public static double? ArrayPopNumber(object receiver) => receiver is NumberArray numbers ? numbers.PopNumber() :
        Cast<double?>(ArrayPop(receiver), "Array element is not a number");
    public static double[] ArrayTryPacked(object receiver, double limit) => receiver is NumberArray numbers ? numbers.TryPacked(limit) : null;
    public static void ArrayReservePushes(object receiver, double additions)
    {
        if (receiver is NumberArray numbers) numbers.ReservePushes(additions);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CanonicalNumber(double value) => double.IsNaN(value) ? double.NaN : value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ForOfLength(object value) => Iteration.Length(value);
    public static object ForOfValue(object value, int position) => Iteration.Value(value, position);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? ForOfNumber(object value, int position) => Iteration.Number(value, position);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ForOfStep(object value, int position) => Iteration.Step(value, position);

    public static object Add(object left, object right)
    {
        if (left is string || right is string) return Text(left) + Text(right);
        return Number(left) + Number(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualNumbers(double? left, double? right) =>
        left.HasValue == right.HasValue && (!left.HasValue || left.GetValueOrDefault() == right.GetValueOrDefault());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualBooleans(bool? left, bool? right) => left == right;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Remainder(double value, double divisor)
    {
        // Preserve general floating-point semantics outside this exact integer case.
        // Excluding zero also preserves the sign of negative zero in the fallback.
        if (divisor >= 1 && divisor <= int.MaxValue && value > 0 && value <= int.MaxValue)
        {
            int modulus = (int)divisor;
            int integer = (int)value;
            if (modulus == divisor && integer == value && (modulus & (modulus - 1)) == 0)
                return integer & (modulus - 1);
        }
        return value % divisor;
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
