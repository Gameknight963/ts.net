using System;
using System.Collections.Generic;

// Generators reuse the async control-flow records, but resume synchronously.
public static partial class Js
{
    sealed record GeneratorYield(object Value, JsFunction Continuation);
    sealed class GeneratorDelegate(TsObject iterator, JsFunction continuation)
    {
        internal readonly TsObject Iterator = iterator;
        internal readonly object Next = iterator.Get("next");
        internal readonly JsFunction Continuation = continuation;
    }
    sealed class GeneratorValue(JsFunction start) : TsObject
    {
        readonly Stack<AsyncTryFrame> frames = new();
        JsFunction continuation = start;
        GeneratorDelegate delegated;
        bool started, running, completed;
        internal TsObject FunctionPrototype;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : FunctionPrototype ?? GeneratorPrototype;
        static object Invoke(JsFunction function) => function.InvokeWithReceiver(Undefined, Array.Empty<object>());

        internal object Resume(CompletionKind kind, object value)
        {
            if (running) throw new InvalidOperationException("Generator is already running");
            if (completed || !started && kind != CompletionKind.Normal)
            {
                completed = true; continuation = null;
                if (kind == CompletionKind.Throw) throw MakeThrown(value);
                return IteratorResult(kind == CompletionKind.Return ? value : Undefined, true);
            }
            running = true;
            try
            {
                object result;
                try
                {
                    if (delegated != null) result = DelegateStep(kind, value);
                    else if (kind == CompletionKind.Throw) result = new AsyncCompletion(kind, MakeThrown(value));
                    else if (kind == CompletionKind.Return) result = new AsyncCompletion(kind, value);
                    else if (!started) { started = true; result = Invoke(continuation); }
                    else result = continuation.InvokeWithReceiver(Undefined, new[] { value });
                }
                catch (Exception error) { delegated = null; result = new AsyncCompletion(CompletionKind.Throw, error); }
                return Run(result);
            }
            finally { running = false; }
        }
        object DelegateStep(CompletionKind kind, object value)
        {
            var current = delegated;
            object method = kind == CompletionKind.Normal ? current.Next : current.Iterator.Get(kind == CompletionKind.Throw ? "throw" : "return");
            if (IsNullish(method))
            {
                delegated = null;
                if (kind == CompletionKind.Return) return new AsyncCompletion(kind, value);
                object close = current.Iterator.Get("return");
                if (!IsNullish(close) && CallMethod(close, current.Iterator, Array.Empty<object>()) is not TsObject)
                    throw new InvalidOperationException("Iterator return must return an object");
                throw new InvalidOperationException("Delegated iterator has no throw method");
            }
            if (CallMethod(method, current.Iterator, new[] { value }) is not TsObject result)
                throw new InvalidOperationException("Iterator result must be an object");
            if (!Truthy(result.Get("done"))) return new DelegatedYield(result);
            value = result.Get("value"); delegated = null;
            return kind == CompletionKind.Return ? new AsyncCompletion(kind, value) : current.Continuation.InvokeWithReceiver(Undefined, new[] { value });
        }
        sealed record DelegatedYield(TsObject Result);
        object Run(object result)
        {
            while (true)
            {
                try
                {
                    if (result is DelegatedYield forwarded) return forwarded.Result;
                    if (result is GeneratorYield yielded)
                    {
                        continuation = yielded.Continuation;
                        return IteratorResult(yielded.Value, false);
                    }
                    if (result is GeneratorDelegate delegation)
                    {
                        delegated = delegation;
                        result = DelegateStep(CompletionKind.Normal, Undefined);
                        continue;
                    }
                    if (result is AsyncTryRecord region)
                    {
                        frames.Push(new AsyncTryFrame(region)); result = Invoke(region.Body); continue;
                    }
                    if (result is AsyncResumeRecord resume)
                    {
                        if (frames.Count == 0 || frames.Peek().Region.Region == resume.Region)
                        { result = Invoke(resume.Continuation); continue; }
                        result = new AsyncCompletion(CompletionKind.Transfer, resume);
                    }
                    if (result is AsyncTryEndRecord end)
                    {
                        var current = frames.Peek();
                        if (current.Region.Region != end.Region) throw new InvalidOperationException("Invalid generator region completion");
                        if (current.Phase == 2)
                        {
                            frames.Pop();
                            result = current.Pending.Kind == CompletionKind.Normal ? Invoke(current.Region.Next) : current.Pending;
                            continue;
                        }
                        result = new AsyncCompletion(CompletionKind.Normal, Undefined);
                    }
                    var completion = result as AsyncCompletion ?? new AsyncCompletion(CompletionKind.Return, result);
                    if (frames.Count == 0)
                    {
                        if (completion.Kind == CompletionKind.Transfer)
                        { result = Invoke(((AsyncResumeRecord)completion.Value).Continuation); continue; }
                        completed = true; continuation = null; delegated = null;
                        if (completion.Kind == CompletionKind.Throw) throw (Exception)completion.Value;
                        return IteratorResult(completion.Value, true);
                    }
                    var frame = frames.Peek();
                    if (completion.Kind == CompletionKind.Transfer && frame.Region.Region == ((AsyncResumeRecord)completion.Value).Region)
                    { result = Invoke(((AsyncResumeRecord)completion.Value).Continuation); continue; }
                    if (completion.Kind == CompletionKind.Throw && frame.Phase == 0 && frame.Region.Catch != null)
                    {
                        frame.Phase = 1;
                        result = frame.Region.Catch.InvokeWithReceiver(Undefined, new[] { CaughtValue((Exception)completion.Value) });
                        continue;
                    }
                    if (frame.Phase != 2 && frame.Region.Finally != null)
                    {
                        frame.Phase = 2; frame.Pending = completion;
                        result = Invoke(frame.Region.Finally); continue;
                    }
                    if (frame.Phase == 2 && frame.Region.IteratorClose && frame.Pending.Kind == CompletionKind.Throw && completion.Kind == CompletionKind.Throw)
                        completion = frame.Pending;
                    frames.Pop();
                    result = completion.Kind == CompletionKind.Normal ? Invoke(frame.Region.Next) : completion;
                }
                catch (Exception error)
                {
                    if (completed) throw;
                    delegated = null;
                    result = new AsyncCompletion(CompletionKind.Throw, error);
                }
            }
        }
    }
    static class GeneratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var prototype = new JsClassInstance { ParentPrototype = NativeIteratorPrototype };
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Generator");
            foreach (var item in new[] { ("next", CompletionKind.Normal), ("throw", CompletionKind.Throw), ("return", CompletionKind.Return) })
                prototype.DefineMethod(item.Item1, new CollectionMethod(item.Item1, 1, (receiver, args) => receiver is GeneratorValue generator
                    ? generator.Resume(item.Item2, FunctionArgument(args, 0)) : throw new InvalidOperationException("Generator receiver required")));
            return prototype;
        }
    }
    static TsObject GeneratorPrototype => GeneratorMethods.Prototype;
    sealed class GeneratorFunctionObject : JsFunction
    {
        readonly Invocation invocation;
        sealed class Invocation(JsFunction function)
        {
            internal GeneratorFunctionObject Owner;
            internal object Invoke(object receiver, object[] args)
            {
                var result = (GeneratorValue)function.InvokeWithReceiver(receiver, args);
                result.FunctionPrototype = Owner.Get("prototype") as TsObject ?? GeneratorPrototype;
                return result;
            }
        }
        internal GeneratorFunctionObject(JsFunction function) : this(new Invocation(function), function) { }
        GeneratorFunctionObject(Invocation invocation, JsFunction function) : base((FunctionBody)(args => invocation.Invoke(Undefined, args)), null, (string)function.Get("name"), (int)(double)function.Get("length"))
        {
            this.invocation = invocation; invocation.Owner = this;
            UseObjectPrototype(new JsClassInstance { ParentPrototype = GeneratorPrototype });
        }
        public override object InvokeWithReceiver(object receiver, object[] args) => invocation.Invoke(receiver, args);
    }
    public static object GeneratorFunction(object function) => new GeneratorFunctionObject((JsFunction)function);
    static TsObject GeneratorIterator(object source) => TryGetIterableObject(source, out var iterator) ? iterator : (TsObject)IteratorFrom(source);
    static class GeneratorFunctions
    {
        internal static readonly JsFunction Create = new((FunctionBody)(static args => new GeneratorValue((JsFunction)FunctionArgument(args, 0))), null, "", 1);
        internal static readonly JsFunction Yield = new((FunctionBody)(static args => new GeneratorYield(FunctionArgument(args, 0), (JsFunction)FunctionArgument(args, 1))), null, "", 2);
        internal static readonly JsFunction Delegate = new((FunctionBody)(static args => new GeneratorDelegate(GeneratorIterator(FunctionArgument(args, 0)), (JsFunction)FunctionArgument(args, 1))), null, "", 2);
    }
}
