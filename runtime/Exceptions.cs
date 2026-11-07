using System;
using System.Collections.Generic;

public sealed class JsThrownException(object value) : Exception("JavaScript exception")
{
    public object Value { get; } = value;
    public override string ToString() => Value is JsCaughtError error ? Js.ErrorText(error) : Js.Text(Value);
}
public class JsCaughtError : TsObject
{
    readonly TsObject prototype;
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : prototype;
    readonly string stack = Environment.StackTrace;
    readonly HashSet<string> hidden = ["stack"];
    internal override object OwnPropertyNames() => Js.MergeOwnNames(this, base.OwnKeys(), "stack");
    internal override object ForInOwnKeys() => Js.ForInNames(base.OwnKeys(), "stack");
    internal override bool ForInEnumerable(string key) => !hidden.Contains(key);
    public JsCaughtError(string name, string message, bool hasMessage = true, object cause = null, bool hasCause = false)
    {
        prototype = (TsObject)((JsFunction)Js.GetErrorConstructor(name)).Get("prototype");
        if (hasMessage) DefineHidden("message", message);
        if (hasCause) DefineHidden("cause", cause);
    }
    public void DefineHidden(string key, object value) { DefineOwn(key, value); hidden.Add(key); }
    public override bool HasOwn(string key) => key == "stack" || base.HasOwn(key);
    public override object Get(string key) => key == "stack" && !base.HasOwn(key) ? Js.ErrorText(this) + "\n" + stack : base.Get(key);
    public override object OwnKeys()
    {
        var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0);
        for (uint i = 0; i < keys.Length(); i++) { object key = keys.ElementAt(i); if (!hidden.Contains((string)key)) result.Append(key); }
        return result;
    }
}
public static partial class Js
{
    sealed class ErrorStringFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Error toString requires an object")), null, "toString", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => ErrorStringValue(receiver);
        public override double? InvokeMethodNumber0(object receiver) => Cast<double?>(ErrorStringValue(receiver), "Function result violates number type");
        public override double? InvokeMethodNumber1(object receiver, double? first) => InvokeMethodNumber0(receiver);
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => InvokeMethodNumber0(receiver);
    }
    static class ErrorStringMethod { internal static readonly JsFunction Value = new ErrorStringFunction(); }
    public static object GetErrorToString() => ErrorStringMethod.Value;
    static string ErrorStringValue(object receiver) => receiver is TsObject value ? ErrorText(value) : throw new InvalidOperationException("Error toString requires an object");
    sealed class ErrorConstructor : NativeNamespaceFunction
    {
        readonly string family;
        internal ErrorConstructor(string name, ErrorConstructor parent) : base(name, (FunctionBody)(arguments => NewError(name, arguments)), name == "SuppressedError" ? 3 : name == "AggregateError" ? 2 : 1)
        {
            family = name;
            if (parent is null) Member("isError", new JsFunction((FunctionBody)(static args => BoxBoolean(FunctionArgument(args, 0) is JsCaughtError)), null, "isError", 1));
            var prototype = new JsClassInstance { ParentPrototype = parent is null ? DefaultClassObjectPrototype : (TsObject)parent.Get("prototype") };
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod("name", name);
            prototype.DefineMethod("message", "");
            if (parent is null) prototype.DefineMethod("toString", GetErrorToString());
            else SetPrototype(parent);
            UseObjectPrototype(prototype);
        }
        internal override bool IsReadOnlyOwnProperty(string key) => key == "prototype" || base.IsReadOnlyOwnProperty(key);
        public override object Set(string key, object value) => key == "prototype" ? throw new InvalidOperationException("Function prototype is read-only") : base.Set(key, value);
        public override object Construct(object[] arguments) => NewError(family, arguments);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments)
        {
            var result = (TsObject)NewError(family, arguments);
            result.SetPrototype(((TsObject)instance).PropertyPrototype);
            return result;
        }
    }
    static class ErrorConstructors
    {
        internal static readonly Dictionary<string, JsFunction> Values = Create();
        static Dictionary<string, JsFunction> Create()
        {
            var root = new ErrorConstructor("Error", null);
            var values = new Dictionary<string, JsFunction> { ["Error"] = root };
            foreach (string name in new[] { "TypeError", "RangeError", "ReferenceError", "SyntaxError", "URIError", "EvalError", "AggregateError", "SuppressedError" }) values[name] = new ErrorConstructor(name, root);
            return values;
        }
    }
    public static object GetErrorConstructor(string name) => ErrorConstructors.Values[name];
    static object NewError(string name, object[] arguments)
    {
        int offset = name == "SuppressedError" ? 2 : name == "AggregateError" ? 1 : 0;
        object message = FunctionArgument(arguments, offset), options = FunctionArgument(arguments, offset + 1);
        bool hasMessage = !IsUndefined(message); string text = hasMessage ? StringValue(message) : "";
        bool hasCause = name != "SuppressedError" && options is TsObject obj && obj.HasProperty("cause");
        var result = new JsCaughtError(name, text, hasMessage, hasCause ? Get(options, "cause") : null, hasCause);
        if (name == "SuppressedError") {
            result.DefineHidden("error", FunctionArgument(arguments, 0));
            result.DefineHidden("suppressed", FunctionArgument(arguments, 1));
        }
        if (name == "AggregateError")
        {
            object input = Iteration.Start(FunctionArgument(arguments, 0)); var errors = new JsArray(0);
            for (int i = 0; i < Iteration.Length(input); i += Iteration.Step(input, i)) errors.Append(Iteration.Value(input, i));
            result.DefineHidden("errors", errors);
        }
        return result;
    }
    public static string ErrorText(TsObject error)
    {
        object name = error.Get("name");
        string prefix = IsUndefined(name) ? "Error" : StringValue(name);
        object message = error.Get("message");
        string text = IsUndefined(message) ? "" : StringValue(message);
        return prefix.Length == 0 ? text : text.Length == 0 ? prefix : prefix + ": " + text;
    }
    public static object Construct(object target, object[] arguments) => target is JsFunction function ? function.Construct(arguments) : throw new InvalidOperationException("Value is not a constructor");
    public static object ConstructArguments(object target, object arguments)
    {
        int length = checked((int)Iteration.Length(arguments)); var values = new object[length];
        for (int i = 0; i < length; i++) values[i] = Iteration.Value(arguments, i);
        return Construct(target, values);
    }
    public static bool InstanceOf(object value, object target)
    {
        if (target is not TsObject obj) throw new InvalidOperationException("Right operand of instanceof is not an object");
        object method = obj.Get(PropertyKey(HasInstanceSymbol));
        if (ReferenceEquals(method, OrdinaryInstanceCheck.Method))
            return target is JsFunction ordinary && ordinary.HasInstance(value);
        if (!IsNullish(method)) {
            if (method is not JsFunction custom) throw new InvalidOperationException("Symbol.hasInstance is not callable");
            return Truthy(custom.InvokeWithReceiver(obj, new[] { value }));
        }
        return target is JsFunction function ? function.HasInstance(value)
            : throw new InvalidOperationException("Right operand of instanceof is not callable");
    }
    public static Exception MakeThrown(object value) => new JsThrownException(value);
    public static object CaughtValue(Exception error)
    {
        if (error is JsThrownException thrown) return thrown.Value;
        string kind = error.Message.StartsWith("Cannot access binding before initialization", StringComparison.Ordinal) ? "ReferenceError" :
            error is ArgumentOutOfRangeException or OverflowException || error.Message == "Invalid array length" ? "RangeError" :
            error is InvalidCastException or InvalidOperationException or NullReferenceException ? "TypeError" : "Error";
        return new JsCaughtError(kind, error.Message);
    }
}
