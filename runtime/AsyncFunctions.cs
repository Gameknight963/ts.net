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
        CompleteAsyncResult(result, pair);
        return promise;
    }
    static void CompleteAsyncResult(object result, PromiseResolvingFunctions pair)
    {
        // Internal synchronous transfers unwind generated functions without
        // adding Promise jobs or growing the stack across case fallthrough.
        try
        {
            while (result is AsyncResumeRecord resume)
                result = resume.Continuation.InvokeWithReceiver(Undefined, Array.Empty<object>());
        }
        catch (Exception error) { pair.RejectError(error); return; }
        if (result is AsyncTryRecord) new AsyncExecution(pair).Run(result);
        else if (result is AsyncAwaitRecord awaiting) awaiting.Attach(pair);
        else pair.ResolveValue(result);
    }
    sealed class AsyncResumeRecord(JsFunction continuation, int region = 0)
    {
        internal readonly JsFunction Continuation = continuation;
        internal readonly int Region = region;
    }
    public static object AsyncResume(object continuation) => new AsyncResumeRecord((JsFunction)continuation);
    public static object AsyncResumeRegion(object continuation, int region) => new AsyncResumeRecord((JsFunction)continuation, region);
    sealed class AsyncAwaitRecord(object awaited, JsFunction continuation = null) : PromiseReactionBase
    {
        // Before attachment this holds the operand; afterwards it holds the
        // destination. The same record becomes the queued await reaction.
        object destination = awaited;
        AsyncExecution execution;

        void AttachDestination(object next)
        {
            var operand = destination;
            destination = next;
            if (operand is JsPromise promise) promise.Then(this);
            else Enqueue(operand, false);
        }
        internal void AttachGenerator(Action<object> run) => AttachDestination(run);
        internal void Attach(PromiseResolvingFunctions pair, AsyncExecution execution = null)
        {
            this.execution = execution;
            // Intrinsic reactions preserve the asynchronous ordering even for
            // fulfilled operands, without invoking public then or species.
            AttachDestination(pair);
        }
        internal override void Run(object value, bool rejected)
        {
            var next = destination;
            destination = null;
            if (next is Action<object> run)
            {
                object completion;
                try { completion = rejected ? new AsyncCompletion(CompletionKind.Throw, MakeThrown(value)) : continuation == null ? value : continuation.InvokeContinuation(value); }
                catch (Exception error) { completion = new AsyncCompletion(CompletionKind.Throw, error); }
                run(completion);
                return;
            }
            var pair = (PromiseResolvingFunctions)next;
            if (rejected)
            {
                if (execution != null) execution.Run(new AsyncCompletion(CompletionKind.Throw, MakeThrown(value)));
                else pair.RejectValue(value);
                return;
            }
            object result;
            try { result = continuation == null ? value : continuation.InvokeContinuation(value); }
            catch (Exception error)
            {
                if (execution != null) execution.Run(new AsyncCompletion(CompletionKind.Throw, error));
                else pair.RejectError(error);
                return;
            }
            if (execution != null) execution.Run(result);
            else CompleteAsyncResult(result, pair);
        }
    }
    // Primitive resolution cannot invoke user code. Queue its reaction directly,
    // retaining await's asynchronous ordering without a temporary promise.
    static AsyncAwaitRecord AwaitRecord(object value, JsFunction continuation = null) =>
        new(value is TsObject ? PromiseResolve(RuntimeNamespaces.Promise, value) : value, continuation);
    public static object AsyncAwait(object value) => AwaitRecord(value);
    public static object AsyncAwaitContinue(object value, object continuation) => AwaitRecord(value, (JsFunction)continuation);
    sealed class AsyncFunctionObject : JsFunction
    {
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : AsyncFunctionFamily.Value.Prototype;
        readonly JsFunction function;
        internal override string SourceText => function.SourceText;
        internal override void SetObjectHome(TsObject home) => function.SetObjectHome(home);
        internal AsyncFunctionObject(JsFunction function) : base((FunctionBody)(args => InvokeAsyncFunction(function, Undefined, args)), null, (string)function.Get("name"), (int)(double)function.Get("length")) { this.function = function; }
        public override object InvokeWithReceiver(object receiver, object[] arguments) => InvokeAsyncFunction(function, receiver, arguments);
    }
    public static object AsyncFunction(object value) => new AsyncFunctionObject((JsFunction)value);
}
