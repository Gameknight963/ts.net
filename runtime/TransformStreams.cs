using System;

public static partial class Js
{
    static (object Mark, object Size) TransformStrategyProperties(object options)
    {
        if (IsNullish(options)) return (Undefined,Undefined);
        if (options is not TsObject strategy) throw new InvalidOperationException("Transform queuing strategy must be an object");
        object mark = strategy.Get("highWaterMark");return (mark,strategy.Get("size"));
    }
    static TsObject NormalizeTransformStrategy((object Mark, object Size) options, double fallback)
    {
        double mark = IsUndefined(options.Mark) ? fallback : Number(options.Mark);
        if (double.IsNaN(mark) || mark < 0) throw new ArgumentOutOfRangeException(nameof(options),"Invalid high water mark");
        var result = new JsClassInstance();result.DefineOwn("highWaterMark",mark);result.DefineOwn("size",options.Size);return result;
    }
    sealed class TransformStreamValue : JsDescriptorObject
    {
        internal readonly CustomReadableStream Readable;
        internal readonly WritableStreamValue Writable;
        readonly TransformController controller;
        TsObject transformer;
        JsFunction transform, flush, cancel;
        JsPromise pressureChange = new() {Handled = true}, finishPromise;
        bool backpressure = true, failed;
        object error;
        Action detachAbort;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)TransformBuiltins.Stream.Get("prototype");
        internal TransformStreamValue(object input, object writableStrategy, object readableStrategy)
        {
            if (!IsUndefined(input) && input is not TsObject) throw new InvalidOperationException("Transformer must be an object");
            transformer = input as TsObject;
            object readableType = transformer == null ? Undefined : transformer.Get("readableType");
            object writableType = transformer == null ? Undefined : transformer.Get("writableType");
            if (!IsUndefined(readableType) || !IsUndefined(writableType)) throw new ArgumentOutOfRangeException(nameof(input),"Transform stream types must be undefined");
            var start = StreamCallback(transformer,"start",true);
            var readOptions = TransformStrategyProperties(readableStrategy);
            var writeOptions = TransformStrategyProperties(writableStrategy);
            var normalizedReadable = NormalizeTransformStrategy(readOptions,0);
            var normalizedWritable = NormalizeTransformStrategy(writeOptions,1);
            controller = new TransformController(this);
            var starting = new JsPromise {Handled = true};
            var readableSource = new JsClassInstance();
            readableSource.DefineOwn("start",new JsFunction((FunctionBody)(_ => starting),null,"start",1));
            readableSource.DefineOwn("pull",new JsFunction((FunctionBody)(_ => {SetBackpressure(false);return pressureChange;}),null,"pull",1));
            readableSource.DefineOwn("cancel",new JsFunction((FunctionBody)(args => Cancel(FunctionArgument(args,0),true)),null,"cancel",1));
            var sink = new JsClassInstance();
            sink.DefineOwn("start",new JsFunction((FunctionBody)(args => {
                var writableController = (WritableController)FunctionArgument(args,0);
                detachAbort = writableController.Signal.Subscribe(() => SetBackpressure(false));return starting;
            }),null,"start",1));
            sink.DefineOwn("write",new JsFunction((FunctionBody)(args => Write(FunctionArgument(args,0))),null,"write",1));
            sink.DefineOwn("close",new JsFunction((FunctionBody)(_ => Flush()),null,"close",0));
            sink.DefineOwn("abort",new JsFunction((FunctionBody)(args => Cancel(FunctionArgument(args,0),false)),null,"abort",1));
            Writable = new WritableStreamValue(sink,normalizedWritable);
            Readable = new CustomReadableStream(readableSource,normalizedReadable,0);
            transform = StreamCallback(transformer,"transform",true);flush = StreamCallback(transformer,"flush",true);cancel = StreamCallback(transformer,"cancel",true);
            object completion = start == null ? Undefined : start.InvokeWithReceiver(input,new object[] {controller});
            starting.ResolveValue(completion);
        }
        void SetBackpressure(bool value)
        {
            if (backpressure == value) return;
            backpressure = value;pressureChange.Settle(Undefined,false);pressureChange = new JsPromise {Handled = true};
        }
        internal void Enqueue(object value)
        {
            try {Readable.Enqueue(value);}
            catch (Exception exception) {Error(CaughtValue(exception));throw;}
            if (!Readable.TransformWantsPull) SetBackpressure(true);
        }
        internal void Error(object reason)
        {
            Readable.Error(reason);ErrorWritable(reason);
        }
        void ErrorWritable(object reason)
        {
            if (!failed) {failed = true;error = reason;}
            Writable.Error(reason);SetBackpressure(false);
            ClearTransformer();
        }
        void ClearTransformer() {transformer = null;transform = flush = cancel = null;detachAbort?.Invoke();detachAbort = null;}
        internal void Terminate()
        {
            if (Readable.CanTransformClose) Readable.Close();
            ErrorWritable(new JsCaughtError("TypeError","Transform stream was terminated"));
        }
        JsPromise Write(object chunk)
        {
            var result = new JsPromise();
            void Perform()
            {
                if (Writable.PipeState is 1 or 3) {result.Settle(Writable.PipeError,true);return;}
                try {
                    object completion;
                    if (transform == null) {Enqueue(chunk);completion = Undefined;}
                    else completion = transform.InvokeWithReceiver(transformer,new object[] {chunk,controller});
                    ObserveGeneratorAwait(completion,(value,rejected) => {if (rejected) Error(value);result.Settle(rejected ? value : Undefined,rejected);});
                }
                catch (Exception exception) {object reason = CaughtValue(exception);Error(reason);result.Settle(reason,true);}
            }
            if (backpressure) pressureChange.Then(new PromiseReaction(null,null,null,(_,_) => Perform()));else Perform();
            return result;
        }
        JsPromise Flush()
        {
            var result = finishPromise = new JsPromise();
            try {
                object completion = flush == null ? Undefined : flush.InvokeWithReceiver(transformer,new object[] {controller});
                ObserveGeneratorAwait(completion,(value,rejected) => {
                    if (rejected) {Error(value);result.Settle(value,true);return;}
                    if (failed) {result.Settle(error,true);return;}
                    if (Readable.CanTransformClose) Readable.Close();
                    ClearTransformer();result.Settle(Undefined,false);
                });
            }
            catch (Exception exception) {object reason = CaughtValue(exception);Error(reason);result.Settle(reason,true);}
            return result;
        }
        JsPromise Cancel(object reason, bool readable)
        {
            if (finishPromise != null) return finishPromise;
            var result = finishPromise = new JsPromise();
            try {
                object completion = cancel == null ? Undefined : cancel.InvokeWithReceiver(transformer,new[] {reason});
                ObserveGeneratorAwait(completion,(value,rejected) => {
                    if (rejected) {Error(value);result.Settle(value,true);return;}
                    if (readable && Writable.PipeState == 3) {result.Settle(Writable.PipeError,true);return;}
                    Error(reason);result.Settle(Undefined,false);
                });
            }
            catch (Exception exception) {object value = CaughtValue(exception);Error(value);result.Settle(value,true);}
            return result;
        }
    }
    sealed class TransformController(TransformStreamValue owner) : JsDescriptorObject
    {
        internal readonly TransformStreamValue Owner = owner;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)TransformBuiltins.Controller.Get("prototype");
    }
    sealed class TransformConstructor : NativeNamespaceFunction
    {
        readonly bool controller;
        internal TransformConstructor(bool controller) : base(controller ? "TransformStreamDefaultController" : "TransformStream",static _ => throw new InvalidOperationException("Constructor requires new"),0)
        {
            this.controller = controller;
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor",this);prototype.DefineMethod(PropertyKey(ToStringTagSymbol),controller ? "TransformStreamDefaultController" : "TransformStream",false);
            TransformStreamValue Require(object receiver) => receiver as TransformStreamValue ?? throw new InvalidOperationException("Transform stream receiver required");
            TransformController RequireController(object receiver) => receiver as TransformController ?? throw new InvalidOperationException("Transform controller receiver required");
            if (!controller) {
                prototype.DefineAccessor("readable",new CollectionMethod("get readable",0,(receiver,_) => Require(receiver).Readable),1);
                prototype.DefineAccessor("writable",new CollectionMethod("get writable",0,(receiver,_) => Require(receiver).Writable),1);
            }
            else {
                prototype.DefineAccessor("desiredSize",new CollectionMethod("get desiredSize",0,(receiver,_) => RequireController(receiver).Owner.Readable.DesiredSize),1);
                prototype.DefineOwn("enqueue",new CollectionMethod("enqueue",0,(receiver,args) => {RequireController(receiver).Owner.Enqueue(FunctionArgument(args,0));return Undefined;}));
                prototype.DefineOwn("error",new CollectionMethod("error",0,(receiver,args) => {RequireController(receiver).Owner.Error(FunctionArgument(args,0));return Undefined;}));
                prototype.DefineOwn("terminate",new CollectionMethod("terminate",0,(receiver,_) => {RequireController(receiver).Owner.Terminate();return Undefined;}));
            }
        }
        public override object Construct(object[] args) => controller ? throw new InvalidOperationException("Illegal constructor") : new TransformStreamValue(FunctionArgument(args,0),FunctionArgument(args,1),FunctionArgument(args,2));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);var result = (TsObject)Construct(args);result.SetPrototype(prototype);return result;
        }
    }
    static class TransformBuiltins
    {
        internal static readonly JsFunction Stream = new TransformConstructor(false),Controller = new TransformConstructor(true);
    }
}
