using System;

public static partial class Js
{
    static object ObjectFromEntries(object input)
    {
        object source = Iteration.Start(input);
        var result = new JsDynamicObject();
        int position = 0; bool closeable = false;
        try
        {
            while (true)
            {
                closeable = false;
                if (position >= Iteration.Length(source)) break;
                object entry = Iteration.Value(source, position);
                position += Iteration.Step(source, position); closeable = true;
                if (entry is not TsObject) throw new InvalidOperationException("Object entry is not an object");
                // Read both entry properties before coercing the key.
                object key = Get(entry, "0"), value = Get(entry, "1");
                result.DefineOwn(PropertyKey(key), value);
            }
        }
        catch { if (closeable) CloseIterationAt(source, position, true); throw; }
        return result;
    }
}
