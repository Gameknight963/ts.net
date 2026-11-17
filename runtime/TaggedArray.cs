using System;
using System.Runtime.CompilerServices;

// Keep numeric payloads unboxed while reference values remain visible to the GC.
// Operations requiring the ordinary backing store materialize it once, retaining
// this wrapper so aliases continue to observe the same array.
public sealed partial class TaggedArray : JsArray
{
    byte[] tags = Array.Empty<byte>(); // 0: hole, 1: number, 2: reference
    double[] numbers = Array.Empty<double>();
    // Numeric maps need this only for reference values or cached number boxes.
    object[] references = Array.Empty<object>();
    uint length;
    JsArray materialized;
    public TaggedArray(int count) { length = (uint)count; }
    public TaggedArray(object[] items)
    {
        length = (uint)items.Length;
        ReserveCapacity(items.Length);
        for (int i = 0; i < items.Length; i++)
            if (!ReferenceEquals(items[i], HoleValue())) Write((uint)i, items[i]);
    }
    // Fresh intrinsic map results and literals cannot escape until filled.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void StoreMappedNumber(int index, double? value)
    {
        if (!value.HasValue) { EnsureReferences(); references[index] = Js.Undefined; }
        tags[index] = value.HasValue ? (byte)1 : (byte)2;
        numbers[index] = value.GetValueOrDefault();
    }
    public void InitializeLiteralNumber(int index, double? value) => StoreMappedNumber(index, value);
    public void InitializeLiteralElement(int index, object value) => Write((uint)index, value);
    void EnsureReferences()
    {
        if (references.Length == 0) references = new object[tags.Length];
    }
    internal double[] TryPackedNumbers(double limit)
    {
        if (TryTags(limit) == null) return null;
        if (tags.AsSpan(0, (int)limit).IndexOfAnyExcept((byte)1) >= 0) return null;
        return numbers;
    }
    internal double[] TryWritablePackedNumbers(double limit)
    {
        if (ElementsFrozen || limit < 0) return null;
        // A fractional counted-loop limit still visits its final integer index.
        double extent = Math.Ceiling(limit);
        double[] buffer = TryPackedNumbers(extent);
        if (buffer == null) return null;
        // Compiler guards exclude callbacks while the buffer is in use. Drop
        // cached primitive boxes so subsequent object reads see the new values.
        if (references.Length != 0) Array.Clear(references, 0, (int)extent);
        return buffer;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double? GetNumberAt(int index)
    {
        if (!HasIndexedAccessors && materialized == null && (uint)index < length && (uint)index < (uint)tags.Length && tags[index] == 1)
            return numbers[index];
        return Js.Cast<double?>(Js.IndexGet(this, (double)index), "Array element is not a number");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryReadNumericPayload(int index, out double value)
    {
        if (!HasIndexedAccessors && materialized == null && (uint)index < length && (uint)index < (uint)tags.Length && tags[index] == 1) {
            value = numbers[index]; return true;
        }
        value = 0; return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object GetStoredAt(uint index)
    {
        if (!HasIndexedAccessors && materialized == null && index < length && index < (uint)tags.Length)
        {
            if (tags[index] == 2) return references[index];
            if (tags[index] == 1)
            {
                object[] boxes = references;
                return index < (uint)boxes.Length ? boxes[index] ??= numbers[index] : CacheNumber(index);
            }
        }
        return Read(index);
    }
    protected override uint Count => materialized != null ? (uint)materialized.Length() : length;
    public override bool HasElement(uint index) => HasDefinedElement(index) || (materialized != null ? materialized.HasElement(index) : index < length && index < (uint)tags.Length && tags[index] != 0);
    protected override object Read(uint index)
    {
        if (TryReadDefinedElement(index, out var defined)) return defined;
        if (materialized != null)
            return materialized.HasElement(index) ? materialized.ElementAt(index) : InheritedElement(index);
        return index < length && index < (uint)tags.Length && tags[index] != 0 ? BoxAt(index) : InheritedElement(index);
    }
    protected override void DeleteElement(uint index)
    {
        if(materialized!=null){materialized.DeleteOwn(index.ToString(System.Globalization.CultureInfo.InvariantCulture));return;}
        if(index>=length || index>=(uint)tags.Length)return;
        tags[index]=0;numbers[index]=0;if (index < (uint)references.Length) references[index]=null;
    }
    object BoxAt(uint index)
    {
        // Read and the internal copy loop check the representation and range.
        // Reuse the reference slot to cache a box only when this numeric slot
        // is read as an object. Numeric writes already clear that slot.
        if (tags[index] == 1) {
            object[] boxes = references;
            return index < (uint)boxes.Length ? boxes[index] ??= numbers[index] : CacheNumber(index);
        }
        return tags[index] == 2 ? references[index] : Js.Undefined;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    object CacheNumber(uint index)
    {
        EnsureReferences(); return references[index] ??= numbers[index];
    }
    JsArray Materialize()
    {
        if (materialized != null) return materialized;
        int end = (int)Math.Min(length, (uint)tags.Length);
        var items = new object[tags.Length];
        for (int i = 0; i < end; i++) items[i] = tags[i] == 0 ? HoleValue() : BoxAt((uint)i);
        var result = new JsArray(items);
        result.SetLength(length);
        materialized = result;
        tags = Array.Empty<byte>(); numbers = Array.Empty<double>(); references = Array.Empty<object>();
        return result;
    }
    internal override void ReserveCapacity(int capacity)
    {
        if (materialized != null) { materialized.ReserveCapacity(capacity); return; }
        if (capacity <= tags.Length) return;
        // Numeric payloads are read only when their initialized number tag is set.
        // Publish only after all allocations succeed.
        var newTags = new byte[capacity];var newNumbers = GC.AllocateUninitializedArray<double>(capacity);var newReferences = references.Length == 0 ? Array.Empty<object>() : new object[capacity];
        Array.Copy(tags, newTags, tags.Length);Array.Copy(numbers, newNumbers, numbers.Length);Array.Copy(references, newReferences, references.Length);
        tags = newTags;numbers = newNumbers;references = newReferences;
    }
    bool Prepare(uint index)
    {
        CheckGrowth(index);
        if (materialized != null) return false;
        if (index < (uint)tags.Length) return true;
        if (index >= (uint)Array.MaxLength || index > Math.Max(16u, (uint)tags.Length * 2)) { Materialize();return false; }
        ReserveCapacity((int)Math.Min((uint)Array.MaxLength, Math.Max(index + 1, Math.Max(16u, (uint)tags.Length * 2))));
        return true;
    }
    protected override void Write(uint index, object value)
    {
        if (TryWriteDefinedElement(index, value)) return;
        if (!Prepare(index)) { materialized.Set(index.ToString(System.Globalization.CultureInfo.InvariantCulture), value);return; }
        if (value is double number) { tags[index] = 1;numbers[index] = number;if (index < (uint)references.Length) references[index] = null; }
        else { EnsureReferences();tags[index] = 2;references[index] = value;numbers[index] = 0; }
        if (index >= length) length = index + 1;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNumberAt(int index, double? value)
    {
        // An existing ordinary slot cannot grow the array or invoke an
        // inherited setter. Keep descriptor and frozen-array writes below.
        if ((uint)index < length && materialized == null && (uint)index < (uint)tags.Length && tags[index] != 0 &&
            !HasIndexedAccessors && !ElementsFrozen && value.HasValue)
        {
            tags[index] = 1;numbers[index] = value.Value;if ((uint)index < (uint)references.Length) references[index] = null;
            return;
        }
        SetNumberAtSlow(index, value);
    }
    void SetNumberAtSlow(int index, double? value)
    {
        if (NeedsPrototypeSet((uint)index) && SetThroughPrototype((uint)index, Js.BoxNumber(value))) return;
        if (TryWriteDefinedNumber((uint)index, value)) return;
        if (!value.HasValue) { Write((uint)index, Js.Undefined);return; }
        if (!Prepare((uint)index)) { materialized.SetAt(index, value.Value);return; }
        tags[index] = 1;numbers[index] = value.Value;if ((uint)index < (uint)references.Length) references[index] = null;
        if ((uint)index >= length) length = (uint)index + 1;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // The compiler uses this entry point only for statically nonnumeric values.
    public void SetReferenceAt(int index, object value)
    {
        if (SetThroughPrototype((uint)index, value)) return;
        if (TryWriteDefinedElement((uint)index, value)) return;
        if (!Prepare((uint)index)) { materialized.SetAt(index, value);return; }
        EnsureReferences();tags[index] = 2;references[index] = value;
        if ((uint)index >= length) length = (uint)index + 1;
    }
    protected override void Resize(uint count)
    {
        CheckLengthWritable();
        RemoveDefinedElements(count);
        if (materialized != null) { materialized.SetLength(count);return; }
        if (count < length && count < (uint)tags.Length)
        {
            int end = (int)Math.Min(length, (uint)tags.Length), start = (int)count;
            Array.Clear(tags, start, end - start);Array.Clear(numbers, start, end - start);if (references.Length != 0) Array.Clear(references, start, end - start);
        }
        length = count;
    }
    internal object[] MaterializedDenseRange(double limit) => materialized != null ? materialized.DenseRange(limit) : limit >= 0 && limit <= length && limit <= tags.Length ? Materialize().DenseRange(limit) : null;
    public byte[] TryTags(double limit) => CanUseElementStorage && materialized == null && limit >= 0 && limit <= length && limit <= tags.Length ? tags : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryReadNumberAt(int index, out double? value)
    {
        value = null;
        if (HasIndexedAccessors || materialized != null || (uint)index >= length || (uint)index >= (uint)tags.Length) return false;
        if (tags[index] == 1) { value = numbers[index]; return true; }
        return tags[index] == 2 && Js.IsUndefined(references[index]);
    }
    public bool IsStringAt(int index)
    {
        if (TryReadDefinedElement((uint)index, out var defined)) return defined is string;
        if (materialized != null) return (materialized.HasElement((uint)index) ? materialized.ElementAt((uint)index) : InheritedElement((uint)index)) is string;
        if ((uint)index >= length || (uint)index >= (uint)tags.Length || tags[index] == 0) return InheritedElement((uint)index) is string;
        return tags[index] == 2 && references[index] is string;
    }
    public bool IsNumberAt(int index)
    {
        if (TryReadDefinedElement((uint)index, out var defined)) return Js.IsNumber(defined);
        if (materialized != null) return Js.IsNumber(materialized.HasElement((uint)index) ? materialized.ElementAt((uint)index) : InheritedElement((uint)index));
        if ((uint)index >= length || (uint)index >= (uint)tags.Length || tags[index] == 0) return Js.IsNumber(InheritedElement((uint)index));
        return tags[index] == 1;
    }
    protected override JsArray SliceRange(uint start, uint count) => Materialize().Slice((double)start, (double)start + count);
    internal override double Search(object value, object fromIndex, bool includes)
    {
        uint count = Count;
        if (count == 0) return -1;
        uint start = SliceBound(fromIndex, count);
        return CanUseElementStorage && count == Count ? Materialize().Search(value, (double)start, includes)
            : Js.SearchArrayRange(this, value, count, start, includes);
    }
    internal override double SearchNumber(double? value, object fromIndex, bool includes)
    {
        uint count = Count;
        if (count == 0) return -1;
        uint start = SliceBound(fromIndex, count);
        return CanUseElementStorage && count == Count ? Materialize().SearchNumber(value, (double)start, includes)
            : Js.SearchArrayRange(this, Js.BoxNumber(value), count, start, includes);
    }
    public override JsArray Reverse() { if (!CanUseElementStorage || !IsExtensible) return (JsArray)Js.ReverseArrayLike(this); Materialize().Reverse();return this; }
}

public static partial class Js
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CheckedArrayNumberValue(object receiver, double? index)
    {
        if (receiver is JsArray array && index.HasValue) {
            double number = index.GetValueOrDefault();
            if (number >= 0 && number <= int.MaxValue && number == (int)number) {
                int position = (int)number;
                if (array is TaggedArray tagged && tagged.TryReadNumericPayload(position, out double value)) return value;
                if (JsArray.TryReadOwnDenseValue(array, position, out object stored) && stored is double numeric) return numeric;
            }
        }
        return CheckedNumberValue(ArrayIndexGetNumberKey(receiver, index));
    }
    // A missing result means the loop must use ordinary lookup. This probe
    // never invokes accessors or reads inherited elements.
    public static double? TryReadOwnArrayNumber(object receiver, int index)
    {
        if (receiver is TaggedArray tagged && tagged.TryReadNumericPayload(index, out double value)) return value;
        if (receiver is JsArray array && JsArray.TryReadOwnDenseValue(array, index, out object stored) && stored is double numeric) return numeric;
        return null;
    }
    // Inlining these index checks enlarged callers and slowed mixed-array loops.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ArrayIsNumberKey(object receiver, double? index)
    {
        if (index.HasValue && index.Value >= 0 && index.Value <= int.MaxValue && index.Value == (int)index.Value)
        {
            if (receiver is NumberArray numbers && !numbers.HasIndexedAccessors && numbers.TryPacked(index.Value + 1) != null) return true;
            if (receiver is TaggedArray tagged) return tagged.IsNumberAt((int)index.Value);
        }
        return IsNumber(ArrayIndexGetNumberKey(receiver, index));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ArrayIsStringKey(object receiver, double? index)
    {
        if (index.HasValue && index.Value >= 0 && index.Value <= int.MaxValue && index.Value == (int)index.Value)
        {
            if (receiver is NumberArray numbers && !numbers.HasIndexedAccessors && numbers.TryPacked(index.Value + 1) != null) return false;
            if (receiver is TaggedArray tagged) return tagged.IsStringAt((int)index.Value);
        }
        return IsString(ArrayIndexGetNumberKey(receiver, index));
    }
    public static bool IsString(object value) => value is string;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ArrayIsStringAt(object receiver, int index) => receiver is TaggedArray array ? array.IsStringAt(index) : IsString(ArrayGetAt(receiver, index));
    public static byte[] ArrayTryTags(object receiver, double limit) => receiver is TaggedArray array ? array.TryTags(limit) : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ArrayIsNumberAt(object receiver, int index) => receiver is TaggedArray array ? array.IsNumberAt(index) : IsNumber(ArrayGetAt(receiver, index));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ArraySetTaggedReferenceAt(object receiver, int index, object value)
    {
        if (receiver is TaggedArray array) array.SetReferenceAt(index, value);
        else ArraySetAt(receiver, index, value);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ArraySetTaggedNumberAt(object receiver, int index, double? value)
    {
        if (receiver is TaggedArray array) array.SetNumberAt(index, value);
        else if (receiver is NumberArray numbers) numbers.SetNumberAt(index, value);
        else ArraySetAt(receiver, index, BoxNumber(value));
    }
}
