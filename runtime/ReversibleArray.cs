using System;

// Selected at known reverse call sites. Ordinary arrays keep their direct
// indexing path; only this representation pays for mapping reversed indexes.
public sealed partial class ReversibleArray : JsArray
{
    bool reversed;
    public ReversibleArray(int length) : base(length) { }
    public ReversibleArray(object[] items) : base(items) { }
    ReversibleArray(ArrayStorage<object> storage) : base(storage) { }
    uint PhysicalIndex(uint index) => reversed && index < Count ? Count - 1 - index : index;
    void NormalizeOrder()
    {
        if (!reversed) return;
        base.Reverse();
        reversed = false;
    }
    public override JsArray Reverse()
    {
        if (Count > 1) reversed = !reversed;
        return this;
    }
    protected override object Read(uint index) {object value=values.Get(PhysicalIndex(index));return ReferenceEquals(value,HoleValue())?InheritedElement(index):value;}
    protected override void DeleteElement(uint index) => base.DeleteElement(PhysicalIndex(index));
    public override bool HasElement(uint index) => base.HasElement(PhysicalIndex(index));
    protected override void Write(uint index, object value)
    {
        if (reversed && index >= Count) NormalizeOrder();
        base.Write(PhysicalIndex(index), value);
    }
    protected override void Resize(uint length)
    {
        if (length != Count) NormalizeOrder();
        base.Resize(length);
    }
    protected override JsArray SliceRange(uint start, uint count)
    {
        var copy = new ReversibleArray(values.Slice(reversed ? Count - start - count : start, count));
        copy.reversed = reversed;
        return copy;
    }
    internal new object[] DenseRange(double limit)
    {
        if (!(limit >= 0 && limit <= Count && limit <= values.Dense.Length)) return null;
        NormalizeOrder();
        return base.DenseRange(limit);
    }
    internal override double Search(object value, object fromIndex, bool includes)
    {
        NormalizeOrder();
        return base.Search(value, fromIndex, includes);
    }
    internal override double SearchNumber(double? value, object fromIndex, bool includes)
    {
        NormalizeOrder();
        return base.SearchNumber(value, fromIndex, includes);
    }
}
