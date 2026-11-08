using System;
using System.Collections.Generic;

public abstract class JsDescriptorObject : TsObject
{
    internal bool Extensible = true;
    internal override bool IsExtensible => Extensible;
    internal sealed record Descriptor(string Key, object Value, JsFunction Getter, JsFunction Setter, bool Accessor, bool Writable, bool Enumerable, bool Configurable);
    Dictionary<string, Descriptor> descriptors;
    internal void DefineMethod(string key, object value, bool writable = true) => Define(new(key, value, null, null, false, writable, false, true));
    internal virtual void Define(Descriptor descriptor)
    {
        (descriptors ??= new())[descriptor.Key] = descriptor;
        DefineOwn(descriptor.Key, descriptor.Value);
        if (descriptor.Getter != null) DefineAccessor(descriptor.Key, descriptor.Getter, 1, false);
        if (descriptor.Setter != null) DefineAccessor(descriptor.Key, descriptor.Setter, 2, false);
    }
    internal void DefineJsonData(string key, object value) { descriptors?.Remove(key); DefineOwn(key, value); }
    internal override bool IsReadOnlyOwnProperty(string key) => descriptors != null && descriptors.TryGetValue(key, out var descriptor) &&
        (descriptor.Accessor ? descriptor.Setter == null : !descriptor.Writable);
    internal override object OwnPropertyDescriptor(string key)
    {
        if (descriptors == null || !descriptors.TryGetValue(key, out var descriptor)) return base.OwnPropertyDescriptor(key);
        return descriptor.Accessor
            ? Js.AccessorDescriptor(descriptor.Getter, descriptor.Setter, descriptor.Enumerable, descriptor.Configurable)
            : Js.DataDescriptor(Get(key), descriptor.Writable, descriptor.Enumerable, descriptor.Configurable);
    }
    internal override bool ForInEnumerable(string key) => base.ForInEnumerable(key) && (descriptors == null || !descriptors.TryGetValue(key, out var descriptor) || descriptor.Enumerable);
    internal override object ForInOwnKeys() => base.OwnKeys();
    public override object OwnKeys()
    {
        if (descriptors == null) return base.OwnKeys();
        var names = (JsArray)base.OwnKeys(); var result = new JsArray(0);
        for (uint index = 0; index < names.Length(); index++)
        {
            string key = (string)names.ElementAt(index);
            if (ForInEnumerable(key)) result.Append(key);
        }
        return result;
    }
    public override double OwnKeyCount() => descriptors == null ? base.OwnKeyCount() : Js.StringPropertyKeyCount(OwnKeys());
    public override object CloneForSpread()
    {
        var result = new JsDynamicObject(); Js.ObjectSpread(result, this); return result;
    }
    public override bool DeleteOwn(string key)
    {
        if (descriptors != null && descriptors.TryGetValue(key, out var descriptor))
        {
            if (!descriptor.Configurable) return false;
            descriptors.Remove(key);
        }
        return base.DeleteOwn(key);
    }
}

