using System;

internal sealed class JsFunctionMethod(string name, int mode) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Function method requires a callable receiver")), null, name, mode == 1 ? 2 : 1)
{
    object Execute(object receiver, object[] arguments)
    {
        if (receiver is not JsFunction function) throw new InvalidOperationException("Function method requires a callable receiver");
        object thisValue = Js.FunctionArgument(arguments, 0);
        if (mode == 2) return JsBoundFunction.Create(function, thisValue, Tail(arguments));
        return Js.CallMethod(function, thisValue, mode == 0 ? Tail(arguments) : ApplyArguments(Js.FunctionArgument(arguments, 1)));
    }
    static object[] Tail(object[] arguments)
    {
        if (arguments.Length <= 1) return Array.Empty<object>();
        var result = new object[arguments.Length - 1]; Array.Copy(arguments, 1, result, 0, result.Length); return result;
    }
    internal static object[] ApplyArguments(object value)
    {
        if (Js.IsNullish(value)) return Array.Empty<object>();
        if (value is not TsObject) throw new InvalidOperationException("apply requires an array-like object");
        double length = Js.Number(Js.Get(value, "length"));
        if (double.IsNaN(length) || length <= 0) return Array.Empty<object>();
        length = Math.Floor(length);
        if (length > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(value), "Arguments list is too large");
        var result = new object[(int)length];
        for (int index = 0; index < result.Length; index++) result[index] = Js.IndexGet(value, (double)index);
        return result;
    }
    public override object InvokeWithReceiver(object receiver, object[] arguments) => Execute(receiver, arguments);
    public override double? InvokeMethodNumber0(object receiver) => Js.Cast<double?>(Execute(receiver, Array.Empty<object>()), "Function result violates number type");
    public override double? InvokeMethodNumber1(object receiver, double? first) => Js.Cast<double?>(Execute(receiver, new[] { Js.BoxNumber(first) }), "Function result violates number type");
    public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Js.Cast<double?>(Execute(receiver, new[] { Js.BoxNumber(first), Js.BoxNumber(second) }), "Function result violates number type");
}
internal sealed class JsBoundFunction : JsFunction
{
    readonly JsFunction target;
    readonly object[] prefix;
    readonly object boundReceiver;
    readonly double boundLength;
    readonly TsObject inheritedPrototype;
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : inheritedPrototype;
    JsBoundFunction(JsFunction target, object receiver, object[] prefix, string name, double length, TsObject prototype)
        : base((FunctionBody)(arguments => Js.CallMethod(target, receiver, Combine(prefix, arguments))), null, name, 0)
    {
        this.target = target; this.prefix = prefix; boundReceiver = receiver; boundLength = length;
        inheritedPrototype = prototype;
    }
    internal static JsBoundFunction Create(JsFunction target, object receiver, object[] prefix)
    {
        TsObject prototype = target.PropertyPrototype;
        object lengthValue = target.HasOwn("length") ? target.Get("length") : Js.Undefined;
        double length = lengthValue is double number && !double.IsNaN(number) ? Math.Max(0, Math.Truncate(number) - prefix.Length) : 0;
        string name = target.Get("name") as string ?? "";
        return new(target, receiver, prefix, "bound " + name, length, prototype);
    }
    static object[] Combine(object[] first, object[] second)
    {
        if (first.Length == 0) return second;
        var result = new object[checked(first.Length + second.Length)]; Array.Copy(first, result, first.Length); Array.Copy(second, 0, result, first.Length, second.Length); return result;
    }
    public override object Get(string key) => key == "length" && HasIntrinsicLength ? boundLength : base.Get(key);
    // Match the existing nullable-number call ABI; other values retain the general path.
    bool NumberPrefix(int index, out double? value)
    {
        object argument = prefix[index]; value = argument is double number ? number : null;
        return argument is double || Js.IsUndefined(argument);
    }
    public override double? InvokeNumber0()
    {
        if (prefix.Length == 0) return Js.CallMethodNumber0(target, boundReceiver);
        if (prefix.Length == 1 && NumberPrefix(0, out var first)) return Js.CallMethodNumber1(target, boundReceiver, first);
        if (prefix.Length == 2 && NumberPrefix(0, out first) && NumberPrefix(1, out var second)) return Js.CallMethodNumber2(target, boundReceiver, first, second);
        return base.InvokeNumber0();
    }
    public override double? InvokeNumber1(double? value)
    {
        if (prefix.Length == 0) return Js.CallMethodNumber1(target, boundReceiver, value);
        if (prefix.Length == 1 && NumberPrefix(0, out var first)) return Js.CallMethodNumber2(target, boundReceiver, first, value);
        return base.InvokeNumber1(value);
    }
    public override double? InvokeNumber2(double? first, double? second) => prefix.Length == 0 ? Js.CallMethodNumber2(target, boundReceiver, first, second) : base.InvokeNumber2(first, second);
    public override object InvokeWithReceiver(object receiver, object[] arguments) => Invoke(arguments);
    public override object Construct(object[] arguments) => target.Construct(Combine(prefix, arguments));
    internal override bool CanInitializeDerived => target.CanInitializeDerived;
    internal override object InitializeDerived(object instance, object[] arguments) => target.InitializeDerived(instance, Combine(prefix, arguments));
    internal override object InitializeDerived(object instance, object[] arguments, JsFunction newTarget) => target.InitializeDerived(instance, Combine(prefix, arguments), newTarget);
    internal override object ConstructWithNewTarget(object[] arguments, JsFunction newTarget) => target.ConstructWithNewTarget(Combine(prefix, arguments), ReferenceEquals(newTarget, this) ? target : newTarget);
    public override bool HasInstance(object value) => Js.InstanceOf(value, target);
}
public static partial class Js
{
    static class FunctionMethods
    {
        internal static readonly JsFunction Call = new JsFunctionMethod("call", 0);
        internal static readonly JsFunction Apply = new JsFunctionMethod("apply", 1);
        internal static readonly JsFunction Bind = new JsFunctionMethod("bind", 2);
    }
    internal static object FunctionMethod(string key) => key switch { "call" => FunctionMethods.Call, "apply" => FunctionMethods.Apply, "bind" => FunctionMethods.Bind, _ => Undefined };
}
