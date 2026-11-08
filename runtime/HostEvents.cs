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
        readonly Dictionary<Action, bool> cancellations = new();
        int references;
        readonly AutoResetEvent wake = new(false);
        bool closed;
        internal bool HasReferences { get { lock (gate) return references != 0; } }
        internal void Register(Action cancel, bool referenced = false)
        {
            lock (gate) {
                if (closed) throw new ObjectDisposedException(nameof(HostEventInbox));
                if (cancellations.TryAdd(cancel, referenced) && referenced) references++;
            }
        }
        internal void Unregister(Action cancel)
        {
            lock (gate) if (cancellations.Remove(cancel, out bool referenced) && referenced) references--;
        }
        internal void Post(Action action) { lock (gate) { if (closed) return; ready.Enqueue(action); wake.Set(); } }
        internal bool RunNext() { if (!ready.TryDequeue(out var action)) return false; action(); return true; }
        internal void Wait(int milliseconds) => wake.WaitOne(milliseconds);
        public void Dispose()
        {
            Action[] pending;
            lock (gate) { closed = true; pending = cancellations.Keys.ToArray(); cancellations.Clear(); references = 0; ready.Clear(); wake.Dispose(); }
            foreach (var cancel in pending) cancel();
        }
    }
    [ThreadStatic] static HostEventInbox hostEvents;
    static HostEventInbox CaptureHostEvents() => hostEvents ??= new();
    static bool HasReferencedHostEvents => hostEvents?.HasReferences ?? false;
    static bool RunNextHostEvent() => hostEvents?.RunNext() ?? false;
    static void ClearHostEvents() { var previous = hostEvents; hostEvents = null; previous?.Dispose(); }
}
