using System;

public static partial class Js
{
    internal static object IteratorResult(object value, bool done)
    {
        var result = new JsDynamicObject();
        result.DefineOwn("value", value);
        result.DefineOwn("done", done ? BoxedTrue : BoxedFalse);
        return result;
    }
    static void ObserveSyncIteratorValue(object value, PromiseReaction reaction)
    {
        // Primitive resolution cannot run user code. Queue the same intrinsic
        // reaction without creating an otherwise unobservable fulfilled promise.
        if (value is TsObject) ((JsPromise)PromiseResolve(RuntimeNamespaces.Promise, value)).Then(reaction);
        else reaction.Enqueue(value, false);
    }

    public static object ForAwaitNext(object state)
    {
        var cursor = (AsyncIterationState)state;
        cursor.NeedsClose = false;
        if (!cursor.Started && TryGetAsyncIterator(cursor.Source, out var asyncSource, out var asyncNext)) {
            cursor.Source = asyncSource; cursor.AsyncNext = asyncNext; cursor.Asynchronous = true; cursor.Started = true;
        }
        if (cursor.Asynchronous)
            return PromiseResolve(RuntimeNamespaces.Promise, CallMethod(cursor.AsyncNext, cursor.Source, Array.Empty<object>()));
        var result = new JsPromise();
        var pair = new PromiseResolvingFunctions(result);
        bool more = false;
        try
        {
            more = NextAsyncIteration(state);
            cursor.NeedsClose = false;
            // Unwrap the sync iterator's value in an intrinsic reaction. The
            // loop then awaits the resulting iterator-result promise separately.
            ObserveSyncIteratorValue(more ? cursor.Current : Undefined, new PromiseReaction(null, null, null, (resolved, rejected) =>
            {
                if (rejected) { if (more) CloseIterationAt(cursor.Source, unchecked((int)cursor.Position), true); pair.RejectValue(resolved); }
                else pair.ResolveValue(IteratorResult(resolved, !more));
            }));
        }
        catch (Exception error) { if (more) CloseIterationAt(cursor.Source, unchecked((int)cursor.Position), true); pair.RejectError(error); }
        return result;
    }

    public static object RequireIteratorResult(object result) => result is TsObject ? result : throw new InvalidOperationException("Iterator result is not an object");
    public static bool ForAwaitAccept(object state, object result)
    {
        RequireIteratorResult(result);
        var cursor = (AsyncIterationState)state;
        if (Truthy(Get(result, "done"))) { cursor.Finished = true; return false; }
        cursor.Current = Get(result, "value");
        cursor.NeedsClose = true;
        return true;
    }
    public static bool ForAwaitNeedsClose(object state) => ((AsyncIterationState)state).NeedsClose;
    public static object ForAwaitClose(object state)
    {
        var cursor = (AsyncIterationState)state;
        cursor.NeedsClose = false;
        if (cursor.Asynchronous) {
            object close = ((TsObject)cursor.Source).Get("return");
            return PromiseResolve(RuntimeNamespaces.Promise, IsNullish(close)
                ? IteratorResult(Undefined, true) : CallMethod(close, cursor.Source, Array.Empty<object>()));
        }
        // A missing return method still produces a promise of an iterator
        // result. Its resolution must observe inherited then properties.
        var result = new JsPromise();
        var pair = new PromiseResolvingFunctions(result);
        try
        {
            TsObject iterator = IterationObjectAt(cursor.Source, unchecked((int)cursor.Position));
            object method = iterator == null ? Undefined : iterator.Get("return");
            if (IsNullish(method)) pair.ResolveValue(IteratorResult(Undefined, true));
            else
            {
                if (method is not JsFunction function) throw new InvalidOperationException("Iterator return is not callable");
                object returned = RequireIteratorResult(function.InvokeWithReceiver(iterator, Array.Empty<object>()));
                bool done = Truthy(Get(returned, "done"));
                ObserveSyncIteratorValue(Get(returned, "value"), new PromiseReaction(null, null, null, (resolved, rejected) =>
                {
                    if (rejected) pair.RejectValue(resolved);
                    else pair.ResolveValue(IteratorResult(resolved, done));
                }));
            }
        }
        catch (Exception error) { pair.RejectError(error); }
        return result;
    }
}
