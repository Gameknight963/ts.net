using System;
using System.Collections.Generic;

public static partial class Js
{
    class EventValue : JsDescriptorObject
    {
        internal string Type;
        internal bool Bubbles, Cancelable, Cancelled, Stopped, ImmediateStopped, Dispatching, Passive;
        internal readonly bool Composed;
        internal bool Trusted = false;
        internal EventTargetValue Target, CurrentTarget;
        internal readonly double Timestamp = Now();
        internal EventValue(string type, object settings)
        {
            Type = type;
            var options = EventOptions(settings);
            Bubbles = options != null && Truthy(options.Get("bubbles"));
            Cancelable = options != null && Truthy(options.Get("cancelable"));
            Composed = options != null && Truthy(options.Get("composed"));
        }
        internal void PreventDefault() { if (Cancelable && !Passive) Cancelled = true; }
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)EventConstructors.Event.Get("prototype");
    }
    sealed class CustomEventValue(string type, object settings) : EventValue(type, settings)
    {
        internal readonly object Detail = EventDetail(settings);
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)EventConstructors.Custom.Get("prototype");
    }
    sealed class EventListener(string type, object callback, bool capture, bool once, bool passive)
    {
        internal readonly string Type = type;
        internal object Callback = callback;
        internal readonly bool Capture = capture, Once = once, Passive = passive;
        internal bool Removed;
        internal Action OnRemove;
        internal HashSet<AbortSignalValue> AbortSources;
    }
    class EventTargetValue : JsDescriptorObject
    {
        List<EventListener> listeners;
        internal bool HasListeners => listeners != null && listeners.Count != 0;
        internal virtual void ListenerChanged() { }
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)EventConstructors.Target.Get("prototype");
        internal EventListener Add(string type, object callback, bool capture, bool once, bool passive)
        {
            if (IsNullish(callback)) return null;
            if (callback is not TsObject) throw new InvalidOperationException("Event listener must be a function or object");
            if (listeners != null) foreach (var listener in listeners)
                if (listener.Type == type && ReferenceEquals(listener.Callback, callback) && listener.Capture == capture) return listener;
            var result = new EventListener(type, callback, capture, once, passive);
            (listeners ??= new()).Add(result); ListenerChanged(); return result;
        }
        internal void Remove(EventListener listener)
        {
            if (listener == null || listener.Removed) return;
            listener.Removed = true; listeners?.Remove(listener); listener.Callback = null; listener.AbortSources = null;
            var cleanup = listener.OnRemove; listener.OnRemove = null; cleanup?.Invoke(); ListenerChanged();
        }
        internal void Remove(string type, object callback, bool capture)
        {
            if (listeners == null) return;
            foreach (var listener in listeners)
                if (listener.Type == type && ReferenceEquals(listener.Callback, callback) && listener.Capture == capture) { Remove(listener); return; }
        }
        internal bool Dispatch(EventValue value)
        {
            if (value.Dispatching) throw MakeThrown(new DomExceptionValue("Event is already being dispatched", "InvalidStateError"));
            value.Dispatching = true; value.Target = this; value.CurrentTarget = this;
            try {
                var snapshot = listeners?.ToArray() ?? Array.Empty<EventListener>();
                foreach (var listener in snapshot) {
                    if (value.ImmediateStopped) break;
                    if (listener.Removed || listener.Type != value.Type) continue;
                    object callback = listener.Callback;
                    if (listener.Once) Remove(listener);
                    value.Passive = listener.Passive;
                    try {
                        if (callback is JsFunction function) function.InvokeWithReceiver(this, new object[] { value });
                        else CallMethod(((TsObject)callback).Get("handleEvent"), callback, new object[] { value });
                    } finally { value.Passive = false; }
                    if (value.ImmediateStopped) break;
                }
                return !value.Cancelled;
            } finally {
                value.Dispatching = false; value.CurrentTarget = null;
                value.Stopped = value.ImmediateStopped = value.Passive = false;
            }
        }
    }
    static TsObject EventOptions(object settings) => IsNullish(settings) ? null : settings as TsObject ?? throw new InvalidOperationException("Event options must be an object");
    static object EventDetail(object settings)
    {
        object value = EventOptions(settings)?.Get("detail") ?? Undefined;
        return IsUndefined(value) ? null : value;
    }
    static EventValue RequireEvent(object receiver) => receiver as EventValue ?? throw new InvalidOperationException("Event receiver required");
    static EventTargetValue RequireEventTarget(object receiver) => receiver as EventTargetValue ?? throw new InvalidOperationException("EventTarget receiver required");
    static bool EventCapture(object options) => options is TsObject value ? Truthy(value.Get("capture")) : Truthy(options);
    sealed class EventConstructor : NativeNamespaceFunction
    {
        readonly bool custom;
        internal EventConstructor(bool custom) : base(custom ? "CustomEvent" : "Event", static _ => throw new InvalidOperationException("Event requires new"), 1)
        {
            this.custom = custom;
            var prototype = new JsClassInstance { ParentPrototype = custom ? (TsObject)EventConstructors.Event.Get("prototype") : DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), custom ? "CustomEvent" : "Event", false);
            void Getter(string key, Func<EventValue, object> read) => prototype.DefineClassAccessor(key, new CollectionMethod("get " + key, 0, (receiver, _) => read(RequireEvent(receiver))), 1);
            if (custom) {
                prototype.DefineClassAccessor("detail", new CollectionMethod("get detail", 0, static (receiver, _) => receiver is CustomEventValue value ? value.Detail : throw new InvalidOperationException("CustomEvent receiver required")), 1);
                return;
            }
            Getter("type", value => value.Type); Getter("target", value => value.Target); Getter("srcElement", value => value.Target); Getter("currentTarget", value => value.CurrentTarget);
            Getter("bubbles", value => BoxBoolean(value.Bubbles)); Getter("cancelable", value => BoxBoolean(value.Cancelable)); Getter("composed", value => BoxBoolean(value.Composed));
            Getter("defaultPrevented", value => BoxBoolean(value.Cancelled)); Getter("isTrusted", value => BoxBoolean(value.Trusted)); Getter("timeStamp", value => value.Timestamp);
            Getter("eventPhase", value => value.Dispatching ? 2d : 0d); Getter("cancelBubble", value => BoxBoolean(value.Stopped)); Getter("returnValue", value => BoxBoolean(!value.Cancelled));
            prototype.DefineClassAccessor("cancelBubble", new CollectionMethod("set cancelBubble", 1, static (receiver, args) => { var value = RequireEvent(receiver); if (Truthy(FunctionArgument(args, 0))) value.Stopped = true; return Undefined; }), 2);
            prototype.DefineClassAccessor("returnValue", new CollectionMethod("set returnValue", 1, static (receiver, args) => { var value = RequireEvent(receiver); if (!Truthy(FunctionArgument(args, 0))) value.PreventDefault(); return Undefined; }), 2);
            prototype.DefineMethod("initEvent", new CollectionMethod("initEvent", 1, static (receiver, args) =>
            {
                var value = RequireEvent(receiver);
                if (args.Length == 0) throw new InvalidOperationException("Event type is required");
                string type = StringValue(args[0]);
                bool bubbles = Truthy(FunctionArgument(args, 1)), cancelable = Truthy(FunctionArgument(args, 2));
                if (value.Dispatching) return Undefined;
                value.Stopped = value.ImmediateStopped = value.Cancelled = false;
                value.Trusted = false; value.Target = null;
                value.Type = type; value.Bubbles = bubbles; value.Cancelable = cancelable;
                return Undefined;
            }));
            prototype.DefineMethod("preventDefault", new CollectionMethod("preventDefault", 0, static (receiver, _) => { RequireEvent(receiver).PreventDefault(); return Undefined; }));
            prototype.DefineMethod("stopPropagation", new CollectionMethod("stopPropagation", 0, static (receiver, _) => { RequireEvent(receiver).Stopped = true; return Undefined; }));
            prototype.DefineMethod("stopImmediatePropagation", new CollectionMethod("stopImmediatePropagation", 0, static (receiver, _) => { var value = RequireEvent(receiver); value.Stopped = value.ImmediateStopped = true; return Undefined; }));
            prototype.DefineMethod("composedPath", new CollectionMethod("composedPath", 0, static (receiver, _) => { var value = RequireEvent(receiver); return new JsArray(value.Dispatching ? new object[] { value.Target } : Array.Empty<object>()); }));
            foreach (var constant in new[] { ("NONE", 0d), ("CAPTURING_PHASE", 1d), ("AT_TARGET", 2d), ("BUBBLING_PHASE", 3d) }) { Member(constant.Item1, constant.Item2); prototype.DefineMethod(constant.Item1, constant.Item2); }
        }
        public override object Construct(object[] args)
        {
            if (args.Length == 0) throw new InvalidOperationException("Event type is required");
            string type = StringValue(args[0]); object options = FunctionArgument(args, 1);
            return custom ? new CustomEventValue(type, options) : new EventValue(type, options);
        }
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget) { var result = (TsObject)Construct(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result; }
    }
    sealed class EventTargetConstructor : NativeNamespaceFunction
    {
        internal EventTargetConstructor() : base("EventTarget", static _ => throw new InvalidOperationException("EventTarget requires new"), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "EventTarget", false);
            prototype.DefineMethod("addEventListener", new CollectionMethod("addEventListener", 2, static (receiver, args) => {
                var target = RequireEventTarget(receiver);
                if (args.Length < 2) throw new InvalidOperationException("Event type and listener are required");
                string type = StringValue(args[0]); object options = FunctionArgument(args, 2);
                bool capture = EventCapture(options), once = options is TsObject settings && Truthy(settings.Get("once")), passive = options is TsObject values && Truthy(values.Get("passive"));
                object requested = options is TsObject bag ? bag.Get("signal") : Undefined;
                var signal = IsUndefined(requested) ? null : RequireAbortSignal(requested);
                if (signal != null && signal.Aborted) return Undefined;
                var listener = target.Add(type, args[1], capture, once, passive);
                if (listener != null && signal != null && (listener.AbortSources ??= new(ReferenceEqualityComparer.Instance)).Add(signal))
                    listener.OnRemove += signal.Subscribe(() => target.Remove(listener));
                return Undefined;
            }));
            prototype.DefineMethod("removeEventListener", new CollectionMethod("removeEventListener", 2, static (receiver, args) => {
                var target = RequireEventTarget(receiver);
                if (args.Length < 2) throw new InvalidOperationException("Event type and listener are required");
                string type = StringValue(args[0]); target.Remove(type, args[1], EventCapture(FunctionArgument(args, 2))); return Undefined;
            }));
            prototype.DefineMethod("dispatchEvent", new CollectionMethod("dispatchEvent", 1, static (receiver, args) => BoxBoolean(RequireEventTarget(receiver).Dispatch(RequireEvent(FunctionArgument(args, 0))))));
        }
        public override object Construct(object[] args) => new EventTargetValue();
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget) { var result = (TsObject)Construct(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result; }
    }
    static class EventConstructors
    {
        internal static readonly JsFunction Event = new EventConstructor(false);
        internal static readonly JsFunction Custom = new EventConstructor(true);
        internal static readonly JsFunction Target = new EventTargetConstructor();
    }
}
