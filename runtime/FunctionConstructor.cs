using System;

public static partial class Js
{
    sealed class FunctionConstructor : NativeNamespaceFunction
    {
        internal FunctionConstructor(TsObject prototype) : base("Function", static _ => throw new NotSupportedException("Compiling function source strings at runtime is not supported"), 1)
        {
            UseObjectPrototype(prototype);
        }
        public override object Construct(object[] arguments) => throw new NotSupportedException("Compiling function source strings at runtime is not supported");
        internal override bool CanInitializeDerived => true;
        internal override bool IsReadOnlyOwnProperty(string key) => key == "prototype" || base.IsReadOnlyOwnProperty(key);
    }
}
