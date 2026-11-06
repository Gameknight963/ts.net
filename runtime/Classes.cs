using System;
using System.Collections.Generic;

public class JsClassInstance : TsObject
{
    internal JsClassFunction Owner;
    internal JsClassInstance ParentPrototype;
    internal JsClassInstance InheritedPrototype => Owner is null ? ParentPrototype : Owner.Prototype;
    HashSet<string> hidden;
    public override bool HasProperty(string key) => HasOwn(key) || InheritedPrototype is not null && InheritedPrototype.HasProperty(key);
    public override object Get(string key) => GetWithReceiver(key, this);
    public override object GetWithReceiver(string key, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Read(receiver) : HasOwn(key) ? base.Get(key) : InheritedPrototype is null ? Js.Undefined : InheritedPrototype.GetWithReceiver(key, receiver);
    public override object Set(string key, object value) => SetWithReceiver(key, value, this);
    public override object SetWithReceiver(string key, object value, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Write(receiver, value) : HasOwn(key) || InheritedPrototype is null ? ((TsObject)receiver).SetOwnValue(key, value) : InheritedPrototype.SetWithReceiver(key, value, receiver);
    public override object SetOwnValue(string key, object value) { if (TryOwnAccessor(key, out _)) throw new InvalidOperationException("Cannot replace accessor through assignment"); return base.Set(key, value); }
    internal void DefineClassAccessor(string key, object function, int mode) { DefineAccessor(key, function, mode); (hidden ??= new()).Add(key); }
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
    internal readonly JsClassFunction Parent;
    readonly bool derived;
    public JsClassFunction(string name, object factory, object fields, object constructor, object parent, bool derived)
        : base((FunctionBody)(static _ => throw new InvalidOperationException("Class constructor requires new")), null, name, constructor is JsFunction fn ? Convert.ToInt32(fn.Get("length")) : 0)
    {
        this.derived = derived;
        Parent = parent as JsClassFunction;
        if (derived && parent is not null && Parent is null) throw new InvalidOperationException("Base class must be a compiled class");
        this.factory = (FunctionBody)factory; this.fields = (JsFunction)fields; this.constructor = constructor as JsFunction;
        Prototype = (JsClassInstance)this.factory(Array.Empty<object>());
        Prototype.ParentPrototype = Parent?.Prototype;
        Prototype.DefineMethod("constructor", this);
    }
    public override bool HasOwn(string key) => key == "prototype" || base.HasOwn(key);
    public override bool HasProperty(string key) => HasOwn(key) || Parent is not null && Parent.HasProperty(key);
    public override object Get(string key) => GetWithReceiver(key, this);
    public override object GetWithReceiver(string key, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Read(receiver) : key == "prototype" ? Prototype : HasOwn(key) ? base.Get(key) : Parent is null ? Js.Undefined : Parent.GetWithReceiver(key, receiver);
    public override object Set(string key, object value) => SetWithReceiver(key, value, this);
    public override object SetWithReceiver(string key, object value, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Write(receiver, value) : HasOwn(key) || Parent is null ? ((TsObject)receiver).SetOwnValue(key, value) : Parent.SetWithReceiver(key, value, receiver);
    public override object SetOwnValue(string key, object value)
    {
        if (key == "prototype") throw new InvalidOperationException("Class prototype is read-only");
        if (TryOwnAccessor(key, out _)) throw new InvalidOperationException("Cannot replace accessor through assignment"); return base.Set(key, value);
    }
    internal void DefineClassAccessor(string key, object function, int mode) { if (key == "prototype") throw new InvalidOperationException("Cannot redefine class prototype"); DefineAccessor(key, function, mode); hidden.Add(key); }
    internal void DefineMethod(string name, object value, bool isStatic)
    {
        if (isStatic) { if (name == "prototype") throw new InvalidOperationException("Cannot redefine class prototype"); DefineOwn(name, value); hidden.Add(name); } else Prototype.DefineMethod(name, value);
    }
    public override object OwnKeys()
    {
        var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0);
        for (uint i = 0; i < keys.Length(); i++) { object key = keys.ElementAt(i); if (!hidden.Contains((string)key)) result.Append(key); }
        return result;
    }
    internal void DefineField(string key, object value) { DefineOwn(key, value); hidden.Remove(key); }
    internal void InitializeFields(object instance) => fields.InvokeWithReceiver(instance, Array.Empty<object>());
    internal object Initialize(object instance, object[] arguments)
    {
        if (!derived)
        {
            InitializeFields(instance);
            object result = constructor?.InvokeWithReceiver(instance, arguments);
            return result is TsObject ? result : instance;
        }
        if (constructor is null)
        {
            if (Parent is null) throw new InvalidOperationException("Super constructor is not callable");
            object result = Parent.Initialize(instance, arguments); InitializeFields(result); return result;
        }
        var frame = new JsConstructorFrame(this, instance);
        object returned = constructor.InvokeWithReceiver(frame, arguments);
        if (returned is TsObject) return returned;
        if (!Js.IsUndefined(returned)) throw new InvalidOperationException("Derived constructor may only return an object or undefined");
        return Js.ResolveThis(frame);
    }
    internal JsClassInstance Allocate()
    {
        var instance = (JsClassInstance)factory(Array.Empty<object>()); instance.Owner = this; return instance;
    }
    public override object Construct(object[] arguments) => Initialize(Allocate(), arguments);
    public override bool HasInstance(object value)
    {
        if (value is not JsClassInstance instance) return false;
        for (var prototype = instance.InheritedPrototype; prototype is not null; prototype = prototype.InheritedPrototype)
            if (ReferenceEquals(prototype, Prototype)) return true;
        return false;
    }

}
public static partial class Js
{
    public static object CreateClass(string name, object factory, object fields, object constructor, object parent, bool derived) => new JsClassFunction(name, factory, fields, constructor, parent, derived);
    public static void DefineClassMethod(object target, string key, object method, bool isStatic, int mode)
    {
        var home = (JsClassFunction)target;
        if (mode != 0) { if (isStatic) home.DefineClassAccessor(key, method, mode); else home.Prototype.DefineClassAccessor(key, method, mode); }
        else { if (method is JsFunction fn) fn.InferPropertyName(key); home.DefineMethod(key, method, isStatic); }
    }
    public static void InitializeClass(object target, object fields) => ((JsFunction)fields).InvokeWithReceiver(target, Array.Empty<object>());
    public static void DefineClassField(object target, string key, object value) { target = ResolveThis(target); if (target is JsClassFunction fn) fn.DefineField(key, value); else if (target is JsClassInstance instance) instance.DefineField(key, value); else ((TsObject)target).DefineOwn(key, value); }
}
