using System;
using System.Collections.Generic;

// Generators share control-flow records with async functions.
public static partial class Js
{
    struct GeneratorStep
    {
        internal object Value;
        internal bool Done, Direct;
    }
    sealed record GeneratorYield(object Value, JsFunction Continuation);
    sealed class GeneratorDelegate(TsObject iterator, JsFunction continuation, bool asyncIterator = false, object next = null)
    {
        internal readonly TsObject Iterator = iterator;
        internal readonly object Next = asyncIterator ? next : iterator.Get("next");
        internal readonly bool AsyncIterator = asyncIterator;
        internal readonly JsFunction Continuation = continuation;
    }
    sealed partial class GeneratorValue(JsFunction start, bool asynchronous = false) : JsDescriptorObject
    {
        internal readonly bool Asynchronous = asynchronous;
        readonly Stack<AsyncTryFrame> frames = new();
        JsFunction continuation = start;
        GeneratorDelegate delegated;
        bool started, running, completed;
        internal TsObject FunctionPrototype;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : FunctionPrototype ?? (Asynchronous ? AsyncGeneratorPrototype : GeneratorPrototype);
        static object Invoke(JsFunction function) => function.InvokeWithReceiver(Undefined, Array.Empty<object>());

        internal object Resume(CompletionKind kind, object value) => Resume(kind, value, false, out _);
        internal object Resume(CompletionKind kind, object value, bool direct, out GeneratorStep step)
        {
            step = default;
            if (running) throw new InvalidOperationException("Generator is already running");
            if (completed || !started && kind != CompletionKind.Normal)
            {
                completed = true; continuation = null;
                if (kind == CompletionKind.Throw) throw MakeThrown(value);
                return StepResult(kind == CompletionKind.Return ? value : Undefined, true, direct, out step);
            }
            running = true;
            try
            {
                object result;
                try
                {
                    if (delegated != null) result = Asynchronous ? BeginDelegateStep(kind, value) : DelegateStep(kind, value);
                    else if (kind == CompletionKind.Throw) result = new AsyncCompletion(kind, MakeThrown(value));
                    else if (kind == CompletionKind.Return) result = new AsyncCompletion(kind, value);
                    else if (!started) { started = true; result = Invoke(continuation); }
                    else result = continuation.InvokeWithReceiver(Undefined, new[] { value });
                }
                catch (Exception error) { delegated = null; result = new AsyncCompletion(CompletionKind.Throw, error); }
                return Run(result, direct, out step);
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
        static object StepResult(object value, bool done, bool direct, out GeneratorStep step)
        {
            step = new GeneratorStep { Value = value, Done = done, Direct = direct };
            return direct ? null : IteratorResult(value, done);
        }
        object Run(object result) => Run(result, false, out _);
        object Run(object result, bool direct, out GeneratorStep step)
        {
            step = default;
            while (true)
            {
                try
                {
                    if (ReferenceEquals(result, AsyncGeneratorPending)) return result;
                    if (Asynchronous && result is AsyncAwaitRecord awaiting)
                    {
                        awaiting.AttachGenerator(ContinueAsyncRun);
                        return AsyncGeneratorPending;
                    }
                    if (result is DelegatedYield forwarded) return forwarded.Result;
                    if (result is GeneratorYield yielded)
                    {
                        continuation = yielded.Continuation;
                        if (Asynchronous)
                        {
                            ObserveGeneratorAwait(yielded.Value, (value, rejected) =>
                            {
                                if (rejected) ContinueAsyncRun(new AsyncCompletion(CompletionKind.Throw, MakeThrown(value)));
                                else CompleteRequest(IteratorResult(value, false), null);
                            });
                            return AsyncGeneratorPending;
                        }
                        return StepResult(yielded.Value, false, direct, out step);
                    }
                    if (result is GeneratorDelegate delegation)
                    {
                        delegated = delegation;
                        result = Asynchronous ? BeginDelegateStep(CompletionKind.Normal, Undefined) : DelegateStep(CompletionKind.Normal, Undefined);
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
                        return StepResult(completion.Value, true, direct, out step);
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
        internal static JsFunction Next;
        internal static readonly TsObject Prototype = Create(false), AsyncPrototype = Create(true);
        static TsObject Create(bool asynchronous)
        {
            var prototype = new JsClassInstance { ParentPrototype = asynchronous ? NativeAsyncIteratorPrototype : NativeIteratorPrototype };
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), asynchronous ? "AsyncGenerator" : "Generator", false);
            foreach (var item in new[] { ("next", CompletionKind.Normal), ("throw", CompletionKind.Throw), ("return", CompletionKind.Return) })
            {
                var method = new CollectionMethod(item.Item1, 1, (receiver, args) => receiver is GeneratorValue generator && generator.Asynchronous == asynchronous
                    ? (asynchronous ? generator.QueueResume(item.Item2, FunctionArgument(args, 0)) : generator.Resume(item.Item2, FunctionArgument(args, 0))) : throw new InvalidOperationException("Generator receiver required"));
                prototype.DefineMethod(item.Item1, method);
                if (!asynchronous && item.Item2 == CompletionKind.Normal) Next = method;
            }
            return prototype;
        }
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    internal static bool TryReadGenerator(TsObject source, object next, out object result, out object value, out bool done)
    {
        if (source is GeneratorValue { Asynchronous: false } generator && ReferenceEquals(next, GeneratorMethods.Next))
        {
            result = generator.Resume(CompletionKind.Normal, Undefined, true, out var step);
            value = step.Value; done = step.Done;
            return step.Direct;
        }
        result = null; value = null; done = false;
        return false;
    }
    static TsObject GeneratorPrototype => GeneratorMethods.Prototype;
    static TsObject AsyncGeneratorPrototype => GeneratorMethods.AsyncPrototype;
    sealed class GeneratorFunctionObject : JsFunction
    {
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (invocation.Asynchronous ? AsyncGeneratorFunctionFamily.Value : GeneratorFunctionFamily.Value).Prototype;
        readonly Invocation invocation;
        sealed class Invocation(JsFunction function, bool asynchronous)
        {
            internal GeneratorFunctionObject Owner;
            internal bool Asynchronous => asynchronous;
            internal string SourceText => function.SourceText;
            internal void SetObjectHome(TsObject home) => function.SetObjectHome(home);
            internal object Invoke(object receiver, object[] args)
            {
                var result = (GeneratorValue)function.InvokeWithReceiver(receiver, args);
                result.FunctionPrototype = Owner.Get("prototype") as TsObject ?? (asynchronous ? AsyncGeneratorPrototype : GeneratorPrototype);
                return result;
            }
        }
        internal GeneratorFunctionObject(JsFunction function, bool asynchronous = false) : this(new Invocation(function, asynchronous), function, asynchronous) { }
        GeneratorFunctionObject(Invocation invocation, JsFunction function, bool asynchronous) : base((FunctionBody)(args => invocation.Invoke(Undefined, args)), null, (string)function.Get("name"), (int)(double)function.Get("length"))
        {
            this.invocation = invocation; invocation.Owner = this;
            var family = asynchronous ? AsyncGeneratorFunctionFamily.Value : GeneratorFunctionFamily.Value;
            UseObjectPrototype(new JsClassInstance { ParentPrototype = family.IteratorPrototype });
        }
        internal override string SourceText => invocation.SourceText;
        internal override void SetObjectHome(TsObject home) => invocation.SetObjectHome(home);
        public override object InvokeWithReceiver(object receiver, object[] args) => invocation.Invoke(receiver, args);
    }
    public static object AsyncGeneratorFunction(object function) => new GeneratorFunctionObject((JsFunction)function, true);
    public static object GeneratorFunction(object function) => new GeneratorFunctionObject((JsFunction)function);
    public static object YieldGenerator(object value, object continuation) => new GeneratorYield(value, (JsFunction)continuation);
    static TsObject GeneratorIterator(object source) => TryGetIterableObject(source, out var iterator) ? iterator : (TsObject)IteratorFrom(source);
    static class GeneratorFunctions
    {
        internal static readonly JsFunction Create = new((FunctionBody)(static args => new GeneratorValue((JsFunction)FunctionArgument(args, 0))), null, "", 1);
        internal static readonly JsFunction Yield = new((FunctionBody)(static args => YieldGenerator(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "", 2);
        internal static readonly JsFunction Delegate = new((FunctionBody)(static args => new GeneratorDelegate(GeneratorIterator(FunctionArgument(args, 0)), (JsFunction)FunctionArgument(args, 1))), null, "", 2);
    }
}
