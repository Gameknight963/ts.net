using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class PullInto(BinaryBuffer buffer,int offset,int capacity,int elementSize,string name,int minimum,JsPromise promise,bool defaultReader=false)
    {
        internal BinaryBuffer Buffer=buffer;
        internal readonly int Offset=offset,Capacity=capacity,ElementSize=elementSize,Minimum=minimum,BufferLength=buffer.Bytes.Length;
        internal readonly string Name=name;
        internal readonly JsPromise Promise=promise;
        internal readonly bool DefaultReader=defaultReader;
        internal int Filled;
        internal bool Released;
    }
    static (BinaryBuffer Buffer,int Offset,int Length,int Size,string Name) ByobView(object value)
    {
        if(value is TypedArrayValue array && !array.OutOfBounds)return(array.Buffer,array.Offset,checked(array.Count*array.Size),array.Size,array.Name);
        if(value is BinaryView view && !view.OutOfBounds)return(view.Buffer,view.Offset,view.Length,1,null);
        throw new InvalidOperationException("An attached ArrayBuffer view is required");
    }
    static double ByobByteCount(object amount)
    {
        double number=Math.Truncate(Number(amount));
        if(!double.IsFinite(number) || number<0 || number>=18446744073709551616d)throw new InvalidOperationException("Invalid response byte count");
        return number;
    }
    sealed partial class CustomReadableStream
    {
        internal ByobStreamReader ByobReader;
        internal bool Locked=>Reader!=null || ByobReader!=null;
        bool HasPendingReads=>(Reader?.Pending.Count ?? 0)>0 || ByobReader!=null && pullIntos.Count>0;
        readonly Queue<PullInto> pullIntos=new();
        ByteReadRequest byobRequest;
        int autoAllocateChunkSize;
        bool TakeChunk(out (object Value,double Size) chunk)
        {
            if(chunks.First==null){chunk=default;return false;}
            chunk=chunks.First.Value;chunks.RemoveFirst();return true;
        }
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
            if(!ByteMode)throw new InvalidOperationException("BYOB readers require byte streams");
            if(Locked)throw new InvalidOperationException("Stream is locked");
            var reader=ByobReader=new ByobStreamReader(this);
            if(state!=0)reader.ClosedPromise.Settle(state==1?Undefined:error,state==2);
            return reader;
        }
        internal object ByobRequest {
            get {
                if(pullIntos.Count==0)return null;
                var first=pullIntos.Peek();
                return byobRequest ??= new ByteReadRequest(this,new TypedArrayValue("Uint8Array",first.Buffer,first.Offset+first.Filled,first.Capacity-first.Filled));
            }
        }
        void InvalidateByobRequest(){if(byobRequest!=null){byobRequest.Owner=null;byobRequest.View=null;byobRequest=null;}}
        void AllocateDefaultRead(JsPromise promise)=>pullIntos.Enqueue(new PullInto(new BinaryBuffer(autoAllocateChunkSize),0,autoAllocateChunkSize,1,"Uint8Array",1,promise,true));
        internal JsPromise ReadInto(object input,object options,ByobStreamReader requester)
        {
            var view=ByobView(input);
            if(view.Length==0 || view.Buffer.Bytes.Length==0)throw new InvalidOperationException("BYOB views must not be empty");
            if(!IsNullish(options) && options is not TsObject)throw new InvalidOperationException("Read options must be an object");
            object requested=options is TsObject settings ? settings.Get("min") : Undefined;
            double min=IsUndefined(requested)?1:Math.Truncate(Number(requested));
            if(!double.IsFinite(min) || min<1 || min>=18446744073709551616d)throw new InvalidOperationException("Invalid minimum read length");
            if(min>view.Length/view.Size)throw new ArgumentOutOfRangeException(nameof(options),"Minimum read length exceeds the view");
            EnsureAttached(view.Buffer);
            if(requester.Owner!=this || ByobReader!=requester)throw new InvalidOperationException("Reader lock has been released");
            view=ByobView(input);
            if(view.Length==0)throw new InvalidOperationException("BYOB view became empty during conversion");
            if(min>view.Length/view.Size)throw new ArgumentOutOfRangeException(nameof(options),"Minimum read length exceeds the view");
            Disturbed=true;
            var result=new JsPromise();
            if(state==2){result.Settle(error,true);return result;}
            var transferred=(BinaryBuffer)TransferBuffer(view.Buffer,Undefined,false);
            var descriptor=new PullInto(transferred,view.Offset,view.Length,view.Size,view.Name,(int)min*view.Size,result);
            if(state==1 && pullIntos.Count==0){CommitInto(descriptor,true);return result;}
            pullIntos.Enqueue(descriptor);
            try {ProcessPullIntos();}catch when(state==2){return result;}
            if(closeRequested && chunks.Count==0 && pullIntos.Contains(descriptor) && descriptor.Filled<descriptor.Minimum){Error(new JsCaughtError("TypeError","Closed stream cannot satisfy the minimum read length"));}
            else PullIfNeeded();
            return result;
        }
        object IntoView(PullInto descriptor)
        {
            descriptor.Buffer=(BinaryBuffer)TransferBuffer(descriptor.Buffer,Undefined,false);
            return descriptor.Name==null ? new BinaryView(descriptor.Buffer,descriptor.Offset,descriptor.Filled) : new TypedArrayValue(descriptor.Name,descriptor.Buffer,descriptor.Offset,descriptor.Filled/descriptor.ElementSize);
        }
        void CommitInto(PullInto descriptor,bool done)
        {
            InvalidateByobRequest();
            if(descriptor.DefaultReader && Reader!=null && Reader.Pending.Count>0)Reader.Pending.Dequeue();
            descriptor.Promise.Settle(IteratorResult(IntoView(descriptor),done),false);
        }
        void ProcessPullIntos()
        {
            while(pullIntos.Count>0 && totalSize>0) {
                var first=pullIntos.Peek();if(first.Released)break;
                int available=(int)Math.Min(first.Capacity-first.Filled,totalSize);
                int aligned=(first.Filled+available)/first.ElementSize*first.ElementSize;
                int count=aligned>=first.Minimum ? aligned-first.Filled : available;
                while(count>0) {
                    var entry=chunks.First.Value;var bytes=(TypedArrayValue)entry.Value;
                    int take=Math.Min(count,bytes.Count);
                    bytes.Buffer.Bytes.AsSpan(bytes.Offset,take).CopyTo(first.Buffer.Bytes.AsSpan(first.Offset+first.Filled));
                    chunks.RemoveFirst();if(take<bytes.Count)chunks.AddFirst((new TypedArrayValue("Uint8Array",bytes.Buffer,bytes.Offset+take,bytes.Count-take),bytes.Count-take));
                    first.Filled+=take;count-=take;totalSize-=take;
                }
                if(first.Filled<first.Minimum)break;
                pullIntos.Dequeue();CommitInto(first,false);
            }
            if(closeRequested && chunks.Count==0 && state==0) {
                if(pullIntos.Count>0 && pullIntos.Peek().Filled<pullIntos.Peek().Minimum){Error(new JsCaughtError("TypeError","Closed stream cannot satisfy the minimum read length"));return;}
                ValidateByteClose();FinishClose();
            }
        }
        void EnqueueBytes(TypedArrayValue value)
        {
            if(pullIntos.Count>0) {
                var first=pullIntos.Peek();first.Buffer=(BinaryBuffer)TransferBuffer(first.Buffer,Undefined,false);InvalidateByobRequest();
                if(first.Released)ReturnReleasedInto();
            }
            chunks.AddLast((value,value.Count));totalSize+=value.Count;
            if(ByobReader!=null)ProcessPullIntos();DrainDefaultReads();PullIfNeeded();
        }
        void DrainDefaultReads()
        {
            while(Reader!=null && Reader.Pending.Count>0 && chunks.Count>0 && (pullIntos.Count==0 || pullIntos.Peek().DefaultReader)) {
                if(pullIntos.Count>0){pullIntos.Dequeue();InvalidateByobRequest();}
                TakeChunk(out var chunk);totalSize-=chunk.Size;Reader.Pending.Dequeue().Settle(IteratorResult(chunk.Value,false),false);
            }
            if(closeRequested && chunks.Count==0 && state==0)FinishClose();
        }
        void ValidateByteClose()
        {
            if(ByteMode && totalSize==0 && pullIntos.Count>0 && pullIntos.Peek().Filled%pullIntos.Peek().ElementSize!=0) {
                var reason=new JsCaughtError("TypeError","The final bytes do not fill a complete element");Error(reason);throw MakeThrown(reason);
            }
        }
        void ClearPullIntos(object reason,bool rejected)
        {
            InvalidateByobRequest();
            while(pullIntos.TryDequeue(out var pending))if(!pending.Released)pending.Promise.Settle(rejected?reason:IteratorResult(Undefined,true),rejected);
        }
        internal void ReleasePullIntos()
        {
            if(pullIntos.Count==0)return;
            var first=pullIntos.Dequeue();
            var reason=new JsCaughtError("TypeError","Reader lock has been released");first.Promise.Settle(reason,true);first.Released=true;
            while(pullIntos.TryDequeue(out var pending))pending.Promise.Settle(reason,true);
            pullIntos.Enqueue(first);
        }
        void ReturnReleasedInto()
        {
            var first=pullIntos.Dequeue();InvalidateByobRequest();
            if(first.Filled>0){var bytes=first.Buffer.Bytes.AsSpan(first.Offset,first.Filled).ToArray();chunks.AddFirst((new TypedArrayValue("Uint8Array",new BinaryBuffer(0){Bytes=bytes},0,bytes.Length),bytes.Length));totalSize+=bytes.Length;}
        }
        internal void RespondBytes(object amount)
        {
            double number=ByobByteCount(amount);
            if(pullIntos.Count==0)throw new InvalidOperationException("BYOB request is no longer active");
            var first=pullIntos.Peek();EnsureAttached(first.Buffer);
            if(state==1 && number!=0 || state==0 && number==0)throw new InvalidOperationException("Invalid response size for the stream state");
            if(number>first.Capacity-first.Filled)throw new ArgumentOutOfRangeException(nameof(amount));
            first.Buffer=(BinaryBuffer)TransferBuffer(first.Buffer,Undefined,false);InvalidateByobRequest();first.Filled+=(int)number;
            if(state==1) {
                while(pullIntos.TryDequeue(out var pending))if(!pending.Released)CommitInto(pending,true);
            } else if(first.Released){ReturnReleasedInto();ProcessPullIntos();DrainDefaultReads();}
            else if(first.Filled>=first.Minimum) {
                pullIntos.Dequeue();int remainder=first.Filled%first.ElementSize;
                if(remainder>0){first.Filled-=remainder;var extra=first.Buffer.Bytes.AsSpan(first.Offset+first.Filled,remainder).ToArray();chunks.AddFirst((new TypedArrayValue("Uint8Array",new BinaryBuffer(0){Bytes=extra},0,remainder),remainder));totalSize+=remainder;}
                CommitInto(first,false);ProcessPullIntos();
            }
            PullIfNeeded();
        }
        internal void RespondView(object input)
        {
            var view=ByobView(input);
            if(pullIntos.Count==0)throw new InvalidOperationException("BYOB request is no longer active");
            var first=pullIntos.Peek();
            if(state==1 && view.Length!=0 || state==0 && view.Length==0)throw new InvalidOperationException("Invalid replacement size for the stream state");
            if(view.Buffer.Bytes.Length!=first.BufferLength || view.Offset!=first.Offset+first.Filled || view.Length>first.Capacity-first.Filled)throw new ArgumentOutOfRangeException(nameof(input),"Replacement view does not match the pending read");
            RequireBuffer(view.Buffer);
            first.Buffer=view.Buffer;RespondBytes((double)view.Length);
        }
        internal void FinishByteTee()
        {
            Close();if(pullIntos.Count>0)RespondBytes(0d);
        }
    }
    sealed class ByobStreamReader(CustomReadableStream owner) : JsDescriptorObject
    {
        internal CustomReadableStream Owner=owner;
        internal JsPromise ClosedPromise=new(){Handled=true};
        internal override TsObject PropertyPrototype=>TryExplicitPrototype(out var value)?value:ByobReaderMethods.Prototype;
        internal void Release()
        {
            if(Owner==null)return;
            Owner.ReleasePullIntos();Owner.ByobReader=null;Owner=null;
            var reason=new JsCaughtError("TypeError","Reader lock has been released");ClosedPromise.Settle(reason,true);ClosedPromise=new JsPromise{Handled=true};ClosedPromise.Settle(reason,true);
        }
    }
    sealed class ByteReadRequest(CustomReadableStream owner,TypedArrayValue view) : JsDescriptorObject
    {
        internal CustomReadableStream Owner=owner;
        internal TypedArrayValue View=view;
        internal override TsObject PropertyPrototype=>TryExplicitPrototype(out var value)?value:ByobRequestMethods.Prototype;
    }
}
