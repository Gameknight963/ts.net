using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public static partial class Js
{
    sealed class StreamUpload : HttpContent
    {
        readonly CustomStreamReader reader;
        readonly HostEventInbox inbox;
        TaskCompletionSource<byte[]> pending;
        bool ended, stopped;
        internal CancellationToken NetworkCancellation;
        internal StreamUpload(CustomReadableStream source, HostEventInbox inbox)
        {
            reader = source.GetReader(Undefined);source.Disturbed = true;this.inbox = inbox;
        }
        protected override bool TryComputeLength(out long length) {length = 0;return false;}
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) => SerializeToStreamAsync(stream,context,NetworkCancellation);
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,NetworkCancellation);
            var token = linked.Token;
            for (;;) {
                token.ThrowIfCancellationRequested();
                var next = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                // Promise jobs and stream callbacks run only on the host thread.
                inbox.Post(() => Read(next));
                var bytes = await next.Task.WaitAsync(token).ConfigureAwait(false);
                if (bytes == null) return;
                await stream.WriteAsync(bytes,token).ConfigureAwait(false);
                await stream.FlushAsync(token).ConfigureAwait(false);
            }
        }
        void Read(TaskCompletionSource<byte[]> next)
        {
            if (stopped) {next.TrySetCanceled();return;}
            pending = next;
            reader.Read().Then(new PromiseReaction(null,null,null,(value,rejected) => {
                if (stopped) return;
                pending = null;
                if (rejected) {Fail(next);return;}
                try {
                    var item = (TsObject)value;
                    if (Truthy(item.Get("done"))) {ended = true;next.TrySetResult(null);return;}
                    if (item.Get("value") is not TypedArrayValue {Name:"Uint8Array"} chunk) throw new InvalidOperationException("Upload chunks must be Uint8Array");
                    // The producer may mutate its view after this read completes.
                    next.TrySetResult(EncodingInput(chunk).ToArray());
                }
                catch (Exception) {Fail(next);}
            }));
        }
        static void Fail(TaskCompletionSource<byte[]> next)
        {
            next.TrySetException(new HttpRequestException("Upload stream failed"));
        }
        internal void Stop(object reason)
        {
            if (stopped || ended) return;
            stopped = true;
            pending?.TrySetCanceled();pending = null;
            var cancelled = BodyPromiseOperation(() => reader.Cancel(reason));cancelled.Handled = true;
        }
    }
}
