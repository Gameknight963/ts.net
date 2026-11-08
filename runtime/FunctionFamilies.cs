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
            var constructor = new FunctionFamilyConstructor(name, Prototype);
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
    sealed class FunctionFamilyConstructor : NativeNamespaceFunction
    {
        internal FunctionFamilyConstructor(string name, TsObject prototype) : base(name, static _ => throw new NotSupportedException("Compiling function source strings at runtime is not supported"), 1)
        {
            UseObjectPrototype(prototype);
        }
        public override object Construct(object[] arguments) => throw new NotSupportedException("Compiling function source strings at runtime is not supported");
        internal override bool CanInitializeDerived => true;
        internal override bool IsReadOnlyOwnProperty(string key) => key == "prototype" || base.IsReadOnlyOwnProperty(key);
    }
    static class AsyncFunctionFamily {internal static readonly FunctionFamily Value = new("AsyncFunction", null);}
    static class GeneratorFunctionFamily {internal static readonly FunctionFamily Value = new("GeneratorFunction", false);}
    static class AsyncGeneratorFunctionFamily {internal static readonly FunctionFamily Value = new("AsyncGeneratorFunction", true);}
}
