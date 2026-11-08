using System;
using System.Collections.Generic;

internal sealed class JsCreatedObject : JsDynamicObject
{
    internal sealed record Descriptor(string Key, object Value, JsFunction Getter, JsFunction Setter, bool Accessor, bool Writable, bool Enumerable, bool Configurable);
    readonly Dictionary<string, Descriptor> descriptors = new();
    internal void DefineMethod(string key, object value, bool writable = true) => Define(new(key, value, null, null, false, writable, false, true));
    internal void Define(Descriptor descriptor)
    {
        descriptors.Add(descriptor.Key, descriptor);
        DefineOwn(descriptor.Key, descriptor.Value);
        if (descriptor.Getter != null) DefineAccessor(descriptor.Key, descriptor.Getter, 1, false);
        if (descriptor.Setter != null) DefineAccessor(descriptor.Key, descriptor.Setter, 2, false);
    }
    internal void DefineJsonData(string key, object value) { descriptors.Remove(key); DefineOwn(key, value); }
    internal override bool IsReadOnlyOwnProperty(string key) => descriptors.TryGetValue(key, out var descriptor) &&
        (descriptor.Accessor ? descriptor.Setter == null : !descriptor.Writable);
    internal override object OwnPropertyDescriptor(string key)
    {
        if (!descriptors.TryGetValue(key, out var descriptor)) return base.OwnPropertyDescriptor(key);
        return descriptor.Accessor
            ? Js.AccessorDescriptor(descriptor.Getter, descriptor.Setter, descriptor.Enumerable, descriptor.Configurable)
            : Js.DataDescriptor(Get(key), descriptor.Writable, descriptor.Enumerable, descriptor.Configurable);
    }
    public override object Set(string key, object value)
    {
        if (IsReadOnlyOwnProperty(key)) throw new InvalidOperationException("Property is read-only");
        return base.Set(key, value);
    }
    internal override bool ForInEnumerable(string key) => !descriptors.TryGetValue(key, out var descriptor) || descriptor.Enumerable;
    internal override object ForInOwnKeys() => base.OwnKeys();
    public override object OwnKeys()
    {
        var names = (JsArray)base.OwnKeys(); var result = new JsArray(0);
        for (uint index = 0; index < names.Length(); index++)
        {
            string key = (string)names.ElementAt(index);
            if (ForInEnumerable(key)) result.Append(key);
        }
        return result;
    }
    public override double OwnKeyCount() => Js.StringPropertyKeyCount(OwnKeys());
    public override object CloneForSpread()
    {
        var result = new JsDynamicObject(); Js.ObjectSpread(result, this); return result;
    }
    public override bool DeleteOwn(string key)
    {
        if (descriptors.TryGetValue(key, out var descriptor))
        {
            if (!descriptor.Configurable) return false;
            descriptors.Remove(key);
        }
        return base.DeleteOwn(key);
    }
}

public static partial class Js
{
    static object ObjectCreate(object prototype, object properties)
    {
        if (prototype != null && prototype is not TsObject) throw new InvalidOperationException("Object prototype must be an object or null");
        var result = new JsCreatedObject(); result.SetPrototype((TsObject)prototype);
        if (IsUndefined(properties)) return result;
        TsObject source = ToObject(properties);
        var keys = (JsArray)source.OwnPropertyNames();
        var descriptors = new List<JsCreatedObject.Descriptor>();
        for (uint index = 0; index < keys.Length(); index++)
        {
            string key = (string)keys.ElementAt(index);
            if (!source.HasOwn(key) || !source.ForInEnumerable(key)) continue;
            if (source.Get(key) is not TsObject descriptor) throw new InvalidOperationException("Property descriptor must be an object");
            bool enumerable = descriptor.HasProperty("enumerable") && Truthy(descriptor.Get("enumerable"));
            bool configurable = descriptor.HasProperty("configurable") && Truthy(descriptor.Get("configurable"));
            bool hasValue = descriptor.HasProperty("value");
            object value = hasValue ? descriptor.Get("value") : Undefined;
            bool hasWritable = descriptor.HasProperty("writable");
            bool writable = hasWritable && Truthy(descriptor.Get("writable"));
            bool hasGet = descriptor.HasProperty("get");
            object getter = hasGet ? descriptor.Get("get") : Undefined;
            if (!IsUndefined(getter) && getter is not JsFunction) throw new InvalidOperationException("Property getter is not callable");
            bool hasSet = descriptor.HasProperty("set");
            object setter = hasSet ? descriptor.Get("set") : Undefined;
            if (!IsUndefined(setter) && setter is not JsFunction) throw new InvalidOperationException("Property setter is not callable");
            bool accessor = hasGet || hasSet;
            if (accessor && (hasValue || hasWritable)) throw new InvalidOperationException("Property descriptor mixes data and accessors");
            descriptors.Add(new(key, value, getter as JsFunction, setter as JsFunction, accessor, writable, enumerable, configurable));
        }
        foreach (var descriptor in descriptors) result.Define(descriptor);
        return result;
    }
}
