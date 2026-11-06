using System;
using System.Runtime.CompilerServices;

internal sealed class JsAutoAccessor(TsObject owner, string name, bool isStatic)
{
    internal readonly string StorageName = "#" + name + "_accessor_storage";
    sealed class Cell(object value) { internal object Value = value; }
    readonly ConditionalWeakTable<TsObject, Cell> values = new();
    TsObject Target(object receiver) => (isStatic ? owner : Js.ResolveThis(receiver)) as TsObject
        ?? throw Js.MakeThrown(new JsCaughtError("TypeError", "Invalid auto-accessor receiver"));
    Cell Storage(object receiver)
    {
        if (!values.TryGetValue(Target(receiver), out var cell))
            throw Js.MakeThrown(new JsCaughtError("TypeError", "Auto-accessor backing storage is not initialized"));
        return cell;
    }
    internal object Read(object receiver) => Storage(receiver).Value;
    internal object Write(object receiver, object value) { Storage(receiver).Value = value; return Js.Undefined; }
    internal void Initialize(object receiver, object value)
    {
        if (!values.TryAdd(Target(receiver), new Cell(value)))
            throw Js.MakeThrown(new JsCaughtError("TypeError", "Auto-accessor backing storage already exists"));
    }
    internal sealed class Function(JsAutoAccessor slot, bool setter) : JsFunction(
        (FunctionBody)(args => setter ? slot.Write(Js.Undefined, Js.FunctionArgument(args, 0)) : slot.Read(Js.Undefined)), null, "", setter ? 1 : 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => setter ? slot.Write(receiver, Js.FunctionArgument(arguments, 0)) : slot.Read(receiver);
        public override double? InvokeMethodNumber0(object receiver) => Js.Cast<double?>(InvokeWithReceiver(receiver, Array.Empty<object>()), "Auto-accessor result violates number type");
        public override double? InvokeMethodNumber1(object receiver, double? first) => Js.Cast<double?>(InvokeWithReceiver(receiver, new[] { Js.BoxNumber(first) }), "Auto-accessor result violates number type");
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Js.Cast<double?>(InvokeWithReceiver(receiver, new[] { Js.BoxNumber(first), Js.BoxNumber(second) }), "Auto-accessor result violates number type");
    }
}

public static partial class Js
{
    public static object DefineAutoAccessor(object target, string name, bool isStatic)
    {
        var home = (JsClassFunction)target;
        var slot = new JsAutoAccessor(home, name, isStatic);
        var getter = new JsAutoAccessor.Function(slot, false);
        var setter = new JsAutoAccessor.Function(slot, true);
        if (isStatic) { home.DefineClassAccessor(name, getter, 1); home.DefineClassAccessor(name, setter, 2); }
        else { home.Prototype.DefineClassAccessor(name, getter, 1); home.Prototype.DefineClassAccessor(name, setter, 2); }
        return slot;
    }
    public static void InitializeAutoAccessor(object target, object slot, object value, bool inferName)
    {
        var accessor = (JsAutoAccessor)slot;
        if (inferName) ((JsFunction)value).InferPropertyName(accessor.StorageName);
        accessor.Initialize(target, value);
    }
}
