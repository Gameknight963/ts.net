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
    internal bool TryNext(out object value)
    {
        if (Done || position >= (source is TsObject indexed ? (double)Js.IndexedLength(indexed) : Iteration.Length(source))) { Done = true; source = null; value = Js.Undefined; return false; }
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
        return true;
    }
    internal object Next() => TryNext(out var value) ? Js.IteratorResult(value,false) : Js.IteratorResult(Js.Undefined,true);
}

public static partial class Js
{
    static class StringIteratorMethods
    {
        internal static readonly JsFunction Iterator = new CollectionMethod("[Symbol.iterator]", 0, static (receiver, _) => new JsSequenceIterator(AtString(receiver), 0u));
    }
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
            prototype.DefineMethod(PropertyKey(IteratorSymbol), Self);
            string tagKey = PropertyKey(ToStringTagSymbol);
            prototype.DefineClassAccessor(tagKey, new CollectionMethod("get [Symbol.toStringTag]", 0, (_, _) => "Iterator"), 1);
            prototype.DefineClassAccessor(tagKey, new CollectionMethod("set [Symbol.toStringTag]", 1, (receiver, arguments) =>
            {
                if (receiver is not TsObject target || ReferenceEquals(target, prototype)) throw new InvalidOperationException("Cannot assign the Iterator prototype tag");
                object value = FunctionArgument(arguments, 0);
                if (target.HasOwn(tagKey)) target.Set(tagKey, value);
                else target.DefineOwn(tagKey, value);
                return Undefined;
            }), 2);
            prototype.DefineMethod(PropertyKey(DisposeSymbol),new CollectionMethod("[Symbol.dispose]",0,(receiver,_)=>DisposeIterator(receiver)));
            return prototype;
        }
        internal static readonly JsFunction ArrayNext = NextMethod(false), StringNext = NextMethod(true);
        static JsFunction NextMethod(bool text) => new CollectionMethod("next", 0, (receiver, _) =>
            receiver is JsSequenceIterator iterator && iterator.IsString == text ? iterator.Next()
            : !text && receiver is TypedArrayIterator typed ? typed.Next() : throw new InvalidOperationException("Incompatible iterator receiver"));
        internal static readonly TsObject Array = Create(false), String = Create(true);
        static TsObject Create(bool text)
        {
            var prototype = new JsCreatedObject();
            prototype.SetPrototype(Common);
            prototype.DefineMethod("next", text ? StringNext : ArrayNext);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), text ? "String Iterator" : "Array Iterator", false);
            return prototype;
        }
    }
    internal static TsObject NativeIteratorPrototype => NativeIteratorPrototypes.Common;
    internal static TsObject SequenceIteratorPrototype(bool text) => text ? NativeIteratorPrototypes.String : NativeIteratorPrototypes.Array;
    internal static JsFunction SequenceIteratorIntrinsicNext(bool text) => text ? NativeIteratorPrototypes.StringNext : NativeIteratorPrototypes.ArrayNext;
    static bool HasIntrinsicSequenceNext(bool text)
    {
        var prototype = SequenceIteratorPrototype(text);
        return !prototype.TryOwnAccessor("next", out _) && prototype.HasOwn("next") && ReferenceEquals(prototype.Get("next"), SequenceIteratorIntrinsicNext(text));
    }
    internal static object StartSequenceIterator(JsSequenceIterator iterator)
    {
        object next = iterator.Get("next");
        return new JsProtocolIterator(iterator, next);
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
