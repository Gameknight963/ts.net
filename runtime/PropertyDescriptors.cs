using System;

public abstract partial class TsObject
{
    internal virtual bool OwnPropertyConfigurable(string key) => !IsReadOnlyOwnProperty(key);
    internal virtual object OwnPropertyDescriptor(string key)
    {
        if (!HasOwn(key)) return Js.Undefined;
        if (IsModuleNamespace) return Js.DataDescriptor(Get(key), !Js.IsSymbolKey(key), !Js.IsSymbolKey(key), false);
        bool enumerable = ForInEnumerable(key), configurable = OwnPropertyConfigurable(key);
        if (TryOwnAccessor(key, out var accessor))
            return Js.AccessorDescriptor(accessor.Getter, accessor.Setter, enumerable, configurable);
        return Js.DataDescriptor(Get(key), !IsReadOnlyOwnProperty(key), enumerable, configurable);
    }
}

public partial class JsFunction
{
    internal override bool OwnPropertyConfigurable(string key) => key != "prototype" || !HasOwn(key);
}

public partial class JsArray
{
    internal override bool OwnPropertyConfigurable(string key) => key != "length" && !(ElementsSealed && Index(key, out uint index) && HasElement(index)) && base.OwnPropertyConfigurable(key);
}

public static partial class Js
{
    internal static object DataDescriptor(object value, bool writable, bool enumerable, bool configurable)
    {
        var result = new JsDynamicObject();
        result.DefineOwn("value", value); result.DefineOwn("writable", BoxBoolean(writable));
        result.DefineOwn("enumerable", BoxBoolean(enumerable)); result.DefineOwn("configurable", BoxBoolean(configurable));
        return result;
    }
    internal static object AccessorDescriptor(JsFunction getter, JsFunction setter, bool enumerable, bool configurable)
    {
        var result = new JsDynamicObject();
        result.DefineOwn("get", getter is null ? Undefined : getter); result.DefineOwn("set", setter is null ? Undefined : setter);
        result.DefineOwn("enumerable", BoxBoolean(enumerable)); result.DefineOwn("configurable", BoxBoolean(configurable));
        return result;
    }
    static object ObjectOwnDescriptor(object value, object key)
    {
        TsObject source = ToObject(value);
        return source.OwnPropertyDescriptor(PropertyKey(key));
    }
    static object ObjectOwnDescriptors(object value)
    {
        TsObject source = ToObject(value);
        var names = (JsArray)source.OwnPropertyNames();
        var result = new JsDynamicObject();
        for (uint index = 0; index < names.Length(); index++)
        {
            string key = (string)names.ElementAt(index);
            object descriptor = source.OwnPropertyDescriptor(key);
            if (!IsUndefined(descriptor)) result.DefineOwn(key, descriptor);
        }
        return result;
    }
}
