using System;

partial struct ArrayStorage<T>
{
    public void Reverse<TMatch>(ref TMatch hole) where TMatch : struct, IArrayMatch<T>
    {
        if (Length <= (uint)dense.Length)
        {
            Array.Reverse(dense, 0, (int)Length);
            return;
        }
        // Mirror stored elements, not the logical range: sparse arrays can
        // have billions of holes. Rebuild before replacing the old storage.
        var reversed = new ArrayStorage<T>(new T[dense.Length], missing) { Length = Length };
        Array.Fill(reversed.dense, missing);
        for (uint i = 0; i < (uint)dense.Length; i++)
            if (!hole.Matches(dense[i])) reversed.Set(Length - 1 - i, dense[i]);
        if (sparse != null)
            foreach (var item in sparse)
                if (item.Key < Length && !hole.Matches(item.Value)) reversed.Set(Length - 1 - item.Key, item.Value);
        this = reversed;
    }
}

public partial class JsArray
{
    struct ObjectHole : IArrayMatch<object>
    {
        public bool Matches(object value) => ReferenceEquals(value, Hole);
    }
    protected void ReverseStorage()
    {
        var hole = new ObjectHole();
        values.Reverse(ref hole);
    }
    public virtual JsArray Reverse()
    {
        if (!CanUseElementStorage) return (JsArray)Js.ReverseArrayLike(this);
        ReverseStorage();
        return this;
    }
}

public sealed partial class NumberArray
{
    struct NumberHole : IArrayMatch<double>
    {
        public bool Matches(double value) => BitConverter.DoubleToInt64Bits(value) == MissingBits;
    }
    public override JsArray Reverse()
    {
        if (!CanUseElementStorage) return (JsArray)Js.ReverseArrayLike(this);
        bool packed = packedPrefix == numbers.Length;
        var hole = new NumberHole();
        numbers.Reverse(ref hole);
        if (!packed) packedPrefix = 0;
        return this;
    }
}
