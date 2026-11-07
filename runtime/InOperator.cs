using System;

public abstract partial class TsObject
{
    // Membership includes non-enumerable prototype names. Keep it separate from
    // value lookup while the standard-library prototype methods remain partial.
    internal virtual bool HasInProperty(string key)
    {
        if (HasOwn(key)) return true;
        if (IsModuleNamespace) return false;
        return HasProperty(key) || PropertyPrototype is not null && PropertyPrototype.HasInProperty(key) || this is JsArray && Js.IsArrayPrototypeKey(key);
    }
}

public static partial class Js
{
    internal static bool IsObjectPrototypeKey(string key) => key is "constructor" or "toString" or "valueOf" or "toLocaleString" or "hasOwnProperty" or "isPrototypeOf" or "propertyIsEnumerable" or "__proto__" or "__defineGetter__" or "__defineSetter__" or "__lookupGetter__" or "__lookupSetter__";
    internal static bool IsArrayPrototypeKey(string key) => key is "at" or "concat" or "copyWithin" or "fill" or "find" or "findIndex" or "findLast" or "findLastIndex" or "lastIndexOf" or "pop" or "push" or "reverse" or "shift" or "unshift" or "slice" or "sort" or "splice" or "includes" or "indexOf" or "join" or "keys" or "entries" or "values" or "forEach" or "filter" or "flat" or "flatMap" or "map" or "every" or "some" or "reduce" or "reduceRight" or "toReversed" or "toSorted" or "toSpliced" or "with";
    private sealed class ClassObjectPrototype : JsClassInstance
    {
        internal ClassObjectPrototype()
        {
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
        internal override bool HasInProperty(string key) => HasOwn(key) || IsObjectPrototypeKey(key);
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
            return array.HasElement((uint)key.Value);
        return value.HasInProperty(key.HasValue ? NumberKeyString(key.Value) : "undefined");
    }
}
