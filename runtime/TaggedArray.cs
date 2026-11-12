using System;
using System.Runtime.CompilerServices;

// Keep numeric payloads unboxed while reference values remain visible to the GC.
// Operations requiring the ordinary backing store materialize it once, retaining
// this wrapper so aliases continue to observe the same array.
public sealed partial class TaggedArray : JsArray
{
    byte[] tags = Array.Empty<byte>(); // 0: hole, 1: number, 2: reference
    double[] numbers = Array.Empty<double>();
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
    protected override uint Count => materialized != null ? (uint)materialized.Length() : length;
    public override bool HasElement(uint index) => HasDefinedElement(index) || (materialized != null ? materialized.HasElement(index) : index < length && index < (uint)tags.Length && tags[index] != 0);
    protected override object Read(uint index) => TryReadDefinedElement(index, out var defined) ? defined : HasElement(index) ? BoxAt(index) : InheritedElement(index);
    protected override void DeleteElement(uint index)
    {
        if(materialized!=null){materialized.DeleteOwn(index.ToString(System.Globalization.CultureInfo.InvariantCulture));return;}
        if(index>=length || index>=(uint)tags.Length)return;
        tags[index]=0;numbers[index]=0;references[index]=null;
    }
    object BoxAt(uint index)
    {
        if (materialized != null) return materialized.ElementAt(index);
        if (index >= length || index >= (uint)tags.Length) return Js.Undefined;
        // Reuse the reference slot to cache a box only when this numeric slot
        // is read as an object. Numeric writes already clear that slot.
        if (tags[index] == 1) return references[index] ??= numbers[index];
        return tags[index] == 2 ? references[index] : Js.Undefined;
    }
    JsArray Materialize()
    {
        if (materialized != null) return materialized;
        var result = new JsArray(0);
        result.ReserveCapacity(tags.Length);
        result.SetLength(length);
        uint end = Math.Min(length, (uint)tags.Length);
        for (uint i = 0; i < end; i++) if (tags[i] != 0) result.SetAt((int)i, BoxAt(i));
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
        var newTags = new byte[capacity];var newNumbers = GC.AllocateUninitializedArray<double>(capacity);var newReferences = new object[capacity];
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
        if (value is double number) { tags[index] = 1;numbers[index] = number;references[index] = null; }
        else { tags[index] = 2;references[index] = value;numbers[index] = 0; }
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
            tags[index] = 1;numbers[index] = value.Value;references[index] = null;
            return;
        }
        if (NeedsPrototypeSet((uint)index) && SetThroughPrototype((uint)index, Js.BoxNumber(value))) return;
        if (TryWriteDefinedNumber((uint)index, value)) return;
        if (!value.HasValue) { Write((uint)index, Js.Undefined);return; }
        if (!Prepare((uint)index)) { materialized.SetAt(index, value.Value);return; }
        tags[index] = 1;numbers[index] = value.Value;references[index] = null;
        if ((uint)index >= length) length = (uint)index + 1;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // The compiler uses this entry point only for statically nonnumeric values.
    public void SetReferenceAt(int index, object value)
    {
        if (SetThroughPrototype((uint)index, value)) return;
        if (TryWriteDefinedElement((uint)index, value)) return;
        if (!Prepare((uint)index)) { materialized.SetAt(index, value);return; }
        tags[index] = 2;references[index] = value;
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
            Array.Clear(tags, start, end - start);Array.Clear(numbers, start, end - start);Array.Clear(references, start, end - start);
        }
        length = count;
    }
    internal object[] MaterializedDenseRange(double limit) => materialized != null ? materialized.DenseRange(limit) : limit >= 0 && limit <= length && limit <= tags.Length ? Materialize().DenseRange(limit) : null;
    public byte[] TryTags(double limit) => CanUseElementStorage && materialized == null && limit >= 0 && limit <= length && limit <= tags.Length ? tags : null;
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
