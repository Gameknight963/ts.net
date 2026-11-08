using System;
using System.Collections.Generic;

internal sealed class JsAccessor
{
    internal JsFunction Getter;
    internal JsFunction Setter;
    internal object Read(object receiver) => Getter is null ? Js.Undefined : Getter.InvokeWithReceiver(receiver, Array.Empty<object>());
    internal object Write(object receiver, object value)
    {
        if (Setter is null) throw new InvalidOperationException("Property has no setter");
        Setter.InvokeWithReceiver(receiver, new[] { value }); return value;
    }
}
public abstract partial class TsObject
{
    Dictionary<string, JsAccessor> accessors;
    internal bool HasPropertyOverrides => extra != null || accessors != null;
    internal bool TryOwnAccessor(string key, out JsAccessor accessor)
    {
        accessor = null; return accessors is not null && accessors.TryGetValue(key, out accessor);
    }
    public void DefineAccessor(string key, object function, int mode) => DefineAccessor(key, function, mode, true);
    internal void DefineAccessor(string key, object function, int mode, bool inferName)
    {
        if (!IsExtensible && !HasOwn(key)) throw new InvalidOperationException("Object is not extensible");
        if (!TryOwnAccessor(key, out var accessor))
        {
            accessor = new JsAccessor(); (accessors ??= new())[key] = accessor;
            (extra ??= new OwnProperties())[key] = Js.Undefined;
        }
        var callable = (JsFunction)function;
        if (inferName) callable.InferPropertyName((mode == 1 ? "get " : "set ") + Js.PropertyFunctionName(key));
        if (mode == 1) accessor.Getter = callable; else accessor.Setter = callable;
    }
    public virtual object GetWithReceiver(string key, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Read(receiver) : HasOwn(key) ? Get(key) : InheritedValue(key, receiver);
    public virtual object SetWithReceiver(string key, object value, object receiver)
    {
        if (IsModuleNamespace) throw new InvalidOperationException("Module namespace is read-only");
        if (TryOwnAccessor(key, out var accessor)) return accessor.Write(receiver, value);
        bool own = HasOwn(key);
        if (!own && PropertyPrototype is TsObject prototype) return prototype.SetWithReceiver(key, value, receiver);
        if (own && IsReadOnlyOwnProperty(key)) throw new InvalidOperationException("Inherited property is read-only");
        return ((TsObject)receiver).SetOwnValue(key, value);
    }
    public virtual object SetOwnValue(string key, object value)
    {
        if (TryOwnAccessor(key, out _)) throw new InvalidOperationException("Cannot replace accessor through assignment");
        if (IsModuleNamespace || IsReadOnlyOwnProperty(key)) throw new InvalidOperationException("Property is read-only");
        DefineOwn(key, value);
        return value;
    }
}
public static partial class Js
{
    public static void DefineAccessor(object target, string key, object function, int mode) => ((TsObject)target).DefineAccessor(key, function, mode);
}
