using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public static partial class Js
{
    sealed class HostEventInbox : IDisposable
    {
        readonly object gate = new();
        readonly ConcurrentQueue<Action> ready = new();
        readonly HashSet<Action> cancellations = new();
        readonly AutoResetEvent wake = new(false);
        bool closed;
        internal void Register(Action cancel) { lock (gate) { if (closed) throw new ObjectDisposedException(nameof(HostEventInbox)); cancellations.Add(cancel); } }
        internal void Unregister(Action cancel) { lock (gate) cancellations.Remove(cancel); }
        internal void Post(Action action) { lock (gate) { if (closed) return; ready.Enqueue(action); wake.Set(); } }
        internal bool RunNext() { if (!ready.TryDequeue(out var action)) return false; action(); return true; }
        internal void Wait(int milliseconds) => wake.WaitOne(milliseconds);
        public void Dispose()
        {
            Action[] pending;
            lock (gate) { closed = true; pending = cancellations.ToArray(); cancellations.Clear(); ready.Clear(); wake.Dispose(); }
            foreach (var cancel in pending) cancel();
        }
    }
    [ThreadStatic] static HostEventInbox hostEvents;
    static HostEventInbox CaptureHostEvents() => hostEvents ??= new();
    static bool RunNextHostEvent() => hostEvents?.RunNext() ?? false;
    static void ClearHostEvents() { var previous = hostEvents; hostEvents = null; previous?.Dispose(); }
}
