using System;

public static partial class Js
{
    static class FunctionPrototypeState
    {
        internal static readonly FunctionPrototypeValue Prototype = new();
        internal static readonly JsFunction Constructor = new FunctionConstructor(Prototype);
        internal static readonly TsObject Value = Initialize();
        static TsObject Initialize()
        {
            Prototype.DefineMethod("constructor", Constructor);
            return Prototype;
        }
    }
    internal static TsObject SharedFunctionPrototype => FunctionPrototypeState.Value;
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
