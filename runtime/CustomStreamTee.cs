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
        readonly bool byteMode=reader.Owner.ByteMode;
        readonly CustomReadableStream original=reader.Owner;
        ByobStreamReader byobReader;

        internal JsArray CreateBranches()
        {
            for (int index = 0; index < 2; index++) {
                int branch = index;
                var source = new JsClassInstance();
                if(byteMode)source.DefineOwn("type","bytes");
                source.DefineOwn("pull",new JsFunction((FunctionBody)(_ => {Pull();return Undefined;}),null,"pull",1));
                source.DefineOwn("cancel",new JsFunction((FunctionBody)(args => Cancel(branch,FunctionArgument(args,0))),null,"cancel",1));
                branches[index] = new CustomReadableStream(source,Undefined);
            }
            WatchReader();
            return new JsArray(new object[] {branches[0],branches[1]});
        }
        void WatchReader()
        {
            object selected=(object)reader ?? byobReader;
            var closed=reader!=null ? reader.ClosedPromise : byobReader.ClosedPromise;
            closed.Then(new PromiseReaction(null,null,null,(reason,rejected)=>{
                if(rejected && (ReferenceEquals(selected,reader) || ReferenceEquals(selected,byobReader)))Error(reason);
            }));
        }
        void Pull()
        {
            if (finished) return;
            if (reading) {readAgain = true;return;}
            reading = true;
            int byobBranch=-1;
            if(byteMode)for(int index=0;index<2;index++)if(!cancelled[index] && branches[index].ByobReader!=null && branches[index].ByobRequest!=null){byobBranch=index;break;}
            JsPromise pending;
            try {
                if(byobBranch>=0) {
                    if(byobReader==null){var old=reader;reader=null;old.Release();byobReader=original.GetByobReader();WatchReader();}
                    var request=(ByteReadRequest)branches[byobBranch].ByobRequest;
                    pending=original.ReadInto(request.View,Undefined,byobReader);
                } else {
                    if(reader==null){var old=byobReader;byobReader=null;old.Release();reader=original.GetReader(Undefined);WatchReader();}
                    pending=reader.Read();
                }
            }catch(Exception exception){Error(CaughtValue(exception));return;}
            pending.Then(new PromiseReaction(null,null,null,(result,rejected) => {
                if (finished) return;
                if (rejected) {reading = false;Error(result);return;}
                try {
                var item = (TsObject)result;
                if (Truthy(item.Get("done"))) {
                    reading = false;
                    if(byobBranch>=0 && !cancelled[byobBranch]){branches[byobBranch].Close();branches[byobBranch].RespondView(item.Get("value"));}
                    for (int branch = 0; branch < 2; branch++) if (!cancelled[branch]) {
                        if(branch==byobBranch)continue;
                        if(byteMode)branches[branch].FinishByteTee();else branches[branch].Close();
                    }
                    finished=true;
                    if (!cancelled[0] || !cancelled[1]) cancellation.Settle(Undefined,false);
                    return;
                }
                readAgain = false;
                object value = item.Get("value");
                if(byobBranch>=0) {
                    int other=1-byobBranch;TypedArrayValue copy=null;
                    if(!cancelled[other]){var bytes=EncodingInput(value).ToArray();copy=new TypedArrayValue("Uint8Array",new BinaryBuffer(0){Bytes=bytes},0,bytes.Length);}
                    if(!cancelled[byobBranch])branches[byobBranch].RespondView(value);
                    if(copy!=null)branches[other].Enqueue(copy);
                } else if(byteMode && !cancelled[0] && !cancelled[1]) {
                    var bytes=EncodingInput(value).ToArray();
                    var copy=new TypedArrayValue("Uint8Array",new BinaryBuffer(0){Bytes=bytes},0,bytes.Length);
                    branches[0].Enqueue(value);branches[1].Enqueue(copy);
                } else for (int branch = 0; branch < 2; branch++) if (!cancelled[branch]) branches[branch].Enqueue(value);
                reading = false;
                if (readAgain) Pull();
                }catch(Exception exception){Error(CaughtValue(exception));}
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
                var reasonList=new JsArray(new[] {reasons[0],reasons[1]});
                (reader!=null ? reader.Cancel(reasonList) : byobReader.Owner.Cancel(reasonList)).Then(new PromiseReaction(null,null,null,(value,rejected) => cancellation.Settle(value,rejected)));
            }
            return cancellation;
        }
    }
    static JsArray TeeCustomStream(CustomReadableStream source) => new CustomStreamTee(source.GetReader(Undefined)).CreateBranches();
}
