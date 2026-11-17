using System;
public static partial class Js
{
    public static bool ProxyMatchesType<T>(object value) => value is IProxyValue proxy && typeof(T).IsAssignableFrom(proxy.Proxy.TargetType);
    static bool IsProxyArray(object value)
    {
        while (value is IProxyValue proxy) value = proxy.Proxy.Target;
        return value is JsArray;
    }
    static TsObject ProxyDescriptorObject(PropertyDefinition definition, bool complete)
    {
        var result = new JsDynamicObject();
        bool Has(DescriptorFields field) => complete || (definition.Fields & field) != 0;
        if (Has(DescriptorFields.Enumerable)) result.DefineOwn("enumerable", BoxBoolean(definition.Enumerable));
        if (Has(DescriptorFields.Configurable)) result.DefineOwn("configurable", BoxBoolean(definition.Configurable));
        if (definition.Accessor)
        {
            if (Has(DescriptorFields.Get)) result.DefineOwn("get", definition.Getter ?? Undefined);
            if (Has(DescriptorFields.Set)) result.DefineOwn("set", definition.Setter ?? Undefined);
        }
        else
        {
            if (Has(DescriptorFields.Value)) result.DefineOwn("value", definition.Value);
            if (Has(DescriptorFields.Writable)) result.DefineOwn("writable", BoxBoolean(definition.Writable));
        }
        return result;
    }
    static bool ProxyCompatible(PropertyDefinition definition, TsObject current, bool extensible)
    {
        if (current == null) return extensible;
        if (Truthy(current.Get("configurable"))) return true;
        bool Has(DescriptorFields field) => (definition.Fields & field) != 0;
        bool accessor = !current.HasOwn("writable");
        bool changesKind = (definition.Accessor || Has(DescriptorFields.Value | DescriptorFields.Writable)) && accessor != definition.Accessor;
        if (Has(DescriptorFields.Configurable) && definition.Configurable ||
           Has(DescriptorFields.Enumerable) && definition.Enumerable != Truthy(current.Get("enumerable")) || changesKind) return false;
        if (accessor) return !(Has(DescriptorFields.Get) && !SameValue(current.Get("get"), definition.Getter ?? Undefined) ||
                           Has(DescriptorFields.Set) && !SameValue(current.Get("set"), definition.Setter ?? Undefined));
        if (Truthy(current.Get("writable"))) return true;
        return !(Has(DescriptorFields.Writable) && definition.Writable || Has(DescriptorFields.Value) && !SameValue(current.Get("value"), definition.Value));
    }
    sealed partial class ProxyState
    {
        internal object Describe(string key)
        {
            var original = Target; var trap = Trap("getOwnPropertyDescriptor");
            if (trap.Method == null) return original.OwnPropertyDescriptor(key);
            object result = trap.Invoke(original, Key(key));
            if (!IsUndefined(result) && result is not TsObject) throw new InvalidOperationException("Proxy descriptor must be an object or undefined");
            var current = original.OwnPropertyDescriptor(key) as TsObject;
            if (IsUndefined(result))
            {
                if (current != null && (Fixed(current) || !ObjectExtensible(original))) throw new InvalidOperationException("Proxy cannot hide a protected descriptor");
                return Undefined;
            }
            bool extensible = ObjectExtensible(original);
            var definition = ReadPropertyDefinition(result);
            var completed = definition with { Fields = definition.Accessor ? DescriptorFields.Get | DescriptorFields.Set | DescriptorFields.Enumerable | DescriptorFields.Configurable : DescriptorFields.Value | DescriptorFields.Writable | DescriptorFields.Enumerable | DescriptorFields.Configurable };
            if (!ProxyCompatible(completed, current, extensible) || !definition.Configurable &&
                (current == null || !Fixed(current) || !definition.Accessor && !definition.Writable && Truthy(current.Get("writable"))))
                throw new InvalidOperationException("Proxy descriptor conflicts with its target");
            return ProxyDescriptorObject(completed, true);
        }
        internal bool Define(string key, PropertyDefinition definition)
        {
            var original = Target; var trap = Trap("defineProperty");
            if (trap.Method == null)
            {
                // Fixed layouts already support ordinary value stores. Forward
                // compatible data writes without requiring descriptor storage.
                if (original is not JsDescriptorObject && original is not JsArray &&
                   !original.IsModuleNamespace && original is not JsRawJson)
                {
                    var existing = original.OwnPropertyDescriptor(key) as TsObject;
                    bool defaults = definition.Writable && definition.Enumerable && definition.Configurable;
                    bool valueOnly = definition.Fields == DescriptorFields.Value && existing != null &&
                        existing.HasOwn("writable") && Truthy(existing.Get("writable"));
                    if (!definition.Accessor && (valueOnly || defaults && (existing == null ||
                       Truthy(existing.Get("writable")) && Truthy(existing.Get("enumerable")) && Truthy(existing.Get("configurable")))) &&
                       ProxyCompatible(definition, existing, ObjectExtensible(original)))
                    {
                        original.SetOwnValue(key, definition.Value); return true;
                    }
                }
                return TryApplyPropertyDefinition(original, key, definition);
            }
            if (!Truthy(trap.Invoke(original, Key(key), ProxyDescriptorObject(definition, false)))) return false;
            var current = original.OwnPropertyDescriptor(key) as TsObject;
            bool setsFixed = (definition.Fields & DescriptorFields.Configurable) != 0 && !definition.Configurable;
            if (!ProxyCompatible(definition, current, ObjectExtensible(original)) || setsFixed && (current == null || !Fixed(current)) ||
                current != null && Fixed(current) && Truthy(current.Get("writable")) &&
                (definition.Fields & DescriptorFields.Writable) != 0 && !definition.Writable)
                throw new InvalidOperationException("Proxy definition conflicts with its target");
            return true;
        }
        internal object DefineValue(string key, object value)
        {
            var fields = DescriptorFields.Value;
            if (IsUndefined(Describe(key))) fields |= DescriptorFields.Writable | DescriptorFields.Enumerable | DescriptorFields.Configurable;
            if (!Define(key, new(value, null, null, true, true, true, fields))) throw new InvalidOperationException("Proxy rejected property definition");
            return value;
        }
    }
}
