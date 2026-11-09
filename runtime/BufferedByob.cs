using System;

public static partial class Js
{
    sealed partial class BufferedBody
    {
        internal int Position;
        internal JsCaughtError Failure;
        internal ByobStreamReader ByobReader;
        internal TsObject AcquireReader(object options)
        {
            if(!IsNullish(options) && options is not TsObject)throw new InvalidOperationException("Reader options must be an object");
            object mode=options is TsObject settings ? settings.Get("mode") : Undefined;
            if(IsUndefined(mode))return GetReader(Undefined);
            if(StringValue(mode)!="byob")throw new InvalidOperationException("Invalid reader mode");
            return GetByobReader();
        }
        internal ByobStreamReader GetByobReader()
        {
            if(Locked)throw new InvalidOperationException("Body stream is locked");
            var reader=ByobReader=new ByobStreamReader(this);
            if(Failure!=null)reader.ClosedPromise.Settle(Failure,true);
            else if(Closed)reader.ClosedPromise.Settle(Undefined,false);
            return reader;
        }
        internal JsPromise ReadInto(object input,object options,ByobStreamReader reader)
        {
            var view=ValidateByobRead(input,options);
            if(reader.BufferedOwner!=this || ByobReader!=reader)throw new InvalidOperationException("Reader lock has been released");
            Disturbed=true;
            if(Failure!=null)return BodyOperation(()=>throw MakeThrown(Failure));
            var buffer=(BinaryBuffer)TransferBuffer(view.Buffer,Undefined,false);
            int remaining=Closed ? 0 : Bytes.Length-Position;
            if(remaining>0)Tee?.BranchRead();
            int available=Math.Min(view.Length,remaining),aligned=available/view.Size*view.Size;
            bool done=Closed;
            int count=aligned;
            if(!Closed && aligned<view.Minimum) {
                if(remaining%view.Size!=0 || Position>0) {
                    Failure=new JsCaughtError("TypeError","The remaining body bytes cannot satisfy this read");
                    reader.ClosedPromise.Settle(Failure,true);
                    return BodyOperation(()=>throw MakeThrown(Failure));
                }
                count=remaining;done=true;
            }
            if(count>0){Bytes.AsSpan(Position,count).CopyTo(buffer.Bytes.AsSpan(view.Offset,count));Position+=count;}
            if(Position==Bytes.Length || done){Closed=true;reader.ClosedPromise.Settle(Undefined,false);}
            object output=view.Name==null ? new BinaryView(buffer,view.Offset,count) : new TypedArrayValue(view.Name,buffer,view.Offset,count/view.Size);
            return BodyOperation(()=>IteratorResult(output,done));
        }
    }
}
