using System;

public static partial class Js
{
    static bool ReflectSet(TsObject target, string key, object value, object receiver)
    {
        if (target is IProxyValue proxy) return proxy.Proxy.Set(key, value, receiver);
        if (target is TypedArrayValue typedTarget && TypedArrayValue.NumericKey(key, out double index)) {
            if (ReferenceEquals(target, receiver)) {
                typedTarget.WriteIndex(index, value);
                return true;
            }
            if (!typedTarget.Valid(index)) return true;
        }
        if (target.IsModuleNamespace) return false;
        object own = target.OwnPropertyDescriptor(key);
        if (IsUndefined(own)) {
            if (target.PropertyPrototype is TsObject prototype) return ReflectSet(prototype, key, value, receiver);
        } else {
            var descriptor = (TsObject)own;
            if (!descriptor.HasOwn("writable")) {
                object setter = descriptor.Get("set");
                if (IsUndefined(setter)) return false;
                CallMethod(setter, receiver, new[] { value });
                return true;
            }
            if (!Truthy(descriptor.Get("writable"))) return false;
        }
        if (receiver is not TsObject destination || destination.IsModuleNamespace || destination is JsRawJson or JsTemplateArray) return false;
        object existing = destination.OwnPropertyDescriptor(key);
        if (!IsUndefined(existing)) {
            var descriptor = (TsObject)existing;
            if (!descriptor.HasOwn("writable") || !Truthy(descriptor.Get("writable"))) return false;
        }
        if (destination is IProxyValue || destination is TypedArrayValue && TypedArrayValue.NumericKey(key, out _)) {
            // Definitions validate typed-array indices before conversion and
            // let proxies decide without a separate extensibility trap.
            var fields = DescriptorFields.Value;
            if (IsUndefined(existing)) fields |= DescriptorFields.Writable | DescriptorFields.Enumerable | DescriptorFields.Configurable;
            return TryApplyPropertyDefinition(destination, key, new(value, null, null, true, true, true, fields));
        }
        if (IsUndefined(existing) && !destination.IsExtensible) return false;
        try { destination.SetOwnValue(key, value); }
        catch (JsArray.LengthBlockedException) { return false; }
        return true;
    }
}
