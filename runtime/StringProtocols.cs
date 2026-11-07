using System;

public static partial class Js
{
    static bool TryStringProtocol(object receiver, object pattern, JsSymbol symbol, object second, bool twoArguments, out object result)
    {
        if (IsNullish(receiver)) throw new InvalidOperationException("String method requires a receiver");
        result = Undefined;
        if (IsNullish(pattern)) return false;
        object method = Get(pattern, PropertyKey(symbol));
        if (IsNullish(method)) return false;
        if (method is not JsFunction function) throw new InvalidOperationException("String Symbol method is not callable");
        result = function.InvokeWithReceiver(pattern, twoArguments ? new[] { receiver, second } : new[] { receiver });
        return true;
    }
}
