using System;

public static partial class Js
{
    static object ObjectAssign(object[] arguments)
    {
        var target = ToObject(FunctionArgument(arguments, 0));
        for (int i = 1; i < arguments.Length; i++)
        {
            object input = arguments[i];
            if (IsNullish(input)) continue;
            var source = ToObject(input);
            var keys = (JsArray)source.OwnPropertyNames();
            for (uint index = 0; index < keys.Length(); index++)
            {
                string key = (string)keys.ElementAt(index);
                if (source.HasOwn(key) && source.ForInEnumerable(key))
                    target.SetWithReceiver(key, source.Get(key), target);
            }
        }
        return target;
    }
}
