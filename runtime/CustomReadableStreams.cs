using System;
using System.Collections.Generic;

public static partial class Js
{
    static bool StreamLocked(object receiver) => receiver switch {
        CustomReadableStream stream => stream.Locked,
        BufferedBody body => body.Locked,
        _ => throw new InvalidOperationException("Readable stream receiver required")
    };
    static TsObject GetDefaultStreamReader(object receiver, object options) => receiver switch {
        CustomReadableStream stream => stream.AcquireReader(options),
        BufferedBody body => body.AcquireReader(options),
        _ => throw new InvalidOperationException("Readable stream receiver required")
    };
    static JsFunction StreamCallback(TsObject source, string name, bool nullIsAbsent = false)
    {
        object value = source == null ? Undefined : source.Get(name);
        return IsUndefined(value) || nullIsAbsent && value == null ? null : value as JsFunction ?? throw new InvalidOperationException("Stream callback must be callable");
    }
    sealed partial class CustomReadableStream : JsDescriptorObject
    {
        // 0 readable, 1 closed, 2 errored. Closing waits for queued chunks.
        int state;
        object error;
        readonly LinkedList<(object Value, double Size)> chunks = new();
        double totalSize;
        readonly double highWaterMark;
        TsObject source;
        JsFunction pull, cancel, size;
        bool started, pulling, pullAgain, closeRequested;
        internal CustomStreamReader Reader;
        internal readonly bool ByteMode;
        internal bool Disturbed;
        internal bool PipeReadable => state == 0;
        internal bool CanTransformClose => state == 0 && !closeRequested;
        internal bool TransformWantsPull => state == 0 && !closeRequested && (HasPendingReads || highWaterMark > totalSize);
        readonly TsObject controller;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : BufferedBodyMethods.Prototype;
        internal CustomReadableStream(object sourceValue, object strategyValue, double defaultHighWaterMark = 1)
        {
            if (!IsNullish(sourceValue) && sourceValue is not TsObject) throw new InvalidOperationException("Underlying source must be an object");
            if (!IsNullish(strategyValue) && strategyValue is not TsObject) throw new InvalidOperationException("Queuing strategy must be an object");
            source = sourceValue as TsObject;
            var strategy = strategyValue as TsObject;
            object sizeValue = strategy == null ? Undefined : strategy.Get("size");
            object mark = strategy == null ? Undefined : strategy.Get("highWaterMark");
            object type = source == null ? Undefined : source.Get("type");
            if (!IsUndefined(type)) {
                if (StringValue(type) != "bytes") throw new InvalidOperationException("Invalid stream type");
                ByteMode = true;
            }
            if(ByteMode && !IsUndefined(sizeValue))throw new ArgumentOutOfRangeException(nameof(strategyValue),"Byte stream strategies cannot define size");
            size = IsUndefined(sizeValue) ? null : sizeValue as JsFunction ?? throw new InvalidOperationException("Stream size must be callable");
            highWaterMark = IsUndefined(mark) ? ByteMode ? 0 : defaultHighWaterMark : Number(mark);
            if (double.IsNaN(highWaterMark) || highWaterMark < 0) throw new ArgumentOutOfRangeException(nameof(strategyValue),"Invalid high water mark");
            var start = StreamCallback(source,"start",true);
            pull = StreamCallback(source,"pull",true);
            cancel = StreamCallback(source,"cancel",true);
            if(ByteMode) {
                object allocation=source.Get("autoAllocateChunkSize");
                if(!IsUndefined(allocation)) {
                    double amount=Number(allocation);
                    if(!double.IsFinite(amount) || amount<1 || amount>9007199254740991d || Math.Truncate(amount)!=amount)throw new ArgumentOutOfRangeException(nameof(sourceValue),"Invalid autoAllocateChunkSize");
                    autoAllocateChunkSize = BinaryIndex(amount);
                }
            }
            controller = ByteMode ? new ByteStreamController(this) : new CustomStreamController(this);
            // start throws synchronously; its returned promise settles asynchronously.
            object result = start == null ? Undefined : start.InvokeWithReceiver(sourceValue,new object[] {controller});
            ObserveGeneratorAwait(result,(value,rejected) => {
                if (rejected) Error(value);
                else {started = true; PullIfNeeded();}
            });
        }
        internal CustomStreamReader GetReader(object options)
        {
            if (!IsNullish(options) && options is not TsObject) throw new InvalidOperationException("Reader options must be an object");
            object mode = options is TsObject settings ? settings.Get("mode") : Undefined;
            if (!IsUndefined(mode)) {
                if (StringValue(mode) != "byob") throw new InvalidOperationException("Invalid reader mode");
                throw new InvalidOperationException("A default stream cannot use a BYOB reader");
            }
            if (Locked) throw new InvalidOperationException("Stream is locked");
            return Reader = new CustomStreamReader(this);
        }
        internal object DesiredSize => state == 2 ? null : BoxNumber(state == 1 ? 0 : highWaterMark-totalSize);
        void ClearAlgorithms() {source = null; pull = cancel = size = null;}
        void PullIfNeeded()
        {
            if (!started || state != 0 || closeRequested || !(HasPendingReads || highWaterMark > totalSize)) return;
            if (pulling) {pullAgain = true;return;}
            pulling = true;
            object result;
            try {result = pull == null ? Undefined : pull.InvokeWithReceiver(source,new object[] {controller});}
            catch (Exception exception) {Error(CaughtValue(exception));return;}
            ObserveGeneratorAwait(result,(value,rejected) => {
                pulling = false;
                if (rejected) Error(value);
                else if (pullAgain) {pullAgain = false;PullIfNeeded();}
            });
        }
        internal void Enqueue(object value)
        {
            if (state != 0 || closeRequested) throw new InvalidOperationException("Stream is not open for enqueue");
            if(ByteMode) {EnqueueBytes(TransferByteChunk(value));return;}
            if (Reader != null && Reader.Pending.TryDequeue(out var waiting)) waiting.Settle(IteratorResult(value,false),false);
            else {
                double amount;
                try {
                    amount = ByteMode ? ((TypedArrayValue)value).Count : size == null ? 1 : Number(size.InvokeWithReceiver(Undefined,new[] {value}));
                    if (!double.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(value),"Invalid chunk size");
                }
                catch (Exception exception) {Error(CaughtValue(exception));throw;}
                chunks.AddLast((value,amount));totalSize += amount;
            }
            PullIfNeeded();
        }
        internal void Close()
        {
            if (state != 0 || closeRequested) throw new InvalidOperationException("Stream is not open for close");
            ValidateByteClose();
            closeRequested = true;
            if (chunks.Count == 0) FinishClose();
        }
        void FinishClose()
        {
            state = 1;ClearAlgorithms();Reader?.ClosedPromise.Settle(Undefined,false);ByobReader?.ClosedPromise.Settle(Undefined,false);
            if (Reader != null) while (Reader.Pending.TryDequeue(out var pending)) pending.Settle(IteratorResult(Undefined,true),false);
        }
        internal void Error(object reason)
        {
            if (state != 0) return;
            state = 2;error = reason;chunks.Clear();totalSize = 0;ClearAlgorithms();
            ClearPullIntos(reason,true);ByobReader?.ClosedPromise.Settle(reason,true);
            Reader?.ClosedPromise.Settle(reason,true);
            if (Reader != null) while (Reader.Pending.TryDequeue(out var pending)) pending.Settle(reason,true);
        }
        internal JsPromise Read(CustomStreamReader reader)
        {
            Disturbed = true;
            var result = new JsPromise();
            if (state == 2) result.Settle(error,true);
            else if (TakeChunk(out var chunk)) {
                totalSize = Math.Max(0,totalSize-chunk.Size);
                if (closeRequested && chunks.Count == 0) FinishClose();else PullIfNeeded();
                result.Settle(IteratorResult(chunk.Value,false),false);
            }
            else if (state == 1) result.Settle(IteratorResult(Undefined,true),false);
            else {reader.Pending.Enqueue(result);if(autoAllocateChunkSize>0)AllocateDefaultRead(result);PullIfNeeded();}
            return result;
        }
        internal JsPromise CancelUnlocked(object reason)
        {
            if (Locked) throw new InvalidOperationException("Stream is locked");
            return Cancel(reason);
        }
        internal JsPromise Cancel(object reason)
        {
            Disturbed = true;
            if (state == 1) return BodyOperation(() => Undefined);
            if (state == 2) {var failed = new JsPromise();failed.Settle(error,true);return failed;}
            var operation = cancel;var receiver = source;
            chunks.Clear();totalSize = 0;ClearPullIntos(Undefined,false);FinishClose();
            var result = new JsPromise();
            try {
                object completion = operation == null ? Undefined : operation.InvokeWithReceiver(receiver,new[] {reason});
                ObserveGeneratorAwait(completion,(value,rejected) => result.Settle(rejected ? value : Undefined,rejected));
            }
            catch (Exception exception) {result.Settle(CaughtValue(exception),true);}
            return result;
        }
        internal void InitializeReader(CustomStreamReader reader)
        {
            if (state != 0) reader.ClosedPromise.Settle(state == 1 ? Undefined : error,state == 2);
        }
    }
    sealed class CustomStreamReader : JsDescriptorObject
    {
        internal CustomReadableStream Owner;
        internal readonly Queue<JsPromise> Pending = new();
        internal JsPromise ClosedPromise = new() {Handled = true};
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : BufferedReaderMethods.Prototype;
        internal CustomStreamReader(CustomReadableStream owner) {Owner = owner;owner.InitializeReader(this);}
        internal JsPromise Read() => BodyPromiseOperation(() => Owner == null ? throw new InvalidOperationException("Reader lock has been released") : Owner.Read(this));
        internal JsPromise Cancel(object reason) => Owner == null ? throw new InvalidOperationException("Reader lock has been released") : Owner.Cancel(reason);
        internal void Release()
        {
            if (Owner == null) return;
            Owner.ReleasePullIntos();Owner.Reader = null;Owner = null;
            var reason = new JsCaughtError("TypeError","Reader lock has been released");
            ClosedPromise.Settle(reason,true);
            ClosedPromise = new JsPromise {Handled = true};
            ClosedPromise.Settle(reason,true);
            while (Pending.TryDequeue(out var pending)) pending.Settle(reason,true);
        }
    }
    sealed class CustomStreamController(CustomReadableStream owner) : JsDescriptorObject
    {
        internal readonly CustomReadableStream Owner = owner;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : CustomControllerMethods.Prototype;
    }
    static class CustomControllerMethods
    {
        internal static readonly TsObject Prototype = Create();
        internal static readonly JsFunction Constructor = CreateConstructor();
        static JsFunction CreateConstructor()
        {
            var constructor = new IllegalStreamControllerConstructor();
            ((JsDescriptorObject)Prototype).DefineMethod("constructor",constructor);
            return constructor;
        }
        static CustomStreamController Require(object value) => value as CustomStreamController ?? throw new InvalidOperationException("Stream controller receiver required");
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};
            result.DefineMethod(PropertyKey(ToStringTagSymbol),"ReadableStreamDefaultController",false);
            result.DefineAccessor("desiredSize",new CollectionMethod("get desiredSize",0,(receiver,_) => Require(receiver).Owner.DesiredSize),1);
            result.DefineOwn("enqueue",new CollectionMethod("enqueue",0,(receiver,args) => {Require(receiver).Owner.Enqueue(FunctionArgument(args,0));return Undefined;}));
            result.DefineOwn("close",new CollectionMethod("close",0,(receiver,_) => {Require(receiver).Owner.Close();return Undefined;}));
            result.DefineOwn("error",new CollectionMethod("error",0,(receiver,args) => {Require(receiver).Owner.Error(FunctionArgument(args,0));return Undefined;}));
            return result;
        }
    }
    sealed class IllegalStreamControllerConstructor : NativeNamespaceFunction
    {
        internal IllegalStreamControllerConstructor() : base("ReadableStreamDefaultController",static _ => throw new InvalidOperationException("Illegal constructor"),0)
        {UseObjectPrototype(CustomControllerMethods.Prototype);}
        public override object Construct(object[] args) => throw new InvalidOperationException("Illegal constructor");
    }
    sealed class CustomStreamConstructor : NativeNamespaceFunction
    {
        internal CustomStreamConstructor() : base("ReadableStream",static _ => throw new InvalidOperationException("ReadableStream requires new"),0)
        {
            UseObjectPrototype(BufferedBodyMethods.Prototype);
            ((JsDescriptorObject)BufferedBodyMethods.Prototype).DefineMethod("constructor",this);
            Member("from",new JsFunction((FunctionBody)(args => StreamFromIterable(FunctionArgument(args,0))),null,"from",1));
        }
        public override object Construct(object[] args) => new CustomReadableStream(FunctionArgument(args,0),FunctionArgument(args,1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);
            var result = (TsObject)Construct(args);result.SetPrototype(prototype);return result;
        }
    }
    static class ReadableStreams {internal static readonly JsFunction Constructor = BufferedBodyMethods.Constructor;}
}
