using System;

public static partial class Js
{
    sealed class TypedArrayIterator(TypedArrayValue source, string mode) : JsDescriptorObject
    {
        int index;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : SequenceIteratorPrototype(false);
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
}
