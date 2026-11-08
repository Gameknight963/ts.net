using System;

public static partial class Js
{
    sealed class BufferedReaderConstructor : NativeNamespaceFunction
    {
        internal BufferedReaderConstructor(TsObject prototype) : base("ReadableStreamDefaultReader", static _ => throw new InvalidOperationException("ReadableStreamDefaultReader requires new"), 1)
        {
            UseObjectPrototype(prototype);
        }
        public override object Construct(object[] args) => GetDefaultStreamReader(FunctionArgument(args, 0), Undefined);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args)
        {
            var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result;
        }
        internal override bool IsReadOnlyOwnProperty(string key) => key == "prototype" || base.IsReadOnlyOwnProperty(key);
    }
}
