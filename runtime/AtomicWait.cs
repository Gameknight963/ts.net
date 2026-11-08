using System;
using System.Collections.Generic;
using System.Threading;

public static partial class Js
{
    sealed class AtomicWaiter(bool asynchronous = false) : IDisposable
    {
        internal readonly ManualResetEventSlim Ready = asynchronous ? null : new(false, 0);
        internal bool Notified;
        internal Action Completion;
        public void Dispose() => Ready?.Dispose();
    }
    static TypedArrayValue AtomicWaitArray(object value)
    {
        var array = AtomicArray(value);
        if (array.Kind is not ("Int32" or "BigInt64")) throw new InvalidOperationException("Waiting requires Int32Array or BigInt64Array");
        return array;
    }
    static object AtomicWait(object[] args)
    {
        var array = AtomicWaitArray(FunctionArgument(args, 0));
        int index = BinaryIndex(FunctionArgument(args, 1)); AtomicBounds(array, index);
        if (array.Buffer is not SharedBuffer buffer) throw new InvalidOperationException("Waiting requires shared storage");
        object expected = AtomicCell(array, AtomicValue(array, FunctionArgument(args, 2)));
        double timeout = Number(FunctionArgument(args, 3));
        timeout = double.IsNaN(timeout) ? double.PositiveInfinity : Math.Max(timeout, 0);
        var storage = buffer.Storage; int address = array.Offset + index * array.Size;
        AtomicWaiter waiter; LinkedList<AtomicWaiter> queue; LinkedListNode<AtomicWaiter> node;
        lock (storage) {
            AtomicBounds(array, index);
            if (!Equals(array.Read(index), expected)) return "not-equal";
            if (timeout == 0) return "timed-out";
            waiter = new();
            storage.Waiters ??= new();
            if (!storage.Waiters.TryGetValue(address, out queue)) storage.Waiters.Add(address, queue = new());
            node = queue.AddLast(waiter);
        }
        bool notified = false;
        try {
            if (double.IsPositiveInfinity(timeout)) waiter.Ready.Wait();
            else {
                double start = Now(), remaining = timeout;
                while (remaining > 0 && !waiter.Ready.Wait((int)Math.Ceiling(Math.Min(remaining, int.MaxValue))))
                    remaining = timeout - (Now() - start);
            }
        } finally {
            lock (storage) {
                notified = waiter.Notified;
                if (node.List != null) queue.Remove(node);
                if (queue.Count == 0 && storage.Waiters.TryGetValue(address, out var current) && ReferenceEquals(queue, current)) storage.Waiters.Remove(address);
                waiter.Dispose();
            }
        }
        return notified ? "ok" : "timed-out";
    }
    static object AtomicNotify(object[] args)
    {
        var array = AtomicWaitArray(FunctionArgument(args, 0));
        int index = BinaryIndex(FunctionArgument(args, 1)); AtomicBounds(array, index);
        object value = FunctionArgument(args, 2);
        double count = IsUndefined(value) ? double.PositiveInfinity : Number(value);
        count = double.IsNaN(count) ? 0 : Math.Max(0, Math.Truncate(count));
        if (array.Buffer is not SharedBuffer buffer) return 0d;
        var storage = buffer.Storage; int address = array.Offset + index * array.Size;
        lock (storage) {
            AtomicBounds(array, index);
            if (count == 0 || storage.Waiters == null || !storage.Waiters.TryGetValue(address, out var queue)) return 0d;
            int notified = 0;
            while (queue.First != null && notified < count) {
                var waiter = queue.First.Value; queue.RemoveFirst();
                waiter.Notified = true; waiter.Ready?.Set(); waiter.Completion?.Invoke(); notified++;
            }
            if (queue.Count == 0) storage.Waiters.Remove(address);
            return (double)notified;
        }
    }
}
