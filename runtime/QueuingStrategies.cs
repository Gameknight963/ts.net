using System;

public static partial class Js
{
    sealed class QueuingStrategyValue(double highWaterMark, bool bytes, JsFunction constructor) : JsDescriptorObject
    {
        internal readonly double HighWaterMark = highWaterMark;
        internal readonly bool Bytes = bytes;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)constructor.Get("prototype");
    }
    sealed class QueuingStrategyConstructor : NativeNamespaceFunction
    {
        readonly bool bytes;
        internal QueuingStrategyConstructor(bool bytes) : base(bytes ? "ByteLengthQueuingStrategy" : "CountQueuingStrategy",static _ => throw new InvalidOperationException("Queuing strategy requires new"),1)
        {
            this.bytes = bytes;
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};
            UseObjectPrototype(prototype);prototype.DefineMethod("constructor",this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol),bytes ? "ByteLengthQueuingStrategy" : "CountQueuingStrategy",false);
            var size = new JsFunction((FunctionBody)(args => bytes ? Js.Get(FunctionArgument(args,0),"byteLength") : 1d),null,"size",bytes ? 1 : 0);
            QueuingStrategyValue Require(object receiver) => receiver is QueuingStrategyValue value && value.Bytes == bytes ? value : throw new InvalidOperationException("Queuing strategy receiver required");
            prototype.DefineAccessor("highWaterMark",new CollectionMethod("get highWaterMark",0,(receiver,_) => BoxNumber(Require(receiver).HighWaterMark)),1);
            prototype.DefineAccessor("size",new CollectionMethod("get size",0,(receiver,_) => {Require(receiver);return size;}),1);
        }
        public override object Construct(object[] args)
        {
            if (FunctionArgument(args,0) is not TsObject options) throw new InvalidOperationException("Queuing strategy options are required");
            object mark = options.Get("highWaterMark");
            if (IsUndefined(mark)) throw new InvalidOperationException("highWaterMark is required");
            // Constructors retain unrestricted doubles. A stream validates the
            // mark when it consumes the strategy, so NaN and negatives survive here.
            return new QueuingStrategyValue(Number(mark),bytes,this);
        }
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);
            var result = (TsObject)Construct(args);result.SetPrototype(prototype);return result;
        }
    }
    static class QueuingStrategies
    {
        internal static readonly JsFunction Count = new QueuingStrategyConstructor(false);
        internal static readonly JsFunction Bytes = new QueuingStrategyConstructor(true);
    }
}
