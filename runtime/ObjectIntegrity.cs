using System;

public static partial class Js
{
    static bool ObjectExtensible(object value) => value is TsObject target && target.IsExtensible && !target.IsModuleNamespace && target is not (JsTemplateArray or JsRawJson);
    static object ObjectPreventExtensions(object value)
    {
        if (value is not TsObject target || !ObjectExtensible(value)) return value;
        if (target is not JsDynamicObject dictionary) throw new NotSupportedException("Preventing extensions on this object layout is not implemented yet");
        dictionary.Extensible = false;
        return value;
    }
    static bool ObjectIntegrity(object value, bool frozen)
    {
        if (value is not TsObject target) return true;
        if (ObjectExtensible(target)) return false;
        var names = (JsArray)target.OwnPropertyNames();
        for (uint index = 0; index < names.Length(); index++) {
            var descriptor = (TsObject)target.OwnPropertyDescriptor((string)names.ElementAt(index));
            if (Truthy(descriptor.Get("configurable"))) return false;
            if (frozen && descriptor.HasOwn("writable") && Truthy(descriptor.Get("writable"))) return false;
        }
        return true;
    }
    static object SetObjectIntegrity(object value, bool frozen)
    {
        if (value is not TsObject target || ObjectIntegrity(value, frozen)) return value;
        ObjectPreventExtensions(value);
        var names = (JsArray)target.OwnPropertyNames();
        for (uint index = 0; index < names.Length(); index++)
        {
            string key = (string)names.ElementAt(index);
            var descriptor = target.OwnPropertyDescriptor(key) as TsObject;
            if (descriptor == null) continue;
            DescriptorFields fields = DescriptorFields.Configurable;
            if (frozen && descriptor.HasOwn("writable")) fields |= DescriptorFields.Writable;
            ApplyPropertyDefinition(target, key, new(Undefined, null, null, false, false, false, fields));
        }
        return value;
    }
}
