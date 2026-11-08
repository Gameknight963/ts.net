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
    sealed class BufferedBody(byte[] bytes) : TsObject
    {
        internal readonly byte[] Bytes = bytes;
        internal bool Disturbed, Consuming;
        internal bool Closed = bytes.Length == 0;
        internal BufferedBodyReader Reader;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : BufferedBodyMethods.Prototype;
        internal bool Locked => Consuming || Reader != null;
        internal BufferedBodyReader GetReader(object options)
        {
            if (!IsNullish(options) && options is not TsObject) throw new InvalidOperationException("Reader options must be an object");
            object mode = options is TsObject settings ? settings.Get("mode") : Undefined;
            if (!IsUndefined(mode)) throw new InvalidOperationException("Buffered response bodies currently support default readers");
            if (Locked) throw new InvalidOperationException("Body stream is locked");
            return Reader = new BufferedBodyReader(this);
        }
        internal byte[] Consume()
        {
            if (Disturbed || Locked) throw new InvalidOperationException("Body has already been consumed or locked");
            Disturbed = Closed = true;
            Consuming = true;
            return Bytes;
        }
        internal void Cancel() { Disturbed = Closed = true; Reader?.Finish(); }
    }
    sealed class BufferedBodyReader : TsObject
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
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};
            result.DefineMethod(PropertyKey(ToStringTagSymbol),"ReadableStream");
            result.DefineAccessor("locked",new CollectionMethod("get locked",0,(receiver,_) => BoxBoolean(RequireBufferedBody(receiver).Locked)),1);
            result.DefineOwn("getReader",new CollectionMethod("getReader",0,(receiver,args) => RequireBufferedBody(receiver).GetReader(FunctionArgument(args,0))));
            var values = new CollectionMethod("values",0,(receiver,args) => {
                var body = RequireBufferedBody(receiver); object options = FunctionArgument(args,0);
                if (!IsNullish(options) && options is not TsObject) throw new InvalidOperationException("Iterator options must be an object");
                bool preventCancel = options is TsObject settings && Truthy(settings.Get("preventCancel"));
                return new BufferedBodyIterator(body.GetReader(Undefined),preventCancel);
            });
            result.DefineOwn("values",values); result.DefineMethod(PropertyKey(AsyncIteratorSymbol),values);
            result.DefineOwn("cancel",new CollectionMethod("cancel",0,(receiver,_) => BodyOperation(() => {
                var body = RequireBufferedBody(receiver); if (body.Locked) throw new InvalidOperationException("Body stream is locked");
                body.Cancel(); return Undefined;
            })));
            return result;
        }
    }
    sealed class BufferedBodyIterator(BufferedBodyReader reader, bool preventCancel) : TsObject
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
        internal object Return(object value)
        {
            if (!done) {
                done = true;
                if (!preventCancel) reader.Owner.Cancel();
                reader.Release();
            }
            return IteratorResult(value,true);
        }
    }
    static class BufferedIteratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};
            result.DefineMethod(PropertyKey(AsyncIteratorSymbol),new CollectionMethod("[Symbol.asyncIterator]",0,(receiver,_) => receiver));
            result.DefineMethod("next",new CollectionMethod("next",0,(receiver,_) => BodyOperation(() => receiver is BufferedBodyIterator iterator ? iterator.Next() : throw new InvalidOperationException("Body iterator receiver required"))));
            result.DefineMethod("return",new CollectionMethod("return",1,(receiver,args) => BodyOperation(() => receiver is BufferedBodyIterator iterator ? iterator.Return(FunctionArgument(args,0)) : throw new InvalidOperationException("Body iterator receiver required"))));
            return result;
        }
    }
    static class BufferedReaderMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};
            result.DefineMethod(PropertyKey(ToStringTagSymbol),"ReadableStreamDefaultReader");
            result.DefineAccessor("closed",new CollectionMethod("get closed",0,(receiver,_) => RequireBufferedReader(receiver).ClosedPromise),1);
            result.DefineOwn("read",new CollectionMethod("read",0,(receiver,_) => BodyOperation(() => RequireBufferedReader(receiver).Read())));
            result.DefineOwn("cancel",new CollectionMethod("cancel",1,(receiver,_) => BodyOperation(() => {
                var reader = RequireBufferedReader(receiver);
                if (reader.Owner == null) throw new InvalidOperationException("Reader lock has been released");
                reader.Owner.Cancel(); return Undefined;
            })));
            result.DefineOwn("releaseLock",new CollectionMethod("releaseLock",0,(receiver,_) => {RequireBufferedReader(receiver).Release();return Undefined;}));
            return result;
        }
    }
    static (byte[] Bytes,string ContentType,BufferedBody Stream) ExtractResponseBody(object input)
    {
        if (IsNullish(input)) return (null,null,null);
        if (input is BufferedBody stream) {
            if (stream.Locked || stream.Disturbed) throw new InvalidOperationException("Body stream has already been consumed or locked");
            return (stream.Bytes,null,stream);
        }
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
