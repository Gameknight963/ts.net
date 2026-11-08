using System;

public static partial class Js
{
    sealed class StreamIterable(TsObject iterator, object next, bool asynchronous)
    {
        internal JsPromise Pull(CustomStreamController controller) => BodyPromiseOperation(() => {
            object step = CallMethod(next,iterator,Array.Empty<object>());
            var result = new JsPromise();
            void Accept(object value, bool rejected)
            {
                if (rejected) {result.Settle(value,true);return;}
                try {
                    var item = (TsObject)RequireIteratorResult(value);
                    bool done = Truthy(item.Get("done"));
                    if (done) controller.Owner.Close();else controller.Owner.Enqueue(item.Get("value"));
                    result.Settle(Undefined,false);
                }
                catch (Exception error) {result.Settle(CaughtValue(error),true);}
            }
            if (asynchronous) ObserveGeneratorAwait(step,Accept);
            else ObserveSyncResult(step,true).Then(new PromiseReaction(null,null,null,Accept));
            return result;
        });
        JsPromise ObserveSyncResult(object step, bool closeOnRejection) => BodyPromiseOperation(() => {
            var item = (TsObject)RequireIteratorResult(step);
            bool done = Truthy(item.Get("done"));
            object value = item.Get("value");
            var result = new JsPromise();
            ObserveGeneratorAwait(value,(resolved,rejected) => {
                if (rejected && closeOnRejection && !done) {
                    // Preserve the yielded-value failure if iterator closing
                    // also fails, as the async-from-sync adapter does.
                    try {
                        object close = iterator.Get("return");
                        if (!IsNullish(close)) CallMethod(close,iterator,Array.Empty<object>());
                    }
                    catch { }
                }
                result.Settle(rejected ? resolved : IteratorResult(resolved,done),rejected);
            });
            return result;
        });
        internal JsPromise Cancel(object reason) => BodyPromiseOperation(() => {
            object close = iterator.Get("return");
            if (IsNullish(close)) return BodyOperation(() => Undefined);
            object step = CallMethod(close,iterator,new[] {reason});
            var result = new JsPromise();
            void Complete(object value, bool rejected)
            {
                if (rejected) {result.Settle(value,true);return;}
                try {RequireIteratorResult(value);result.Settle(Undefined,false);}
                catch (Exception error) {result.Settle(CaughtValue(error),true);}
            }
            if (asynchronous) ObserveGeneratorAwait(step,Complete);
            else ObserveSyncResult(step,false).Then(new PromiseReaction(null,null,null,Complete));
            return result;
        });
    }
    static CustomReadableStream StreamFromIterable(object input)
    {
        bool asynchronous = TryGetAsyncIterator(input,out var iterator,out var next);
        if (!asynchronous) {
            object method = Get(input,PropertyKey(IteratorSymbol));
            if (method is not JsFunction function) throw new InvalidOperationException("Stream source is not iterable");
            iterator = InvokeIterableMethod(function,input);next = iterator.Get("next");
        }
        var state = new StreamIterable(iterator,next,asynchronous);
        var source = new JsClassInstance();
        source.DefineOwn("pull",new JsFunction((FunctionBody)(args => state.Pull((CustomStreamController)FunctionArgument(args,0))),null,"pull",1));
        source.DefineOwn("cancel",new JsFunction((FunctionBody)(args => state.Cancel(FunctionArgument(args,0))),null,"cancel",1));
        var strategy = new JsClassInstance();strategy.DefineOwn("highWaterMark",0d);
        return new CustomReadableStream(source,strategy);
    }
}
