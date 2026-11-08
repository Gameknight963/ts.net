using System;
using System.Collections.Generic;

public static partial class Js
{
    [Flags]
    enum DescriptorFields { None = 0, Enumerable = 1, Configurable = 2, Value = 4, Writable = 8, Get = 16, Set = 32 }
    readonly record struct PropertyDefinition(object Value, JsFunction Getter, JsFunction Setter,
        bool Writable, bool Enumerable, bool Configurable, DescriptorFields Fields)
    {
        internal bool Accessor => (Fields & (DescriptorFields.Get | DescriptorFields.Set)) != 0;
        internal JsDescriptorObject.Descriptor Complete(string key) => new(key, Value, Getter, Setter, Accessor, Writable, Enumerable, Configurable);
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
        return ApplyPropertyDefinition(obj, key, definition);
    }
    static List<(string Key, PropertyDefinition Definition)> ReadPropertyDefinitions(object properties)
    {
        var source = ToObject(properties);
        var keys = (JsArray)source.OwnPropertyNames();
        var definitions = new List<(string, PropertyDefinition)>();
        for (uint index = 0; index < keys.Length(); index++)
        {
            string key = (string)keys.ElementAt(index);
            if (source.HasOwn(key) && source.ForInEnumerable(key)) definitions.Add((key, ReadPropertyDefinition(source.Get(key))));
        }
        return definitions;
    }
    static object ObjectDefineProperties(object target, object properties)
    {
        if (target is not TsObject obj) throw new InvalidOperationException("Property target must be an object");
        var definitions = ReadPropertyDefinitions(properties);
        foreach (var entry in definitions) ApplyPropertyDefinition(obj, entry.Key, entry.Definition);
        return obj;
    }
    internal static void DefineDictionaryField(JsDescriptorObject target, string key, object value)
    {
        if (!target.HasPropertyDescriptors && target is JsDynamicObject or JsClassInstance) {
            target.DefineOwn(key, value);
            return;
        }
        ApplyPropertyDefinition(target, key,
            new(value, null, null, true, true, true, DescriptorFields.Value | DescriptorFields.Writable | DescriptorFields.Enumerable | DescriptorFields.Configurable));
    }
    static object ReflectDefineProperty(object target, object property, object attributes)
    {
        if (target is not TsObject obj) throw new InvalidOperationException("Property target must be an object");
        string key = PropertyKey(property);
        PropertyDefinition definition = ReadPropertyDefinition(attributes);
        return BoxBoolean(TryApplyPropertyDefinition(obj, key, definition));
    }
    static object ApplyPropertyDefinition(TsObject obj, string key, PropertyDefinition definition)
    {
        if (!TryApplyPropertyDefinition(obj, key, definition)) throw new InvalidOperationException("Cannot redefine a non-configurable property");
        return obj;
    }
    static void InstallPropertyDefinition(TsObject target, JsDescriptorObject.Descriptor descriptor)
    {
        if (target is JsArray array) array.DefineArrayProperty(descriptor);
        else ((JsDescriptorObject)target).Define(descriptor);
    }
    static bool TryApplyPropertyDefinition(TsObject obj, string key, PropertyDefinition definition)
    {
        if (obj is JsArray ordinaryArray && key == "length") return DefineArrayLength(ordinaryArray, definition);
        if (obj is TypedArrayValue array && TypedArrayValue.NumericKey(key, out double index))
        {
            bool HasElementField(DescriptorFields field) => (definition.Fields & field) != 0;
            if (!array.Valid(index) || definition.Accessor || HasElementField(DescriptorFields.Configurable) && !definition.Configurable ||
                HasElementField(DescriptorFields.Enumerable) && !definition.Enumerable || HasElementField(DescriptorFields.Writable) && !definition.Writable) return false;
            if (HasElementField(DescriptorFields.Value)) array.Write((int)index, definition.Value);
            return true;
        }
        if (obj.IsModuleNamespace || obj is not JsDescriptorObject && obj is not JsArray && obj is not JsRawJson)
            throw new NotSupportedException("Property definition on this object layout is not implemented yet");
        if (obj is JsArray indexed && JsArray.Index(key, out uint element) && element >= indexed.Length() && !indexed.LengthWritable) return false;
        object existing = obj.OwnPropertyDescriptor(key);
        if (IsUndefined(existing)) {
            if (!obj.IsExtensible) return false;
            InstallPropertyDefinition(obj, definition.Complete(key)); return true;
        }
        var current = (TsObject)existing;
        bool Has(DescriptorFields field) => (definition.Fields & field) != 0;
        bool configurable = Truthy(current.Get("configurable")), enumerable = Truthy(current.Get("enumerable"));
        bool accessor = !current.HasOwn("writable");
        bool dataDefinition = Has(DescriptorFields.Value | DescriptorFields.Writable);
        bool changesKind = (definition.Accessor || dataDefinition) && accessor != definition.Accessor;
        if (!configurable)
        {
            if (Has(DescriptorFields.Configurable) && definition.Configurable || Has(DescriptorFields.Enumerable) && definition.Enumerable != enumerable || changesKind) return false;
            if (accessor)
            {
                if (Has(DescriptorFields.Get) && !SameValue(current.Get("get"), definition.Getter ?? Undefined) ||
                    Has(DescriptorFields.Set) && !SameValue(current.Get("set"), definition.Setter ?? Undefined)) return false;
            }
            else if (!Truthy(current.Get("writable")))
            {
                if (Has(DescriptorFields.Writable) && definition.Writable || Has(DescriptorFields.Value) && !SameValue(current.Get("value"), definition.Value)) return false;
            }
        }
        // Raw JSON properties are immutable; compatible definitions need no storage update.
        if (obj is JsRawJson) return true;
        bool nextAccessor = changesKind ? definition.Accessor : accessor;
        object value = nextAccessor ? Undefined : Has(DescriptorFields.Value) ? definition.Value : accessor ? Undefined : current.Get("value");
        bool writable = Has(DescriptorFields.Writable) ? definition.Writable : !accessor && Truthy(current.Get("writable"));
        JsFunction getter = !nextAccessor ? null : Has(DescriptorFields.Get) ? definition.Getter : accessor ? current.Get("get") as JsFunction : null;
        JsFunction setter = !nextAccessor ? null : Has(DescriptorFields.Set) ? definition.Setter : accessor ? current.Get("set") as JsFunction : null;
        InstallPropertyDefinition(obj, new(key, value, getter, setter, nextAccessor, writable,
            Has(DescriptorFields.Enumerable) ? definition.Enumerable : enumerable,
            Has(DescriptorFields.Configurable) ? definition.Configurable : configurable));
        return true;
    }
}
