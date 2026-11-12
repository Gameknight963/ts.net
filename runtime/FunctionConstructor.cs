using System;

public static partial class Js
{
    sealed class EmptyConstructedFunction : JsFunction
    {
        internal EmptyConstructedFunction() : base((FunctionBody)(static _ => Undefined), null, "anonymous", 0)
        {
            sourceConstructor = true;
            MarkOrdinaryPrototype();
        }
        internal override string SourceText => "function anonymous(\n) {\n\n}";
    }
    static object CreateEmptyFunction(object[] arguments)
    {
        if (arguments.Length != 0) throw new NotSupportedException("Compiling function source strings at runtime is not supported");
        return new EmptyConstructedFunction();
    }
    sealed class FunctionConstructor : NativeNamespaceFunction
    {
        internal FunctionConstructor(TsObject prototype) : base("Function", CreateEmptyFunction, 1)
        {
            UseObjectPrototype(prototype);
        }
        public override object Construct(object[] arguments) => CreateEmptyFunction(arguments);
        internal override object InitializeDerived(object instance, object[] arguments)
        {
            var result = (TsObject)Construct(arguments);
            result.SetPrototype(((TsObject)instance).PropertyPrototype); return result;
        }
        internal override object InitializeDerived(object instance, object[] arguments, JsFunction newTarget)
        {
            var result = (TsObject)Construct(arguments);
            result.SetPrototype(ConstructionPrototype(newTarget)); return result;
        }
        internal override bool CanInitializeDerived => true;
        internal override bool IsReadOnlyOwnProperty(string key) => key == "prototype" || base.IsReadOnlyOwnProperty(key);
    }
}
