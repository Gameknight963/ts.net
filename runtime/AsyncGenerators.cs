using System;
using System.Collections.Generic;

public static partial class Js
{
    static readonly object AsyncGeneratorPending = new();
    static void ObserveGeneratorAwait(object value, Action<object, bool> next)
    {
        var promise = (JsPromise)PromiseResolve(RuntimeNamespaces.Promise, value);
        promise.Then(new PromiseContinuationReaction(next));
    }
    sealed partial class GeneratorValue
    {
        sealed class Request
        {
            internal CompletionKind Kind;
            internal object Value;
            internal readonly JsPromise Promise = new();
            internal readonly PromiseResolvingFunctions Pair;
            internal Request(CompletionKind kind, object value)
            {
                Kind = kind; Value = value;
                Pair = new PromiseResolvingFunctions(Promise);
            }
        }
        Queue<Request> requests;
        bool pumping, waiting;
        internal object QueueResume(CompletionKind kind, object value)
        {
            var request = new Request(kind, value);
            (requests ??= new()).Enqueue(request);
            PumpRequests();
            return request.Promise;
        }
        void PumpRequests()
        {
            if (pumping || waiting) return;
            pumping = true;
            try
            {
                while (!waiting && requests.Count != 0)
                {
                    waiting = true;
                    var request = requests.Peek();
                    if (request.Kind == CompletionKind.Return)
                    {
                        try
                        {
                            ObserveGeneratorAwait(request.Value, (value, rejected) =>
                            {
                                request.Value = value;
                                if (rejected) request.Kind = CompletionKind.Throw;
                                ResumeRequest(request);
                            });
                        }
                        catch (Exception error) { CompleteRequest(null, error); }
                    }
                    else ResumeRequest(request);
                }
            }
            finally { pumping = false; }
        }
        void ResumeRequest(Request request)
        {
            try
            {
                object result = Resume(request.Kind, request.Value);
                if (!ReferenceEquals(result, AsyncGeneratorPending)) CompleteRequest(result, null);
            }
            catch (Exception error) { CompleteRequest(null, error); }
        }
        void ContinueAsyncRun(object result)
        {
            try
            {
                result = Run(result);
                if (!ReferenceEquals(result, AsyncGeneratorPending)) CompleteRequest(result, null);
            }
            catch (Exception error) { CompleteRequest(null, error); }
        }
        void CompleteRequest(object result, Exception error)
        {
            var request = requests.Dequeue();
            if (error != null) request.Pair.RejectError(error);
            else request.Pair.Resolve.InvokeWithReceiver(Undefined, new[] { result });
            waiting = false;
            PumpRequests();
        }
        object BeginDelegateStep(CompletionKind kind, object value)
        {
            var current = delegated;
            object method = kind == CompletionKind.Normal ? current.Next : current.Iterator.Get(kind == CompletionKind.Throw ? "throw" : "return");
            if (IsNullish(method))
            {
                delegated = null;
                if (kind == CompletionKind.Return) return new AsyncCompletion(kind, value);
                object close = current.Iterator.Get("return");
                if (IsNullish(close)) throw new InvalidOperationException("Delegated iterator has no throw method");
                object closing = CallMethod(close, current.Iterator, Array.Empty<object>());
                void Closed(object result, bool rejected)
                {
                    if (rejected) ContinueAsyncRun(new AsyncCompletion(CompletionKind.Throw, MakeThrown(result)));
                    else
                    {
                        Exception error = result is TsObject ? new InvalidOperationException("Delegated iterator has no throw method") : new InvalidOperationException("Iterator return must return an object");
                        ContinueAsyncRun(new AsyncCompletion(CompletionKind.Throw, error));
                    }
                }
                if (current.AsyncIterator) ObserveGeneratorAwait(closing, Closed);
                else if (closing is TsObject obj) ObserveGeneratorAwait(obj.Get("value"), (value, rejected) => Closed(rejected ? value : closing, rejected));
                else Closed(closing, false);
                return AsyncGeneratorPending;
            }
            object returned = CallMethod(method, current.Iterator, new[] { value });
            void Accepted(object result, bool rejected)
            {
                try
                {
                    if (rejected) { delegated = null; ContinueAsyncRun(new AsyncCompletion(CompletionKind.Throw, MakeThrown(result))); return; }
                    if (result is not TsObject obj) throw new InvalidOperationException("Iterator result must be an object");
                    bool done = Truthy(obj.Get("done"));
                    object item = obj.Get("value");
                    if (current.AsyncIterator) Finish(item, false, done);
                    else ObserveGeneratorAwait(item, (resolved, failed) => Finish(resolved, failed, done));
                }
                catch (Exception error) { delegated = null; ContinueAsyncRun(new AsyncCompletion(CompletionKind.Throw, error)); }
            }
            void Finish(object item, bool rejected, bool done)
            {
                if (rejected) { delegated = null; ContinueAsyncRun(new AsyncCompletion(CompletionKind.Throw, MakeThrown(item))); return; }
                if (!done) { CompleteRequest(IteratorResult(item, false), null); return; }
                delegated = null;
                if (kind == CompletionKind.Return)
                {
                    ObserveGeneratorAwait(item, (resolved, failed) => ContinueAsyncRun(failed
                        ? new AsyncCompletion(CompletionKind.Throw, MakeThrown(resolved)) : new AsyncCompletion(CompletionKind.Return, resolved)));
                }
                else
                {
                    object next;
                    try { next = current.Continuation.InvokeWithReceiver(Undefined, new[] { item }); }
                    catch (Exception error) { next = new AsyncCompletion(CompletionKind.Throw, error); }
                    ContinueAsyncRun(next);
                }
            }
            if (current.AsyncIterator) ObserveGeneratorAwait(returned, Accepted);
            else Accepted(returned, false);
            return AsyncGeneratorPending;
        }
    }
    static GeneratorDelegate AsyncGeneratorDelegate(object source, JsFunction continuation)
    {
        if (TryGetAsyncIterator(source, out var iterator, out var next)) return new GeneratorDelegate(iterator, continuation, true, next);
        return new GeneratorDelegate(GeneratorIterator(source), continuation);
    }
    static class AsyncGeneratorFunctions
    {
        internal static readonly JsFunction Create = new((FunctionBody)(static args => new GeneratorValue((JsFunction)FunctionArgument(args, 0), true)), null, "", 1);
        internal static readonly JsFunction Delegate = new((FunctionBody)(static args => AsyncGeneratorDelegate(FunctionArgument(args, 0), (JsFunction)FunctionArgument(args, 1))), null, "", 2);
    }
}
