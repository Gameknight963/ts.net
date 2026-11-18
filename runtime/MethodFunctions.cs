using System;

// Simple generated methods bind the receiver directly. They cannot observe
// arguments and do not use an object-method home, so no invocation frame or
// forwarding closure is needed. The compiler emits InvokeWithReceiver.
public abstract class JsMethodFunction : JsFunction
{
    protected JsMethodFunction(object body, object numeric, string name, int length)
        : base(body, numeric, name, length) { }

    public sealed override object Invoke(object[] arguments) => InvokeWithReceiver(Js.GetUndefined(), arguments);
    public abstract override object InvokeWithReceiver(object receiver, object[] arguments);

    public override double? InvokeNumber0() => InvokeMethodNumber0(Js.GetUndefined());
    public override double? InvokeNumber1(double? first) => InvokeMethodNumber1(Js.GetUndefined(), first);
    public override double? InvokeNumber2(double? first, double? second) => InvokeMethodNumber2(Js.GetUndefined(), first, second);
    public override double? InvokeMethodNumber0(object receiver) =>
        Js.Cast<double?>(InvokeWithReceiver(receiver, Array.Empty<object>()), "Function result violates number type");
    public override double? InvokeMethodNumber1(object receiver, double? first) =>
        Js.Cast<double?>(InvokeWithReceiver(receiver, new[] {Js.BoxNumber(first)}), "Function result violates number type");
    public override double? InvokeMethodNumber2(object receiver, double? first, double? second) =>
        Js.Cast<double?>(InvokeWithReceiver(receiver, new[] {Js.BoxNumber(first), Js.BoxNumber(second)}), "Function result violates number type");
}
