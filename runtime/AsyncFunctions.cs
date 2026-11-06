using System;

public static partial class Js
{
    static object InvokeAsyncFunction(JsFunction function, object receiver, object[] arguments)
    {
        var promise = new JsPromise();
        var pair = new PromiseResolvingFunctions(promise);
        object result;
        try { result = function.InvokeWithReceiver(receiver, arguments); }
        catch (Exception error) { pair.RejectError(error); return promise; }
        CompleteAsyncResult(result, promise, pair);
        return promise;
    }
    static void CompleteAsyncResult(object result, JsPromise promise, PromiseResolvingFunctions pair)
    {
        if (result is AsyncAwaitRecord awaiting) awaiting.Attach(promise, pair);
        else pair.Resolve.InvokeWithReceiver(Undefined, new[] { result });
    }
    sealed class AsyncAwaitRecord(JsPromise awaited, JsFunction continuation = null)
    {
        internal void Attach(JsPromise target, PromiseResolvingFunctions pair)
        {
            var capability = new PromiseCapability { Promise = target, Resolve = pair.Resolve, Reject = pair.Reject };
            // Await uses intrinsic reactions, bypassing public then and species.
            // Even an already fulfilled value resumes through a queued job.
            if (continuation == null) { awaited.Then(new PromiseReaction(capability, null, null)); return; }
            awaited.Then(new PromiseReaction(null, null, null, (value, rejected) =>
            {
                if (rejected) { pair.Reject.InvokeWithReceiver(Undefined, new[] { value }); return; }
                object result;
                try { result = continuation.InvokeWithReceiver(Undefined, new[] { value }); }
                catch (Exception error) { pair.RejectError(error); return; }
                CompleteAsyncResult(result, target, pair);
            }));
        }
    }
    public static object AsyncAwait(object value) => new AsyncAwaitRecord((JsPromise)PromiseResolve(RuntimeNamespaces.Promise, value));
    public static object AsyncAwaitContinue(object value, object continuation) => new AsyncAwaitRecord((JsPromise)PromiseResolve(RuntimeNamespaces.Promise, value), (JsFunction)continuation);
    sealed class AsyncFunctionObject : JsFunction
    {
        readonly JsFunction function;
        internal AsyncFunctionObject(JsFunction function) : base((FunctionBody)(args => InvokeAsyncFunction(function, Undefined, args)), null, (string)function.Get("name"), (int)(double)function.Get("length")) { this.function = function; }
        public override object InvokeWithReceiver(object receiver, object[] arguments) => InvokeAsyncFunction(function, receiver, arguments);
    }
    public static object AsyncFunction(object value) => new AsyncFunctionObject((JsFunction)value);
}
