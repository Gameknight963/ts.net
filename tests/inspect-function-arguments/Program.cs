try
{
    var function = new SeparateCalls();
    if (!Equals(Js.CallFunctionArgument(function, "value"), "function"))
        throw new Exception("An ordinary function call used the receiver entry point");
    if (!Equals(Js.CallMethodArgument(function, "receiver", "value"), "method"))
        throw new Exception("A method call lost its receiver");
    var receiverFunction = new JsReceiverFunction((FunctionBody)(arguments => {
        if (arguments.Length != 2 || !Equals(arguments[1], "value")) throw new Exception("Argument list changed");
        return ReferenceEquals(arguments[0], Js.GetUndefined()) ? "function" : "method";
    }), null, "receiver", 1);
    if (!Equals(Js.CallFunctionArgument(receiverFunction, "value"), "function") ||
        !Equals(Js.CallMethodArgument(receiverFunction, "receiver", "value"), "method"))
        throw new Exception("Receiver functions changed their argument convention");
    var specialized = new SeparateReceiverCalls();
    if (!Equals(Js.CallFunctionArgument(specialized, "value"), "custom function") ||
        !Equals(Js.CallMethodArgument(specialized, "receiver", "value"), "method"))
        throw new Exception("A receiver function's ordinary call override was bypassed");
    if (!Equals(Js.CallMethod0(new SeparateZeroCalls(), "receiver"), "method zero"))
        throw new Exception("A zero-argument method bypassed its native receiver override");
    if (!Equals(Js.CallMethod0(new DirectZeroCalls(), "receiver"), "direct zero"))
        throw new Exception("A zero-argument method bypassed its direct entry point");
    Console.WriteLine("PASS ordinary function calls, receiver calls, and zero-argument overrides");
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    Environment.ExitCode = 1;
}

sealed class SeparateCalls() : JsFunction((FunctionBody)(_ => "delegate"), null, "separate", 1)
{
    public override object Invoke(object[] arguments) => "function";
    public override object InvokeWithReceiver(object receiver, object[] arguments) =>
        arguments.Length == 1 && Equals(arguments[0], "value")
            ? Equals(receiver, "receiver") ? "method" : "unexpected receiver"
            : throw new Exception("Method arguments changed");
}

sealed class SeparateReceiverCalls() : JsReceiverFunction((FunctionBody)(_ => "method"), null, "separate receiver", 1)
{
    public override object Invoke(object[] arguments) => "custom function";
}

sealed class SeparateZeroCalls() : JsFunction((FunctionBody)(_ => "delegate"), null, "separate zero", 0)
{
    public override object Invoke(object[] arguments) => "function zero";
    public override object InvokeWithReceiver(object receiver, object[] arguments) =>
        arguments.Length == 0 && Equals(receiver, "receiver") ? "method zero"
            : throw new Exception("Zero-argument method receiver or arguments changed");
}
sealed class DirectZeroCalls() : JsFunction((FunctionBody)(_ => "delegate"), null, "direct zero", 0)
{
    public override object InvokeReceiver0(object receiver) => Equals(receiver, "receiver") ? "direct zero"
        : throw new Exception("Direct zero-argument method receiver changed");
}
