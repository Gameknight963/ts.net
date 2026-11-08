using System;

public static partial class Js
{
    static object ConsumeIterator(object receiver, object[] arguments, string mode)
    {
        if (receiver is not TsObject source) throw new InvalidOperationException("Iterator method requires an object");
        object callback = FunctionArgument(arguments, 0);
        if (mode != "toArray" && callback is not JsFunction)
        {
            try { CloseIteratorObject(source); } catch { }
            throw new InvalidOperationException("Iterator callback is not callable");
        }
        var cursor = (JsProtocolIterator)StartIteratorObject(source);
        var values = mode == "toArray" ? new JsArray(0) : null;
        object accumulator = FunctionArgument(arguments, 1);
        bool hasAccumulator = arguments.Length > 1, closeable = false;
        long index = 0;
        try
        {
            while (true)
            {
                closeable = false;
                if (!cursor.Ensure()) break;
                object value = cursor.TakeValue(); closeable = true;
                if (mode == "toArray") values.Append(value);
                else if (mode == "reduce" && !hasAccumulator) { accumulator = value; hasAccumulator = true; }
                else
                {
                    object result = CallMethod(callback, Undefined, mode == "reduce" ? new[] { accumulator, value, (object)(double)index } : new[] { value, (object)(double)index });
                    if (mode == "reduce") accumulator = result;
                    else if (mode is "some" or "every" or "find")
                    {
                        bool truth = Truthy(result);
                        if (mode == "every" ? !truth : truth)
                        {
                            closeable = false; CloseIteration(cursor, false);
                            return mode == "find" ? value : BoxBoolean(mode == "some");
                        }
                    }
                }
                index++;
            }
        }
        catch { if (closeable) CloseIteration(cursor, true); throw; }
        if (mode == "reduce" && !hasAccumulator) throw new InvalidOperationException("Cannot reduce an empty iterator without an initial value");
        return mode switch { "toArray" => values, "reduce" => accumulator, "some" => BoxBoolean(false), "every" => BoxBoolean(true), _ => Undefined };
    }
    static void DefineIteratorTerminals(JsClassInstance prototype)
    {
        foreach (string name in new[] { "toArray", "forEach", "reduce", "some", "every", "find" })
            prototype.DefineMethod(name, new CollectionMethod(name, name == "toArray" ? 0 : 1, (receiver, arguments) => ConsumeIterator(receiver, arguments, name)));
    }
}
