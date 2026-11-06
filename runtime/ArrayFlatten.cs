using System;
using System.Collections.Generic;

public static partial class Js
{
    struct FlattenFrame(TsObject source, long length, double depth, bool map)
    {
        internal TsObject Source = source;
        internal long Length = length;
        internal long Index;
        internal double Depth = depth;
        internal bool Map = map;
    }
    sealed class ArrayFlattenFunction(string name, bool map) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, name, map ? 1 : 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var source = ToObject(receiver);
            long length = IndexedLength(source);
            JsFunction callback = null;
            double depth = 1;
            if (map) callback = FunctionArgument(arguments, 0) as JsFunction ?? throw new InvalidOperationException("Array callback is not callable");
            else
            {
                object argument = FunctionArgument(arguments, 0);
                if (!IsUndefined(argument)) { depth = Number(argument); depth = double.IsNaN(depth) || depth < 0 ? 0 : Math.Truncate(depth); }
            }
            object thisValue = FunctionArgument(arguments, 1);
            var result = new JsArray(0);
            var stack = new List<FlattenFrame> { new(source, length, depth, map) };
            while (stack.Count != 0)
            {
                int top = stack.Count - 1;
                var frame = stack[top];
                if (frame.Index == frame.Length) { stack.RemoveAt(top); continue; }
                long index = frame.Index++;
                stack[top] = frame;
                if (!TryIndexedValue(frame.Source, index, out object value)) continue;
                if (frame.Map) value = callback.InvokeWithReceiver(thisValue, new object[] { value, (double)index, source });
                if (frame.Depth > 0 && value is JsArray array)
                {
                    // Bound recursive input just as an engine bounds its call stack.
                    // Ordinary deep input uses managed frames rather than C# recursion.
                    if (stack.Count >= 65536) throw new ArgumentOutOfRangeException(nameof(receiver), "Maximum flatten depth exceeded");
                    stack.Add(new FlattenFrame(array, IndexedLength(array), frame.Depth - 1, false));
                }
                else result.Append(value);
            }
            return result;
        }
    }
}
