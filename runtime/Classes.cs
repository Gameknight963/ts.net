using System;
using System.Collections.Generic;

public class JsClassInstance : TsObject
{
    internal JsClassFunction Owner;
    HashSet<string> hidden;
    public override bool HasProperty(string key) => HasOwn(key) || Owner is not null && Owner.Prototype.HasProperty(key);
    public override object Get(string key) => HasOwn(key) ? base.Get(key) : Owner is null ? Js.Undefined : Owner.Prototype.Get(key);
    internal void DefineMethod(string name, object value) { DefineOwn(name, value); (hidden ??= new()).Add(name); }
    internal void DefineField(string name, object value) { DefineOwn(name, value); hidden?.Remove(name); }
    public override object OwnKeys()
    {
        if (hidden is null) return base.OwnKeys();
        var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0);
        for (uint i = 0; i < keys.Length(); i++) { object key = keys.ElementAt(i); if (!hidden.Contains((string)key)) result.Append(key); }
        return result;
    }
}
public sealed class JsClassFunction : JsFunction
{
    readonly FunctionBody factory;
    readonly JsFunction fields;
    readonly JsFunction constructor;
    readonly HashSet<string> hidden = new();
    public readonly JsClassInstance Prototype;
    public JsClassFunction(string name, object factory, object fields, object constructor)
        : base((FunctionBody)(static _ => throw new InvalidOperationException("Class constructor requires new")), null, name, constructor is JsFunction fn ? Convert.ToInt32(fn.Get("length")) : 0)
    {
        this.factory = (FunctionBody)factory; this.fields = (JsFunction)fields; this.constructor = constructor as JsFunction;
        Prototype = (JsClassInstance)this.factory(Array.Empty<object>());
        Prototype.DefineMethod("constructor", this);
    }
    public override bool HasOwn(string key) => key == "prototype" || base.HasOwn(key);
    public override object Get(string key) => key == "prototype" ? Prototype : base.Get(key);
    public override object Set(string key, object value) => key == "prototype" ? throw new InvalidOperationException("Class prototype is read-only") : base.Set(key, value);
    internal void DefineMethod(string name, object value, bool isStatic)
    {
        if (isStatic) { DefineOwn(name, value); hidden.Add(name); } else Prototype.DefineMethod(name, value);
    }
    public override object OwnKeys()
    {
        var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0);
        for (uint i = 0; i < keys.Length(); i++) { object key = keys.ElementAt(i); if (!hidden.Contains((string)key)) result.Append(key); }
        return result;
    }
    internal void DefineField(string key, object value) { DefineOwn(key, value); hidden.Remove(key); }
    public override object Construct(object[] arguments)
    {
        var instance = (JsClassInstance)factory(Array.Empty<object>()); instance.Owner = this;
        fields.InvokeWithReceiver(instance, Array.Empty<object>());
        object result = constructor?.InvokeWithReceiver(instance, arguments);
        return result is TsObject ? result : instance;
    }
    public override bool HasInstance(object value) => value is JsClassInstance instance && ReferenceEquals(instance.Owner, this);
}
public static partial class Js
{
    public static object CreateClass(string name, object factory, object fields, object constructor) => new JsClassFunction(name, factory, fields, constructor);
    public static void DefineClassMethod(object target, string key, object method, bool isStatic)
    {
        if (method is JsFunction fn) fn.InferPropertyName(key);
        ((JsClassFunction)target).DefineMethod(key, method, isStatic);
    }
    public static void InitializeClass(object target, object fields) => ((JsFunction)fields).InvokeWithReceiver(target, Array.Empty<object>());
    public static void DefineClassField(object target, string key, object value) { if (target is JsClassFunction fn) fn.DefineField(key, value); else if (target is JsClassInstance instance) instance.DefineField(key, value); else ((TsObject)target).DefineOwn(key, value); }
}
