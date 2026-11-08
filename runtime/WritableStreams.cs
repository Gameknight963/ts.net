using System;
using System.Collections.Generic;

public static partial class Js
{
    static JsPromise StreamRejection(object reason) {var result = new JsPromise();result.Settle(reason,true);return result;}
    sealed class WritableStreamValue : JsDescriptorObject
    {
        sealed record Entry(object Value, double Size, JsPromise Promise, bool Close);
        readonly Queue<Entry> queue = new();
        readonly double highWaterMark;
        double totalSize;
        int state; // writable, erroring, closed, errored
        object storedError;
        bool started, inFlight, closing, backpressure, rejectAbort;
        JsPromise abortRequest;
        object abortReason;
        TsObject sink;
        JsFunction write, close, abort, size;
        internal WritableWriter Writer;
        internal int PipeState => state;
        internal object PipeError => storedError;
        internal bool PipeClosing => closing;
        readonly WritableController controller;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)WritableBuiltins.Stream.Get("prototype");
        internal WritableStreamValue(object source, object strategyValue)
        {
            if (!IsNullish(source) && source is not TsObject) throw new InvalidOperationException("Underlying sink must be an object");
            if (!IsNullish(strategyValue) && strategyValue is not TsObject) throw new InvalidOperationException("Queuing strategy must be an object");
            sink = source as TsObject;
            if (!IsUndefined(sink == null ? Undefined : sink.Get("type"))) throw new ArgumentOutOfRangeException(nameof(source),"Writable stream type must be undefined");
            var strategy = strategyValue as TsObject;
            size = StreamCallback(strategy,"size");
            object mark = strategy == null ? Undefined : strategy.Get("highWaterMark");
            highWaterMark = IsUndefined(mark) ? 1 : Number(mark);
            if (double.IsNaN(highWaterMark) || highWaterMark < 0) throw new ArgumentOutOfRangeException(nameof(strategyValue),"Invalid high water mark");
            var start = StreamCallback(sink,"start",true);
            write = StreamCallback(sink,"write",true);close = StreamCallback(sink,"close",true);abort = StreamCallback(sink,"abort",true);
            backpressure = highWaterMark == 0;
            controller = new WritableController(this);
            object completion = start == null ? Undefined : start.InvokeWithReceiver(source,new object[] {controller});
            ObserveGeneratorAwait(completion,(value,rejected) => {
                started = true;if (rejected) Error(value);Advance();
            });
        }
        internal WritableWriter GetWriter()
        {
            if (Writer != null) throw new InvalidOperationException("Writable stream is locked");
            return Writer = new WritableWriter(this);
        }
        internal object DesiredSize => state is 1 or 3 ? null : BoxNumber(state == 2 ? 0 : highWaterMark-totalSize);
        internal void Initialize(WritableWriter writer)
        {
            if (state is 1 or 3) writer.RejectReady(storedError);
            else if (!backpressure || closing || state == 2) writer.ResolveReady();
            if (state == 2) writer.Closed.Settle(Undefined,false);
            else if (state == 3) writer.Closed.Settle(storedError,true);
        }
        void UpdateBackpressure()
        {
            if (state != 0 || closing) return;
            bool next = totalSize >= highWaterMark;
            if (next != backpressure) {
                backpressure = next;
                if (next) Writer?.WaitReady();else Writer?.ResolveReady();
            }
        }
        internal JsPromise Write(WritableWriter writer, object value)
        {
            double amount = 1;
            try {
                if (size != null) amount = Number(size.InvokeWithReceiver(Undefined,new[] {value}));
            }
            catch (Exception exception) {Error(CaughtValue(exception));}
            if (!ReferenceEquals(writer.Owner,this)) return StreamRejection(new JsCaughtError("TypeError","Writer lock has been released"));
            if (state is 1 or 3) return StreamRejection(storedError);
            if (state == 2 || closing) return StreamRejection(new JsCaughtError("TypeError","Writable stream is closing or closed"));
            var result = new JsPromise();
            if (!double.IsFinite(amount) || amount < 0) {
                var error = new JsCaughtError("RangeError","Invalid chunk size");Error(error);result.Settle(error,true);return result;
            }
            queue.Enqueue(new(value,amount,result,false));totalSize += amount;UpdateBackpressure();Advance();return result;
        }
        internal JsPromise Close()
        {
            if (state == 2 || closing) return StreamRejection(new JsCaughtError("TypeError","Writable stream is closing or closed"));
            if (state == 3) return StreamRejection(storedError);
            var result = new JsPromise();closing = true;Writer?.ResolveReady();
            queue.Enqueue(new(Undefined,0,result,true));Advance();return result;
        }
        internal JsPromise Abort(object reason)
        {
            if (state is 2 or 3) return BodyOperation(() => Undefined);
            if (abortRequest != null) return abortRequest;
            controller.Signal.Abort(reason);
            if (state is 2 or 3) return BodyOperation(() => Undefined);
            abortRequest = new JsPromise();var result = abortRequest;
            rejectAbort = state == 1;abortReason = reason;
            Error(reason);Advance();return result;
        }
        internal void Error(object reason)
        {
            if (state != 0) return;
            state = 1;storedError = reason;Writer?.RejectReady(reason);Advance();
        }
        void ClearAlgorithms() {sink = null;write = close = abort = size = null;}
        void FinishError()
        {
            state = 3;
            while (queue.TryDequeue(out var entry)) entry.Promise.Settle(storedError,true);
            totalSize = 0;
            var callback = abort;var receiver = sink;ClearAlgorithms();
            void FinishAbort(object reason, bool rejected)
            {
                abortReason = null;
                abortRequest?.Settle(reason,rejected);abortRequest = null;
                Writer?.Closed.Settle(storedError,true);
            }
            if (abortRequest == null) {Writer?.Closed.Settle(storedError,true);return;}
            if (rejectAbort) {FinishAbort(storedError,true);return;}
            try {
                object completion = callback == null ? Undefined : callback.InvokeWithReceiver(receiver,new[] {abortReason});
                ObserveGeneratorAwait(completion,(value,rejected) => FinishAbort(rejected ? value : Undefined,rejected));
            }
            catch (Exception exception) {FinishAbort(CaughtValue(exception),true);}
        }
        void Advance()
        {
            if (!started || inFlight) return;
            if (state == 1) {FinishError();return;}
            if (state != 0 || !queue.TryPeek(out var entry)) return;
            inFlight = true;
            void Complete(object value, bool rejected)
            {
                inFlight = false;queue.Dequeue();
                if (rejected) {
                    if (entry.Close && abortRequest != null) {abortRequest.Settle(value,true);abortRequest = null;}
                    entry.Promise.Settle(value,true);Error(value);Advance();return;
                }
                entry.Promise.Settle(Undefined,false);
                if (entry.Close) {
                    state = 2;storedError = abortReason = null;ClearAlgorithms();abortRequest?.Settle(Undefined,false);abortRequest = null;
                    Writer?.Closed.Settle(Undefined,false);return;
                }
                totalSize = Math.Max(0,totalSize-entry.Size);UpdateBackpressure();Advance();
            }
            try {
                var callback = entry.Close ? close : write;
                object completion = callback == null ? Undefined : callback.InvokeWithReceiver(sink,entry.Close ? Array.Empty<object>() : new object[] {entry.Value,controller});
                ObserveGeneratorAwait(completion,Complete);
            }
            catch (Exception exception) {Complete(CaughtValue(exception),true);}
        }
    }
    sealed class WritableWriter : JsDescriptorObject
    {
        internal WritableStreamValue Owner;
        internal JsPromise Ready = new() {Handled = true}, Closed = new() {Handled = true};
        bool readyPending = true;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)WritableBuiltins.Writer.Get("prototype");
        internal WritableWriter(WritableStreamValue owner) {Owner = owner;owner.Initialize(this);}
        internal void WaitReady() {if (!readyPending) {Ready = new JsPromise {Handled = true};readyPending = true;}}
        internal void ResolveReady() {Ready.Settle(Undefined,false);readyPending = false;}
        internal void RejectReady(object reason) {WaitReady();Ready.Settle(reason,true);readyPending = false;}
        internal WritableStreamValue RequireOwner() => Owner ?? throw new InvalidOperationException("Writer lock has been released");
        internal void Release()
        {
            if (Owner == null) return;
            Owner.Writer = null;Owner = null;
            var reason = new JsCaughtError("TypeError","Writer lock has been released");
            RejectReady(reason);Closed.Settle(reason,true);Closed = new JsPromise {Handled = true};Closed.Settle(reason,true);
        }
    }
    sealed class WritableController(WritableStreamValue owner) : JsDescriptorObject
    {
        internal readonly WritableStreamValue Owner = owner;
        internal readonly AbortSignalValue Signal = new();
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)WritableBuiltins.Controller.Get("prototype");
    }
}
