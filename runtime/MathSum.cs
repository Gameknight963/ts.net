using System;
using System.Numerics;

public static partial class Js
{
    static object MathSumPrecise(object[] arguments)
    {
        object input = FunctionArgument(arguments, 0);
        TsObject iterable = ToObject(input);
        string iteratorKey = PropertyKey(IteratorSymbol);
        object iteratorMethod = iterable.Get(iteratorKey), source;
        if (iteratorMethod is JsFunction method) {
            if (method.InvokeWithReceiver(input, Array.Empty<object>()) is not TsObject iterator)
                throw new InvalidOperationException("Iterator must be an object");
            source = StartIteratorObject(iterator);
        } else if (IsNullish(iteratorMethod) && !iterable.HasOwn(iteratorKey) && input is JsArray or JsArguments or JsKeyedCollection or string or JsBoxedPrimitive {Value:string})
            source = Iteration.Start(input);
        else throw new InvalidOperationException("Math.sumPrecise requires an iterable");
        // Each bin covers 16 adjacent binary exponents. A 53-bit significand,
        // at most 15 shift bits and fewer than 2^53 items need at most 121
        // magnitude bits, so signed 128-bit bins cannot overflow.
        Span<Int128> bins = stackalloc Int128[128];
        bins.Clear();
        long count = 0;
        int position = 0;
        bool closeable = false, positiveInfinity = false, negativeInfinity = false, nan = false, negativeZero = true;
        try {
            while (true) {
                closeable = false;
                if (position >= Iteration.Length(source)) break;
                object item = Iteration.Value(source, position);
                position += Iteration.Step(source, position); closeable = true;
                // Protocol cursors keep their own position; do not overflow
                // this unused array index for long-running generators.
                if (source is JsProtocolIterator) position = 0;
                if (count++ >= 9007199254740991L) throw new ArgumentOutOfRangeException("items", "Too many summation items");
                if (item is not double value) throw new InvalidOperationException("Math.sumPrecise items must be numbers");
                if (double.IsNaN(value)) {nan = true; continue;}
                if (double.IsPositiveInfinity(value)) {positiveInfinity = true; continue;}
                if (double.IsNegativeInfinity(value)) {negativeInfinity = true; continue;}
                if (value != 0 || !double.IsNegative(value)) negativeZero = false;
                ulong bits = BitConverter.DoubleToUInt64Bits(value);
                int exponent = (int)((bits >> 52) & 2047);
                ulong significand = bits & 0xfffffffffffffUL;
                if (exponent != 0) significand |= 1UL << 52;
                int exponentShift = Math.Max(0, exponent - 1);
                Int128 term = (Int128)significand << (exponentShift & 15);
                bins[exponentShift >> 4] += (bits >> 63) == 0 ? term : -term;
            }
        }
        catch { if (closeable) CloseIterationAt(source, position, true); throw; }
        if (nan || positiveInfinity && negativeInfinity) return double.NaN;
        if (positiveInfinity) return double.PositiveInfinity;
        if (negativeInfinity) return double.NegativeInfinity;
        if (negativeZero) return -0d;
        BigInteger exact = BigInteger.Zero;
        for (int exponent = 0; exponent < bins.Length; exponent++)
            if (bins[exponent] != 0) exact += (BigInteger)bins[exponent] << (exponent * 16);
        if (exact.IsZero) return 0d;
        bool negative = exact.Sign < 0;
        exact = BigInteger.Abs(exact);
        int shift = Math.Max(0, (int)exact.GetBitLength() - 53);
        BigInteger rounded = exact >> shift;
        if (shift != 0) {
            BigInteger remainder = exact - (rounded << shift), halfway = BigInteger.One << (shift - 1);
            if (remainder > halfway || remainder == halfway && !rounded.IsEven) rounded++;
        }
        double result = Math.ScaleB((double)rounded, shift - 1074);
        return negative ? -result : result;
    }
}
