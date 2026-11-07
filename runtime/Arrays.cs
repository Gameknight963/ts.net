using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

// Dense storage handles ordinary arrays. Distant indices stay sparse so a write
// near the JavaScript length limit does not request a multi-gigabyte buffer.
partial struct ArrayStorage<T>
{
    T[] dense;
    Dictionary<uint, T> sparse;
    readonly T missing;
    public uint Length;
    public T[] Dense => dense;
    public bool InitializeCopy(T[] source, int count, int extra)
    {
        if (Length != 0 || dense.Length != 0) return false;
        // Every slot is initialized before publishing the new backing array.
        T[] copy = GC.AllocateUninitializedArray<T>(count + extra);
        Array.Copy(source, copy, count);
        Array.Fill(copy, missing, count, extra);
        dense = copy;Length = (uint)count;
        return true;
    }
    public void Reserve(int capacity)
    {
        if (capacity <= dense.Length) return;
        int old = dense.Length;
        Array.Resize(ref dense, capacity);
        Array.Fill(dense, missing, old, capacity - old);
        if (sparse != null)
        {
            var keys = new List<uint>(sparse.Keys);
            foreach (uint key in keys)
                if (key < (uint)capacity) { dense[key] = sparse[key]; sparse.Remove(key); }
        }
    }
    public ArrayStorage(int length, T missing)
    {
        this.missing = missing;
        Length = (uint)length;
        dense = length == 0 ? Array.Empty<T>() : new T[Math.Min(length, 4096)];
        Array.Fill(dense, missing);
    }
    public ArrayStorage(T[] items, T missing)
    {
        this.missing = missing;dense = items;Length = (uint)items.Length;
    }
    public ArrayStorage<T> Slice(uint start, uint count)
    {
        uint end = Math.Min(Length, (uint)dense.Length);
        int copied = start < end ? (int)Math.Min(count, end - start) : 0;
        T[] buffer = copied == 0 ? Array.Empty<T>() : GC.AllocateUninitializedArray<T>(copied);
        if (copied != 0) Array.Copy(dense, (int)start, buffer, 0, copied);
        var result = new ArrayStorage<T>(buffer, missing) { Length = count };
        if (sparse != null)
            foreach (var item in sparse)
                if (item.Key >= start && item.Key - start < count) result.Set(item.Key - start, item.Value);
        return result;
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
        if (index < (uint)Array.MaxLength && index <= Math.Max(16u, (uint)dense.Length * 2))
        {
            int capacity = (int)Math.Min((uint)Array.MaxLength, Math.Max(index + 1, Math.Max(16u, (uint)dense.Length * 2)));
            Reserve(capacity);
            dense[index] = value;
        }
        else (sparse ??= new Dictionary<uint, T>())[index] = value;
        if (index >= Length) Length = index + 1;
    }
    public void Remove(uint index)
    {
        if(index>=Length)return;
        if(index<(uint)dense.Length)dense[index]=missing;
        else sparse?.Remove(index);
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

public partial class JsArray : TsObject
{
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.ArrayPrototype;
    static readonly object Hole = new object();
    private protected ArrayStorage<object> values;
    public JsArray(int length) { values = new ArrayStorage<object>(length, Hole); }
    // Generated code transfers its freshly initialized buffer to this array.
    public JsArray(object[] items) { values = new ArrayStorage<object>(items, Hole); }
    private protected JsArray(ArrayStorage<object> storage) { values = storage; }
    public static object HoleValue() => Hole;
    protected JsArray() { }
    protected virtual uint Count => values.Length;
    protected virtual object Read(uint index) { object value = values.Get(index); return ReferenceEquals(value, Hole) ? InheritedElement(index) : value; }
    protected object InheritedElement(uint index) => PropertyPrototype is TsObject prototype ? prototype.GetWithReceiver(index.ToString(CultureInfo.InvariantCulture),this) : Js.Undefined;
    protected virtual void DeleteElement(uint index) => values.Remove(index);
    public override bool DeleteOwn(string key)
    {
        if(key=="length")return false;
        if(Index(key,out uint index)){DeleteElement(index);return true;}
        return base.DeleteOwn(key);
    }
    public virtual bool HasElement(uint index) => index < Count && !ReferenceEquals(values.Get(index), Hole);
    protected virtual void Write(uint index, object value) => values.Set(index, value);
    protected virtual void Resize(uint length) => values.Resize(length);
    public double Length() => Count;
    internal static uint SliceBound(object value, uint length)
    {
        double index = Math.Truncate(Js.Number(value));
        if (double.IsNaN(index)) return 0;
        return (uint)(index < 0 ? Math.Max(length + index, 0) : Math.Min(index, length));
    }
    public JsArray Slice(object start, object end)
    {
        uint length = Count, first = SliceBound(start, length);
        uint last = Js.IsUndefined(end) ? length : SliceBound(end, length);
        return SliceRange(first, last > first ? last - first : 0);
    }
    internal bool CanCopySliceRange(uint start, uint count)
    {
        if (HasPropertyOverrides) return false;
        if (this is NumberArray numeric && numeric.TryPacked((double)start + count) != null) return true;
        if (GetType() == typeof(JsArray) && (ulong)start + count <= values.Length && (ulong)start + count <= (ulong)values.Dense.Length)
        {
            bool present = true;
            for (uint index = start; index < start + count; index++) if (ReferenceEquals(values.Dense[index], Hole)) { present = false; break; }
            if (present) return true;
        }
        return Js.ArraySlicePrototypesHaveNoIndices();
    }
    internal JsArray CopySliceRange(uint start, uint count) => SliceRange(start, count);
    protected virtual JsArray SliceRange(uint start, uint count) => new JsArray(values.Slice(start, count));
    public void SetLength(double length)
    {
        if (!(length >= 0 && length <= uint.MaxValue && length == (uint)length))
            throw new InvalidOperationException("Invalid array length");
        Resize((uint)length);
    }
    internal static bool Index(string key, out uint index)
    {
        index = 0;
        if (key.Length == 0 || key.Length > 10 || key.Length > 1 && key[0] == '0') return false;
        ulong value = 0;
        for (int i = 0; i < key.Length; i++)
        {
            uint digit = (uint)(key[i] - '0');
            if (digit > 9) return false;
            value = value * 10 + digit;
        }
        index = (uint)value;
        return value < uint.MaxValue;
    }
    public override bool HasOwn(string key) => key == "length" || (Index(key, out uint index) ? HasElement(index) : base.HasOwn(key));
    public override object Get(string key) => key == "length" ? (double)Count : Index(key, out uint index) ? Read(index) :
        base.Get(key);
    public override object SetOwnValue(string key, object value)
    {
        if (TryOwnAccessor(key, out _)) throw new InvalidOperationException("Cannot replace accessor through assignment");
        return Set(key, value);
    }
    public override object Set(string key, object value)
    {
        if (key == "length") SetLength(Js.Number(value));
        else if (Index(key, out uint index)) Write(index, value);
        else return base.Set(key, value);
        return value;
    }
    public object GetIndex(object key) => Get(Js.PropertyKey(key));
    public object ElementAt(uint index) => Read(index);
    internal virtual void ReserveCapacity(int capacity) => values.Reserve(capacity);
    internal object[] DenseRange(double limit) => limit >= 0 && limit <= values.Length && limit <= values.Dense.Length ? values.Dense : null;
    internal static object DecodeElement(object value) => ReferenceEquals(value, Hole) ? Js.Undefined : value;
    public void SetAt(int index, object value)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        Write((uint)index, value);
    }
    public void SetIndex(object key, object value) => Set(Js.PropertyKey(key), value);
    public void Append(object value)
    {
        uint index = Count;
        if (index == uint.MaxValue) throw new InvalidOperationException("Invalid array length");
        Write(index, value);
    }
    public void AppendHole()
    {
        if (Count == uint.MaxValue) throw new InvalidOperationException("Invalid array length");
        Resize(Count + 1);
    }
    public void Spread(object source, int extra)
    {
        source = Iteration.Start(source);
        if (this is NumberArray destination && source is NumberArray numbers)
        {
            if (destination.AppendPacked(numbers, extra)) return;
            for (uint i = 0; i < numbers.Length(); i++) destination.PushNumber(numbers.GetNumberAt(unchecked((int)i)));
            return;
        }
        uint nextPosition = 0; bool closeable = false;
        try
        {
            while (true)
            {
                closeable = false;
                if (nextPosition >= Iteration.Length(source)) break;
                int position = unchecked((int)nextPosition);
                object value = Iteration.Value(source, position);
                nextPosition += (uint)Iteration.Step(source, position); closeable = true;
                Append(value);
            }
        }
        catch { if (closeable) Js.CloseIterationAt(source, unchecked((int)nextPosition), true); throw; }
    }
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

public sealed partial class NumberArray : JsArray
{
    // Canonicalize stored numeric NaNs, reserving a distinct NaN payload for
    // holes and explicit undefined. Public reads use the nullable scalar ABI.
    const long MissingBits = 0x7ff8000000000001;
    static readonly double Missing = BitConverter.Int64BitsToDouble(MissingBits);
    static readonly double Undefined = BitConverter.Int64BitsToDouble(MissingBits + 1);
    ArrayStorage<double> numbers;
    uint packedPrefix;
    public NumberArray(int length) { numbers = new ArrayStorage<double>(length, Missing); }
    NumberArray(ArrayStorage<double> storage, uint prefix) { numbers = storage;packedPrefix = prefix; }
    protected override JsArray SliceRange(uint start, uint count) =>
        new NumberArray(numbers.Slice(start, count), start < packedPrefix ? Math.Min(count, packedPrefix - start) : 0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double Encode(double? value) => !value.HasValue ? Undefined : double.IsNaN(value.Value) ? double.NaN : value.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double? Decode(double value) => unchecked((ulong)(BitConverter.DoubleToInt64Bits(value) - MissingBits)) <= 1 ? null : value;
    public override bool HasElement(uint index) => index < Count && BitConverter.DoubleToInt64Bits(numbers.Get(index)) != MissingBits;
    internal override void ReserveCapacity(int capacity) => numbers.Reserve(capacity);
    protected override uint Count => numbers.Length;
    protected override object Read(uint index) {double value=numbers.Get(index);return BitConverter.DoubleToInt64Bits(value)==MissingBits ? InheritedElement(index) : Js.BoxNumber(Decode(value));}
    protected override void DeleteElement(uint index) {numbers.Remove(index);packedPrefix=Math.Min(packedPrefix,index);}
    protected override void Write(uint index, object value) => Store(index, Js.Cast<double?>(value, "Array write violates number element type"));
    protected override void Resize(uint length)
    {
        numbers.Resize(length);packedPrefix = Math.Min(packedPrefix, length);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Store(uint index, double? value)
    {
        numbers.Set(index, Encode(value));
        if (!value.HasValue) { if (index < packedPrefix) packedPrefix = index; }
        else if (index == packedPrefix) packedPrefix++;
    }
    public double[] TryPacked(double limit) =>
        limit >= 0 && limit <= packedPrefix && limit <= numbers.Dense.Length ? numbers.Dense : null;
    public bool AppendPacked(NumberArray source, int extra)
    {
        uint count = source.numbers.Length, start = numbers.Length;
        double[] buffer = source.TryPacked(count);
        ulong required = (ulong)start + count;
        if (buffer == null || required + (uint)extra > (ulong)Array.MaxLength) return false;
        if (numbers.InitializeCopy(buffer, (int)count, extra)) { packedPrefix = count; return true; }
        int capacity = (int)(required + (uint)extra);
        if (capacity > numbers.Dense.Length && numbers.Dense.Length != 0)
            capacity = (int)Math.Min(Array.MaxLength, Math.Max((long)capacity, (long)numbers.Dense.Length * 2));
        numbers.Reserve(capacity);
        Array.Copy(buffer, 0, numbers.Dense, (int)start, (int)count);
        numbers.Length = (uint)required;
        if (packedPrefix == start) packedPrefix += count;
        return true;
    }
    public void ReservePushes(double additions)
    {
        if (additions > 0 && additions <= 1048576 && numbers.Length <= 1048576 - Math.Ceiling(additions))
            numbers.Reserve((int)(numbers.Length + Math.Ceiling(additions)));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double? GetNumber(double index)
    {
        if (index >= 0 && index < uint.MaxValue && index == (uint)index) return Decode(numbers.Get((uint)index));
        return Js.Cast<double?>(Get(Js.PropertyKey(index)), "Array element is not a number");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double? GetNumberAt(int index) => Decode(numbers.Get((uint)index));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNumberAt(int index, double? value) => Store((uint)index, value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNumber(double index, double? value)
    {
        if (index >= 0 && index < uint.MaxValue && index == (uint)index) Store((uint)index, value);
        else Set(Js.PropertyKey(index), Js.BoxNumber(value));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double PushNumber(double? value)
    {
        uint index = numbers.Length;
        if (index == uint.MaxValue) throw new InvalidOperationException("Invalid array length");
        Store(index, value);return numbers.Length;
    }
    public double? PopNumber()
    {
        uint length = numbers.Length;
        if (length == 0) return null;
        double? value = Decode(numbers.Get(length - 1));Resize(length - 1);return value;
    }
}
