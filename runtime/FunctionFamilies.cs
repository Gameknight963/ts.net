using System;

public static partial class Js
{
    sealed class FunctionFamily
    {
        internal readonly JsClassInstance Prototype;
        internal readonly TsObject IteratorPrototype;
        internal FunctionFamily(string name, bool? asynchronous)
        {
            Prototype = new JsClassInstance {ParentPrototype = SharedFunctionPrototype};
            var constructor = new FunctionFamilyConstructor(name, Prototype, asynchronous);
            constructor.SetPrototype(FunctionPrototypeState.Constructor);
            Prototype.DefineMethod("constructor", constructor, false);
            Prototype.DefineMethod(PropertyKey(ToStringTagSymbol), name, false);
            if (asynchronous.HasValue) {
                IteratorPrototype = asynchronous.Value ? AsyncGeneratorPrototype : GeneratorPrototype;
                Prototype.DefineMethod("prototype", IteratorPrototype, false);
                ((JsDescriptorObject)IteratorPrototype).DefineMethod("constructor", Prototype, false);
            }
        }
    }
    sealed class EmptyFamilyBody(bool? asynchronous) : JsFunction((FunctionBody)(static _ => Undefined), null, "anonymous", 0)
    {
        static readonly JsFunction GeneratorBody = new((FunctionBody)(static _ => Undefined), null, "", 0);
        internal override string SourceText => (asynchronous.HasValue ? asynchronous.Value ? "async function*" : "function*" : "async function") + " anonymous(\n) {\n\n}";
        public override object InvokeWithReceiver(object receiver, object[] arguments) => asynchronous.HasValue
            ? new GeneratorValue(GeneratorBody, asynchronous.Value) : Undefined;
    }
    static object CreateEmptyFamilyFunction(object[] arguments, bool? asynchronous)
    {
        if (arguments.Length != 0) throw new NotSupportedException("Compiling function source strings at runtime is not supported");
        var body = new EmptyFamilyBody(asynchronous);
        return asynchronous.HasValue ? new GeneratorFunctionObject(body, asynchronous.Value) : new AsyncFunctionObject(body);
    }
    sealed class FunctionFamilyConstructor : NativeNamespaceFunction
    {
        readonly bool? asynchronous;
        internal FunctionFamilyConstructor(string name, TsObject prototype, bool? asynchronous) : base(name, arguments => CreateEmptyFamilyFunction(arguments, asynchronous), 1)
        {
            this.asynchronous = asynchronous;
            UseObjectPrototype(prototype);
        }
        public override object Construct(object[] arguments) => CreateEmptyFamilyFunction(arguments, asynchronous);
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
    static class AsyncFunctionFamily {internal static readonly FunctionFamily Value = new("AsyncFunction", null);}
    static class GeneratorFunctionFamily {internal static readonly FunctionFamily Value = new("GeneratorFunction", false);}
    static class AsyncGeneratorFunctionFamily {internal static readonly FunctionFamily Value = new("AsyncGeneratorFunction", true);}
}
