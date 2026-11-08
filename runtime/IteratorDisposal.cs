using System;

public static partial class Js
{
    static object DisposeIterator(object receiver)
    {
        object method = Get(receiver,"return");
        if (!IsNullish(method)) CallMethod(method,receiver,Array.Empty<object>());
        return Undefined;
    }
    static object DisposeAsyncIterator(object receiver)
    {
        var result = new JsPromise();
        try {
            object method = Get(receiver,"return");
            if (IsNullish(method)) result.Settle(Undefined,false);
            else ObserveGeneratorAwait(CallMethod(method,receiver,new[] {Undefined}),
                (value,rejected) => result.Settle(rejected ? value : Undefined,rejected));
        }
        catch (Exception error) {result.Settle(CaughtValue(error),true);}
        return result;
    }
    static class AsyncIteratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};
            result.DefineMethod(PropertyKey(AsyncIteratorSymbol),new CollectionMethod("[Symbol.asyncIterator]",0,(receiver,_)=>receiver));
            result.DefineMethod(PropertyKey(AsyncDisposeSymbol),new CollectionMethod("[Symbol.asyncDispose]",0,(receiver,_)=>DisposeAsyncIterator(receiver)));
            return result;
        }
    }
    static TsObject NativeAsyncIteratorPrototype => AsyncIteratorMethods.Prototype;
}
