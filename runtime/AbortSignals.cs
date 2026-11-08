using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

public static partial class Js
{
    sealed class AbortSignalValue : EventTargetValue
    {
        internal bool Aborted;
        internal object Reason = Undefined;
        internal JsFunction Handler;
        internal List<AbortSignalValue> Sources;
        EventListener handlerListener;
        List<Action> algorithms;
        List<Action> retentions;
        List<WeakReference<AbortSignalValue>> dependents;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)AbortConstructors.Signal.Get("prototype");
        internal Action Subscribe(Action callback)
        {
            if (Aborted) { callback(); return static () => { }; }
            (algorithms ??= new()).Add(callback); RefreshRetention();
            return () => { algorithms?.Remove(callback); RefreshRetention(); };
        }
        internal override void ListenerChanged() => RefreshRetention();
        void ReleaseRetentions()
        {
            var callbacks = retentions; retentions = null;
            if (callbacks != null) foreach (var callback in callbacks) callback();
        }
        void RefreshRetention()
        {
            if (Aborted || !HasListeners && (algorithms == null || algorithms.Count == 0)) { ReleaseRetentions(); return; }
            if (Sources == null || retentions != null) return;
            // A live source keeps observed composites alive. Unobserved composites stay weak.
            var pending = new List<AbortSignalValue>(Sources);
            var seen = new HashSet<AbortSignalValue>(ReferenceEqualityComparer.Instance);
            retentions = new();
            Action keepAlive = () => GC.KeepAlive(this);
            for (int index = 0; index < pending.Count; index++) {
                var source = pending[index];
                if (!seen.Add(source) || source.Aborted) continue;
                if (source.Sources == null) retentions.Add(source.Subscribe(keepAlive));
                else pending.AddRange(source.Sources);
            }
        }
        internal void AddDependent(AbortSignalValue value)
        {
            dependents ??= new();
            dependents.RemoveAll(static reference => !reference.TryGetTarget(out var target) || target.Aborted);
            dependents.Add(new(value));
        }
        internal void SetHandler(object value)
        {
            Handler = value as JsFunction;
            if (Handler == null) { Remove(handlerListener); handlerListener = null; return; }
            if (handlerListener != null) return;
            var callback = new JsFunction((FunctionBody)(args => { Handler?.InvokeWithReceiver(this, args); return Undefined; }), null, "", 1);
            handlerListener = Add("abort", callback, false, false, false);
        }
        internal void Abort(object reason)
        {
            if (Aborted) return;
            Reason = IsUndefined(reason) ? new DomExceptionValue("This operation was aborted", "AbortError") : reason;
            Aborted = true;
            var pending = new List<AbortSignalValue> { this };
            // Mark linked signals before delivering callbacks, without recursive dispatch.
            for (int index = 0; index < pending.Count; index++) {
                var source = pending[index];
                source.Sources = null;
                if (source.dependents == null) continue;
                foreach (var reference in source.dependents) if (reference.TryGetTarget(out var target) && !target.Aborted) {
                    target.Aborted = true; target.Reason = Reason; pending.Add(target);
                }
                source.dependents = null;
            }
            Exception failure = null;
            foreach (var signal in pending) {
                signal.ReleaseRetentions();
                var callbacks = signal.algorithms?.ToArray() ?? Array.Empty<Action>(); signal.algorithms = null;
                foreach (var callback in callbacks) try { callback(); } catch (Exception error) { failure ??= error; }
                try {
                    if (signal.HasListeners) signal.Dispatch(new EventValue("abort", Undefined) { Trusted = true });
                } catch (Exception error) { failure ??= error; }
            }
            if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
    sealed class AbortControllerValue : JsDescriptorObject
    {
        internal readonly AbortSignalValue Signal = new();
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)AbortConstructors.Controller.Get("prototype");
    }
    static AbortSignalValue RequireAbortSignal(object value) => value as AbortSignalValue ?? throw new InvalidOperationException("AbortSignal receiver required");
    static AbortControllerValue RequireAbortController(object value) => value as AbortControllerValue ?? throw new InvalidOperationException("AbortController receiver required");
    static object AbortAny(object value)
    {
        var sources = new List<AbortSignalValue>();
        object iterator = Iteration.Start(value); int position = 0;
        try {
            while (position < Iteration.Length(iterator)) {
                sources.Add(RequireAbortSignal(Iteration.Value(iterator, position)));
                position += Iteration.Step(iterator, position);
            }
        } catch { CloseIterationAt(iterator, position, true); throw; }
        var result = new AbortSignalValue();
        foreach (var source in sources) if (source.Aborted) { result.Abort(source.Reason); return result; }
        result.Sources = sources;
        var seen = new HashSet<AbortSignalValue>(ReferenceEqualityComparer.Instance);
        foreach (var source in sources) if (seen.Add(source)) source.AddDependent(result);
        return result;
    }
    static object AbortTimeout(object value)
    {
        if (value is not double delay) throw new InvalidOperationException("Abort timeout must be a number");
        if (!double.IsFinite(delay) || delay < 0 || delay > uint.MaxValue || delay != Math.Truncate(delay)) throw new ArgumentOutOfRangeException(nameof(value), "Invalid abort timeout");
        var result = new AbortSignalValue();
        var callback = new JsFunction((FunctionBody)(_ => { result.Abort(new DomExceptionValue("The operation was aborted due to timeout", "TimeoutError")); return Undefined; }), null, "", 0);
        ScheduleTimer(new object[] { callback, delay }, false, false);
        return result;
    }
    sealed class AbortControllerConstructor : NativeNamespaceFunction
    {
        internal AbortControllerConstructor() : base("AbortController", static _ => throw new InvalidOperationException("AbortController requires new"), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "AbortController", false);
            prototype.DefineClassAccessor("signal", new CollectionMethod("get signal", 0, static (receiver, _) => RequireAbortController(receiver).Signal), 1);
            prototype.DefineMethod("abort", new CollectionMethod("abort", 0, static (receiver, args) => { RequireAbortController(receiver).Signal.Abort(FunctionArgument(args, 0)); return Undefined; }));
        }
        public override object Construct(object[] args) => new AbortControllerValue();
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget) { var result = (TsObject)Construct(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result; }
    }
    sealed class AbortSignalConstructor : NativeNamespaceFunction
    {
        internal AbortSignalConstructor() : base("AbortSignal", static _ => throw new InvalidOperationException("Illegal AbortSignal constructor"), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = (TsObject)EventConstructors.Target.Get("prototype") }; UseObjectPrototype(prototype);
            SetPrototype(EventConstructors.Target);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "AbortSignal", false);
            prototype.DefineClassAccessor("aborted", new CollectionMethod("get aborted", 0, static (receiver, _) => BoxBoolean(RequireAbortSignal(receiver).Aborted)), 1);
            prototype.DefineClassAccessor("reason", new CollectionMethod("get reason", 0, static (receiver, _) => RequireAbortSignal(receiver).Reason), 1);
            prototype.DefineClassAccessor("onabort", new CollectionMethod("get onabort", 0, static (receiver, _) => RequireAbortSignal(receiver).Handler), 1);
            prototype.DefineClassAccessor("onabort", new CollectionMethod("set onabort", 1, static (receiver, args) => { RequireAbortSignal(receiver).SetHandler(FunctionArgument(args, 0)); return Undefined; }), 2);
            prototype.DefineMethod("throwIfAborted", new CollectionMethod("throwIfAborted", 0, static (receiver, _) => { var signal = RequireAbortSignal(receiver); if (signal.Aborted) throw MakeThrown(signal.Reason); return Undefined; }));
            Member("abort", new JsFunction((FunctionBody)(static args => { var value = new AbortSignalValue(); value.Abort(FunctionArgument(args, 0)); return value; }), null, "abort", 0));
            Member("any", new JsFunction((FunctionBody)(static args => AbortAny(FunctionArgument(args, 0))), null, "any", 1));
            Member("timeout", new JsFunction((FunctionBody)(static args => AbortTimeout(FunctionArgument(args, 0))), null, "timeout", 1));
        }
        public override object Construct(object[] args) => throw new InvalidOperationException("Illegal AbortSignal constructor");
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) => throw new InvalidOperationException("Illegal AbortSignal constructor");
    }
    static class AbortConstructors
    {
        internal static readonly JsFunction Controller = new AbortControllerConstructor();
        internal static readonly JsFunction Signal = new AbortSignalConstructor();
    }
}
