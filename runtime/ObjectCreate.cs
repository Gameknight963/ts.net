using System;
using System.Collections.Generic;

internal sealed class JsCreatedObject : JsDynamicObject
{
    internal sealed record Descriptor(string Key, object Value, JsFunction Getter, JsFunction Setter, bool Accessor, bool Writable, bool Enumerable, bool Configurable);
    readonly Dictionary<string, Descriptor> descriptors = new();
    internal void DefineMethod(string key, object value, bool writable = true) => Define(new(key, value, null, null, false, writable, false, true));
    internal void Define(Descriptor descriptor)
    {
        descriptors[descriptor.Key] = descriptor;
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
            descriptors.Add(ReadPropertyDefinition(source.Get(key)).Complete(key));
        }
        foreach (var descriptor in descriptors) result.Define(descriptor);
        return result;
    }
}
