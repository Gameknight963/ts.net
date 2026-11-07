using System;
using System.Collections.Generic;

public class JsClassInstance : TsObject
{
    internal JsClassFunction Owner;
    internal TsObject ParentPrototype;
    internal TsObject InheritedPrototype => TryExplicitPrototype(out var value) ? value : Owner is null ? ParentPrototype : Owner.Prototype;
    internal override TsObject PropertyPrototype => InheritedPrototype;
    HashSet<string> hidden;
    internal override object ForInOwnKeys() => base.OwnKeys();
    internal override bool ForInEnumerable(string key) => hidden == null || !hidden.Contains(key);
    public override bool DeleteOwn(string key) { bool removed = base.DeleteOwn(key); if (removed) hidden?.Remove(key); return removed; }
    internal override bool HasInProperty(string key) => HasOwn(key) || InheritedPrototype is not null && InheritedPrototype.HasInProperty(key);
    public override bool HasProperty(string key) => HasOwn(key) || InheritedPrototype is not null && InheritedPrototype.HasProperty(key);
    public override object Get(string key) => GetWithReceiver(key, this);
    public override object GetWithReceiver(string key, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Read(receiver) : HasOwn(key) ? base.Get(key) : InheritedPrototype is null ? Js.Undefined : InheritedPrototype.GetWithReceiver(key, receiver);
    public override object Set(string key, object value) => SetWithReceiver(key, value, this);
    public override object SetWithReceiver(string key, object value, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Write(receiver, value) : HasOwn(key) || InheritedPrototype is null ? ((TsObject)receiver).SetOwnValue(key, value) : InheritedPrototype.SetWithReceiver(key, value, receiver);
    public override object SetOwnValue(string key, object value) { if (TryOwnAccessor(key, out _)) throw Js.MakeThrown(new JsCaughtError("TypeError", "Cannot replace accessor through assignment")); DefineOwn(key, value); return value; }
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
    internal override string SourceText => Js.SourceOf(factory);
    readonly JsFunction fields;
    readonly JsFunction constructor;
    readonly HashSet<string> hidden = new();
    internal override object ForInOwnKeys() => base.ForInOwnKeys();
    internal override bool ForInEnumerable(string key) => key != "prototype" && !hidden.Contains(key) && base.ForInEnumerable(key);
    public readonly JsClassInstance Prototype;
    internal readonly JsFunction Parent;
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : Parent ?? base.PropertyPrototype;
    readonly bool derived;
    public JsClassFunction(string name, object factory, object fields, object constructor, object parent, bool derived)
        : base((FunctionBody)(static _ => throw new InvalidOperationException("Class constructor requires new")), null, name, constructor is JsFunction fn ? Convert.ToInt32(fn.Get("length")) : 0)
    {
        this.derived = derived;
        Parent = parent as JsFunction;
        if (derived && parent is not null && (Parent is null || !Parent.CanInitializeDerived)) throw Js.MakeThrown(new JsCaughtError("TypeError", "Base class must be a supported constructor"));
        object parentPrototype = Parent?.Get("prototype");
        if (Parent is not null && parentPrototype is not null && parentPrototype is not TsObject) throw Js.MakeThrown(new JsCaughtError("TypeError", "Base constructor prototype must be an object or null"));
        this.factory = (FunctionBody)factory; this.fields = (JsFunction)fields; this.constructor = constructor as JsFunction;
        Prototype = (JsClassInstance)this.factory(Array.Empty<object>());
        Prototype.ParentPrototype = Parent is not null ? (TsObject)parentPrototype : derived ? null : Js.DefaultClassObjectPrototype;
        Prototype.DefineMethod("constructor", this);
    }
    public override bool DeleteOwn(string key) { bool removed = base.DeleteOwn(key); if (removed) hidden.Remove(key); return removed; }
    internal override bool IsReadOnlyOwnProperty(string key) => key == "prototype" || base.IsReadOnlyOwnProperty(key);
    public override bool HasOwn(string key) => key == "prototype" || base.HasOwn(key);
    public override bool HasProperty(string key) => HasOwn(key) || (Parent is not null ? Parent.HasProperty(key):base.HasProperty(key));
    public override object Get(string key) => GetWithReceiver(key, this);
    public override object GetWithReceiver(string key, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Read(receiver) : key == "prototype" ? Prototype : HasOwn(key) ? base.Get(key) : Parent is null ? base.Get(key) : Parent.GetWithReceiver(key, receiver);
    public override object Set(string key, object value) => SetWithReceiver(key, value, this);
    public override object SetWithReceiver(string key, object value, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Write(receiver, value) : HasOwn(key) ? ((TsObject)receiver).SetOwnValue(key, value) : Parent is null ? base.SetWithReceiver(key, value, receiver) : Parent.SetWithReceiver(key, value, receiver);
    public override object SetOwnValue(string key, object value)
    {
        if (key == "prototype") throw new InvalidOperationException("Class prototype is read-only");
        if (TryOwnAccessor(key, out _)) throw Js.MakeThrown(new JsCaughtError("TypeError", "Cannot replace accessor through assignment"));
        if (HasOwn(key) && IsReadOnlyOwnProperty(key)) throw new InvalidOperationException("Property is read-only");
        DefineOwn(key, value); return value;
    }
    internal void DefineClassAccessor(string key, object function, int mode) { if (key == "prototype") throw Js.MakeThrown(new JsCaughtError("TypeError", "Cannot redefine class prototype")); ReplaceIntrinsicMetadata(key); DefineAccessor(key, function, mode); hidden.Add(key); }
    internal void DefineMethod(string name, object value, bool isStatic)
    {
        if (isStatic) { if (name == "prototype") throw Js.MakeThrown(new JsCaughtError("TypeError", "Cannot redefine class prototype")); ReplaceIntrinsicMetadata(name); DefineOwn(name, value); hidden.Add(name); } else Prototype.DefineMethod(name, value);
    }
    public override object OwnKeys()
    {
        var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0);
        for (uint i = 0; i < keys.Length(); i++) { object key = keys.ElementAt(i); if (!hidden.Contains((string)key)) result.Append(key); }
        return result;
    }
    internal void DefineField(string key, object value) { if (key == "prototype") throw Js.MakeThrown(new JsCaughtError("TypeError", "Cannot redefine class prototype")); ReplaceIntrinsicMetadata(key); DefineOwn(key, value); hidden.Remove(key); }
    internal void InitializeFields(object instance) => fields.InvokeWithReceiver(instance, Array.Empty<object>());
    internal object Initialize(object instance, object[] arguments) => Initialize(instance, arguments, ((JsClassInstance)instance).Owner);
    internal object Initialize(object instance, object[] arguments, JsFunction newTarget)
    {
        if (!derived)
        {
            ((TsObject)instance).SetPrototype(ConstructionPrototype(newTarget));
            InitializeFields(instance);
            object result = constructor?.InvokeWithReceiver(constructor.ConstructionReceiver(instance, newTarget), arguments);
            return result is TsObject ? result : instance;
        }
        if (constructor is null)
        {
            if (Parent is null) throw new InvalidOperationException("Super constructor is not callable");
            object result = Parent.InitializeDerived(instance, arguments, newTarget); InitializeFields(result); return result;
        }
        var frame = new JsConstructorFrame(this, instance, newTarget);
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
    internal override bool CanInitializeDerived => true;
    internal override object InitializeDerived(object instance, object[] arguments) => Initialize(instance, arguments);
    internal override object InitializeDerived(object instance, object[] arguments, JsFunction newTarget) => Initialize(instance, arguments, newTarget);
    public override bool HasInstance(object value) => value is TsObject instance && Js.PrototypeContains(instance, Prototype);


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
    public static void DefineClassField(object target, string key, object value) { target = ResolveThis(target); if (target is JsTemplateArray) throw MakeThrown(new JsCaughtError("TypeError", "Template strings arrays are frozen")); if (target is JsRawJson) throw MakeThrown(new JsCaughtError("TypeError", "Cannot define a field on raw JSON")); if (target is JsClassFunction fn) fn.DefineField(key, value); else if (target is JsClassInstance instance) instance.DefineField(key, value); else { if (target is JsBoxedPrimitive boxed && boxed.IsReadOnlyOwnProperty(key)) throw MakeThrown(new JsCaughtError("TypeError", "Cannot redefine a string property")); ((TsObject)target).DefineOwn(key, value); } }
}
