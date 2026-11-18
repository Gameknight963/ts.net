using System;
using System.Collections.Generic;

public static partial class Js
{
    interface IProxyValue { ProxyState Proxy { get; } }
    internal static bool IsProxy(object value) => value is IProxyValue;
    sealed partial class ProxyState
    {
        TsObject target, handler;
        internal readonly bool Constructable;
        // Keep the layout proof after revocation without retaining the target.
        internal readonly Type TargetType;
        internal ProxyState(TsObject target, TsObject handler)
        {
            this.target = target;
            this.handler = handler;
            TargetType = target is IProxyValue nested ? nested.Proxy.TargetType : target.GetType();
            Constructable = target is JsFunction function && function.CanInitializeDerived;
        }
        internal void Revoke() { target = null; handler = null; }
        internal TsObject Target => target ?? throw new InvalidOperationException("Proxy has been revoked");
        readonly record struct ProxyTrap(JsFunction Method, TsObject Receiver)
        {
            internal object Invoke(params object[] args) => Method.InvokeWithReceiver(Receiver, args);
        }
        ProxyTrap Trap(string name)
        {
            _ = Target;
            // A trap getter may revoke this proxy. The current operation keeps
            // using the target and handler captured before that getter ran.
            var receiver = handler; object method = receiver.Get(name);
            return new(IsNullish(method) ? null : method as JsFunction ?? throw new InvalidOperationException("Proxy trap is not callable"), receiver);
        }
        static object Key(string key) => symbolKeys.TryGetValue(key, out var symbol) ? symbol : key;
        static bool Fixed(TsObject descriptor) => descriptor != null && !Truthy(descriptor.Get("configurable"));
        internal object Get(string key, object receiver)
        {
            var original = Target; var trap = Trap("get");
            if (trap.Method == null) return original.GetWithReceiver(key, receiver);
            object result = trap.Invoke(original, Key(key), receiver);
            var descriptor = original.OwnPropertyDescriptor(key) as TsObject;
            if (Fixed(descriptor))
            {
                if (descriptor.HasOwn("writable") && !Truthy(descriptor.Get("writable")) && !SameValue(result, descriptor.Get("value")) ||
                    !descriptor.HasOwn("writable") && IsUndefined(descriptor.Get("get")) && !IsUndefined(result))
                    throw new InvalidOperationException("Proxy get violates a fixed property");
            }
            return result;
        }
        internal bool Set(string key, object value, object receiver)
        {
            var original = Target; var trap = Trap("set");
            if (trap.Method == null) return ReflectSet(original, key, value, receiver);
            if (!Truthy(trap.Invoke(original, Key(key), value, receiver))) return false;
            var descriptor = original.OwnPropertyDescriptor(key) as TsObject;
            if (Fixed(descriptor))
            {
                if (descriptor.HasOwn("writable") && !Truthy(descriptor.Get("writable")) && !SameValue(value, descriptor.Get("value")) ||
                    !descriptor.HasOwn("writable") && IsUndefined(descriptor.Get("set")))
                    throw new InvalidOperationException("Proxy set violates a fixed property");
            }
            return true;
        }
        internal bool Has(string key)
        {
            var original = Target; var trap = Trap("has");
            if (trap.Method == null) return original.HasInProperty(key);
            bool result = Truthy(trap.Invoke(original, Key(key)));
            if (!result)
            {
                var descriptor = original.OwnPropertyDescriptor(key) as TsObject;
                if (descriptor != null && (Fixed(descriptor) || !original.IsExtensible)) throw new InvalidOperationException("Proxy cannot hide a protected property");
            }
            return result;
        }
        internal bool Delete(string key)
        {
            var original = Target; var trap = Trap("deleteProperty");
            if (trap.Method == null) return original.DeleteOwn(key);
            if (!Truthy(trap.Invoke(original, Key(key)))) return false;
            var descriptor = original.OwnPropertyDescriptor(key) as TsObject;
            if (descriptor != null && (Fixed(descriptor) || !original.IsExtensible)) throw new InvalidOperationException("Proxy cannot delete a protected property");
            return true;
        }
        internal object Keys(bool enumerable)
        {
            var original = Target; var trap = Trap("ownKeys");
            if (trap.Method == null)
            {
                var own = (JsArray)original.OwnPropertyNames();
                if (!enumerable) return own;
                var visible = new JsArray(0);
                for (uint i = 0; i < own.Length(); i++)
                {
                    string key = (string)own.ElementAt(i);
                    if (!IsSymbolKey(key) && Describe(key) is TsObject descriptor && Truthy(descriptor.Get("enumerable"))) visible.Append(key);
                }
                return visible;
            }
            if (trap.Invoke(original) is not TsObject list) throw new InvalidOperationException("Proxy ownKeys must return an object");
            var names = new JsArray(0); var seen = new HashSet<string>(StringComparer.Ordinal);
            long count = (long)IndexedLength(list);
            for (long i = 0; i < count; i++)
            {
                object key = IndexedValue(list, i);
                if (key is not string && key is not JsSymbol) throw new InvalidOperationException("Proxy ownKeys contains a non-key");
                string name = PropertyKey(key);
                if (!seen.Add(name)) throw new InvalidOperationException("Proxy ownKeys contains duplicate keys");
                names.Append(name);
            }
            bool extensible = ObjectExtensible(original);
            var actual = (JsArray)original.OwnPropertyNames();
            for (uint i = 0; i < actual.Length(); i++)
            {
                string name = (string)actual.ElementAt(i);
                if ((!extensible || Fixed(original.OwnPropertyDescriptor(name) as TsObject)) && !seen.Contains(name))
                    throw new InvalidOperationException("Proxy ownKeys omitted a required key");
                if (!extensible) seen.Remove(name);
            }
            if (!extensible && seen.Count != 0) throw new InvalidOperationException("Proxy ownKeys added keys to a sealed target");
            if (!enumerable) return names;
            var result = new JsArray(0);
            for (uint i = 0; i < names.Length(); i++) { string name = (string)names.ElementAt(i); if (!IsSymbolKey(name) && Describe(name) is TsObject descriptor && Truthy(descriptor.Get("enumerable"))) result.Append(name); }
            return result;
        }
        internal object Call(object receiver, object[] arguments)
        {
            var original = (JsFunction)Target; var trap = Trap("apply");
            return trap.Method == null ? original.InvokeWithReceiver(receiver, arguments) : trap.Invoke(original, receiver, new JsArray(arguments));
        }
        internal object Construct(object[] arguments, JsFunction newTarget)
        {
            if (!Constructable) throw new InvalidOperationException("Proxy target is not a constructor");
            var original = (JsFunction)Target; var trap = Trap("construct");
            object result = trap.Method == null ? original.ConstructWithNewTarget(arguments, newTarget) : trap.Invoke(original, new JsArray(arguments), newTarget);
            return result is TsObject ? result : throw new InvalidOperationException("Proxy constructor must return an object");
        }
    }
    static object NewProxy(object target, object handler)
    {
        if (target is not TsObject obj || handler is not TsObject hooks) throw new InvalidOperationException("Proxy target and handler must be objects");
        var state = new ProxyState(obj, hooks);
        return target is JsFunction ? new ProxyFunction(state) : new ProxyObject(state);
    }
    sealed class ProxyConstructor : JsFunction
    {
        internal ProxyConstructor() : base((FunctionBody)(static _ => throw new InvalidOperationException("Proxy requires new")), null, "Proxy", 2)
        {
            DefineMethod("revocable", new JsFunction((FunctionBody)(static args =>
            {
                object proxy = NewProxy(FunctionArgument(args, 0), FunctionArgument(args, 1));
                var state = ((IProxyValue)proxy).Proxy;
                var result = new JsDynamicObject(); result.DefineOwn("proxy", proxy);
                result.DefineOwn("revoke", new JsFunction((FunctionBody)(_ => { state.Revoke(); return Undefined; }), null, "", 0));
                return result;
            }), null, "revocable", 2));
        }
        public override object Construct(object[] args) => NewProxy(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object ConstructWithNewTarget(object[] args, JsFunction newTarget) => Construct(args);
    }
    static class ProxyBuiltins { internal static readonly JsFunction Constructor = new ProxyConstructor(); }
}
