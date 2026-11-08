using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public static partial class Js
{
    sealed class ScheduledTimer(long id, JsFunction callback, object[] arguments, int delay, bool repeating, bool referenced)
    {
        internal readonly long Id = id;
        internal readonly int Delay = delay;
        internal readonly bool Repeating = repeating, Referenced = referenced;
        internal JsFunction Callback = callback;
        internal object[] Arguments = arguments;
    }
    [ThreadStatic] static PriorityQueue<ScheduledTimer, (long Due, long Order)> timers;
    [ThreadStatic] static Dictionary<long, ScheduledTimer> activeTimers;
    [ThreadStatic] static long nextTimerId, timerOrder;
    [ThreadStatic] static int referencedTimers;

    static object ScheduleTimer(object[] args, bool repeating, bool referenced = true)
    {
        if (FunctionArgument(args, 0) is not JsFunction callback)
            throw MakeThrown(new JsCaughtError("TypeError", "Timer callback must be a function"));
        double value = Number(FunctionArgument(args, 1));
        int delay = !double.IsFinite(value) || value < 1 || value > int.MaxValue ? 1 : (int)value;
        var arguments = args.Length > 2 ? args[2..] : Array.Empty<object>();
        var timer = new ScheduledTimer(++nextTimerId, callback, arguments, delay, repeating, referenced);
        if (referenced) referencedTimers++;
        (activeTimers ??= new()).Add(timer.Id, timer);
        (timers ??= new()).Enqueue(timer, (Environment.TickCount64 + delay, ++timerOrder));
        return (double)timer.Id;
    }
    static object CancelTimer(object[] args)
    {
        double value = Number(FunctionArgument(args, 0));
        if (double.IsFinite(value) && value >= 1 && value <= long.MaxValue && value == Math.Truncate(value)
            && activeTimers != null && activeTimers.Remove((long)value, out var timer))
        {
            if (timer.Referenced) referencedTimers--;
            timer.Callback = null; timer.Arguments = Array.Empty<object>();
            if (activeTimers.Count == 0) timers.Clear();
        }
        return Undefined;
    }
    static bool RunNextTimer()
    {
        if (RunNextHostEvent()) return true;
        // AbortSignal timeout timers run alongside live work, but never keep it alive.
        if (referencedTimers == 0) return false;
        while (timers != null && timers.TryPeek(out var timer, out var priority))
        {
            if (RunNextHostEvent()) return true;
            if (!activeTimers.ContainsKey(timer.Id)) { timers.Dequeue(); continue; }
            long remaining = priority.Due - Environment.TickCount64;
            if (remaining > 0)
            {
                // Keep user callbacks and Promise jobs on the entry thread.
                int delay=(int)Math.Min(remaining,int.MaxValue);
                if(hostEvents != null) hostEvents.Wait(delay);
                else Task.Delay(delay).GetAwaiter().GetResult();
                continue;
            }
            timers.Dequeue();
            if (!timer.Repeating) { activeTimers.Remove(timer.Id); if (timer.Referenced) referencedTimers--; }
            timer.Callback.InvokeWithReceiver(Undefined, timer.Arguments);
            if (timer.Repeating && activeTimers.ContainsKey(timer.Id))
                timers.Enqueue(timer, (Environment.TickCount64 + timer.Delay, ++timerOrder));
            return true;
        }
        return false;
    }
    static void ClearTimers() { timers = null; activeTimers = null; referencedTimers = 0; }
    static class TimerFunctions
    {
        internal static readonly JsFunction Timeout = new((FunctionBody)(static args => ScheduleTimer(args, false)), null, "setTimeout", 1);
        internal static readonly JsFunction Interval = new((FunctionBody)(static args => ScheduleTimer(args, true)), null, "setInterval", 1);
        static readonly JsFunction ClearTimeout = new((FunctionBody)CancelTimer, null, "clearTimeout", 1);
        static readonly JsFunction ClearInterval = new((FunctionBody)CancelTimer, null, "clearInterval", 1);
        internal static JsFunction Clear(string name) => name == "clearTimeout" ? ClearTimeout : ClearInterval;
    }
}
