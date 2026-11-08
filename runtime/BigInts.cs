using System;
using System.Globalization;
using System.Numerics;
using System.Text;

public static partial class Js
{
    static double ExplicitNumber(object value)
    {
        if (value is TsObject obj) value = ToPrimitive(obj, "number");
        return value is BigInteger integer ? (double)integer : Number(value);
    }

    public static object NumericNegate(object value)
    {
        if (value is TsObject obj) value = ToPrimitive(obj, "number");
        return value is BigInteger integer ? -integer : -Number(value);
    }

    static bool BigIntEqual(BigInteger integer, object value)
    {
        if (value is TsObject obj) return BigIntEqual(integer, ToPrimitive(obj, "default"));
        if (value is BigInteger other) return integer == other;
        if (value is bool boolean) return integer == (boolean ? BigInteger.One : BigInteger.Zero);
        if (value is double number) return double.IsFinite(number) && number == Math.Truncate(number) && integer == new BigInteger(number);
        if (value is string text) { try { return integer == ParseBigInt(text); } catch (JsThrownException) { return false; } }
        return false;
    }

    static int? CompareBigInt(BigInteger integer, object value)
    {
        if (value is BigInteger other) return integer.CompareTo(other);
        if (value is string text) { try { return integer.CompareTo(ParseBigInt(text)); } catch (JsThrownException) { return null; } }
        double number = Number(value);
        if (double.IsNaN(number)) return null;
        if (double.IsPositiveInfinity(number)) return -1;
        if (double.IsNegativeInfinity(number)) return 1;
        int order = integer.CompareTo(new BigInteger(number));
        return order != 0 || number == Math.Truncate(number) ? order : number > 0 ? -1 : 1;
    }

    static BigInteger ParseBigInt(string source)
    {
        ReadOnlySpan<char> text = source.AsSpan();
        int first = 0, last = text.Length;
        while (first < last && NumberSpace(text[first])) first++;
        while (last > first && NumberSpace(text[last - 1])) last--;
        text = text[first..last];
        if (text.Length == 0) return BigInteger.Zero;
        int radix = 10, start = 0;
        if (text.Length > 2 && text[0] == '0') {
            radix = char.ToLowerInvariant(text[1]) switch { 'x' => 16, 'o' => 8, 'b' => 2, _ => 10 };
            if (radix != 10) start = 2;
        }
        bool negative = false;
        if (radix == 10 && text[0] is '+' or '-') { negative = text[0] == '-'; start = 1; }
        if (start == text.Length) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid BigInt string"));
        BigInteger result = BigInteger.Zero;
        for (int i = start; i < text.Length; i++) {
            char c = char.ToLowerInvariant(text[i]);
            int digit = c is >= '0' and <= '9' ? c - '0' : c is >= 'a' and <= 'f' ? c - 'a' + 10 : -1;
            if (digit < 0 || digit >= radix) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid BigInt string"));
            result = result * radix + digit;
        }
        return negative ? -result : result;
    }

    static BigInteger BigIntValue(object value, bool allowNumber)
    {
        if (value is TsObject obj) value = ToPrimitive(obj, "number");
        return value switch {
            BigInteger integer => integer,
            string text => ParseBigInt(text),
            bool boolean => boolean ? BigInteger.One : BigInteger.Zero,
            double number when allowNumber => double.IsFinite(number) && number == Math.Truncate(number)
                ? new BigInteger(number) : throw new ArgumentOutOfRangeException(nameof(value), "BigInt requires an integer"),
            _ => throw new InvalidOperationException("Cannot convert value to BigInt")
        };
    }

    static BigInteger RequireBigInt(object receiver) => (receiver is JsBoxedPrimitive boxed ? boxed.Value : receiver) is BigInteger integer
        ? integer : throw new InvalidOperationException("BigInt method requires a bigint");

    static string BigIntText(BigInteger integer, object radixValue)
    {
        double requested = IsUndefined(radixValue) ? 10 : Number(radixValue);
        requested = double.IsNaN(requested) ? 0 : Math.Truncate(requested);
        if (requested < 2 || requested > 36) throw new ArgumentOutOfRangeException(nameof(radixValue), "Radix must be between 2 and 36");
        int radix = (int)requested;
        if (radix == 10) return integer.ToString(CultureInfo.InvariantCulture);
        if (integer.IsZero) return "0";
        bool negative = integer.Sign < 0;
        integer = BigInteger.Abs(integer);
        var digits = new StringBuilder();
        while (!integer.IsZero) {
            integer = BigInteger.DivRem(integer, radix, out var remainder);
            digits.Append("0123456789abcdefghijklmnopqrstuvwxyz"[(int)remainder]);
        }
        if (negative) digits.Append('-');
        char[] result = digits.ToString().ToCharArray(); Array.Reverse(result);
        return new string(result);
    }

    static object BigIntWidth(object[] args, bool signed)
    {
        double bits = Number(FunctionArgument(args, 0));
        bits = double.IsNaN(bits) ? 0 : Math.Truncate(bits);
        if (bits < 0 || bits > 9007199254740991d) throw new ArgumentOutOfRangeException(nameof(args), "BigInt width is out of range");
        BigInteger value = BigIntValue(FunctionArgument(args, 1), false);
        if (bits == 0) return BigInteger.Zero;
        if (signed && value.GetBitLength() < bits || !signed && value.Sign >= 0 && value.GetBitLength() <= bits) return value;
        if (bits > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(args), "BigInt result exceeds the supported size");
        int count = (int)bits;
        BigInteger modulus = BigInteger.One << count;
        BigInteger result = value % modulus;
        if (result.Sign < 0) result += modulus;
        if (signed && result >= (modulus >> 1)) result -= modulus;
        return result;
    }

    sealed class BigIntFunction : NativeNamespaceFunction
    {
        internal BigIntFunction() : base("BigInt", static args => BigIntValue(FunctionArgument(args, 0), true), 1)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype };
            UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "BigInt");
            prototype.DefineMethod("valueOf", new CollectionMethod("valueOf", 0, (receiver, _) => RequireBigInt(receiver)));
            prototype.DefineMethod("toString", new CollectionMethod("toString", 0, (receiver, args) => BigIntText(RequireBigInt(receiver), FunctionArgument(args, 0))));
            prototype.DefineMethod("toLocaleString", new NumberLocaleFunction(true));
            Member("asIntN", new JsFunction((FunctionBody)(static args => BigIntWidth(args, true)), null, "asIntN", 2));
            Member("asUintN", new JsFunction((FunctionBody)(static args => BigIntWidth(args, false)), null, "asUintN", 2));
        }
        public override object Construct(object[] args) => throw new InvalidOperationException("BigInt is not a constructor");
    }
}
