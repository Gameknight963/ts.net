using System;
using System.Collections.Generic;

public static partial class Js
{
    // A nonnegative index identifies a settled combinator job; ordinary jobs use -1.
    readonly struct PromiseJob(object target, object value, bool rejected, int index = -1)
    {
        internal void Run()
        {
            if (target is JsPromise native) native.RunReaction(value, rejected);
            else if (target is PromiseReactionBase reaction) reaction.Run(value, rejected);
            else if (index >= 0) ((PromiseCombinationState)target).RunJob(index, value, rejected);
            else if (target is Action callback) callback();
            else ((JsFunction)target).InvokeWithReceiver(Undefined, Array.Empty<object>());
        }
    }
    [ThreadStatic] static Queue<PromiseJob> promiseJobs;
    [ThreadStatic] static HashSet<JsPromise> unhandledPromises;
    static void EnqueuePromiseJob(Action job) => (promiseJobs ??= new()).Enqueue(new(job, null, false));
    static void EnqueueMicrotask(JsFunction callback) =>
        (promiseJobs ??= new()).Enqueue(new(callback, null, false));
    static void EnqueuePromiseReaction(IPromiseReaction reaction, object value, bool rejected) =>
        (promiseJobs ??= new()).Enqueue(new(reaction, value, rejected));
    static void EnqueuePromiseCombination(PromiseCombinationState combination, int index, object value, bool rejected) =>
        (promiseJobs ??= new()).Enqueue(new(combination, value, rejected, index));
    static void DrainPromiseJobs()
    {
        ClearKeptWeakTargets();
        while (promiseJobs != null && promiseJobs.TryDequeue(out var job))
            try { job.Run(); } finally { ClearKeptWeakTargets(); }
        CheckFinalizationRegistries();
        if (unhandledPromises != null)
            foreach (var promise in unhandledPromises)
                if (!promise.Handled) throw MakeThrown(promise.Result);
    }
    sealed class PromiseCapability(JsPromise native = null) : PromiseResolvingFunctions(native)
    {
        internal TsObject Promise;
        internal new JsFunction Resolve { get => base.Resolve; set => resolve = value; }
        internal new JsFunction Reject { get => base.Reject; set => reject = value; }
        internal void Complete(object value, bool rejected)
        {
            if (promise != null)
            {
                if (rejected) RejectValue(value);
                else ResolveValue(value);
                return;
            }
            (rejected ? Reject : Resolve).InvokeWithReceiver(Undefined, new[] { value });
        }
    }
    class PromiseResolvingFunctions
    {
        bool resolved;
        protected readonly JsPromise promise;
        protected JsFunction resolve, reject;
        // Internal capabilities usually never expose these functions to JavaScript.
        // Materialize each function only when needed, preserving its identity.
        internal JsFunction Resolve => resolve ??= new JsFunction((FunctionBody)(args =>
        {
            ResolveValue(FunctionArgument(args, 0)); return Undefined;
        }), null, "", 1);
        internal JsFunction Reject => reject ??= new JsFunction((FunctionBody)(args =>
        {
            RejectValue(FunctionArgument(args, 0)); return Undefined;
        }), null, "", 1);
        internal PromiseResolvingFunctions(JsPromise promise)
        {
            this.promise = promise;
        }
        internal void ResolveValue(object value)
        {
            if (resolved) return;
            resolved = true; promise.ResolveValue(value);
        }
        internal void RejectValue(object value)
        {
            if (resolved) return;
            resolved = true; promise.Settle(value, true);
        }
        internal void RejectError(Exception error) => RejectValue(CaughtValue(error));
    }
    interface IPromiseReaction { void Run(object value, bool rejected); }
    abstract class PromiseReactionBase : IPromiseReaction
    {
        internal void Enqueue(object value, bool rejected) => EnqueuePromiseReaction(this, value, rejected);
        internal abstract void Run(object value, bool rejected);
        void IPromiseReaction.Run(object value, bool rejected) => Run(value, rejected);
    }
    sealed class PromiseReaction(PromiseCapability capability, JsFunction fulfilled, JsFunction rejected)
        : PromiseReactionBase
    {
        internal override void Run(object value, bool isRejected)
        {
            var handler = isRejected ? rejected : fulfilled;
            if (handler == null) { capability.Complete(value, isRejected); return; }
            object result;
            try { result = handler.InvokeContinuation(value); }
            catch (Exception error) { capability.Complete(CaughtValue(error), true); return; }
            capability.Complete(result, false);
        }
    }
    sealed class PromiseContinuationReaction(Action<object, bool> continuation) : PromiseReactionBase
    {
        internal override void Run(object value, bool rejected) => continuation(value, rejected);
    }
    sealed class NativePromiseReaction(JsPromise target, JsFunction fulfilled, JsFunction rejected) : PromiseReactionBase
    {
        internal override void Run(object value, bool isRejected)
        {
            var handler = isRejected ? rejected : fulfilled;
            if (handler == null) { Complete(value, isRejected); return; }
            object result;
            try { result = handler.InvokeContinuation(value); }
            catch (Exception error) { Complete(CaughtValue(error), true); return; }
            Complete(result, false);
        }
        void Complete(object value, bool rejected)
        {
            // Each reaction runs once, and these results expose no resolver functions.
            if (rejected) target.Settle(value, true);
            else target.ResolveValue(value);
        }
    }
    sealed class JsPromise : JsDescriptorObject, IPromiseReaction
    {
        int state; // -1 waiting for an incoming reaction, 0 pending, 1 fulfilled, 2 rejected
        // Most pending promises have one reaction; allocate a list only for fan-out.
        object reactions; // null, IPromiseReaction, or List<IPromiseReaction>
        internal object Result;
        internal bool Handled;
        // Until its incoming job runs, a native result holds the fulfillment
        // handler in Result. Settlement replaces it with the actual value.
        internal static JsPromise ForFulfilledReaction(JsFunction handler) => new() { state = -1, Result = handler };
        void IPromiseReaction.Run(object value, bool rejected) => RunReaction(value, rejected);
        internal void RunReaction(object value, bool rejected)
        {
            if (state != -1) return;
            var handler = (JsFunction)Result;
            state = 0; Result = null;
            if (rejected) { Settle(value, true); return; }
            if (handler != null) {
                try { value = handler.InvokeContinuation(value); }
                catch (Exception error) { Settle(CaughtValue(error), true); return; }
            }
            ResolveValue(value);
        }
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)RuntimeNamespaces.Promise.Get("prototype");
        internal void ResolveValue(object value)
        {
            if (ReferenceEquals(this, value)) { Settle(new JsCaughtError("TypeError", "A promise cannot resolve to itself"), true); return; }
            if (value is not TsObject obj) { Settle(value, false); return; }
            object then;
            try { then = obj.Get("then"); }
            catch (Exception error) { Settle(CaughtValue(error), true); return; }
            if (then is not JsFunction function) { Settle(value, false); return; }
            EnqueueThenable(value, function);
        }
        // Keep the captured job state off the primitive-resolution path.
        void EnqueueThenable(object value, JsFunction function)
        {
            EnqueuePromiseJob(() =>
            {
                var pair = new PromiseResolvingFunctions(this);
                try { function.InvokeWithReceiver(value, new object[] { pair.Resolve, pair.Reject }); }
                catch (Exception error) { pair.RejectError(error); }
            });
        }
        internal void Settle(object value, bool rejected)
        {
            if (state > 0) return;
            state = rejected ? 2 : 1; Result = value;
            if (rejected && !Handled) (unhandledPromises ??= new()).Add(this);
            if (reactions is IPromiseReaction single) EnqueuePromiseReaction(single, value, rejected);
            else if (reactions is List<IPromiseReaction> multiple)
                foreach (var reaction in multiple) EnqueuePromiseReaction(reaction, value, rejected);
            reactions = null;
        }
        internal void Then(IPromiseReaction reaction)
        {
            Handled = true; unhandledPromises?.Remove(this);
            if (state <= 0)
            {
                if (reactions == null) reactions = reaction;
                else if (reactions is List<IPromiseReaction> multiple) multiple.Add(reaction);
                else reactions = new List<IPromiseReaction> { (IPromiseReaction)reactions, reaction };
            }
            else EnqueuePromiseReaction(reaction, Result, state == 2);
        }
        internal void ThenCombination(PromiseCapability capability, PromiseCombinationState combination, int index)
        {
            // Custom species must still complete their discarded result promise.
            if (capability == null && state > 0) {
                Handled = true; unhandledPromises?.Remove(this);
                EnqueuePromiseCombination(combination, index, Result, state == 2);
            }
            else Then(new PromiseCombinationReaction(capability, combination, index));
        }
    }
    static PromiseCapability NewPromiseCapability(object constructor)
    {
        if (ReferenceEquals(constructor, RuntimeNamespaces.Promise))
        {
            var intrinsic = new JsPromise();
            return new PromiseCapability(intrinsic) { Promise = intrinsic };
        }
        return NewCustomPromiseCapability(constructor);
    }
    // Isolate the executor's closure so intrinsic promises do not allocate it.
    static PromiseCapability NewCustomPromiseCapability(object constructor)
    {
        if (constructor is not JsFunction function || !function.CanInitializeDerived) throw new InvalidOperationException("Promise receiver is not a constructor");
        object resolve = Undefined, reject = Undefined;
        var executor = new JsFunction((FunctionBody)(args =>
        {
            if (!IsUndefined(resolve) || !IsUndefined(reject)) throw new InvalidOperationException("Promise capability executor was called twice"); resolve = FunctionArgument(args, 0); reject = FunctionArgument(args, 1); return Undefined;
        }), null, "", 2);
        var promise = (TsObject)function.Construct(new object[] { executor });
        if (resolve is not JsFunction resolver || reject is not JsFunction rejecter) throw new InvalidOperationException("Promise resolving functions are not callable");
        return new PromiseCapability { Promise = promise, Resolve = resolver, Reject = rejecter };
    }
    static object PromiseResolve(object constructor, object value)
    {
        if (constructor is not TsObject) throw new InvalidOperationException("Promise receiver must be an object");
        if (value is JsPromise promise && ReferenceEquals(promise.Get("constructor"), constructor)) return value;
        if (ReferenceEquals(constructor, RuntimeNamespaces.Promise))
        {
            var intrinsic = new JsPromise(); intrinsic.ResolveValue(value); return intrinsic;
        }
        var capability = NewPromiseCapability(constructor); capability.Complete(value, false); return capability.Promise;
    }
    static JsFunction PromiseSpecies(TsObject promise)
    {
        object constructor = promise.Get("constructor");
        if (IsUndefined(constructor)) return RuntimeNamespaces.Promise;
        if (constructor is not TsObject obj) throw new InvalidOperationException("Promise constructor must be an object");
        object species = obj.Get(PropertyKey(SpeciesSymbol));
        if (IsNullish(species)) return RuntimeNamespaces.Promise;
        if (species is not JsFunction function || !function.CanInitializeDerived) throw new InvalidOperationException("Promise species must be a constructor");
        return function;
    }
    sealed class PromiseThenFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("then requires a promise")), null, "then", 2)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            if (receiver is not JsPromise promise) throw new InvalidOperationException("then requires a promise");
            return Then(promise, FunctionArgument(arguments, 0), FunctionArgument(arguments, 1));
        }
        internal object InvokeSingle(object receiver, object handler)
        {
            if (receiver is not JsPromise promise) throw new InvalidOperationException("then requires a promise");
            return Then(promise, handler, Undefined);
        }
        static object Then(JsPromise promise, object fulfilled, object rejected)
        {
            var constructor = PromiseSpecies(promise);
            if (ReferenceEquals(constructor, RuntimeNamespaces.Promise))
            {
                if (rejected is not JsFunction) {
                    var result = JsPromise.ForFulfilledReaction(fulfilled as JsFunction);
                    promise.Then(result); return result;
                }
                var target = new JsPromise();
                promise.Then(new NativePromiseReaction(target, fulfilled as JsFunction, rejected as JsFunction));
                return target;
            }
            var capability = NewPromiseCapability(constructor);
            promise.Then(new PromiseReaction(capability, fulfilled as JsFunction, rejected as JsFunction));
            return capability.Promise;
        }
    }
    public static object CallPromiseThen(object target, object receiver, object handler) => target is PromiseThenFunction method
        ? method.InvokeSingle(SuperReceiver(receiver), handler) : CallMethod(target, receiver, new[] { handler });
    public static bool CanReusePromiseHandler(object target, object receiver) =>
        target is PromiseThenFunction && receiver is JsPromise;
    sealed class PromiseCatchFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("catch requires a receiver")), null, "catch", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => CallMethod(Js.Get(receiver, "then"), receiver, new[] { Undefined, FunctionArgument(arguments, 0) });
    }
    sealed class PromiseFinallyFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("finally requires a receiver")), null, "finally", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            if (receiver is not TsObject obj) throw new InvalidOperationException("finally requires an object");
            var constructor = PromiseSpecies(obj); object callback = FunctionArgument(arguments, 0);
            object fulfilled = callback, rejected = callback;
            if (callback is JsFunction function)
            {
                JsFunction Handler(bool throwing) => new((FunctionBody)(args =>
                {
                    object original = FunctionArgument(args, 0);
                    object value = function.InvokeWithReceiver(Undefined, Array.Empty<object>());
                    object resolved = PromiseResolve(constructor, value);
                    var continuation = new JsFunction((FunctionBody)(_ => throwing ? throw MakeThrown(original) : original), null, "", 0);
                    return CallMethod(Js.Get(resolved, "then"), resolved, new object[] { continuation });
                }), null, "", 1);
                fulfilled = Handler(false); rejected = Handler(true);
            }
            return CallMethod(Js.Get(receiver, "then"), receiver, new[] { fulfilled, rejected });
        }
    }
    sealed class PromiseStaticFunction(bool reject) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Promise method requires a constructor")), null, reject ? "reject" : "resolve", 1)
    {
        internal bool IsResolve => !reject;
        public override object InvokeWithReceiver(object receiver, object[] arguments) => InvokeValue(receiver, FunctionArgument(arguments, 0));
        internal object InvokeValue(object receiver, object value)
        {
            if (!reject) return PromiseResolve(receiver, value);
            if (ReferenceEquals(receiver, RuntimeNamespaces.Promise))
            {
                var intrinsic = new JsPromise(); intrinsic.Settle(value, true); return intrinsic;
            }
            var capability = NewPromiseCapability(receiver); capability.Complete(value, true); return capability.Promise;
        }
    }
    public static object CallPromiseStaticNumber(object target, object receiver, double? value) => target is PromiseStaticFunction method
        ? method.InvokeValue(receiver, BoxNumber(value)) : CallMethodNumberArgument(target, receiver, value);
    static JsPromise CreatePromise(object[] arguments, TsObject prototype = null)
    {
        if (FunctionArgument(arguments, 0) is not JsFunction executor) throw new InvalidOperationException("Promise executor is not callable");
        var promise = new JsPromise(); if (prototype != null) promise.SetPrototype(prototype);
        var pair = new PromiseResolvingFunctions(promise);
        try { executor.InvokeWithReceiver(Undefined, new object[] { pair.Resolve, pair.Reject }); }
        catch (Exception error) { pair.RejectError(error); }
        return promise;
    }
    static object PromiseSpeciesReceiver(object receiver, object[] arguments) => receiver;
    sealed class PromiseConstructorFunction : NativeNamespaceFunction
    {
        internal PromiseConstructorFunction() : base("Promise", static _ => throw new InvalidOperationException("Promise requires new"), 1)
        {
            string speciesKey = PropertyKey(SpeciesSymbol);
            Member(speciesKey, Undefined);
            var speciesGetter = new CollectionMethod("get [Symbol.species]", 0, PromiseSpeciesReceiver);
            DefineAccessor(speciesKey, speciesGetter, 1);
            speciesGetter.InferPropertyName("get [Symbol.species]");
            var prototype = new JsCreatedObject(); UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod("then", new PromiseThenFunction());
            prototype.DefineMethod("catch", new PromiseCatchFunction()); prototype.DefineMethod("finally", new PromiseFinallyFunction());
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Promise", false);
            Member("resolve", new PromiseStaticFunction(false)); Member("reject", new PromiseStaticFunction(true));
            Member("all", new PromiseCombinatorFunction("all", PromiseCombination.All));
            Member("allSettled", new PromiseCombinatorFunction("allSettled", PromiseCombination.AllSettled));
            Member("race", new PromiseCombinatorFunction("race", PromiseCombination.Race));
            Member("any", new PromiseCombinatorFunction("any", PromiseCombination.Any));
            Member("withResolvers", new PromiseWithResolversFunction()); Member("try", new PromiseTryFunction());
        }
        public override object Construct(object[] arguments) => CreatePromise(arguments);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments) => CreatePromise(arguments, ((TsObject)instance).PropertyPrototype);
        internal override object InitializeDerived(object instance, object[] arguments, JsFunction newTarget)
        {
            if (FunctionArgument(arguments, 0) is not JsFunction) throw new InvalidOperationException("Promise executor is not callable");
            return CreatePromise(arguments, ConstructionPrototype(newTarget));
        }
    }
    static JsFunction CreateQueueMicrotask() => new((FunctionBody)(args =>
    {
        if (FunctionArgument(args, 0) is not JsFunction callback) throw new InvalidOperationException("Microtask callback is not callable");
        EnqueueMicrotask(callback); return Undefined;
    }), null, "queueMicrotask", 1);
}
