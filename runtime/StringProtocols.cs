using System;

public static partial class Js
{
    static bool IsRegExpValue(object value)
    {
        if (value is not TsObject source) return false;
        object matcher = source.Get(PropertyKey(MatchSymbol));
        return IsUndefined(matcher) ? source is JsRegExp : Truthy(matcher);
    }
    static void RequireGlobalRegExp(object value, string method)
    {
        if (!IsRegExpValue(value)) return;
        object flags = Get(value, "flags");
        if (IsNullish(flags) || !StringValue(flags).Contains('g')) throw new InvalidOperationException(method + " requires a global regular expression");
    }
    static bool TryStringProtocol(object receiver, object pattern, JsSymbol symbol, object second, bool twoArguments, out object result)
    {
        if (IsNullish(receiver)) throw new InvalidOperationException("String method requires a receiver");
        result = Undefined;
        if (IsNullish(pattern)) return false;
        object method = Get(pattern, PropertyKey(symbol));
        if (IsNullish(method)) return false;
        if (method is RegExpProtocolFunction native)
        {
            result = native.Execute(pattern, receiver, second);
            return true;
        }
        if (method is not JsFunction function) throw new InvalidOperationException("String Symbol method is not callable");
        result = function.InvokeWithReceiver(pattern, twoArguments ? new[] { receiver, second } : new[] { receiver });
        return true;
    }
}
