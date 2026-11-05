using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

// Dense storage handles ordinary arrays. Distant indices stay sparse so a write
// near the JavaScript length limit does not request a multi-gigabyte buffer.
sealed class ArrayStorage<T>
{
    T[] dense;
    Dictionary<uint, T> sparse;
    readonly T missing;
    public uint Length;
    public ArrayStorage(int length, T missing)
    {
        this.missing = missing;
        Length = (uint)length;
        dense = length == 0 ? Array.Empty<T>() : new T[Math.Min(length, 4096)];
        Array.Fill(dense, missing);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(uint index)
    {
        if (index >= Length) return missing;
        if (index < (uint)dense.Length) return dense[index];
        return sparse != null && sparse.TryGetValue(index, out T value) ? value : missing;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(uint index, T value)
    {
        if (index < (uint)dense.Length)
        {
            dense[index] = value;
            if (index >= Length) Length = index + 1;
        }
        else SetSlow(index, value);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    void SetSlow(uint index, T value)
    {
        if (index < 1048576 && index <= Math.Max(16u, (uint)dense.Length * 2))
        {
            int old = dense.Length;
            int capacity = (int)Math.Max(index + 1, Math.Max(16u, (uint)old * 2));
            Array.Resize(ref dense, capacity);
            Array.Fill(dense, missing, old, capacity - old);
            if (sparse != null)
            {
                var keys = new List<uint>(sparse.Keys);
                foreach (uint key in keys)
                    if (key < (uint)capacity) { dense[key] = sparse[key]; sparse.Remove(key); }
            }
            dense[index] = value;
        }
        else (sparse ??= new Dictionary<uint, T>())[index] = value;
        if (index >= Length) Length = index + 1;
    }
    public void Resize(uint length)
    {
        if (length < Length)
        {
            uint end = Math.Min(Length, (uint)dense.Length);
            if (length < end) Array.Fill(dense, missing, (int)length, (int)(end - length));
            if (sparse != null)
            {
                var keys = new List<uint>(sparse.Keys);
                foreach (uint key in keys) if (key >= length) sparse.Remove(key);
            }
        }
        Length = length;
    }
}

public class JsArray : TsObject
{
    readonly ArrayStorage<object> values;
    public JsArray(int length) { values = new ArrayStorage<object>(length, Js.Undefined); }
    protected JsArray() { }
    protected virtual uint Count => values.Length;
    protected virtual object Read(uint index) => values.Get(index);
    protected virtual void Write(uint index, object value) => values.Set(index, value);
    protected virtual void Resize(uint length) => values.Resize(length);
    public double Length() => Count;
    public void SetLength(double length)
    {
        if (!(length >= 0 && length <= uint.MaxValue && length == (uint)length))
            throw new InvalidOperationException("Invalid array length");
        Resize((uint)length);
    }
    static bool Index(string key, out uint index) =>
        uint.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out index) &&
        index != uint.MaxValue && key == index.ToString(CultureInfo.InvariantCulture);
    public override object Get(string key) => key == "length" ? (double)Count : Index(key, out uint index) ? Read(index) : base.Get(key);
    public override object Set(string key, object value)
    {
        if (key == "length") SetLength(Js.Number(value));
        else if (Index(key, out uint index)) Write(index, value);
        else return base.Set(key, value);
        return value;
    }
    public object GetIndex(object key) => Get(Js.Text(key));
    public void SetIndex(object key, object value) => Set(Js.Text(key), value);
    public double PushMany(object[] items)
    {
        foreach (object value in items)
        {
            uint index = Count;
            if (index == uint.MaxValue) throw new InvalidOperationException("Invalid array length");
            Write(index, value);
        }
        return Count;
    }
    public object Pop()
    {
        uint length = Count;
        if (length == 0) return Js.Undefined;
        object value = Read(length - 1);Resize(length - 1);return value;
    }
}

public sealed class NumberArray : JsArray
{
    // Canonicalize stored numeric NaNs, reserving a distinct NaN payload for
    // undefined. Public reads still return the nullable scalar ABI.
    const long MissingBits = 0x7ff8000000000001;
    static readonly double Missing = BitConverter.Int64BitsToDouble(MissingBits);
    readonly ArrayStorage<double> numbers;
    public NumberArray(int length) { numbers = new ArrayStorage<double>(length, Missing); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double Encode(double? value) => !value.HasValue ? Missing : double.IsNaN(value.Value) ? double.NaN : value.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double? Decode(double value) => BitConverter.DoubleToInt64Bits(value) == MissingBits ? null : value;
    protected override uint Count => numbers.Length;
    protected override object Read(uint index) => Js.BoxNumber(Decode(numbers.Get(index)));
    protected override void Write(uint index, object value) => numbers.Set(index, Encode(Js.Cast<double?>(value, "Array write violates number element type")));
    protected override void Resize(uint length) => numbers.Resize(length);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double? GetNumber(double index)
    {
        if (index >= 0 && index < uint.MaxValue && index == (uint)index) return Decode(numbers.Get((uint)index));
        return Js.Cast<double?>(Get(Js.Text(index)), "Array element is not a number");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double? GetNumberAt(int index) => Decode(numbers.Get((uint)index));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNumberAt(int index, double? value) => numbers.Set((uint)index, Encode(value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNumber(double index, double? value)
    {
        if (index >= 0 && index < uint.MaxValue && index == (uint)index) numbers.Set((uint)index, Encode(value));
        else Set(Js.Text(index), Js.BoxNumber(value));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double PushNumber(double? value)
    {
        uint index = numbers.Length;
        if (index == uint.MaxValue) throw new InvalidOperationException("Invalid array length");
        numbers.Set(index, Encode(value));return numbers.Length;
    }
    public double? PopNumber()
    {
        uint length = numbers.Length;
        if (length == 0) return null;
        double? value = Decode(numbers.Get(length - 1));numbers.Resize(length - 1);return value;
    }
}
