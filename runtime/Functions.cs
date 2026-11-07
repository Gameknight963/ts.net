using System;
using System.Runtime.CompilerServices;

public delegate object FunctionBody(object[] arguments);
public delegate double? NumberFunction0();
public delegate double? NumberFunction1(double? first);
public delegate double? NumberFunction2(double? first, double? second);
public delegate double RawNumberFunction0();
public delegate double RawNumberFunction1(double first);
public delegate double RawNumberFunction2(double first, double second);

public partial class JsFunction : TsObject
{
    readonly FunctionBody body;
    readonly NumberFunction0 number0;
    readonly NumberFunction1 number1;
    readonly NumberFunction2 number2;
    readonly RawNumberFunction0 raw0;
    readonly RawNumberFunction1 raw1;
    readonly RawNumberFunction2 raw2;
    string name;
    internal string DisplayName => name;
    readonly int length;
    bool deletedName, deletedLength;
    bool nameSlotDeleted, lengthSlotDeleted;
    protected bool HasIntrinsicLength => !deletedLength;
    internal void ReplaceIntrinsicMetadata(string key)
    {
        if (key == "name") deletedName = true;
        else if (key == "length") deletedLength = true;
    }
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.SharedFunctionPrototype;
    public JsFunction(object body, object numeric, string name, int length)
    {
        this.body = (FunctionBody)body;
        number0 = numeric as NumberFunction0;
        number1 = numeric as NumberFunction1;
        number2 = numeric as NumberFunction2;
        raw0 = numeric as RawNumberFunction0;
        raw1 = numeric as RawNumberFunction1;
        raw2 = numeric as RawNumberFunction2;
        this.name = name;this.length = length;
    }
    internal virtual void SetObjectHome(TsObject home) { }
    public virtual object InvokeWithReceiver(object receiver,object[] arguments)=>Invoke(arguments);
    public virtual double? InvokeMethodNumber0(object receiver)=>InvokeNumber0();
    public virtual double? InvokeMethodNumber1(object receiver,double? first)=>InvokeNumber1(first);
    public virtual double? InvokeMethodNumber2(object receiver,double? first,double? second)=>InvokeNumber2(first,second);
    public virtual object Construct(object[] arguments) => ConstructOrdinary(arguments);
    public virtual bool HasInstance(object value) => OrdinaryHasInstance(value);
    internal override bool IsReadOnlyOwnProperty(string key) => key == "name" && !deletedName || key == "length" && !deletedLength;
    internal void InferPropertyName(string value) { name = value; }
    public override bool HasOwn(string key) => key == "name" && !deletedName || key == "length" && !deletedLength || key == "prototype" && ordinaryPrototype != null || base.HasOwn(key);
    public override bool HasProperty(string key)=>HasOwn(key) || base.HasProperty(key);
    public override object Get(string key) => key == "prototype" && ordinaryPrototype != null ? ReadOrdinaryPrototype() : base.HasOwn(key)?base.Get(key):key == "name" && !deletedName ? name : key == "length" && !deletedLength ? (double)length : base.Get(key);
    public override object SetOwnValue(string key, object value)
    {
        if (key == "prototype" && ordinaryPrototype != null) return Set(key, value);
        return base.SetOwnValue(key, value);
    }
    public override object Set(string key, object value)
    {
        if (key == "prototype" && ordinaryPrototype != null) { ordinaryPrototype = value ?? NullPrototypeValue; return value; }
        if (IsReadOnlyOwnProperty(key)) throw new InvalidOperationException("Function property is read-only");
        return base.Set(key, value);
    }
    public override bool DeleteOwn(string key)
    {
        if (key == "prototype" && ordinaryPrototype != null) return false;
        bool removed;
        if (key == "name" && !deletedName) { deletedName = true; removed = !base.HasOwn(key) || base.DeleteOwn(key); }
        else if (key == "length" && !deletedLength) { deletedLength = true; removed = !base.HasOwn(key) || base.DeleteOwn(key); }
        else removed = base.DeleteOwn(key);
        if (removed) { if (key == "name") nameSlotDeleted = true; else if (key == "length") lengthSlotDeleted = true; }
        return removed;
    }
    public object Invoke(object[] arguments) => body(arguments);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual double? InvokeNumber0()
    {
        if (raw0 != null) return raw0();
        if (raw1 != null) return raw1(double.NaN);
        if (raw2 != null) return raw2(double.NaN, double.NaN);
        if (number0 != null) return number0();
        if (number1 != null) return number1(null);
        if (number2 != null) return number2(null, null);
        return Js.Cast<double?>(body(Array.Empty<object>()), "Function result violates number type");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual double? InvokeNumber1(double? first)
    {
        if (raw1 != null) return raw1(Js.NumberValue(first));
        if (raw0 != null) return raw0();
        if (raw2 != null) return raw2(Js.NumberValue(first), double.NaN);
        if (number1 != null) return number1(first);
        if (number0 != null) return number0();
        if (number2 != null) return number2(first, null);
        return Js.Cast<double?>(body(new[] { Js.BoxNumber(first) }), "Function result violates number type");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual double? InvokeNumber2(double? first, double? second)
    {
        if (raw2 != null) return raw2(Js.NumberValue(first), Js.NumberValue(second));
        if (raw1 != null) return raw1(Js.NumberValue(first));
        if (raw0 != null) return raw0();
        if (number2 != null) return number2(first, second);
        if (number1 != null) return number1(first);
        if (number0 != null) return number0();
        return Js.Cast<double?>(body(new[] { Js.BoxNumber(first), Js.BoxNumber(second) }), "Function result violates number type");
    }
}

public static partial class Js
{
    public static object FunctionArgument(object[] arguments, int index) => index < arguments.Length ? arguments[index] : Undefined;
    public static object FunctionRest(object[] arguments, int index, bool numeric)
    {
        JsArray result = numeric ? new NumberArray(0) : new JsArray(0);
        for (; index < arguments.Length; index++) result.Append(arguments[index]);
        return result;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object NamePropertyFunction(object value, string name) { ((JsFunction)value).InferPropertyName(name); return value; }
    public static object CallFunction0(object target) => target is JsFunction function ? function.Invoke(Array.Empty<object>()) : throw new InvalidOperationException("Value is not callable");
    public static object CallFunction(object target, object[] arguments) => target is JsFunction function ? function.Invoke(arguments) : throw new InvalidOperationException("Value is not callable");
    public static object CallFunctionArguments(object target, object arguments)
    {
        int length = checked((int)Iteration.Length(arguments));
        object[] values = length == 0 ? Array.Empty<object>() : new object[length];
        for (int i = 0; i < length; i++) values[i] = Iteration.Value(arguments, i);
        return CallFunction(target, values);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? CallFunctionNumber0(object target) => target is JsFunction function ? function.InvokeNumber0() : throw new InvalidOperationException("Value is not callable");
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? CallFunctionNumber1(object target, double? first) => target is JsFunction function ? function.InvokeNumber1(first) : throw new InvalidOperationException("Value is not callable");
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? CallFunctionNumber2(object target, double? first, double? second) => target is JsFunction function ? function.InvokeNumber2(first, second) : throw new InvalidOperationException("Value is not callable");
}
