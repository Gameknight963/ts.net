using System;

public static partial class Js
{
    sealed class ConcatIterator((TsObject Source, JsFunction Open)[] inputs) : TsObject
    {
        internal (TsObject Source, JsFunction Open)[] Inputs = inputs;
        internal JsProtocolIterator Current;
        internal int Position;
        internal bool Done, Running;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : ConcatIteratorMethods.Prototype;
        internal void Finish() {Done = true; Inputs = null; Current = null;}
        internal object Next()
        {
            if (Running) throw new InvalidOperationException("Iterator helper is already running");
            if (Done) return IteratorResult(Undefined,true);
            Running = true;
            try {
                while (true) {
                    if (Current != null) {
                        if (Current.Ensure()) return IteratorResult(Current.TakeValue(),false);
                        Current = null;
                    }
                    if (Position == Inputs.Length) {Finish();return IteratorResult(Undefined,true);}
                    var input = Inputs[Position]; Inputs[Position++] = default;
                    if (input.Open.InvokeWithReceiver(input.Source,Array.Empty<object>()) is not TsObject iterator)
                        throw new InvalidOperationException("Iterator must be an object");
                    Current = (JsProtocolIterator)StartIteratorObject(iterator);
                }
            }
            catch {Finish();throw;}
            finally {Running = false;}
        }
        internal object Return()
        {
            if (Running) throw new InvalidOperationException("Iterator helper is already running");
            if (Done) return IteratorResult(Undefined,true);
            Running = true;
            var current = Current; Finish();
            try {if (current != null) CloseIteration(current,false);return IteratorResult(Undefined,true);}
            finally {Running = false;}
        }
    }
    static class ConcatIteratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = NativeIteratorPrototype};
            result.DefineMethod("next",new CollectionMethod("next",0,(receiver,_) => receiver is ConcatIterator iterator ? iterator.Next() : throw new InvalidOperationException("Incompatible iterator helper receiver")));
            result.DefineMethod("return",new CollectionMethod("return",0,(receiver,_) => receiver is ConcatIterator iterator ? iterator.Return() : throw new InvalidOperationException("Incompatible iterator helper receiver")));
            return result;
        }
    }
    static class ConcatIntrinsicIterator
    {
        internal static readonly JsFunction Open = new CollectionMethod("values",0,(receiver,_) => receiver switch {
            JsArray or JsArguments => new JsSequenceIterator(receiver,0u),
            JsKeyedCollection collection => new JsCollectionIterator(collection,collection.IsSet ? 1 : 2),
            JsBoxedPrimitive {Value:string text} => new JsSequenceIterator(text,0u),
            _ => throw new InvalidOperationException("Value is not iterable")
        });
    }
    static object IteratorConcat(object[] arguments)
    {
        var inputs = new (TsObject Source,JsFunction Open)[arguments.Length];
        string key = PropertyKey(IteratorSymbol);
        for (int index = 0; index < arguments.Length; index++) {
            if (arguments[index] is not TsObject source) throw new InvalidOperationException("Iterator.concat requires iterable objects");
            object value = source.Get(key);
            JsFunction method = value as JsFunction;
            if (method == null && IsNullish(value) && !source.HasOwn(key) && source is JsArray or JsArguments or JsKeyedCollection or JsBoxedPrimitive {Value:string})
                method = ConcatIntrinsicIterator.Open;
            if (method == null) throw new InvalidOperationException("Iterator.concat requires iterable objects");
            inputs[index] = (source,method);
        }
        return new ConcatIterator(inputs);
    }
}
