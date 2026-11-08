using System;

public abstract partial class TsObject
{
    // Membership checks do not read values or invoke accessors.
    internal virtual bool HasInProperty(string key)
    {
        if (HasOwn(key)) return true;
        if (IsModuleNamespace) return false;
        return HasProperty(key) || PropertyPrototype is not null && PropertyPrototype.HasInProperty(key);
    }
}

public static partial class Js
{
    private sealed class ClassObjectPrototype : JsClassInstance
    {
        internal ClassObjectPrototype()
        {
            DefineMethod("toString", new ObjectStringFunction());
            DefineMethod("toLocaleString", new ObjectLocaleStringFunction());
            DefineMethod("valueOf", new ObjectValueOfFunction());
            DefineMethod("__defineGetter__", new DefineAccessorFunction(false));
            DefineMethod("__defineSetter__", new DefineAccessorFunction(true));
            DefineMethod("__lookupGetter__", new LookupAccessorFunction(false));
            DefineMethod("__lookupSetter__", new LookupAccessorFunction(true));
            DefineMethod("isPrototypeOf", new ObjectPrototypeQuery("isPrototypeOf", 0));
            DefineMethod("hasOwnProperty", new ObjectPrototypeQuery("hasOwnProperty", 1));
            DefineMethod("propertyIsEnumerable", new ObjectPrototypeQuery("propertyIsEnumerable", 2));
            DefineClassAccessor("__proto__",new PrototypeAccessor(0),1);
            DefineClassAccessor("__proto__",new PrototypeAccessor(1),2);
            DefineClassAccessor("constructor",new PrototypeAccessor(2),1);
            DefineClassAccessor("constructor",new PrototypeAccessor(3),2);
        }
        internal override TsObject PropertyPrototype => null;
        internal override bool HasInProperty(string key) => HasOwn(key);
    }
    internal static readonly JsClassInstance DefaultClassObjectPrototype = new ClassObjectPrototype();
    public static bool In(object key, object target)
    {
        if (target is not TsObject value) throw new InvalidOperationException("Right-hand side of in must be an object");
        return value.HasInProperty(PropertyKey(key));
    }
    public static bool InNumber(double? key, object target)
    {
        if (target is not TsObject value) throw new InvalidOperationException("Right-hand side of in must be an object");
        if (value is JsArray array && key.HasValue && key.Value >= 0 && key.Value < uint.MaxValue && key.Value == Math.Truncate(key.Value))
        {
            if (array.HasElement((uint)key.Value)) return true;
            if (array.CanUseElementStorage) return false;
        }
        return value.HasInProperty(key.HasValue ? NumberKeyString(key.Value) : "undefined");
    }
}
