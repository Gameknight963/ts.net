using System;

public static partial class Js
{
    sealed class ObjectValueOfFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Object valueOf requires a receiver")), null, "valueOf", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => ToObject(receiver);
    }
}
