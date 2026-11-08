using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class StreamPipe
    {
        readonly object source;
        readonly TsObject reader;
        readonly WritableStreamValue destination;
        readonly WritableWriter writer;
        readonly bool preventAbort, preventCancel, preventClose;
        readonly AbortSignalValue signal;
        Action unsubscribe;
        JsPromise lastWrite;
        bool reading, ended, stopping, finished;
        internal readonly JsPromise Promise = new();
        internal StreamPipe(object source, WritableStreamValue destination, bool preventAbort, bool preventCancel, bool preventClose, AbortSignalValue signal)
        {
            this.source = source;this.destination = destination;this.preventAbort = preventAbort;this.preventCancel = preventCancel;this.preventClose = preventClose;this.signal = signal;
            reader = GetDefaultStreamReader(source,Undefined);writer = destination.GetWriter();
            if (source is BufferedBody body) body.Disturbed = true;
        }
        bool SourceReadable => source is CustomReadableStream stream ? stream.PipeReadable : !((BufferedBody)source).Closed;
        JsPromise ReaderClosed => reader is CustomStreamReader custom ? custom.ClosedPromise : ((BufferedBodyReader)reader).ClosedPromise;
        JsPromise Read() => reader is CustomStreamReader custom ? custom.Read() : BodyOperation(() => ((BufferedBodyReader)reader).Read());
        JsPromise Cancel(object reason) => BodyPromiseOperation(() => reader is CustomStreamReader custom ? custom.Cancel(reason) : ((BufferedBodyReader)reader).Owner.CancelPromise());
        internal void Start()
        {
            if (signal?.Aborted == true) {Abort();return;}
            if (signal != null) unsubscribe = signal.Subscribe(Abort);
            ReaderClosed.Then(new PromiseReaction(null,null,null,(reason,rejected) => {
                if (rejected) SourceFailed(reason);
                else {ended = true;if (!reading) SourceClosed();}
            }));
            writer.Closed.Then(new PromiseReaction(null,null,null,(reason,rejected) => {
                if (rejected) DestinationFailed(reason);
                else DestinationClosed();
            }));
            EnqueuePromiseJob(() => {
                if (destination.PipeClosing || destination.PipeState == 2) DestinationClosed();
            });
            Step();
        }
        void Step()
        {
            if (stopping) return;
            if (ended) {SourceClosed();return;}
            writer.Ready.Then(new PromiseReaction(null,null,null,(reason,rejected) => {
                if (stopping) return;
                if (rejected) {DestinationFailed(reason);return;}
                reading = true;
                Read().Then(new PromiseReaction(null,null,null,(result,failed) => {
                    reading = false;
                    if (failed) {SourceFailed(result);return;}
                    var item = (TsObject)result;
                    if (Truthy(item.Get("done"))) {ended = true;SourceClosed();return;}
                    if (stopping && (destination.PipeState != 0 || destination.PipeClosing)) return;
                    lastWrite = destination.Write(writer,item.Get("value"));
                    lastWrite.Then(new PromiseReaction(null,null,null,(error,writeFailed) => {if (writeFailed) DestinationFailed(error);}));
                    if (!stopping) Step();
                }));
            }));
        }
        void SourceClosed() => Stop(Undefined,false,preventClose ? null : () => destination.PipeClosing || destination.PipeState == 2 ? BodyOperation(() => Undefined) : destination.Close());
        void SourceFailed(object reason) => Stop(reason,true,preventAbort ? null : () => destination.Abort(reason));
        void DestinationFailed(object reason) => Stop(reason,true,preventCancel ? null : () => Cancel(reason));
        void DestinationClosed() => DestinationFailed(new JsCaughtError("TypeError","Destination is closing or closed"));
        void Abort()
        {
            object reason = signal.Reason;
            Stop(reason,true,() => {
                var actions = new List<JsPromise>();
                if (!preventAbort && destination.PipeState == 0) actions.Add(destination.Abort(reason));
                if (!preventCancel && SourceReadable) actions.Add(Cancel(reason));
                var result = new JsPromise();int remaining = actions.Count;
                if (remaining == 0) result.Settle(Undefined,false);
                foreach (var action in actions) action.Then(new PromiseReaction(null,null,null,(value,rejected) => {
                    if (rejected) result.Settle(value,true);
                    else if (--remaining == 0) result.Settle(Undefined,false);
                }));
                return result;
            });
        }
        void Stop(object error, bool rejected, Func<JsPromise> action)
        {
            if (stopping) return;stopping = true;
            void Perform()
            {
                if (action == null) {Finish(error,rejected);return;}
                BodyPromiseOperation(action).Then(new PromiseReaction(null,null,null,(value,failed) => Finish(failed ? value : error,failed || rejected)));
            }
            void Drain()
            {
                var pending = lastWrite;
                if (destination.PipeState != 0 || destination.PipeClosing || pending == null) {Perform();return;}
                pending.Then(new PromiseReaction(null,null,null,(_,_) => {
                    if (ReferenceEquals(pending,lastWrite)) Perform();else Drain();
                }));
            }
            // An already-fulfilled read may still have its delivery job queued.
            // Allow it to forward that chunk before waiting for outstanding writes.
            EnqueuePromiseJob(Drain);
        }
        void Finish(object value, bool rejected)
        {
            if (finished) return;finished = true;
            unsubscribe?.Invoke();unsubscribe = null;
            writer.Release();
            if (reader is CustomStreamReader custom) custom.Release();else ((BufferedBodyReader)reader).Release();
            Promise.Settle(rejected ? value : Undefined,rejected);
        }
    }
    static JsPromise StartStreamPipe(object source, object target, object options)
    {
        _ = StreamLocked(source);
        var destination = RequireWritable(target);
        if (!IsNullish(options) && options is not TsObject) throw new InvalidOperationException("Pipe options must be an object");
        var settings = options as TsObject;
        bool preventAbort = settings != null && Truthy(settings.Get("preventAbort"));
        bool preventCancel = settings != null && Truthy(settings.Get("preventCancel"));
        bool preventClose = settings != null && Truthy(settings.Get("preventClose"));
        object signalValue = settings == null ? Undefined : settings.Get("signal");
        if (!IsUndefined(signalValue) && signalValue is not AbortSignalValue) throw new InvalidOperationException("Pipe signal must be an AbortSignal");
        if (StreamLocked(source) || destination.Writer != null) throw new InvalidOperationException("Pipe streams must be unlocked");
        var pipe = new StreamPipe(source,destination,preventAbort,preventCancel,preventClose,signalValue as AbortSignalValue);
        pipe.Start();return pipe.Promise;
    }
    static object PipeThroughStreams(object source, object pair, object options)
    {
        _ = StreamLocked(source);
        if (pair is not TsObject transform) throw new InvalidOperationException("Readable/writable pair required");
        object readable = transform.Get("readable"),writable = transform.Get("writable");
        _ = StreamLocked(readable);
        var pipe = StartStreamPipe(source,writable,options);pipe.Handled = true;
        return readable;
    }
}
