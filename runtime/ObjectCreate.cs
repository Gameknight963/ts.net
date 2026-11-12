using System;

internal sealed class JsCreatedObject : JsDynamicObject
{
    public override bool TrySetNumberProperty(string key, double? value) => TrySetStoredNumber(key, value);
    public override double? GetNumberProperty(string key) => GetStoredNumberWithReceiver(key, this);
    public override double? GetNumberWithReceiver(string key, object receiver) => GetStoredNumberWithReceiver(key, receiver);
}

public static partial class Js
{
    static object ObjectCreate(object prototype, object properties)
    {
        if (prototype != null && prototype is not TsObject) throw new InvalidOperationException("Object prototype must be an object or null");
        var result = new JsCreatedObject(); result.SetPrototype((TsObject)prototype);
        if (IsUndefined(properties)) return result;
        foreach (var entry in ReadPropertyDefinitions(properties)) result.Define(entry.Definition.Complete(entry.Key));
        return result;
    }
}
