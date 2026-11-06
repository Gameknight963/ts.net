using System;

public partial class JsFunction
{
    private static readonly object UncreatedPrototype = new();
    private static readonly object NullPrototypeValue = new();
    private object ordinaryPrototype;
    internal FunctionBody constructFactory;
    internal bool sourceConstructor;
    internal virtual bool CanInitializeDerived => sourceConstructor;
    internal virtual object InitializeDerived(object instance, object[] arguments)
    {
        if (!CanInitializeDerived) throw new InvalidOperationException("Base constructor is unsupported");
        object returned = InvokeWithReceiver(instance, arguments);
        return returned is TsObject ? returned : instance;
    }
    internal void MarkOrdinaryPrototype() => ordinaryPrototype = UncreatedPrototype;
    internal void UseObjectPrototype(TsObject prototype) => ordinaryPrototype = prototype;
    private object ConstructOrdinary(object[] arguments)
    {
        if (constructFactory == null) throw new InvalidOperationException("Value is not a constructor");
        var instance = (TsObject)constructFactory(Array.Empty<object>());
        instance.SetPrototype(ReadOrdinaryPrototype() as TsObject ?? Js.DefaultClassObjectPrototype);
        object returned = InvokeWithReceiver(instance, arguments);
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
        internal FunctionPrototypeObject(JsFunction owner) => DefineOwn("constructor", owner);
        public override object OwnKeys()
        {
            var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0);
            for (uint i = 0; i < keys.Length(); i++) if ((string)keys.ElementAt(i) != "constructor") result.Append(keys.ElementAt(i));
            return result;
        }
    }
}

public static partial class Js
{
    public static object MarkFunctionPrototype(object value) { var fn = (JsFunction)value; fn.MarkOrdinaryPrototype(); fn.sourceConstructor = true; return value; }
    public static object MarkFunctionPrototype(object value, object factory) { var fn = (JsFunction)value; fn.MarkOrdinaryPrototype(); fn.sourceConstructor = true; fn.constructFactory = (FunctionBody)factory; return value; }
}
