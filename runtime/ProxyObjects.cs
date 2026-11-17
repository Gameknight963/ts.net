using System;
public static partial class Js
{
    sealed class ProxyObject(ProxyState state) : JsDescriptorObject, IProxyValue
    {
        public ProxyState Proxy => state;
        public override object Get(string key) => state.Get(key, this);
        public override object GetWithReceiver(string key, object receiver) => state.Get(key, receiver);
        public override bool HasOwn(string key) => !IsUndefined(state.Describe(key));
        public override bool HasProperty(string key) => state.Has(key);
        internal override bool HasInProperty(string key) => state.Has(key);
        public override object Set(string key, object value) => SetWithReceiver(key, value, this);
        public override object SetWithReceiver(string key, object value, object receiver)
        {
            if (!state.Set(key, value, receiver)) throw new InvalidOperationException("Proxy rejected a property write"); return value;
        }
        public override object SetOwnValue(string key, object value) => state.DefineValue(key, value);
        public override bool DeleteOwn(string key) => state.Delete(key);
        public override object OwnKeys() => state.Keys(true);
        internal override object OwnPropertyNames() => state.Keys(false);
        internal override object ForInOwnKeys() => state.Keys(false);
        internal override bool ForInEnumerable(string key) => state.Describe(key) is TsObject descriptor && Truthy(descriptor.Get("enumerable"));
        internal override object OwnPropertyDescriptor(string key) => state.Describe(key);
        internal override TsObject PropertyPrototype => state.GetPrototype();
        internal override bool IsExtensible => state.Extensible();
        internal override bool TrySetPrototype(TsObject prototype) => state.SetPrototype(prototype);
        internal override void SetPrototype(TsObject prototype) { if (!TrySetPrototype(prototype)) throw new InvalidOperationException("Proxy prototype change rejected"); }

    }
    sealed class ProxyFunction : JsFunction, IProxyValue
    {
        readonly ProxyState state;
        internal ProxyFunction(ProxyState state) : base((FunctionBody)(args => state.Call(Undefined, args)), null, "", 0) { this.state = state; }
        public override object InvokeWithReceiver(object receiver, object[] args) => state.Call(receiver, args);
        public override double? InvokeMethodNumber0(object receiver) => Cast<double?>(state.Call(receiver, Array.Empty<object>()), "checked conversion failed");
        public override double? InvokeMethodNumber1(object receiver, double? first) => Cast<double?>(state.Call(receiver, new[] { BoxNumber(first) }), "checked conversion failed");
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Cast<double?>(state.Call(receiver, new[] { BoxNumber(first), BoxNumber(second) }), "checked conversion failed");
        public override object Construct(object[] args) => state.Construct(args, this);
        internal override bool CanInitializeDerived => state.Constructable;
        internal override object ConstructWithNewTarget(object[] args, JsFunction newTarget) => state.Construct(args, newTarget);
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget) => state.Construct(args, newTarget);

        public ProxyState Proxy => state;
        public override object Get(string key) => state.Get(key, this);
        public override object GetWithReceiver(string key, object receiver) => state.Get(key, receiver);
        public override bool HasOwn(string key) => !IsUndefined(state.Describe(key));
        public override bool HasProperty(string key) => state.Has(key);
        internal override bool HasInProperty(string key) => state.Has(key);
        public override object Set(string key, object value) => SetWithReceiver(key, value, this);
        public override object SetWithReceiver(string key, object value, object receiver)
        {
            if (!state.Set(key, value, receiver)) throw new InvalidOperationException("Proxy rejected a property write"); return value;
        }
        public override object SetOwnValue(string key, object value) => state.DefineValue(key, value);
        public override bool DeleteOwn(string key) => state.Delete(key);
        public override object OwnKeys() => state.Keys(true);
        internal override object OwnPropertyNames() => state.Keys(false);
        internal override object ForInOwnKeys() => state.Keys(false);
        internal override bool ForInEnumerable(string key) => state.Describe(key) is TsObject descriptor && Truthy(descriptor.Get("enumerable"));
        internal override object OwnPropertyDescriptor(string key) => state.Describe(key);
        internal override TsObject PropertyPrototype => state.GetPrototype();
        internal override bool IsExtensible => state.Extensible();
        internal override bool TrySetPrototype(TsObject prototype) => state.SetPrototype(prototype);
        internal override void SetPrototype(TsObject prototype) { if (!TrySetPrototype(prototype)) throw new InvalidOperationException("Proxy prototype change rejected"); }

    }
}
