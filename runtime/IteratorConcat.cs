using System;

public static partial class Js
{
    sealed class ConcatIterator((TsObject Source, JsFunction Open)[] inputs) : JsDescriptorObject
    {
        internal (TsObject Source, JsFunction Open)[] Inputs = inputs;
        internal JsProtocolIterator Current;
        internal int Position;
        internal bool Done, Running;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : LazyIteratorPrototype;
        internal void Finish() {Done = true; Inputs = null; Current = null;}
        internal object Next() => TryNext(out var value) ? IteratorResult(value,false) : IteratorResult(Undefined,true);
        internal bool TryNext(out object value)
        {
            if (Running) throw new InvalidOperationException("Iterator helper is already running");
            if (Done) {value = Undefined;return false;}
            Running = true;
            try {
                while (true) {
                    if (Current != null) {
                        if (Current.Ensure()) {value = Current.TakeValue();return true;}
                        Current = null;
                    }
                    if (Position == Inputs.Length) {Finish();value = Undefined;return false;}
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
    internal static bool TryReadConcatIterator(TsObject source, object next, out object value, out bool done)
    {
        if (source is ConcatIterator iterator && ReferenceEquals(next,LazyIteratorMethods.Next)) {
            done = !iterator.TryNext(out value); return true;
        }
        value = null; done = false; return false;
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
            if (method == null && IsNullish(value) && !source.HasInProperty(key) && source is JsArray or JsArguments or JsKeyedCollection or JsBoxedPrimitive {Value:string})
                method = ConcatIntrinsicIterator.Open;
            if (method == null) throw new InvalidOperationException("Iterator.concat requires iterable objects");
            inputs[index] = (source,method);
        }
        return new ConcatIterator(inputs);
    }
}
