using System;

public static partial class Js
{
    static TypedArrayValue TypedArraySpeciesCreate(TypedArrayValue source, int length, BinaryBuffer buffer = null, int offset = 0, bool tracking = false)
    {
        JsFunction intrinsic = GetTypedArrayConstructor(source.Name), constructor = intrinsic;
        object candidate = source.Get("constructor");
        if (!IsUndefined(candidate))
        {
            if (candidate is not TsObject obj) throw new InvalidOperationException("Typed array constructor must be an object");
            object species = obj.Get(PropertyKey(SpeciesSymbol));
            if (!IsNullish(species))
            {
                if (species is not JsFunction function || !function.CanInitializeDerived) throw new InvalidOperationException("Typed array species must be a constructor");
                constructor = function;
            }
        }
        if (buffer != null) EnsureAttached(buffer);
        if (ReferenceEquals(constructor, intrinsic)) return buffer == null ? AllocateTypedArray(source.Name, length) : new TypedArrayValue(source.Name, buffer, offset, length, tracking);
        object[] arguments = buffer == null ? new object[] { (double)length } : tracking ? new object[] { buffer, (double)offset } : new object[] { buffer, (double)offset, (double)length };
        if (constructor.Construct(arguments) is not TypedArrayValue result || result.OutOfBounds || buffer == null && result.Count < length)
            throw new InvalidOperationException("Typed array species returned an invalid or short result");
        return result;
    }
}
