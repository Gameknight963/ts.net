using System;

public static partial class Js
{
    static object MapGroupBy(object items, object callback)
    {
        if (IsNullish(items)) throw new InvalidOperationException("Cannot group null or undefined");
        if (callback is not JsFunction function) throw new InvalidOperationException("Grouping callback is not callable");
        object source = Iteration.Start(items);
        var groups = new JsKeyedCollection(false);
        int position = 0; double index = 0; bool closeable = false;
        try
        {
            while (true)
            {
                // The index limit check precedes IteratorStep, and closes even
                // when no next value has been requested on this iteration.
                if (index >= 9007199254740991d) { closeable = true; throw new InvalidOperationException("Too many grouping elements"); }
                closeable = false;
                if (position >= Iteration.Length(source)) break;
                object value = Iteration.Value(source, position);
                position += Iteration.Step(source, position); closeable = true;
                object key = function.InvokeWithReceiver(Undefined, new[] { value, (object)index });
                if (groups.Read(key) is not JsArray group)
                {
                    group = new JsArray(0); groups.Write(key, group);
                }
                group.Append(value); index++;
            }
        }
        catch { if (closeable) CloseIterationAt(source, position, true); throw; }
        return groups;
    }
}
