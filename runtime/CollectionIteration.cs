using System;

internal sealed class JsProtocolIterator(TsObject source, object next)
{
    internal readonly TsObject Source = source;
    internal bool Done;
    bool cached;
    bool direct;
    object result;
    internal bool Ensure()
    {
        if (cached) return true;
        if (Done) return false;
        try
        {
            if (Source is JsSequenceIterator sequence && ReferenceEquals(next,Js.SequenceIteratorIntrinsicNext(sequence.IsString))) {
                direct = true;
                if (!sequence.TryNext(out result)) {Done = true; result = null; return false;}
                cached = true; return true;
            }
            if (Js.TryReadConcatIterator(Source,next,out var value,out bool done)) {
                direct = true; Done = done; result = done ? null : value; cached = !done; return !done;
            }
            direct = false;
            if (next is not JsFunction function) throw new InvalidOperationException("Iterator next is not callable");
            result = Js.RequireIteratorResult(function.InvokeWithReceiver(Source, Array.Empty<object>()));
            if (Js.Truthy(Js.Get(result, "done"))) { Done = true; result = null; return false; }
            cached = true; return true;
        }
        catch { Done = true; result = null; throw; }
    }
    internal object TakeValue()
    {
        if (!Ensure()) return Js.Undefined;
        object current = result; result = null; cached = false;
        try { return direct ? current : Js.Get(current, "value"); }
        catch { Done = true; throw; }
    }
    internal void SkipValue()
    {
        if (!Ensure()) return;
        result = null; cached = false;
    }
}

public static partial class Js
{
    internal static object StartCollectionIterator(JsCollectionIterator iterator)
    {
        object next = iterator.Get("next");
        if (ReferenceEquals(next, KeyedIteratorIntrinsicNext(iterator.IsSetIterator))) return iterator;
        return new JsProtocolIterator(iterator, next);
    }
    public static void CloseIteration(object source, bool suppressError)
    {
        TsObject iterator = IterationObject(source);
        if (iterator == null) return;
        try { CloseIteratorObject(iterator); }
        catch when (suppressError) { }
    }
    static void CloseIteratorObject(TsObject iterator)
    {
        if (iterator == null) return;
        object method = iterator.Get("return");
        if (IsNullish(method)) return;
        if (method is not JsFunction function) throw new InvalidOperationException("Iterator return is not callable");
        RequireIteratorResult(function.InvokeWithReceiver(iterator, Array.Empty<object>()));
    }
    internal static TsObject IterationObject(object source) => source switch
        {
            JsCollectionIterator { Owner: not null } native => native,
            JsProtocolIterator { Done: false } custom => custom.Source,
            JsSequenceIterator { Done: false } native => native,
            JsRegexIterator { Done: false } native => native,
            _ => null
        };
}
