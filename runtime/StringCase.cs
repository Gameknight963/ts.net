using System;

public static partial class Js
{
    sealed class StringCaseFunction(bool upper) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, upper ? "toUpperCase" : "toLowerCase", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            return upper ? text.ToUpperInvariant() : text.ToLowerInvariant();
        }
    }
}
