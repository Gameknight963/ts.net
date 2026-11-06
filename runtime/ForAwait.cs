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

    public static object ForAwaitNext(object state)
    {
        var cursor = (AsyncIterationState)state;
        cursor.NeedsClose = false;
        var result = new JsPromise();
        var pair = new PromiseResolvingFunctions(result);
        bool more = false;
        try
        {
            more = NextAsyncIteration(state);
            cursor.NeedsClose = false;
            var value = (JsPromise)PromiseResolve(RuntimeNamespaces.Promise, more ? cursor.Current : Undefined);
            // Unwrap the sync iterator's value in an intrinsic reaction. The
            // loop then awaits the resulting iterator-result promise separately.
            value.Then(new PromiseReaction(null, null, null, (resolved, rejected) =>
            {
                if (rejected) { if (more) CloseIterationAt(cursor.Source, unchecked((int)cursor.Position), true); pair.Reject.InvokeWithReceiver(Undefined, new[] { resolved }); }
                else pair.Resolve.InvokeWithReceiver(Undefined, new[] { IteratorResult(resolved, !more) });
            }));
        }
        catch (Exception error) { if (more) CloseIterationAt(cursor.Source, unchecked((int)cursor.Position), true); pair.RejectError(error); }
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
        // A missing return method still produces a promise of an iterator
        // result. Its resolution must observe inherited then properties.
        var result = new JsPromise();
        var pair = new PromiseResolvingFunctions(result);
        try
        {
            TsObject iterator = IterationObjectAt(cursor.Source, unchecked((int)cursor.Position));
            object method = iterator == null ? Undefined : iterator.Get("return");
            if (IsNullish(method)) pair.Resolve.InvokeWithReceiver(Undefined, new[] { IteratorResult(Undefined, true) });
            else
            {
                if (method is not JsFunction function) throw new InvalidOperationException("Iterator return is not callable");
                object returned = RequireIteratorResult(function.InvokeWithReceiver(iterator, Array.Empty<object>()));
                bool done = Truthy(Get(returned, "done"));
                var value = (JsPromise)PromiseResolve(RuntimeNamespaces.Promise, Get(returned, "value"));
                value.Then(new PromiseReaction(null, null, null, (resolved, rejected) =>
                {
                    if (rejected) pair.Reject.InvokeWithReceiver(Undefined, new[] { resolved });
                    else pair.Resolve.InvokeWithReceiver(Undefined, new[] { IteratorResult(resolved, done) });
                }));
            }
        }
        catch (Exception error) { pair.RejectError(error); }
        return result;
    }
}
