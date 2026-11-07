using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

public static partial class Js
{
    sealed class ConsoleLabels
    {
        Dictionary<string, long> timers, counters;
        static string Label(object[] args) => IsUndefined(FunctionArgument(args, 0)) ? "default" : StringValue(args[0]);
        internal object Count(object[] args)
        {
            string label = Label(args);
            counters ??= new(StringComparer.Ordinal);
            counters.TryGetValue(label, out long count);
            counters[label] = ++count;
            Console.WriteLine(label + ": " + count.ToString(CultureInfo.InvariantCulture));
            return Undefined;
        }
        internal object Reset(object[] args)
        {
            string label = Label(args);
            if (counters == null || !counters.ContainsKey(label)) Console.Error.WriteLine("Count for '" + label + "' does not exist");
            else counters[label] = 0;
            return Undefined;
        }
        internal object Start(object[] args)
        {
            string label = Label(args);
            timers ??= new(StringComparer.Ordinal);
            if (!timers.TryAdd(label, Stopwatch.GetTimestamp())) Console.Error.WriteLine("Timer '" + label + "' already exists");
            return Undefined;
        }
        internal object Elapsed(object[] args, bool end)
        {
            string label = Label(args);
            if (timers == null || !timers.TryGetValue(label, out long start)) {
                Console.Error.WriteLine("Timer '" + label + "' does not exist");
                return Undefined;
            }
            double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (end) timers.Remove(label);
            string text = label + ": " + elapsed.ToString("F3", CultureInfo.InvariantCulture) + "ms";
            if (!end && args.Length > 1) text += " " + ConsoleMessage(args[1..]);
            Console.WriteLine(text);
            return Undefined;
        }
    }
}
