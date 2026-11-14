using System;
using System.Runtime.CompilerServices;

public static partial class Js
{
    static class FunctionPrototypeState
    {
        internal static readonly FunctionPrototypeValue Prototype = new();
        internal static readonly JsFunction Constructor = new FunctionConstructor(Prototype);
        internal static readonly TsObject Value = Initialize();
        internal static readonly object CallAccessorStructure = Prototype.WatchAccessorStructure();
        static TsObject Initialize()
        {
            Prototype.DefineMethod("constructor", Constructor);
            return Prototype;
        }
    }
    internal static TsObject SharedFunctionPrototype => FunctionPrototypeState.Value;
    public static object GetFunctionCallProperty(object receiver) => receiver is JsFunction function
        ? function.GetCallProperty() : Get(receiver, "call");
    // Called only by generated function classes, which inherit ordinary property lookup.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object ReadFunctionCallProperty(object receiver)
    {
        var function = (JsFunction)receiver;
        if (!function.HasPropertyOverrides) {
            var prototype = FunctionPrototypeState.Prototype;
            if (ReferenceEquals(function.PropertyPrototype, prototype) &&
                ReferenceEquals(prototype.WatchAccessorStructure(), FunctionPrototypeState.CallAccessorStructure) &&
                prototype.TryReadOwnDataSlot("call", out var target)) return target;
        }
        return function.Get("call");
    }
    static class StrictFunctionAccess
    {
        internal static readonly JsFunction Thrower = Create();
        static JsFunction Create()
        {
            var result = new JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Restricted function property cannot be accessed")), null, "", 0);
            result.Define(new("name", "", null, null, false, false, false, false));
            result.Define(new("length", 0d, null, null, false, false, false, false));
            result.PreventExtensions();
            return result;
        }
    }
    internal static JsFunction StrictFunctionThrower => StrictFunctionAccess.Thrower;
    static class OrdinaryInstanceCheck
    {
        internal static readonly JsFunction Method = new CollectionMethod("[Symbol.hasInstance]", 1,
            (receiver, args) => BoxBoolean(receiver is JsFunction function && function.HasInstance(FunctionArgument(args, 0))));
    }
    sealed class FunctionPrototypeValue : JsFunction
    {
        internal FunctionPrototypeValue() : base((FunctionBody)(static _ => Undefined), null, "", 0)
        {
            foreach (string key in new[] {"call", "apply", "bind", "toString"})
                DefineMethod(key, key == "toString" ? new FunctionStringFunction() : FunctionMethod(key));
            foreach (string key in new[] {"caller", "arguments"})
                Define(new(key, Undefined,
                    new CollectionMethod("get " + key, 0, static (_, _) => throw new InvalidOperationException("Restricted function property cannot be accessed")),
                    new CollectionMethod("set " + key, 1, static (_, _) => throw new InvalidOperationException("Restricted function property cannot be accessed")),
                    true, false, false, true));
            Define(new(PropertyKey(HasInstanceSymbol), OrdinaryInstanceCheck.Method, null, null, false, false, false, false));
        }
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : DefaultClassObjectPrototype;
    }
}

public abstract partial class TsObject
{
    // The caller must guard the accessor structure before reading stored data.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryReadOwnDataSlot(string key, out object value)
    {
        value = null;
        return extra != null && extra.TryGetValue(key, out value);
    }
}
