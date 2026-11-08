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
            Define(new(PropertyKey(HasInstanceSymbol), OrdinaryInstanceCheck.Method, null, null, false, false, false, false));
        }
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : DefaultClassObjectPrototype;
    }
}
