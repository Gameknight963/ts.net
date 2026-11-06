using System;

// The hidden first argument is a normal compiler parameter, so arrows can
// capture it without a global receiver or a mutable thread-local slot.
public sealed class JsReceiverFunction : JsFunction
{
    readonly FunctionBody receiverBody;
    public JsReceiverFunction(object body, object numeric, string name, int length)
        : base((FunctionBody)(arguments => InvokeBody((FunctionBody)body, Js.Undefined, arguments)), numeric, name, length)
    {
        receiverBody = (FunctionBody)body;
    }
    static object InvokeBody(FunctionBody body, object receiver, object[] arguments)
    {
        var values = new object[arguments.Length + 1];
        values[0] = receiver;
        Array.Copy(arguments, 0, values, 1, arguments.Length);
        return body(values);
    }
    public override object InvokeWithReceiver(object receiver, object[] arguments) => InvokeBody(receiverBody, receiver, arguments);
    public override double? InvokeMethodNumber0(object receiver) => Js.Cast<double?>(receiverBody(new[] { receiver }), "Function result violates number type");
    public override double? InvokeMethodNumber1(object receiver, double? first) => Js.Cast<double?>(receiverBody(new[] { receiver, Js.BoxNumber(first) }), "Function result violates number type");
    public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Js.Cast<double?>(receiverBody(new[] { receiver, Js.BoxNumber(first), Js.BoxNumber(second) }), "Function result violates number type");
}
