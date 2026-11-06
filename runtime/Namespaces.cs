using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class ConsoleNamespace : TsObject
    {
        internal ConsoleNamespace() { DefineOwn("log", new JsFunction((FunctionBody)(static args => { Log(args); return Undefined; }), null, "log", 0)); }
    }
    sealed class PerformanceNamespace : TsObject
    {
        static readonly JsFunction NowFunction = new((FunctionBody)(static _ => Now()), (RawNumberFunction0)Now, "now", 0);
        public override object Get(string key) => base.HasOwn(key) ? base.Get(key) : key == "now" ? NowFunction : base.Get(key);
        public override bool HasProperty(string key) => key == "now" || base.HasProperty(key);
    }
    class NativeNamespaceFunction(string name, FunctionBody body, int length, object numeric = null) : JsFunction(body, numeric, name, length)
    {
        public override object Set(string key, object value) => key == "prototype" ? throw new InvalidOperationException("Function prototype is read-only") : base.Set(key, value);
        readonly HashSet<string> hidden = new();
        internal void Member(string name, object value) { DefineOwn(name, value); hidden.Add(name); }
        public override object OwnKeys() { var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0); for (uint i = 0; i < keys.Length(); i++) { var key = keys.ElementAt(i); if (!hidden.Contains((string)key)) result.Append(key); } return result; }
    }
    sealed class NumberNamespace : NativeNamespaceFunction
    {
        internal NumberNamespace() : base("Number", static args => args.Length == 0 ? 0d : Number(FunctionArgument(args, 0)), 1, (RawNumberFunction1)(static value => value))
        {
            MarkOrdinaryPrototype();
            Member("isNaN", NumberPredicateFunction(0)); Member("isFinite", NumberPredicateFunction(1)); Member("isInteger", NumberPredicateFunction(2)); Member("isSafeInteger", NumberPredicateFunction(3));
            Member("NaN", double.NaN); Member("POSITIVE_INFINITY", double.PositiveInfinity); Member("NEGATIVE_INFINITY", double.NegativeInfinity); Member("MAX_SAFE_INTEGER", 9007199254740991d); Member("MIN_SAFE_INTEGER", -9007199254740991d); Member("EPSILON", Math.Pow(2, -52)); Member("MAX_VALUE", double.MaxValue); Member("MIN_VALUE", double.Epsilon);
        }
        public override double? InvokeNumber0() => 0d;
    }
    static NativeNamespaceFunction ObjectNamespace()
    {
        var result = new NativeNamespaceFunction("Object", static _ => throw new InvalidOperationException("Object construction is not implemented"), 1);
        result.MarkOrdinaryPrototype();
        result.Member("keys", GetObjectKeysFunction()); result.Member("values", GetObjectValuesFunction()); result.Member("entries", GetObjectEntriesFunction());
        result.Member("hasOwn", new JsFunction((FunctionBody)(static args => BoxBoolean(HasOwn(FunctionArgument(args, 0), FunctionArgument(args, 1)))), null, "hasOwn", 2)); return result;
    }
    static class RuntimeNamespaces
    {
        internal static readonly TsObject Console = new ConsoleNamespace();
        internal static readonly TsObject Performance = new PerformanceNamespace();
        internal static readonly JsFunction Object = ObjectNamespace();
        internal static readonly JsFunction Number = new NumberNamespace();
    }
    public static void ConsoleLog(object[] args) => CallMethod(RuntimeNamespaces.Console.Get("log"), RuntimeNamespaces.Console, args);
    public static double PerformanceNow() => CallMethodNumber0(RuntimeNamespaces.Performance.Get("now"), RuntimeNamespaces.Performance).GetValueOrDefault(double.NaN);
    public static object BuiltinNamespace(string name) => name == "console" ? RuntimeNamespaces.Console : name == "performance" ? RuntimeNamespaces.Performance : name == "Object" ? RuntimeNamespaces.Object : name == "Number" ? RuntimeNamespaces.Number : Undefined;
}
