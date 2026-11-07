using System;

public static partial class Js
{
    sealed class TypedArrayIterator(TypedArrayValue source, string mode) : TsObject
    {
        int index;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : TypedArrayIteratorMethods.Prototype;
        internal object Next()
        {
            if (source == null) return IteratorResult(Undefined, true);
            if (source.OutOfBounds) throw new InvalidOperationException("Typed array is out of bounds");
            if (index >= source.Count) { source = null; return IteratorResult(Undefined, true); }
            int position = index++;
            object value = mode == "keys" ? (double)position : source.Read(position);
            if (mode == "entries") value = new JsArray(new object[] { (double)position, value });
            return IteratorResult(value, false);
        }
    }
    static class TypedArrayIteratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var prototype = new JsClassInstance { ParentPrototype = NativeIteratorPrototype };
            prototype.DefineMethod("next", new CollectionMethod("next", 0, static (receiver, _) => receiver is TypedArrayIterator iterator ? iterator.Next() : throw new InvalidOperationException("Typed array iterator required")));
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Array Iterator");
            return prototype;
        }
    }
}
