using System;
using System.Collections.Generic;

public static partial class Js
{
    enum CompletionKind { Normal, Return, Throw, Transfer }
    sealed record AsyncCompletion(CompletionKind Kind, object Value);
    sealed record AsyncTryRecord(JsFunction Body, JsFunction Catch, JsFunction Finally, JsFunction Next, int Region);
    sealed record AsyncTryEndRecord(int Region);
    sealed class AsyncTryFrame(AsyncTryRecord region)
    {
        internal readonly AsyncTryRecord Region = region;
        internal int Phase; // 0: try, 1: catch, 2: finally
        internal AsyncCompletion Pending;
    }

    // One execution owns its protected regions across intrinsic await reactions.
    // It is allocated only when an async function actually enters an async try.
    sealed class AsyncExecution(JsPromise promise, PromiseResolvingFunctions pair)
    {
        readonly Stack<AsyncTryFrame> frames = new();
        static object Invoke(JsFunction function) => function.InvokeWithReceiver(Undefined, Array.Empty<object>());

        internal void Run(object result)
        {
            while (true)
            {
                try
                {
                    if (result is AsyncTryRecord region)
                    {
                        frames.Push(new AsyncTryFrame(region));
                        result = Invoke(region.Body);
                        continue;
                    }
                    if (result is AsyncAwaitRecord awaiting)
                    {
                        awaiting.Attach(promise, pair, this);
                        return;
                    }
                    if (result is AsyncResumeRecord resume)
                    {
                        if (frames.Count == 0 || frames.Peek().Region.Region == resume.Region)
                        {
                            result = Invoke(resume.Continuation);
                            continue;
                        }
                        result = new AsyncCompletion(CompletionKind.Transfer, resume);
                    }
                    if (result is AsyncTryEndRecord end)
                    {
                        var current = frames.Peek();
                        if (current.Region.Region != end.Region) throw new InvalidOperationException("Invalid async region completion");
                        if (current.Phase == 2)
                        {
                            frames.Pop();
                            if (current.Pending.Kind == CompletionKind.Normal) result = Invoke(current.Region.Next);
                            else result = current.Pending;
                            continue;
                        }
                        result = new AsyncCompletion(CompletionKind.Normal, Undefined);
                    }
                    var completion = result as AsyncCompletion ?? new AsyncCompletion(CompletionKind.Return, result);
                    if (frames.Count == 0)
                    {
                        if (completion.Kind == CompletionKind.Throw) pair.RejectError((Exception)completion.Value);
                        else if (completion.Kind == CompletionKind.Transfer)
                        {
                            result = Invoke(((AsyncResumeRecord)completion.Value).Continuation);
                            continue;
                        }
                        else pair.Resolve.InvokeWithReceiver(Undefined, new[] { completion.Value });
                        return;
                    }
                    var frame = frames.Peek();
                    if (completion.Kind == CompletionKind.Transfer && frame.Region.Region == ((AsyncResumeRecord)completion.Value).Region)
                    {
                        result = Invoke(((AsyncResumeRecord)completion.Value).Continuation);
                        continue;
                    }
                    if (completion.Kind == CompletionKind.Throw && frame.Phase == 0 && frame.Region.Catch != null)
                    {
                        frame.Phase = 1;
                        result = frame.Region.Catch.InvokeWithReceiver(Undefined, new[] { CaughtValue((Exception)completion.Value) });
                        continue;
                    }
                    if (frame.Phase != 2 && frame.Region.Finally != null)
                    {
                        frame.Phase = 2;
                        frame.Pending = completion;
                        result = Invoke(frame.Region.Finally);
                        continue;
                    }
                    frames.Pop();
                    result = completion.Kind == CompletionKind.Normal ? Invoke(frame.Region.Next) : completion;
                }
                catch (Exception error) { result = new AsyncCompletion(CompletionKind.Throw, error); }
            }
        }
    }

    public static object AsyncTry(object body, object handler, object finalizer, object next, int region) =>
        new AsyncTryRecord((JsFunction)body, handler as JsFunction, finalizer as JsFunction, (JsFunction)next, region);
    public static object AsyncTryEnd(int region) => new AsyncTryEndRecord(region);
}
