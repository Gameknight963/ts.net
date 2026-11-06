using System;

public static partial class Js
{
    static object IteratorResult(object value, bool done)
    {
        var result = new JsDynamicObject();
        result.DefineOwn("value", value);
        result.DefineOwn("done", done ? BoxedTrue : BoxedFalse);
        return result;
    }

    public static object ForAwaitNext(object state)
    {
        var cursor = (AsyncIterationState)state;
        cursor.NeedsClose = false;
        var result = new JsPromise();
        var pair = new PromiseResolvingFunctions(result);
        try
        {
            bool more = NextAsyncIteration(state);
            var value = (JsPromise)PromiseResolve(RuntimeNamespaces.Promise, more ? cursor.Current : Undefined);
            // Unwrap the sync iterator's value in an intrinsic reaction. The
            // loop then awaits the resulting iterator-result promise separately.
            value.Then(new PromiseReaction(null, null, null, (resolved, rejected) =>
            {
                if (rejected) pair.Reject.InvokeWithReceiver(Undefined, new[] { resolved });
                else pair.Resolve.InvokeWithReceiver(Undefined, new[] { IteratorResult(resolved, !more) });
            }));
        }
        catch (Exception error) { pair.RejectError(error); }
        return result;
    }

    public static object RequireIteratorResult(object result) => result is TsObject ? result : throw new InvalidOperationException("Iterator result is not an object");
    public static bool ForAwaitAccept(object state, object result)
    {
        RequireIteratorResult(result);
        if (Truthy(Get(result, "done"))) return false;
        var cursor = (AsyncIterationState)state;
        cursor.Current = Get(result, "value");
        cursor.NeedsClose = true;
        return true;
    }
    public static bool ForAwaitNeedsClose(object state) => ((AsyncIterationState)state).NeedsClose;
    public static object ForAwaitClose(object state)
    {
        var cursor = (AsyncIterationState)state;
        cursor.NeedsClose = false;
        cursor.Finished = true;
        // The supported native iterators have no return method. The async-from-
        // sync adapter still returns a promise of an iterator result, and await
        // must observe its resolution (including inherited then properties).
        var result = new JsPromise();
        new PromiseResolvingFunctions(result).Resolve.InvokeWithReceiver(Undefined, new[] { IteratorResult(Undefined, true) });
        return result;
    }
}
