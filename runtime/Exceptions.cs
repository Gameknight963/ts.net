using System;
using System.Collections.Generic;

public sealed class JsThrownException(object value) : Exception("JavaScript exception")
{
    public object Value { get; } = value;
    public override string ToString() => Value is JsCaughtError error ? Js.ErrorText(error) : Js.Text(Value);
}
public sealed class JsCaughtError : TsObject
{
    public readonly string Family;
    readonly string stack = Environment.StackTrace;
    readonly HashSet<string> hidden = ["stack"];
    public JsCaughtError(string name, string message, bool hasMessage = true, object cause = null, bool hasCause = false)
    {
        Family = name;
        if (hasMessage) { base.Set("message", message); hidden.Add("message"); }
        if (hasCause) { base.Set("cause", cause); hidden.Add("cause"); }
    }
    public void DefineHidden(string key, object value) { base.Set(key, value); hidden.Add(key); }
    public override bool HasOwn(string key) => key == "stack" || base.HasOwn(key);
    public override object Get(string key) => base.HasOwn(key) ? base.Get(key) : key switch
    {
        "name" => Family,
        "message" => "",
        "toString" => Js.GetErrorToString(),
        "stack" => Js.ErrorText(this) + "\n" + stack,
        _ => base.Get(key)
    };
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
    sealed class ErrorConstructor : JsFunction
    {
        readonly string family;
        internal ErrorConstructor(string name) : base((FunctionBody)(arguments => NewError(name, arguments)), null, name, name == "AggregateError" ? 2 : 1) { family = name; }
        public override object Construct(object[] arguments) => NewError(family, arguments);
        public override bool HasInstance(object value) => value is JsCaughtError error && (family == "Error" || error.Family == family);
    }
    static class ErrorConstructors
    {
        internal static readonly Dictionary<string, JsFunction> Values = new()
        {
            ["Error"] = new ErrorConstructor("Error"),
            ["TypeError"] = new ErrorConstructor("TypeError"),
            ["RangeError"] = new ErrorConstructor("RangeError"),
            ["ReferenceError"] = new ErrorConstructor("ReferenceError"),
            ["SyntaxError"] = new ErrorConstructor("SyntaxError"),
            ["URIError"] = new ErrorConstructor("URIError"),
            ["EvalError"] = new ErrorConstructor("EvalError"),
            ["AggregateError"] = new ErrorConstructor("AggregateError")
        };
    }
    public static object GetErrorConstructor(string name) => ErrorConstructors.Values[name];
    static object NewError(string name, object[] arguments)
    {
        int offset = name == "AggregateError" ? 1 : 0;
        object message = FunctionArgument(arguments, offset), options = FunctionArgument(arguments, offset + 1);
        bool hasMessage = !IsUndefined(message); string text = hasMessage ? StringValue(message) : "";
        bool hasCause = options is TsObject obj && obj.HasOwn("cause");
        var result = new JsCaughtError(name, text, hasMessage, hasCause ? Get(options, "cause") : null, hasCause);
        if (name == "AggregateError")
        {
            object input = FunctionArgument(arguments, 0); int length = checked((int)Iteration.Length(input)); var errors = new JsArray(0);
            for (int i = 0; i < length; i += Iteration.Step(input, i)) errors.Append(Iteration.Value(input, i));
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
    public static bool InstanceOf(object value, object target) => target is JsFunction function ? function.HasInstance(value) : throw new InvalidOperationException("Right operand of instanceof is not callable");
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
