using System;

public static partial class Js
{
    static BinaryBuffer BufferSpeciesCreate(BinaryBuffer source, int length)
    {
        object candidate = source.Get("constructor");
        JsFunction constructor = null;
        if (!IsUndefined(candidate)) {
            if (candidate is not TsObject owner) throw new InvalidOperationException("ArrayBuffer constructor must be an object");
            object species = owner.Get(PropertyKey(SpeciesSymbol));
            if (!IsNullish(species)) {
                if (species is not JsFunction function || !function.CanInitializeDerived) throw new InvalidOperationException("ArrayBuffer species must be a constructor");
                constructor = function;
            }
        }
        if (constructor == null || ReferenceEquals(constructor, RuntimeNamespaces.ArrayBuffer)) return new BinaryBuffer(length);
        if (constructor.Construct(new object[] { (double)length }) is not BinaryBuffer result)
            throw new InvalidOperationException("ArrayBuffer species must return an ArrayBuffer");
        EnsureAttached(result);
        if (ReferenceEquals(result, source)) throw new InvalidOperationException("ArrayBuffer species returned the source buffer");
        if (result.Bytes.Length < length) throw new InvalidOperationException("ArrayBuffer species returned a short buffer");
        return result;
    }
}
