using System;
using System.Collections.Generic;

internal sealed class JsCreatedObject : JsDynamicObject { }

public static partial class Js
{
    static object ObjectCreate(object prototype, object properties)
    {
        if (prototype != null && prototype is not TsObject) throw new InvalidOperationException("Object prototype must be an object or null");
        var result = new JsCreatedObject(); result.SetPrototype((TsObject)prototype);
        if (IsUndefined(properties)) return result;
        TsObject source = ToObject(properties);
        var keys = (JsArray)source.OwnPropertyNames();
        var descriptors = new List<JsDynamicObject.Descriptor>();
        for (uint index = 0; index < keys.Length(); index++)
        {
            string key = (string)keys.ElementAt(index);
            if (!source.HasOwn(key) || !source.ForInEnumerable(key)) continue;
            descriptors.Add(ReadPropertyDefinition(source.Get(key)).Complete(key));
        }
        foreach (var descriptor in descriptors) result.Define(descriptor);
        return result;
    }
}
