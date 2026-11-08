using System;
using System.Numerics;
using System.Runtime.CompilerServices;

interface IArrayMatch<T>
{
    bool Matches(T value);
}

partial struct ArrayStorage<T>
{
    public double Find<TMatch>(uint start, uint length, ref TMatch match) where TMatch : struct, IArrayMatch<T>
    {
        uint end = Math.Min(length, (uint)dense.Length);
        for (; start < end; start++)
            if (match.Matches(dense[start])) return start;
        if (start >= length) return -1;
        // When holes match, the first absent key terminates the search. When
        // they do not, inspect stored sparse entries rather than every hole.
        if (match.Matches(missing))
        {
            for (; start < length; start++)
                if (match.Matches(Get(start))) return start;
            return -1;
        }
        double found = -1;
        if (sparse != null)
            foreach (var item in sparse)
                if (item.Key >= start && item.Key < length && (found < 0 || item.Key < found) && match.Matches(item.Value))
                    found = item.Key;
        return found;
    }
}

public partial class JsArray
{
    struct ObjectMatch : IArrayMatch<object>
    {
        public object Value;
        public double? Number;
        public bool Numeric, Includes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(object candidate)
        {
            if (ReferenceEquals(candidate, Hole))
                return Includes && (Numeric ? !Number.HasValue : Js.IsUndefined(Value));
            if (Numeric)
                return Number.HasValue ? candidate is double n && (n == Number.Value || Includes && double.IsNaN(n) && double.IsNaN(Number.Value)) : Js.IsUndefined(candidate);
            return Js.Equal(candidate, Value) || Includes && candidate is double a && Value is double b && double.IsNaN(a) && double.IsNaN(b);
        }
    }
    internal virtual double Search(object value, object fromIndex, bool includes)
    {
        if (!CanUseElementStorage) return Js.SearchArrayLike(this, value, fromIndex, includes);
        uint length = Count;
        if (length == 0) return -1;
        var match = new ObjectMatch { Value = value, Includes = includes };
        uint start = SliceBound(fromIndex, length);
        if (!CanUseElementStorage) return Js.SearchArrayRange(this, value, length, start, includes);
        return values.Find(start, length, ref match);
    }
    internal virtual double SearchNumber(double? value, object fromIndex, bool includes)
    {
        if (!CanUseElementStorage) return Js.SearchArrayLike(this, Js.BoxNumber(value), fromIndex, includes);
        uint length = Count;
        if (length == 0) return -1;
        var match = new ObjectMatch { Number = value, Numeric = true, Includes = includes };
        uint start = SliceBound(fromIndex, length);
        if (!CanUseElementStorage) return Js.SearchArrayRange(this, Js.BoxNumber(value), length, start, includes);
        return values.Find(start, length, ref match);
    }
}

public sealed partial class NumberArray
{
    struct NumberMatch : IArrayMatch<double>
    {
        public double? Value;
        public bool Includes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(double candidate)
        {
            long bits = BitConverter.DoubleToInt64Bits(candidate);
            if (bits == MissingBits) return Includes && !Value.HasValue;
            if (bits == MissingBits + 1) return !Value.HasValue;
            return Value.HasValue && (candidate == Value.Value || Includes && double.IsNaN(candidate) && double.IsNaN(Value.Value));
        }
    }
    static int FindNumber(double[] data, int start, int end, double value)
    {
        // The caller handles NaN separately. Ordinary floating-point equality
        // then matches both search methods, including +0 versus -0, while the
        // reserved hole/undefined NaNs cannot produce a false match.
        int index = start;
        if (Vector.IsHardwareAccelerated)
        {
            var target = new Vector<double>(value);
            int width = Vector<double>.Count;
            for (; index <= end - width; index += width)
            {
                if (!Vector.EqualsAny(new Vector<double>(data, index), target)) continue;
                for (int lane = 0; lane < width; lane++)
                    if (data[index + lane] == value) return index + lane - start;
            }
        }
        for (; index < end; index++) if (data[index] == value) return index - start;
        return -1;
    }
    internal override double Search(object value, object fromIndex, bool includes)
    {
        if (!CanUseElementStorage) return Js.SearchArrayLike(this, value, fromIndex, includes);
        if (value is double n) return SearchNumber(n, fromIndex, includes);
        if (Js.IsUndefined(value)) return SearchNumber(null, fromIndex, includes);
        uint length = Count;
        if (length == 0) return -1;
        uint start = SliceBound(fromIndex, length);
        return CanUseElementStorage ? -1 : Js.SearchArrayRange(this, value, length, start, includes);
    }
    internal override double SearchNumber(double? value, object fromIndex, bool includes)
    {
        if (!CanUseElementStorage) return Js.SearchArrayLike(this, Js.BoxNumber(value), fromIndex, includes);
        uint length = Count;
        if (length == 0) return -1;
        uint start = SliceBound(fromIndex, length);
        if (!CanUseElementStorage) return Js.SearchArrayRange(this, Js.BoxNumber(value), length, start, includes);
        var match = new NumberMatch { Value = value, Includes = includes };
        if (value.HasValue && !double.IsNaN(value.Value))
        {
            uint end = Math.Min(length, (uint)numbers.Dense.Length);
            if (start < end)
            {
                int found = FindNumber(numbers.Dense, (int)start, (int)end, value.Value);
                if (found >= 0) return start + (double)found;
                start = end;
            }
        }
        else if (value.HasValue && !includes) return -1;
        return numbers.Find(start, length, ref match);
    }
}
