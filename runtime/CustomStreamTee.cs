using System;

public static partial class Js
{
    sealed class CustomStreamTee(CustomStreamReader reader)
    {
        readonly CustomReadableStream[] branches = new CustomReadableStream[2];
        readonly bool[] cancelled = new bool[2];
        readonly object[] reasons = new object[2];
        readonly JsPromise cancellation = new();
        bool reading, readAgain, finished;

        internal JsArray CreateBranches()
        {
            for (int index = 0; index < 2; index++) {
                int branch = index;
                var source = new JsClassInstance();
                source.DefineOwn("pull",new JsFunction((FunctionBody)(_ => {Pull();return Undefined;}),null,"pull",1));
                source.DefineOwn("cancel",new JsFunction((FunctionBody)(args => Cancel(branch,FunctionArgument(args,0))),null,"cancel",1));
                branches[index] = new CustomReadableStream(source,Undefined);
            }
            reader.ClosedPromise.Then(new PromiseReaction(null,null,null,(reason,rejected) => {
                if (rejected) Error(reason);
            }));
            return new JsArray(new object[] {branches[0],branches[1]});
        }
        void Pull()
        {
            if (finished) return;
            if (reading) {readAgain = true;return;}
            reading = true;
            reader.Read().Then(new PromiseReaction(null,null,null,(result,rejected) => {
                if (finished) return;
                if (rejected) {reading = false;Error(result);return;}
                var item = (TsObject)result;
                if (Truthy(item.Get("done"))) {
                    reading = false;finished = true;
                    for (int branch = 0; branch < 2; branch++) if (!cancelled[branch]) branches[branch].Close();
                    if (!cancelled[0] || !cancelled[1]) cancellation.Settle(Undefined,false);
                    return;
                }
                readAgain = false;
                object value = item.Get("value");
                // Default streams share the original chunk. Byte streams need
                // a separate tee algorithm and cannot reach this path.
                for (int branch = 0; branch < 2; branch++) if (!cancelled[branch]) branches[branch].Enqueue(value);
                reading = false;
                if (readAgain) Pull();
            }));
        }
        void Error(object reason)
        {
            if (finished) return;
            finished = true;reading = false;
            foreach (var branch in branches) branch.Error(reason);
            if (!cancelled[0] || !cancelled[1]) cancellation.Settle(Undefined,false);
        }
        JsPromise Cancel(int branch, object reason)
        {
            cancelled[branch] = true;reasons[branch] = reason;
            if (cancelled[0] && cancelled[1]) {
                reader.Cancel(new JsArray(new[] {reasons[0],reasons[1]})).Then(new PromiseReaction(null,null,null,(value,rejected) => cancellation.Settle(value,rejected)));
            }
            return cancellation;
        }
    }
    static JsArray TeeCustomStream(CustomReadableStream source) => new CustomStreamTee(source.GetReader(Undefined)).CreateBranches();
}
