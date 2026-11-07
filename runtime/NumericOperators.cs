using System;
using System.Numerics;

public static partial class Js
{
    public static object NumericValue(object value)
    {
        if (value is TsObject obj) value = ToPrimitive(obj, "number");
        return value is BigInteger ? value : Number(value);
    }
    public static object NumericStep(object value, int delta) => value is BigInteger integer ? integer + delta : Number(value) + delta;
    public static object NumericBitNot(object value)
    {
        value = NumericValue(value);
        return value is BigInteger integer ? ~integer : (double)~ToInt32((double)value);
    }
    static BigInteger BigIntShift(BigInteger value, BigInteger count, bool left)
    {
        if (count.Sign < 0) return BigIntShift(value, -count, !left);
        if (value.IsZero) return value;
        if (!left && count >= value.GetBitLength()) return value.Sign < 0 ? -BigInteger.One : BigInteger.Zero;
        if (count > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(count), "BigInt shift is too large");
        return left ? value << (int)count : value >> (int)count;
    }
    static BigInteger BigIntPower(BigInteger value, BigInteger exponent)
    {
        if (exponent.Sign < 0) throw new ArgumentOutOfRangeException(nameof(exponent), "BigInt exponent must not be negative");
        if (exponent.IsZero) return BigInteger.One;
        if (value.IsZero || value.IsOne) return value;
        if (value == -BigInteger.One) return exponent.IsEven ? BigInteger.One : -BigInteger.One;
        if (exponent > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(exponent), "BigInt exponent is too large");
        return BigInteger.Pow(value, (int)exponent);
    }
    public static object NumericBinary(object left, object right, string operation)
    {
        left = NumericValue(left); right = NumericValue(right);
        if (left is BigInteger || right is BigInteger) {
            if (left is not BigInteger a || right is not BigInteger b) throw new InvalidOperationException("Cannot mix bigint and number operands");
            return operation switch {
                "-" or "-=" => a - b,
                "*" or "*=" => a * b,
                "/" or "/=" => b.IsZero ? throw new ArgumentOutOfRangeException(nameof(right), "BigInt division by zero") : a / b,
                "%" or "%=" => b.IsZero ? throw new ArgumentOutOfRangeException(nameof(right), "BigInt division by zero") : a % b,
                "**" or "**=" => BigIntPower(a, b),
                "&" or "&=" => a & b,
                "|" or "|=" => a | b,
                "^" or "^=" => a ^ b,
                "<<" or "<<=" => BigIntShift(a, b, true),
                ">>" or ">>=" => BigIntShift(a, b, false),
                ">>>" or ">>>=" => throw new InvalidOperationException("BigInt does not support unsigned right shift"),
                _ => throw new InvalidOperationException("Unknown numeric operator")
            };
        }
        double x = (double)left, y = (double)right;
        return operation switch {
            "-" or "-=" => x - y, "*" or "*=" => x * y, "/" or "/=" => x / y,
            "%" or "%=" => Remainder(x, y), "**" or "**=" => Power(x, y),
            "&" or "&=" => (double)(ToInt32(x) & ToInt32(y)), "|" or "|=" => (double)(ToInt32(x) | ToInt32(y)),
            "^" or "^=" => (double)(ToInt32(x) ^ ToInt32(y)),
            "<<" or "<<=" => (double)(ToInt32(x) << (ToInt32(y) & 31)),
            ">>" or ">>=" => (double)(ToInt32(x) >> (ToInt32(y) & 31)),
            ">>>" or ">>>=" => (double)((uint)ToInt32(x) >> (ToInt32(y) & 31)),
            _ => throw new InvalidOperationException("Unknown numeric operator")
        };
    }
}
