using System;

// The hidden first argument is a normal compiler parameter, so arrows can
// capture it without a global receiver or a mutable thread-local slot.
public sealed class JsReceiverFunction : JsFunction
{
    readonly Invocation invocation;
    sealed class Invocation(FunctionBody body)
    {
        internal string SourceText => Js.SourceOf(body);
        internal bool UsesArguments;
        internal TsObject Home;
        object WrapReceiver(object receiver) => Home == null ? receiver : new JsObjectMethodReceiver(Home, receiver);
        internal object Invoke(object receiver, object[] arguments)
        {
            var values = new object[arguments.Length + 1];
            Array.Copy(arguments, 0, values, 1, arguments.Length);
            values[0] = receiver;
            return InvokeValues(values);
        }
        internal object InvokeValues(object[] values)
        {
            // Callers own this array; slot zero is reserved for the receiver.
            object receiver = WrapReceiver(values[0]);
            if (UsesArguments) receiver = new JsArgumentsReceiver(receiver, values);
            values[0] = receiver;
            return body(values);
        }
    }
    public JsReceiverFunction(object body, object numeric, string name, int length)
        : this(new Invocation((FunctionBody)body), numeric, name, length) { }
    JsReceiverFunction(Invocation invocation, object numeric, string name, int length)
        : base((FunctionBody)(arguments => invocation.Invoke(Js.Undefined, arguments)), numeric, name, length)
    {
        this.invocation = invocation;
    }
    internal override string SourceText => invocation.SourceText;
    internal override void SetObjectHome(TsObject home) => invocation.Home = home;
    internal void EnableArguments() => invocation.UsesArguments = true;
    public override object InvokeWithReceiver(object receiver, object[] arguments) => invocation.Invoke(receiver, arguments);
    internal object InvokeNumberArgument(object receiver, double? value) => invocation.InvokeValues(new[] { receiver, Js.BoxNumber(value) });
    public override double? InvokeMethodNumber0(object receiver) => Js.Cast<double?>(invocation.InvokeValues(new[] { receiver }), "Function result violates number type");
    public override double? InvokeMethodNumber1(object receiver, double? first) => Js.Cast<double?>(invocation.InvokeValues(new[] { receiver, Js.BoxNumber(first) }), "Function result violates number type");
    public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Js.Cast<double?>(invocation.InvokeValues(new[] { receiver, Js.BoxNumber(first), Js.BoxNumber(second) }), "Function result violates number type");
}
