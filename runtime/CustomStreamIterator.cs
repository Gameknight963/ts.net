using System;

public static partial class Js
{
    sealed class CustomStreamIterator(CustomStreamReader reader, bool preventCancel) : JsDescriptorObject
    {
        bool done;
        JsPromise tail;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : CustomIteratorMethods.Prototype;
        JsPromise Sequence(Func<JsPromise> operation)
        {
            var previous = tail;var result = tail = new JsPromise();
            void Run()
            {
                try {ObserveGeneratorAwait(operation(),(value,rejected) => result.Settle(value,rejected));}
                catch (Exception exception) {result.Settle(CaughtValue(exception),true);}
            }
            if (previous == null) Run();else ObserveGeneratorAwait(previous,(_,_) => Run());
            return result;
        }
        internal JsPromise Next() => Sequence(() => {
            if (done) return BodyOperation(() => IteratorResult(Undefined,true));
            var result = new JsPromise();
            ObserveGeneratorAwait(reader.Read(),(value,rejected) => {
                if (rejected || Truthy(((TsObject)value).Get("done"))) {done = true;reader.Release();}
                result.Settle(value,rejected);
            });
            return result;
        });
        internal JsPromise Return(object value) => Sequence(() => {
            if (done) return BodyOperation(() => IteratorResult(value,true));
            done = true;
            if (preventCancel) {reader.Release();return BodyOperation(() => IteratorResult(value,true));}
            var result = new JsPromise();
            var cancellation = reader.Cancel(value);
            reader.Release();
            ObserveGeneratorAwait(cancellation,(reason,rejected) => {
                result.Settle(rejected ? reason : IteratorResult(value,true),rejected);
            });
            return result;
        });
    }
    static class CustomIteratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = NativeAsyncIteratorPrototype};
            result.DefineMethod("next",new CollectionMethod("next",0,(receiver,_) => receiver is CustomStreamIterator iterator ? iterator.Next() : throw new InvalidOperationException("Stream iterator receiver required")));
            result.DefineMethod("return",new CollectionMethod("return",1,(receiver,args) => receiver is CustomStreamIterator iterator ? iterator.Return(FunctionArgument(args,0)) : throw new InvalidOperationException("Stream iterator receiver required")));
            return result;
        }
    }
}
