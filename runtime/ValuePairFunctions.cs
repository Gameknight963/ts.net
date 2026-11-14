using System;
using System.Runtime.CompilerServices;

public partial class JsFunction
{
    // Preserve pure Invoke and method CallMethod dispatch for native subclasses.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual double? InvokeValuePairNumber(object receiver, object first, object second, bool isMethod) =>
        Js.Cast<double?>(isMethod ? Js.CallMethod(this, receiver, new[] { first, second })
            : Invoke(new[] { first, second }), "checked conversion failed");
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual double? InvokeValueNumber(object receiver, object first, double? second, bool isMethod) =>
        Js.Cast<double?>(isMethod ? Js.CallMethod(this, receiver, new[] { first, Js.BoxNumber(second) })
            : Invoke(new[] { first, Js.BoxNumber(second) }), "checked conversion failed");
}

public static partial class Js
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? CallValuePairNumber(object target, object receiver, object first, object second, bool isMethod) =>
        target is JsFunction function ? function.InvokeValuePairNumber(receiver, first, second, isMethod)
            : throw new InvalidOperationException("Value is not callable");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? CallValueNumber(object target, object receiver, object first, double? second, bool isMethod) =>
        target is JsFunction function ? function.InvokeValueNumber(receiver, first, second, isMethod)
            : throw new InvalidOperationException("Value is not callable");
}
