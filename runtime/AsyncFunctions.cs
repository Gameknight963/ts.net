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
        if (result is AsyncAwaitRecord awaiting) awaiting.Attach(promise, pair);
        else pair.Resolve.InvokeWithReceiver(Undefined, new[] { result });
        return promise;
    }
    sealed class AsyncAwaitRecord(JsPromise awaited)
    {
        internal void Attach(JsPromise target, PromiseResolvingFunctions pair)
        {
            var capability = new PromiseCapability { Promise = target, Resolve = pair.Resolve, Reject = pair.Reject };
            // Await uses intrinsic reactions, bypassing public then and species.
            // Even an already fulfilled value resumes through a queued job.
            awaited.Then(new PromiseReaction(capability, null, null));
        }
    }
    public static object AsyncAwait(object value) => new AsyncAwaitRecord((JsPromise)PromiseResolve(RuntimeNamespaces.Promise, value));
    sealed class AsyncFunctionObject : JsFunction
    {
        readonly JsFunction function;
        internal AsyncFunctionObject(JsFunction function) : base((FunctionBody)(args => InvokeAsyncFunction(function, Undefined, args)), null, (string)function.Get("name"), (int)(double)function.Get("length")) { this.function = function; }
        public override object InvokeWithReceiver(object receiver, object[] arguments) => InvokeAsyncFunction(function, receiver, arguments);
    }
    public static object AsyncFunction(object value) => new AsyncFunctionObject((JsFunction)value);
}
