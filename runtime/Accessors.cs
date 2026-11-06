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
    internal bool TryOwnAccessor(string key, out JsAccessor accessor)
    {
        accessor = null; return accessors is not null && accessors.TryGetValue(key, out accessor);
    }
    public void DefineAccessor(string key, object function, int mode)
    {
        if (!TryOwnAccessor(key, out var accessor))
        {
            accessor = new JsAccessor(); (accessors ??= new())[key] = accessor;
            (extra ??= new OwnProperties())[key] = Js.Undefined;
        }
        var callable = (JsFunction)function; callable.InferPropertyName((mode == 1 ? "get " : "set ") + key);
        if (mode == 1) accessor.Getter = callable; else accessor.Setter = callable;
    }
    public virtual object GetWithReceiver(string key, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Read(receiver) : Get(key);
    public virtual object SetWithReceiver(string key, object value, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Write(receiver, value) : ((TsObject)receiver).SetOwnValue(key, value);
    public virtual object SetOwnValue(string key, object value)
    {
        if (TryOwnAccessor(key, out _)) throw new InvalidOperationException("Cannot replace accessor through assignment");
        return Set(key, value);
    }
}
public static partial class Js
{
    public static void DefineAccessor(object target, string key, object function, int mode) => ((TsObject)target).DefineAccessor(key, function, mode);
}
