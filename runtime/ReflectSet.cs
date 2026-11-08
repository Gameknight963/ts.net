using System;

public static partial class Js
{
    static bool ReflectSet(TsObject target, string key, object value, object receiver)
    {
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
        if (IsUndefined(existing) && !destination.IsExtensible) return false;
        if (!IsUndefined(existing)) {
            var descriptor = (TsObject)existing;
            if (!descriptor.HasOwn("writable") || !Truthy(descriptor.Get("writable"))) return false;
        }
        try { destination.SetOwnValue(key, value); }
        catch (JsArray.LengthBlockedException) { return false; }
        return true;
    }
}
