using System;
using System.Globalization;

// The ordinary array/string/arguments paths retain their unboxed index loops.
// Materialize the iterator only when a cleanup method can observe its receiver.
public sealed class JsSequenceIterator : TsObject
{
    object source;
    readonly bool text;
    readonly int mode;
    ulong position;
    internal bool Done;
    internal bool IsString => text;
    internal JsSequenceIterator(object source, uint position) { this.source = source; text = source is string; this.position = position; }
    internal JsSequenceIterator(TsObject source, int mode) { this.source = source; this.mode = mode; }
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.SequenceIteratorPrototype(IsString);
    internal object Next()
    {
        if (Done || position >= (source is TsObject indexed ? (double)Js.IndexedLength(indexed) : Iteration.Length(source))) { Done = true; source = null; return Js.IteratorResult(Js.Undefined, true); }
        object value;
        if (source is string text)
        {
            int index = (int)position, step = Iteration.Step(text, index); position += (uint)step;
            value = text.Substring(index, step);
        }
        else
        {
            ulong index = position++;
            value = mode == 1 ? (double)index : Js.IndexedValue((TsObject)source, (long)index);
            if (mode == 2) value = new JsArray(new object[] { (double)index, value });
        }
        return Js.IteratorResult(value, false);
    }
}

public static partial class Js
{
    sealed class ArrayIteratorFunction(string name, int mode) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, name, 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => new JsSequenceIterator(ToObject(receiver), mode);
    }
    static class NativeIteratorPrototypes
    {
        internal static readonly JsFunction Self = new CollectionMethod("[Symbol.iterator]", 0, (receiver, _) => receiver);
        internal static readonly TsObject Common = CreateCommon();
        static TsObject CreateCommon()
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype };
            prototype.DefineMethod(PropertyKey(RuntimeNamespaces.Symbol.Get("iterator")), Self);
            return prototype;
        }
        internal static readonly TsObject Array = Create(false), String = Create(true);
        static TsObject Create(bool text)
        {
            var prototype = new JsClassInstance { ParentPrototype = Common };
            prototype.DefineMethod("next", new CollectionMethod("next", 0, (receiver, _) =>
                receiver is JsSequenceIterator iterator && iterator.IsString == text ? iterator.Next() : throw new InvalidOperationException("Incompatible iterator receiver")));
            return prototype;
        }
    }
    internal static TsObject NativeIteratorPrototype => NativeIteratorPrototypes.Common;
    internal static TsObject SequenceIteratorPrototype(bool text) => text ? NativeIteratorPrototypes.String : NativeIteratorPrototypes.Array;
    internal static object StartSequenceIterator(JsSequenceIterator iterator)
    {
        object next = iterator.Get("next");
        if (next is not JsFunction function) throw new InvalidOperationException("Iterator next is not callable");
        return new JsProtocolIterator(iterator, function);
    }
    internal static TsObject IterationObjectAt(object source, int position)
    {
        TsObject existing = IterationObject(source);
        if (existing != null) return existing;
        if (source is JsArray or JsArguments or string && SequenceIteratorPrototype(source is string).HasProperty("return"))
            return new JsSequenceIterator(source, unchecked((uint)position));
        return null;
    }
    public static bool SequenceIteratorHasReturn() => SequenceIteratorPrototype(false).HasProperty("return");
    public static void CloseIterationAt(object source, int position, bool suppressError)
    {
        try { CloseIteratorObject(IterationObjectAt(source, position)); }
        catch when (suppressError) { }
    }
}
