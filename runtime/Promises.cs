using System;
using System.Collections.Generic;

public static partial class Js
{
    [ThreadStatic] static Queue<Action> promiseJobs;
    [ThreadStatic] static HashSet<JsPromise> unhandledPromises;
    static void EnqueuePromiseJob(Action job) => (promiseJobs ??= new()).Enqueue(job);
    static void DrainPromiseJobs()
    {
        ClearKeptWeakTargets();
        while (promiseJobs != null && promiseJobs.TryDequeue(out var job))
            try { job(); } finally { ClearKeptWeakTargets(); }
        if (unhandledPromises != null)
            foreach (var promise in unhandledPromises)
                if (!promise.Handled) throw MakeThrown(promise.Result);
    }
    sealed class PromiseCapability
    {
        internal TsObject Promise;
        internal JsFunction Resolve, Reject;
        internal void Complete(object value, bool rejected) => (rejected ? Reject : Resolve).InvokeWithReceiver(Undefined, new[] { value });
    }
    sealed class PromiseResolvingFunctions
    {
        bool resolved;
        internal readonly JsFunction Resolve, Reject;
        internal PromiseResolvingFunctions(JsPromise promise)
        {
            Resolve = new JsFunction((FunctionBody)(args => { if (!resolved) { resolved = true; promise.ResolveValue(FunctionArgument(args, 0)); } return Undefined; }), null, "", 1);
            Reject = new JsFunction((FunctionBody)(args => { if (!resolved) { resolved = true; promise.Settle(FunctionArgument(args, 0), true); } return Undefined; }), null, "", 1);
        }
        internal void RejectError(Exception error) => Reject.InvokeWithReceiver(Undefined, new[] { CaughtValue(error) });
    }
    sealed class PromiseReaction(PromiseCapability capability, JsFunction fulfilled, JsFunction rejected, Action<object, bool> continuation = null)
    {
        internal void Enqueue(object value, bool isRejected) => EnqueuePromiseJob(() =>
        {
            if (continuation != null) { continuation(value, isRejected); return; }
            var handler = isRejected ? rejected : fulfilled;
            if (handler == null) { capability.Complete(value, isRejected); return; }
            object result;
            try { result = handler.InvokeWithReceiver(Undefined, new[] { value }); }
            catch (Exception error) { capability.Complete(CaughtValue(error), true); return; }
            capability.Complete(result, false);
        });
    }
    sealed class JsPromise : JsDescriptorObject
    {
        int state; // 0 pending, 1 fulfilled, 2 rejected
        List<PromiseReaction> reactions;
        internal object Result;
        internal bool Handled;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)RuntimeNamespaces.Promise.Get("prototype");
        internal void ResolveValue(object value)
        {
            if (ReferenceEquals(this, value)) { Settle(new JsCaughtError("TypeError", "A promise cannot resolve to itself"), true); return; }
            if (value is not TsObject obj) { Settle(value, false); return; }
            object then;
            try { then = obj.Get("then"); }
            catch (Exception error) { Settle(CaughtValue(error), true); return; }
            if (then is not JsFunction function) { Settle(value, false); return; }
            EnqueuePromiseJob(() =>
            {
                var pair = new PromiseResolvingFunctions(this);
                try { function.InvokeWithReceiver(value, new object[] { pair.Resolve, pair.Reject }); }
                catch (Exception error) { pair.RejectError(error); }
            });
        }
        internal void Settle(object value, bool rejected)
        {
            if (state != 0) return;
            state = rejected ? 2 : 1; Result = value;
            if (rejected && !Handled) (unhandledPromises ??= new()).Add(this);
            if (reactions != null) { foreach (var reaction in reactions) reaction.Enqueue(value, rejected); reactions = null; }
        }
        internal void Then(PromiseReaction reaction)
        {
            Handled = true; unhandledPromises?.Remove(this);
            if (state == 0) (reactions ??= new()).Add(reaction);
            else reaction.Enqueue(Result, state == 2);
        }
    }
    static PromiseCapability NewPromiseCapability(object constructor)
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
            var capability = NewPromiseCapability(PromiseSpecies(promise));
            promise.Then(new PromiseReaction(capability, FunctionArgument(arguments, 0) as JsFunction, FunctionArgument(arguments, 1) as JsFunction));
            return capability.Promise;
        }
    }
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
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            object value = FunctionArgument(arguments, 0);
            if (!reject) return PromiseResolve(receiver, value);
            var capability = NewPromiseCapability(receiver); capability.Complete(value, true); return capability.Promise;
        }
    }
    static JsPromise CreatePromise(object[] arguments, TsObject prototype = null)
    {
        if (FunctionArgument(arguments, 0) is not JsFunction executor) throw new InvalidOperationException("Promise executor is not callable");
        var promise = new JsPromise(); if (prototype != null) promise.SetPrototype(prototype);
        var pair = new PromiseResolvingFunctions(promise);
        try { executor.InvokeWithReceiver(Undefined, new object[] { pair.Resolve, pair.Reject }); }
        catch (Exception error) { pair.RejectError(error); }
        return promise;
    }
    sealed class PromiseConstructorFunction : NativeNamespaceFunction
    {
        internal PromiseConstructorFunction() : base("Promise", static _ => throw new InvalidOperationException("Promise requires new"), 1)
        {
            string speciesKey = PropertyKey(SpeciesSymbol);
            Member(speciesKey, Undefined);
            var speciesGetter = new CollectionMethod("get [Symbol.species]", 0, static (receiver, _) => receiver);
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
        EnqueuePromiseJob(() => callback.InvokeWithReceiver(Undefined, Array.Empty<object>())); return Undefined;
    }), null, "queueMicrotask", 1);
}
