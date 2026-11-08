using System;
using System.Text;

public static partial class Js
{
    static JsPromise BodyOperation(Func<object> action)
    {
        var promise = new JsPromise();
        try { promise.Settle(action(),false); }
        catch (Exception error) { promise.Settle(CaughtValue(error),true); }
        return promise;
    }
    // Buffered bodies own immutable bytes. Exposed buffers and reader chunks are
    // copies, so consuming or changing one response cannot change a clone.
    sealed class BufferedBody(byte[] bytes) : JsDescriptorObject
    {
        internal readonly byte[] Bytes = bytes;
        internal bool Disturbed, Consuming;
        internal bool Closed = bytes.Length == 0;
        internal BufferedBodyReader Reader;
        internal BufferedTee Tee;
        internal int TeeBranch;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : BufferedBodyMethods.Prototype;
        internal bool Locked => Consuming || Reader != null;
        internal BufferedBodyReader GetReader(object options)
        {
            if (!IsNullish(options) && options is not TsObject) throw new InvalidOperationException("Reader options must be an object");
            object mode = options is TsObject settings ? settings.Get("mode") : Undefined;
            if (!IsUndefined(mode)) {
                string requested = StringValue(mode);
                if (requested != "byob") throw new InvalidOperationException("Invalid reader mode");
                throw new InvalidOperationException("Buffered response bodies currently support default readers");
            }
            if (Locked) throw new InvalidOperationException("Body stream is locked");
            return Reader = new BufferedBodyReader(this);
        }
        internal byte[] Consume()
        {
            if (Disturbed || Locked) throw new InvalidOperationException("Body has already been consumed or locked");
            Tee?.BranchRead();
            Disturbed = Closed = true;
            Consuming = true;
            return Bytes;
        }
        internal void Cancel() { Disturbed = Closed = true; Reader?.Finish(); }
        internal JsPromise CancelPromise()
        {
            bool wasClosed = Closed; Cancel();
            return !wasClosed && Tee != null ? Tee.Cancel(TeeBranch) : BodyOperation(() => Undefined);
        }
    }
    sealed class BufferedBodyReader : JsDescriptorObject
    {
        internal BufferedBody Owner;
        JsPromise closedPromise;
        internal JsPromise ClosedPromise {
            get {
                if (closedPromise != null) return closedPromise;
                closedPromise = new JsPromise {Handled = true};
                if (Owner == null) closedPromise.Settle(new JsCaughtError("TypeError","Reader lock has been released"),true);
                else if (finished) closedPromise.Settle(Undefined,false);
                return closedPromise;
            }
        }
        bool finished;
        internal BufferedBodyReader(BufferedBody owner) {Owner = owner; if (owner.Closed) Finish();}
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : BufferedReaderMethods.Prototype;
        internal void Finish() { finished = true; closedPromise?.Settle(Undefined,false); }
        internal object Read()
        {
            if (Owner == null) throw new InvalidOperationException("Reader lock has been released");
            Owner.Disturbed = true;
            if (Owner.Closed) {Finish();return IteratorResult(Undefined,true);}
            Owner.Tee?.BranchRead();
            Owner.Closed = true; Finish();
            var chunk = AllocateTypedArray("Uint8Array",Owner.Bytes.Length);
            Owner.Bytes.CopyTo(chunk.Buffer.Bytes,0);
            return IteratorResult(chunk,false);
        }
        internal void Release()
        {
            if (Owner == null) return;
            Owner.Reader = null; Owner = null;
            if (finished) closedPromise = null;
            else if (closedPromise != null) closedPromise.Settle(new JsCaughtError("TypeError","Reader lock has been released"),true);
        }
    }
    static BufferedBody RequireBufferedBody(object receiver) => receiver as BufferedBody ?? throw new InvalidOperationException("Body stream receiver required");
    static BufferedBodyReader RequireBufferedReader(object receiver) => receiver as BufferedBodyReader ?? throw new InvalidOperationException("Body reader receiver required");
    static class BufferedBodyMethods
    {
        internal static readonly TsObject Prototype = Create();
        internal static readonly JsFunction Constructor = new CustomStreamConstructor();
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};
            result.DefineMethod(PropertyKey(ToStringTagSymbol),"ReadableStream",false);
            result.DefineAccessor("locked",new CollectionMethod("get locked",0,(receiver,_) => BoxBoolean(StreamLocked(receiver))),1);
            result.DefineOwn("getReader",new CollectionMethod("getReader",0,(receiver,args) => GetDefaultStreamReader(receiver,FunctionArgument(args,0))));
            var values = new CollectionMethod("values",0,(receiver,args) => {
                object options = FunctionArgument(args,0);
                if (!IsNullish(options) && options is not TsObject) throw new InvalidOperationException("Iterator options must be an object");
                bool preventCancel = options is TsObject settings && Truthy(settings.Get("preventCancel"));
                return receiver is CustomReadableStream stream ? new CustomStreamIterator(stream.GetReader(Undefined),preventCancel) : new BufferedBodyIterator(RequireBufferedBody(receiver).GetReader(Undefined),preventCancel);
            });
            result.DefineOwn("values",values); result.DefineMethod(PropertyKey(AsyncIteratorSymbol),values);
            result.DefineOwn("tee",new CollectionMethod("tee",0,(receiver,_) => receiver is CustomReadableStream stream ? TeeCustomStream(stream) : TeeBufferedBody(RequireBufferedBody(receiver))));
            result.DefineOwn("cancel",new CollectionMethod("cancel",0,(receiver,args) => receiver is CustomReadableStream stream ? BodyPromiseOperation(() => stream.CancelUnlocked(FunctionArgument(args,0))) : CancelBufferedBody(receiver,false)));
            return result;
        }
    }
    sealed class BufferedBodyIterator(BufferedBodyReader reader, bool preventCancel) : JsDescriptorObject
    {
        bool done;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : BufferedIteratorMethods.Prototype;
        internal object Next()
        {
            if (done) return IteratorResult(Undefined,true);
            object result = reader.Read();
            if (Truthy(((TsObject)result).Get("done"))) {done = true;reader.Release();}
            return result;
        }
        internal JsPromise Return(object value)
        {
            if (done) return BodyOperation(() => IteratorResult(value,true));
            done = true;
            if (preventCancel) { reader.Release(); return BodyOperation(() => IteratorResult(value,true)); }
            var result = new JsPromise();
            var cancellation = reader.Owner.CancelPromise();
            reader.Release();
            ObserveGeneratorAwait(cancellation, (reason,rejected) => {
                if (rejected) result.Settle(reason,true);
                else result.ResolveValue(IteratorResult(value,true));
            });
            return result;
        }
    }
    static class BufferedIteratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = NativeAsyncIteratorPrototype};
            result.DefineMethod("next",new CollectionMethod("next",0,(receiver,_) => BodyOperation(() => receiver is BufferedBodyIterator iterator ? iterator.Next() : throw new InvalidOperationException("Body iterator receiver required"))));
            result.DefineMethod("return",new CollectionMethod("return",1,(receiver,args) => BodyPromiseOperation(() => receiver is BufferedBodyIterator iterator ? iterator.Return(FunctionArgument(args,0)) : throw new InvalidOperationException("Body iterator receiver required"))));
            return result;
        }
    }
    static class BufferedReaderMethods
    {
        internal static readonly TsObject Prototype = Create();
        internal static readonly JsFunction Constructor = CreateConstructor();
        static JsFunction CreateConstructor()
        {
            var constructor = new BufferedReaderConstructor(Prototype);
            ((JsDescriptorObject)Prototype).DefineMethod("constructor", constructor);
            return constructor;
        }
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};
            result.DefineMethod(PropertyKey(ToStringTagSymbol),"ReadableStreamDefaultReader",false);
            result.DefineAccessor("closed",new CollectionMethod("get closed",0,(receiver,_) => BodyPromiseOperation(() => receiver is CustomStreamReader reader ? reader.ClosedPromise : RequireBufferedReader(receiver).ClosedPromise)),1);
            result.DefineOwn("read",new CollectionMethod("read",0,(receiver,_) => receiver is CustomStreamReader reader ? reader.Read() : BodyOperation(() => RequireBufferedReader(receiver).Read())));
            result.DefineOwn("cancel",new CollectionMethod("cancel",1,(receiver,args) => receiver is CustomStreamReader reader ? BodyPromiseOperation(() => reader.Cancel(FunctionArgument(args,0))) : CancelBufferedBody(receiver,true)));
            result.DefineOwn("releaseLock",new CollectionMethod("releaseLock",0,(receiver,_) => {if(receiver is CustomStreamReader reader)reader.Release();else RequireBufferedReader(receiver).Release();return Undefined;}));
            return result;
        }
    }
    static (byte[] Bytes,string ContentType,BufferedBody Stream) ExtractResponseBody(object input)
    {
        if (IsNullish(input)) return (null,null,null);
        if (input is FormDataValue form) return SerializeFormData(form);
        if (input is BufferedBody stream) {
            if (stream.Locked || stream.Disturbed) throw new InvalidOperationException("Body stream has already been consumed or locked");
            return (stream.Bytes,null,stream);
        }
        if (input is CustomReadableStream) throw new NotSupportedException("Response and Request bodies from custom streams are not supported yet");
        if (input is BlobValue blob) return (blob.Bytes,blob.Type.Length == 0 ? null : blob.Type,null);
        if (input is SearchParamsValue query) return (Encoding.UTF8.GetBytes(query.Serialize()),"application/x-www-form-urlencoded;charset=UTF-8",null);
        if (input is BinaryBuffer or BinaryView or TypedArrayValue) {
            if (input is BinaryBuffer {Detached:true} or BinaryView {OutOfBounds:true} or TypedArrayValue {OutOfBounds:true}) throw new InvalidOperationException("Response body buffer is detached or out of bounds");
            return (EncodingInput(input).ToArray(),null,null);
        }
        return (Encoding.UTF8.GetBytes(StringValue(input)),"text/plain;charset=UTF-8",null);
    }
    static string ResponseText(byte[] bytes)
    {
        string text = Encoding.UTF8.GetString(bytes);
        return text.Length > 0 && text[0] == '\ufeff' ? text[1..] : text;
    }
}
