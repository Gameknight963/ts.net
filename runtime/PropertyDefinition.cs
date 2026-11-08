using System;

public static partial class Js
{
    [Flags]
    enum DescriptorFields { None = 0, Enumerable = 1, Configurable = 2, Value = 4, Writable = 8, Get = 16, Set = 32 }
    readonly record struct PropertyDefinition(object Value, JsFunction Getter, JsFunction Setter,
        bool Writable, bool Enumerable, bool Configurable, DescriptorFields Fields)
    {
        internal bool Accessor => (Fields & (DescriptorFields.Get | DescriptorFields.Set)) != 0;
        internal JsCreatedObject.Descriptor Complete(string key) => new(key, Value, Getter, Setter, Accessor, Writable, Enumerable, Configurable);
    }
    static PropertyDefinition ReadPropertyDefinition(object input)
    {
        if (input is not TsObject descriptor) throw new InvalidOperationException("Property descriptor must be an object");
        DescriptorFields fields = DescriptorFields.None;
        bool Has(string name, DescriptorFields field)
        {
            if (!descriptor.HasInProperty(name)) return false;
            fields |= field;
            return true;
        }
        bool enumerable = Has("enumerable", DescriptorFields.Enumerable) && Truthy(descriptor.Get("enumerable"));
        bool configurable = Has("configurable", DescriptorFields.Configurable) && Truthy(descriptor.Get("configurable"));
        object value = Has("value", DescriptorFields.Value) ? descriptor.Get("value") : Undefined;
        bool writable = Has("writable", DescriptorFields.Writable) && Truthy(descriptor.Get("writable"));
        object getter = Has("get", DescriptorFields.Get) ? descriptor.Get("get") : Undefined;
        if (!IsUndefined(getter) && getter is not JsFunction) throw new InvalidOperationException("Property getter is not callable");
        object setter = Has("set", DescriptorFields.Set) ? descriptor.Get("set") : Undefined;
        if (!IsUndefined(setter) && setter is not JsFunction) throw new InvalidOperationException("Property setter is not callable");
        if ((fields & (DescriptorFields.Get | DescriptorFields.Set)) != 0 && (fields & (DescriptorFields.Value | DescriptorFields.Writable)) != 0)
            throw new InvalidOperationException("Property descriptor mixes data and accessors");
        return new(value, getter as JsFunction, setter as JsFunction, writable, enumerable, configurable, fields);
    }
}
