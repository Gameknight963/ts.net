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
    static object ObjectDefineProperty(object target, object property, object attributes)
    {
        if (target is not TsObject obj) throw new InvalidOperationException("Property target must be an object");
        string key = PropertyKey(property);
        PropertyDefinition definition = ReadPropertyDefinition(attributes);
        if (obj is not JsCreatedObject created) throw new NotSupportedException("Property definition on this object layout is not implemented yet");
        object existing = obj.OwnPropertyDescriptor(key);
        if (IsUndefined(existing)) { created.Define(definition.Complete(key)); return obj; }
        var current = (TsObject)existing;
        bool Has(DescriptorFields field) => (definition.Fields & field) != 0;
        bool configurable = Truthy(current.Get("configurable")), enumerable = Truthy(current.Get("enumerable"));
        bool accessor = !current.HasOwn("writable");
        bool dataDefinition = Has(DescriptorFields.Value | DescriptorFields.Writable);
        bool changesKind = (definition.Accessor || dataDefinition) && accessor != definition.Accessor;
        void Reject() => throw new InvalidOperationException("Cannot redefine a non-configurable property");
        if (!configurable)
        {
            if (Has(DescriptorFields.Configurable) && definition.Configurable || Has(DescriptorFields.Enumerable) && definition.Enumerable != enumerable || changesKind) Reject();
            if (accessor)
            {
                if (Has(DescriptorFields.Get) && !SameValue(current.Get("get"), definition.Getter ?? Undefined) ||
                    Has(DescriptorFields.Set) && !SameValue(current.Get("set"), definition.Setter ?? Undefined)) Reject();
            }
            else if (!Truthy(current.Get("writable")))
            {
                if (Has(DescriptorFields.Writable) && definition.Writable || Has(DescriptorFields.Value) && !SameValue(current.Get("value"), definition.Value)) Reject();
            }
        }
        bool nextAccessor = changesKind ? definition.Accessor : accessor;
        object value = Has(DescriptorFields.Value) ? definition.Value : accessor ? Undefined : current.Get("value");
        bool writable = Has(DescriptorFields.Writable) ? definition.Writable : !accessor && Truthy(current.Get("writable"));
        JsFunction getter = !nextAccessor ? null : Has(DescriptorFields.Get) ? definition.Getter : accessor ? current.Get("get") as JsFunction : null;
        JsFunction setter = !nextAccessor ? null : Has(DescriptorFields.Set) ? definition.Setter : accessor ? current.Get("set") as JsFunction : null;
        created.Define(new(key, value, getter, setter, nextAccessor, writable,
            Has(DescriptorFields.Enumerable) ? definition.Enumerable : enumerable,
            Has(DescriptorFields.Configurable) ? definition.Configurable : configurable));
        return obj;
    }
}
