using System;
using System.Collections.Generic;
using System.Threading;

public static partial class Js
{
    static object AtomicWaitAsyncResult(bool asynchronous, object value)
    {
        var result = new JsDynamicObject(); result.DefineOwn("async", BoxBoolean(asynchronous)); result.DefineOwn("value", value); return result;
    }
    static object AtomicWaitAsync(object[] args)
    {
        var array = AtomicWaitArray(FunctionArgument(args, 0));
        int index = BinaryIndex(FunctionArgument(args, 1)); AtomicBounds(array, index);
        if (array.Buffer is not SharedBuffer buffer) throw new InvalidOperationException("Waiting requires shared storage");
        object expected = AtomicCell(array, AtomicValue(array, FunctionArgument(args, 2)));
        double timeout = Number(FunctionArgument(args, 3));
        timeout = double.IsNaN(timeout) ? double.PositiveInfinity : Math.Max(timeout, 0);
        var storage = buffer.Storage; int address = array.Offset + index * array.Size;
        lock (storage)
        {
            AtomicBounds(array, index);
            if (!Equals(array.Read(index), expected)) return AtomicWaitAsyncResult(false, "not-equal");
            if (timeout == 0) return AtomicWaitAsyncResult(false, "timed-out");
            var promise = new JsPromise(); var inbox = CaptureHostEvents();
            var waiter = new AtomicWaiter(true);
            storage.Waiters ??= new();
            if (!storage.Waiters.TryGetValue(address, out var queue)) storage.Waiters.Add(address, queue = new());
            var node = queue.AddLast(waiter);
            bool finished = false;
            Timer timer = null; Action cancel = null;
            void Finish(string status)
            {
                lock (storage)
                {
                    if (finished) return; finished = true;
                    if (node.List != null) queue.Remove(node);
                    if (queue.Count == 0 && storage.Waiters.TryGetValue(address, out var current) && ReferenceEquals(queue, current)) storage.Waiters.Remove(address);
                    timer?.Dispose(); waiter.Completion = null;
                    inbox.Unregister(cancel);
                    if (status != null) inbox.Post(() => promise.Settle(status, false));
                }
            }
            cancel = () => Finish(null);
            inbox.Register(cancel);
            waiter.Completion = () => Finish("ok");
            if (!double.IsPositiveInfinity(timeout))
            {
                double started = Now();
                void Tick(object _)
                {
                    lock (storage)
                    {
                        if (finished) return;
                        double remaining = timeout - (Now() - started);
                        if (remaining > 0) timer.Change((long)Math.Ceiling(Math.Min(remaining, uint.MaxValue - 1d)), Timeout.Infinite);
                        else Finish("timed-out");
                    }
                }
                timer = new Timer(Tick, null, Timeout.Infinite, Timeout.Infinite);
                timer.Change((long)Math.Ceiling(Math.Min(timeout, uint.MaxValue - 1d)), Timeout.Infinite);
            }
            return AtomicWaitAsyncResult(true, promise);
        }
    }
}
