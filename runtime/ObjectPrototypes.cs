using System;
using System.Runtime.CompilerServices;

public abstract partial class TsObject
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryExplicitPrototype(out TsObject value)
    {
        if (accessors is not {HasExplicitPrototype: true}) {value = null; return false;}
        value = accessors.ExplicitPrototype; return true;
    }
    internal virtual TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : IsModuleNamespace ? null : Js.DefaultClassObjectPrototype;
    internal virtual bool IsReadOnlyOwnProperty(string key) => false;
    internal virtual bool IsExtensible => true;
    internal bool CanSetPrototype(TsObject value)
    {
        if (ReferenceEquals(PropertyPrototype, value)) return false;
        if (!IsExtensible) throw new InvalidOperationException("Object is not extensible");
        if (IsModuleNamespace || ReferenceEquals(this, Js.DefaultClassObjectPrototype)) throw new InvalidOperationException("Object prototype is immutable");
        for (var ancestor = value; ancestor != null; ancestor = ancestor.PropertyPrototype)
            if (ReferenceEquals(ancestor, this)) throw new InvalidOperationException("Cyclic prototype value");
        return true;
    }
    internal virtual bool TrySetPrototype(TsObject value)
    {
        if (ReferenceEquals(PropertyPrototype, value)) return true;
        if (!IsExtensible) return false;
        if (IsModuleNamespace || ReferenceEquals(this, Js.DefaultClassObjectPrototype)) return false;
        for (var ancestor = value; ancestor != null; ancestor = ancestor.PropertyPrototype)
            if (ReferenceEquals(ancestor, this)) return false;
        SetPrototype(value); return true;
    }
    internal virtual void SetPrototype(TsObject value)
    {
        if (CanSetPrototype(value)) {
            (accessors ??= new JsAccessors()).HasExplicitPrototype = true;
            accessors.ExplicitPrototype = value;
        }
    }
    private object InheritedValue(string key, object receiver)
    {
        var prototype = PropertyPrototype;
        return prototype is null ? Js.Undefined : prototype.GetWithReceiver(key, receiver);
    }
    private bool SetInheritedProperty(string key, object value)
    {
        if (HasOwn(key)) return false;
        for (var prototype = PropertyPrototype; prototype != null; prototype = prototype.PropertyPrototype)
        {
            if (prototype.IsModuleNamespace) throw new InvalidOperationException("Module namespace is read-only");
            if (prototype.TryOwnAccessor(key, out var accessor)) { accessor.Write(this, value); return true; }
            if (prototype.HasOwn(key))
            {
                if (prototype.IsReadOnlyOwnProperty(key)) throw new InvalidOperationException("Inherited property is read-only");
                return false;
            }
        }
        return false;
    }
}

public static partial class Js
{
    internal static bool PrototypeContains(TsObject instance, TsObject target)
    {
        for (var prototype = instance.PropertyPrototype; prototype != null; prototype = prototype.PropertyPrototype)
            if (ReferenceEquals(prototype, target)) return true;
        return false;
    }
    private sealed class PrototypeAccessor(int mode) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Prototype accessor requires an object")), null, mode == 0 ? "get __proto__" : mode == 1 ? "set __proto__" : mode == 2 ? "get constructor" : "set constructor", mode % 2)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            if (mode == 0) return ToObject(receiver).PropertyPrototype;
            RequireObject(receiver);
            if (receiver is not TsObject target) return Undefined;
            if (mode == 2) return GetObjectConstructor();
            object value = FunctionArgument(arguments, 0);
            if (mode == 1) { if (value == null || value is TsObject) target.SetPrototype(value as TsObject); }
            else { _ = GetObjectConstructor(); target.DefineOwn("constructor", value); }
            return Undefined;
        }
    }
}
