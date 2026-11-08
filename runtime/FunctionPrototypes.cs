using System;

public partial class JsFunction
{
    private static readonly object UncreatedPrototype = new();
    private static readonly object NullPrototypeValue = new();
    private object ordinaryPrototype;
    internal FunctionBody constructFactory;
    internal bool sourceConstructor;
    internal bool UsesNewTarget;
    internal object ConstructionReceiver(object receiver, object target) => UsesNewTarget ? new JsConstructionReceiver(receiver, target) : receiver;
    internal virtual bool CanInitializeDerived => sourceConstructor;
    internal virtual object InitializeDerived(object instance, object[] arguments)
    {
        if (!CanInitializeDerived) throw new InvalidOperationException("Base constructor is unsupported");
        object returned = InvokeWithReceiver(ConstructionReceiver(instance, ((JsClassInstance)instance).Owner), arguments);
        return returned is TsObject ? returned : instance;
    }
    internal virtual object InitializeDerived(object instance, object[] arguments, JsFunction newTarget)
    {
        ((TsObject)instance).SetPrototype(ConstructionPrototype(newTarget));
        if (!sourceConstructor) return InitializeDerived(instance, arguments);
        object returned = InvokeWithReceiver(ConstructionReceiver(instance, newTarget), arguments);
        return returned is TsObject ? returned : instance;
    }
    internal virtual object ConstructWithNewTarget(object[] arguments, JsFunction newTarget)
    {
        if (ReferenceEquals(this, newTarget)) return Construct(arguments);
        if (!CanInitializeDerived) throw new InvalidOperationException("Value is not a constructor");
        TsObject instance = this is JsClassFunction klass ? klass.Allocate() : constructFactory != null ? (TsObject)constructFactory(Array.Empty<object>()) : new JsDynamicObject();
        return InitializeDerived(instance, arguments, newTarget);
    }
    internal TsObject ConstructionPrototype(JsFunction newTarget)
    {
        if (newTarget.Get("prototype") is TsObject prototype) return prototype;
        return sourceConstructor || this is JsClassFunction ? Js.DefaultClassObjectPrototype : Get("prototype") as TsObject ?? Js.DefaultClassObjectPrototype;
    }
    internal void MarkOrdinaryPrototype() => ordinaryPrototype = UncreatedPrototype;
    internal void UseObjectPrototype(TsObject prototype) => ordinaryPrototype = prototype;
    private object ConstructOrdinary(object[] arguments)
    {
        if (!sourceConstructor) throw new InvalidOperationException("Value is not a constructor");
        var instance = constructFactory != null ? (TsObject)constructFactory(Array.Empty<object>()) : new JsDynamicObject();
        instance.SetPrototype(ReadOrdinaryPrototype() as TsObject ?? Js.DefaultClassObjectPrototype);
        object returned = InvokeWithReceiver(ConstructionReceiver(instance, this), arguments);
        return returned is TsObject ? returned : instance;
    }
    private bool OrdinaryHasInstance(object value)
    {
        if (value is not TsObject instance) return false;
        object prototype = Get("prototype");
        if (prototype is not TsObject target) throw new InvalidOperationException("Function has no object prototype");
        return Js.PrototypeContains(instance, target);
    }
    private object ReadOrdinaryPrototype()
    {
        if (ReferenceEquals(ordinaryPrototype, UncreatedPrototype)) ordinaryPrototype = new FunctionPrototypeObject(this);
        return ReferenceEquals(ordinaryPrototype, NullPrototypeValue) ? null : ordinaryPrototype;
    }
    private sealed class FunctionPrototypeObject : JsDynamicObject
    {
        internal FunctionPrototypeObject(JsFunction owner) => DefineMethod("constructor", owner);
    }

}

public static partial class Js
{
    public static object MarkFunctionPrototype(object value) { var fn = (JsFunction)value; fn.MarkOrdinaryPrototype(); fn.sourceConstructor = true; return value; }
    public static object MarkFunctionPrototype(object value, object factory) { var fn = (JsFunction)value; fn.MarkOrdinaryPrototype(); fn.sourceConstructor = true; fn.constructFactory = (FunctionBody)factory; return value; }
}
