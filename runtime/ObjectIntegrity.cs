using System;

public static partial class Js
{
    static bool ObjectExtensible(object value) => value is TsObject target && target.IsExtensible && !target.IsModuleNamespace && target is not (JsTemplateArray or JsRawJson);
    static object ObjectPreventExtensions(object value)
    {
        if (value is not TsObject target || !ObjectExtensible(value)) return value;
        if (target is TypedArrayValue array && !array.CanPreventExtensions) throw new InvalidOperationException("Cannot prevent extensions on this typed array");
        if (target is JsArray ordinary) { ordinary.PreventExtensions(); return value; }
        if (target is not JsDescriptorObject dictionary) throw new NotSupportedException("Preventing extensions on this object layout is not implemented yet");
        dictionary.PreventExtensions();
        return value;
    }
    static object ReflectPreventExtensions(object value)
    {
        var target = ReflectTarget(value);
        if (target is TypedArrayValue array && !array.CanPreventExtensions) return BoxBoolean(false);
        ObjectPreventExtensions(target);
        return BoxBoolean(true);
    }
    static bool ObjectIntegrity(object value, bool frozen)
    {
        if (value is not TsObject target) return true;
        if (ObjectExtensible(target)) return false;
        if (frozen && target is TypedArrayValue {Count: > 0}) return false;
        if (target is JsTemplateArray) return true;
        if (target is JsArray ordinary) {
            if (frozen && ordinary.LengthWritable) return false;
            if ((!ordinary.ElementsSealed || frozen && !ordinary.ElementsFrozen) && ordinary.ElementKeyCount() != 0) return false;
        }
        var names = (JsArray)(target is TypedArrayValue array ? array.ExtraPropertyNames() : target is JsArray indexed ? indexed.ExtraPropertyNames() : target.OwnPropertyNames());
        for (uint index = 0; index < names.Length(); index++) {
            string key = (string)names.ElementAt(index);
            var descriptor = (TsObject)target.OwnPropertyDescriptor(key);
            if (Truthy(descriptor.Get("configurable"))) return false;
            if (frozen && descriptor.HasOwn("writable") && Truthy(descriptor.Get("writable"))) return false;
        }
        return true;
    }
    static object SetObjectIntegrity(object value, bool frozen)
    {
        if (value is not TsObject target || ObjectIntegrity(value, frozen)) return value;
        ObjectPreventExtensions(value);
        if (target is JsArray ordinary) ordinary.SealElements(frozen);
        var names = (JsArray)(target is TypedArrayValue typed ? typed.ExtraPropertyNames() : target is JsArray indexed ? indexed.ExtraPropertyNames() : target.OwnPropertyNames());
        for (uint index = 0; index < names.Length(); index++)
        {
            string key = (string)names.ElementAt(index);
            var descriptor = target.OwnPropertyDescriptor(key) as TsObject;
            if (descriptor == null) continue;
            DescriptorFields fields = DescriptorFields.Configurable;
            if (frozen && descriptor.HasOwn("writable")) fields |= DescriptorFields.Writable;
            ApplyPropertyDefinition(target, key, new(Undefined, null, null, false, false, false, fields));
        }
        if (target is TypedArrayValue {Count: > 0}) throw new InvalidOperationException("Cannot change typed-array element attributes");
        if (frozen && target is JsArray frozenArray) frozenArray.MakeLengthReadOnly();
        return value;
    }
}
