using System;

public static partial class Js
{
    static JsPromise BodyPromiseOperation(Func<JsPromise> action)
    {
        try { return action(); }
        catch (Exception error) { var result = new JsPromise(); result.Settle(CaughtValue(error), true); return result; }
    }
    sealed class BufferedTee(BufferedBody source)
    {
        readonly JsPromise[] cancellations = new JsPromise[2];
        bool finished, reading, canceling;
        internal void Start()
        {
            // Native buffered sources have one immutable chunk. Pull it on the
            // job queue, retaining the source reader's lock as stream tee does.
            EnqueuePromiseJob(() =>
            {
                if (finished || canceling) return;
                bool empty = source.Closed;
                if (!empty) source.Tee?.BranchRead();
                source.Disturbed = source.Closed = true;
                source.Reader.Finish();
                if (empty) Finish();
            });
        }
        void Finish()
        {
            if (finished) return;
            finished = true;
            foreach (var promise in cancellations) promise?.Settle(Undefined, false);
        }
        internal void BranchRead()
        {
            if (reading || finished) return;
            reading = true;
            // Removing the buffered chunk permits the next pull to observe EOF.
            EnqueuePromiseJob(Finish);
        }
        internal JsPromise Cancel(int branch)
        {
            var result = cancellations[branch] = new JsPromise();
            if (finished) result.Settle(Undefined, false);
            else if (cancellations[1 - branch] != null) {
                canceling = true;
                ObserveGeneratorAwait(source.CancelPromise(), (value, rejected) =>
                {
                    if (rejected)
                    {
                        finished = true;
                        foreach (var promise in cancellations) promise?.Settle(value, true);
                    }
                    else Finish();
                });
            }
            return result;
        }
    }
    static object TeeBufferedBody(BufferedBody source)
    {
        source.GetReader(Undefined);
        if(source.Failure!=null)return new JsArray(new object[]{new BufferedBody(Array.Empty<byte>()){Failure=source.Failure},new BufferedBody(Array.Empty<byte>()){Failure=source.Failure}});
        byte[] bytes = source.Closed ? Array.Empty<byte>() : source.Position==0 ? source.Bytes : source.Bytes.AsSpan(source.Position).ToArray();
        var state = new BufferedTee(source);
        var first = new BufferedBody(bytes) { Tee = state, TeeBranch = 0 };
        var second = new BufferedBody(bytes) { Tee = state, TeeBranch = 1 };
        state.Start();
        return new JsArray(new object[] { first, second });
    }
    static object CancelBufferedBody(object receiver, bool reader)
    {
        try
        {
            var source = reader ? RequireBufferedReader(receiver).Owner : RequireBufferedBody(receiver);
            if (source == null) throw new InvalidOperationException("Reader lock has been released");
            if (!reader && source.Locked) throw new InvalidOperationException("Body stream is locked");
            return source.CancelPromise();
        }
        catch (Exception error)
        {
            var result = new JsPromise(); result.Settle(CaughtValue(error), true); return result;
        }
    }
}
