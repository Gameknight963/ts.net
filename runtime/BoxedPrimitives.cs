using System;
using System.Collections.Generic;
using System.Globalization;

internal sealed class JsBoxedPrimitive(object value, bool intrinsicPrototype = false) : TsObject
{
    internal readonly object Value = value;
    HashSet<string> hidden;
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : intrinsicPrototype ? Js.DefaultClassObjectPrototype : Js.PrimitivePrototype(Value);
    internal void DefineHidden(string key, object value) { DefineOwn(key, value); (hidden ??= new()).Add(key); }
    bool StringIndex(string key, out uint index) => JsArray.Index(key, out index) && Value is string text && index < text.Length;
    internal override bool IsReadOnlyOwnProperty(string key) => Value is string && (key == "length" || StringIndex(key, out _));
    public override bool HasOwn(string key) => Value is string && (key == "length" || StringIndex(key, out _)) || base.HasOwn(key);
    bool NumberMethodFallback(string key) => Value is double && PropertyPrototype is not null && key is "valueOf" or "toString";
    public override bool HasProperty(string key) => NumberMethodFallback(key) || base.HasProperty(key);
    public override object Get(string key)
    {
        if (Value is string text)
        {
            if (key == "length") return (double)text.Length;
            if (StringIndex(key, out uint index)) return Iteration.CodeUnit(text[(int)index]);
        }
        if (base.HasProperty(key)) return base.Get(key);
        return NumberMethodFallback(key) ? Js.PrimitiveMethod(Value, key == "toString") : base.Get(key);
    }
    public override object GetWithReceiver(string key, object receiver) => !base.HasProperty(key) && NumberMethodFallback(key) ? Get(key) : base.GetWithReceiver(key, receiver);
    public override object Set(string key, object value) => IsReadOnlyOwnProperty(key) ? throw new InvalidOperationException("String property is read-only") : base.Set(key, value);
    object AllKeys()
    {
        var keys = new object[Value is string text ? text.Length : 0];
        for (int i = 0; i < keys.Length; i++) keys[i] = i.ToString(CultureInfo.InvariantCulture);
        return KeysFromFixed(keys);
    }
    public override object OwnKeys()
    {
        var keys = (JsArray)AllKeys();
        if (hidden is null) return keys;
        var result = new JsArray(0);
        for (uint i = 0; i < keys.Length(); i++) { var key = keys.ElementAt(i); if (!hidden.Contains((string)key)) result.Append(key); }
        return result;
    }
    internal override object OwnPropertyNames() => Value is string ? Js.MergeOwnNames(this, AllKeys(), "length") : AllKeys();
    internal override object ForInOwnKeys() => Value is string ? Js.ForInNames(AllKeys(), "length") : AllKeys();
    public override bool DeleteOwn(string key) { bool removed = base.DeleteOwn(key); if (removed) hidden?.Remove(key); return removed; }
    internal override bool ForInEnumerable(string key) => (Value is not string || key != "length") && hidden?.Contains(key) != true;
}

public static partial class Js
{
    internal static TsObject PrimitivePrototype(object value) => (TsObject)((JsFunction)BuiltinNamespace(value is string ? "String" : value is double ? "Number" : "Boolean")).Get("prototype");
    sealed class PrimitiveValueFunction(Type type, bool text) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Primitive method requires a receiver")), null, text ? "toString" : "valueOf", 0)
    {
        object Execute(object receiver)
        {
            object value = receiver is JsBoxedPrimitive boxed ? boxed.Value : receiver;
            if (value?.GetType() != type) throw new InvalidOperationException("Primitive method has an incompatible receiver");
            return text ? StringValue(value) : value;
        }
        public override object InvokeWithReceiver(object receiver, object[] arguments) => Execute(receiver);
        public override double? InvokeMethodNumber0(object receiver) => Cast<double?>(Execute(receiver), "Primitive result violates number type");
        public override double? InvokeMethodNumber1(object receiver, double? first) => InvokeMethodNumber0(receiver);
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => InvokeMethodNumber0(receiver);
    }
    static class PrimitiveMethods
    {
        internal static readonly JsFunction StringValue = new PrimitiveValueFunction(typeof(string), false), StringText = new PrimitiveValueFunction(typeof(string), true);
        internal static readonly JsFunction NumberValue = new PrimitiveValueFunction(typeof(double), false), NumberText = new NumberRadixFunction();
        internal static readonly JsFunction BooleanValue = new PrimitiveValueFunction(typeof(bool), false), BooleanText = new PrimitiveValueFunction(typeof(bool), true);
    }
    internal static object PrimitiveMethod(object value, bool text) => value is string ? text ? PrimitiveMethods.StringText : PrimitiveMethods.StringValue : value is double ? text ? PrimitiveMethods.NumberText : PrimitiveMethods.NumberValue : text ? PrimitiveMethods.BooleanText : PrimitiveMethods.BooleanValue;
    internal static TsObject ToObject(object value) => value is TsObject obj ? obj : value is string or double or bool ? new JsBoxedPrimitive(value) : throw new InvalidOperationException("Cannot convert null or undefined to object");
}
