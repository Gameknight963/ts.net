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
    if (!Equals(Js.CallMethodNumberArgument(new SeparateNumericCalls(), "receiver", 2), "method number"))
        throw new Exception("A numeric method bypassed its native receiver override");
    if (!Equals(Js.CallMethodNumberArgument(new DirectNumericCalls(), "receiver", 2), "direct number"))
        throw new Exception("A numeric method bypassed its direct entry point");
    var methodReceiver = new JsDynamicObject();
    var method = new DirectMethod(methodReceiver);
    if (Js.CallMethodNumber0(method, methodReceiver) != 10 ||
        Js.CallMethodNumber1(method, methodReceiver, 2) != 12 ||
        Js.CallMethodNumber2(method, methodReceiver, 2, 3) != 15 ||
        Js.CallFunctionNumber0(method) != -10 || Js.CallFunctionNumber1(method, 2) != -8 ||
        Js.CallFunctionNumber2(method, 2, 3) != -5)
        throw new Exception("Direct method numeric calls lost their receiver or arguments");
    methodReceiver.DefineAccessor("value", new BoundNumericGetter(methodReceiver), 1);
    if (Js.GetNumber(methodReceiver, "value") != 42)
        throw new Exception("A numeric accessor ignored its bound receiver entry point");
    var numericFunction = new JsFunction((FunctionBody)(static _ => Js.GetUndefined()),
        (NumberFunction1)(static value => value + 1), "numeric constructor", 1);
    FunctionBody factory = static _ => {
        var instance = new JsDynamicObject();
        instance.Set("factory", true);
        return instance;
    };
    Js.MarkFunctionPrototype(numericFunction, factory);
    if (!numericFunction.HasOwn("prototype") || numericFunction.InvokeNumber1(2) != 3)
        throw new Exception("Constructor metadata replaced the numeric delegate");
    var prototype = numericFunction.Get("prototype");
    if (prototype is not TsObject || !ReferenceEquals(prototype, numericFunction.Get("prototype")))
        throw new Exception("A lazy function prototype changed identity");
    var constructed = (TsObject)numericFunction.Construct(Array.Empty<object>());
    if (!Equals(constructed.Get("factory"), true) || numericFunction.InvokeNumber1(3) != 4)
        throw new Exception("Materializing a prototype replaced the construction factory or numeric delegate");
    numericFunction.Set("prototype", null);
    if (numericFunction.Get("prototype") != null || !numericFunction.HasOwn("prototype") ||
        !Equals(((TsObject)numericFunction.Construct(Array.Empty<object>())).Get("factory"), true))
        throw new Exception("A null function prototype lost construction metadata");
    numericFunction.Set("prototype", prototype);
    if (!ReferenceEquals(numericFunction.Get("prototype"), prototype) || numericFunction.InvokeNumber1(4) != 5)
        throw new Exception("Replacing a prototype changed unrelated function state");
    Console.WriteLine("PASS ordinary function calls, receiver calls, and zero/numeric argument overrides");
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

sealed class SeparateNumericCalls() : JsFunction((FunctionBody)(_ => "delegate"), null, "separate number", 1)
{
    public override object InvokeWithReceiver(object receiver, object[] arguments) =>
        arguments.Length == 1 && Equals(arguments[0], 2d) && Equals(receiver, "receiver") ? "method number"
            : throw new Exception("Numeric method receiver or argument changed");
}
sealed class DirectNumericCalls() : JsFunction((FunctionBody)(_ => "delegate"), null, "direct number", 1)
{
    public override object InvokeReceiverNumber1(object receiver, double? value) =>
        value == 2 && Equals(receiver, "receiver") ? "direct number"
            : throw new Exception("Direct numeric method receiver or argument changed");
}

sealed class DirectMethod(object expected) : JsMethodFunction((FunctionBody)(_ => throw new Exception("Legacy method adapter ran")), null, "method", 2)
{
    public override object InvokeWithReceiver(object receiver, object[] arguments)
    {
        double result = ReferenceEquals(receiver, expected) ? 10 : ReferenceEquals(receiver, Js.GetUndefined()) ? -10
            : throw new Exception("Direct method receiver changed");
        foreach (var argument in arguments) result += (double)argument;
        return result;
    }
}
sealed class BoundNumericGetter(object expected) : JsFunction((FunctionBody)(_ => 0d), null, "get value", 0)
{
    public override bool SupportsNumberResult() => true;
    public override double? InvokeNumber0() => -1;
    public override double? InvokeMethodNumber0(object receiver) => ReferenceEquals(receiver, expected) ? 42
        : throw new Exception("Numeric getter receiver changed");
}
