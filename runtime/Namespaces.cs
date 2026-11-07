using System.Numerics;
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
        internal override bool IsReadOnlyOwnProperty(string key) => key == "prototype" || base.IsReadOnlyOwnProperty(key);
        public override object Set(string key, object value) => key == "prototype" ? throw new InvalidOperationException("Function prototype is read-only") : base.Set(key, value);
        readonly HashSet<string> hidden = new();
        public override bool DeleteOwn(string key) { bool removed = base.DeleteOwn(key); if (removed) hidden.Remove(key); return removed; }
        internal override bool ForInEnumerable(string key) => !hidden.Contains(key) && base.ForInEnumerable(key);
        internal void Member(string name, object value) { DefineOwn(name, value); hidden.Add(name); }
        public override object OwnKeys() { var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0); for (uint i = 0; i < keys.Length(); i++) { var key = keys.ElementAt(i); if (!hidden.Contains((string)key)) result.Append(key); } return result; }
    }
    sealed class NumberNamespace : NativeNamespaceFunction
    {
        internal NumberNamespace() : base("Number", static args => args.Length == 0 ? 0d : ExplicitNumber(FunctionArgument(args, 0)), 1, (RawNumberFunction1)(static value => value))
        {
            var prototype = new JsBoxedPrimitive(0d, true);
            prototype.DefineHidden("constructor", this);
            prototype.DefineHidden("valueOf", PrimitiveMethod(0d, false));
            prototype.DefineHidden("toString", PrimitiveMethod(0d, true));
            prototype.DefineHidden("toLocaleString", new NumberLocaleFunction());
            prototype.DefineHidden("toFixed", new NumberFixedFunction());
            prototype.DefineHidden("toExponential", new NumberPrecisionFunction(false));
            prototype.DefineHidden("toPrecision", new NumberPrecisionFunction(true));
            UseObjectPrototype(prototype);
            Member("parseInt", NumberParsers.Integer); Member("parseFloat", NumberParsers.Float);
            Member("isNaN", NumberPredicateFunction(0)); Member("isFinite", NumberPredicateFunction(1)); Member("isInteger", NumberPredicateFunction(2)); Member("isSafeInteger", NumberPredicateFunction(3));
            Member("NaN", double.NaN); Member("POSITIVE_INFINITY", double.PositiveInfinity); Member("NEGATIVE_INFINITY", double.NegativeInfinity); Member("MAX_SAFE_INTEGER", 9007199254740991d); Member("MIN_SAFE_INTEGER", -9007199254740991d); Member("EPSILON", Math.Pow(2, -52)); Member("MAX_VALUE", double.MaxValue); Member("MIN_VALUE", double.Epsilon);
        }
        internal override bool IsReadOnlyOwnProperty(string key) => key is "NaN" or "POSITIVE_INFINITY" or "NEGATIVE_INFINITY" or "MAX_SAFE_INTEGER" or "MIN_SAFE_INTEGER" or "EPSILON" or "MAX_VALUE" or "MIN_VALUE" || base.IsReadOnlyOwnProperty(key);
        internal override bool OwnPropertyConfigurable(string key) => key is not ("NaN" or "POSITIVE_INFINITY" or "NEGATIVE_INFINITY" or "MAX_SAFE_INTEGER" or "MIN_SAFE_INTEGER" or "EPSILON" or "MAX_VALUE" or "MIN_VALUE") && base.OwnPropertyConfigurable(key);
        public override double? InvokeNumber0() => 0d;
        public override object Construct(object[] arguments) => new JsBoxedPrimitive(arguments.Length == 0 ? 0d : ExplicitNumber(arguments[0]));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments)
        {
            var result = (TsObject)Construct(arguments);
            result.SetPrototype(((TsObject)instance).PropertyPrototype); return result;
        }
        internal override object InitializeDerived(object instance, object[] arguments, JsFunction newTarget)
        {
            var result = (TsObject)Construct(arguments);
            result.SetPrototype(ConstructionPrototype(newTarget)); return result;
        }
    }
    static object ObjectValue(object value) => IsNullish(value) ? new JsDynamicObject() : ToObject(value);
    sealed class ObjectConstructorFunction() : NativeNamespaceFunction("Object", static args => ObjectValue(FunctionArgument(args, 0)), 1)
    {
        public override object Construct(object[] arguments) => ObjectValue(FunctionArgument(arguments, 0));
        internal override bool CanInitializeDerived => true;
        // Object used as a base constructor initializes the subclass allocation;
        // unlike a direct Object(value) call, it does not return the argument.
        internal override object InitializeDerived(object instance, object[] arguments) => instance;
    }
    static object PrimitiveConstructorValue(object[] arguments, bool text) => text ? arguments.Length == 0 ? "" : arguments[0] is JsSymbol symbol ? symbol.Display : StringValue(arguments[0]) : BoxBoolean(Truthy(FunctionArgument(arguments, 0)));
    sealed class PrimitiveConstructorFunction : NativeNamespaceFunction
    {
        readonly bool text;
        internal PrimitiveConstructorFunction(string name, bool text) : base(name, args => PrimitiveConstructorValue(args, text), 1)
        {
            this.text = text;
            var prototype = new JsBoxedPrimitive(text ? "" : BoxBoolean(false), true);
            if (text)
            {
                DefineStringFormatting(prototype); DefineStringBoundaries(prototype); DefineStringHtml(prototype);
                Member("raw", new JsFunction((FunctionBody)StringRaw, null, "raw", 1));
                Member("fromCharCode", new JsFunction((FunctionBody)StringFromCharCode, null, "fromCharCode", 1));
                Member("fromCodePoint", new JsFunction((FunctionBody)StringFromCodePoint, null, "fromCodePoint", 1));
            }
            prototype.DefineHidden("constructor", this);
            prototype.DefineHidden("valueOf", PrimitiveMethod(prototype.Value, false));
            prototype.DefineHidden("toString", PrimitiveMethod(prototype.Value, true));
            UseObjectPrototype(prototype);
        }
        public override object Construct(object[] arguments)
        {
            if (text && FunctionArgument(arguments, 0) is JsSymbol) throw new InvalidOperationException("Cannot convert a symbol to a string");
            return new JsBoxedPrimitive(PrimitiveConstructorValue(arguments, text));
        }
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments)
        {
            var result = (TsObject)Construct(arguments);
            result.SetPrototype(((TsObject)instance).PropertyPrototype); return result;
        }
        internal override object InitializeDerived(object instance, object[] arguments, JsFunction newTarget)
        {
            var result = (TsObject)Construct(arguments);
            result.SetPrototype(ConstructionPrototype(newTarget)); return result;
        }
    }
    static object SetObjectPrototype(object target, object prototype)
    {
        RequireObject(target);
        if (prototype is not null && prototype is not TsObject) throw new InvalidOperationException("Prototype must be an object or null");
        if (target is TsObject obj) obj.SetPrototype(prototype as TsObject);
        return target;
    }
    static NativeNamespaceFunction ObjectNamespace()
    {
        var result = new ObjectConstructorFunction();
        result.UseObjectPrototype(DefaultClassObjectPrototype);
        DefaultClassObjectPrototype.DefineMethod("constructor",result);
        result.Member("create", new JsFunction((FunctionBody)(static args => ObjectCreate(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "create", 2));
        result.Member("fromEntries", new JsFunction((FunctionBody)(static args => ObjectFromEntries(FunctionArgument(args, 0))), null, "fromEntries", 1));
        result.Member("groupBy", new JsFunction((FunctionBody)(static args => ObjectGroupBy(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "groupBy", 2));
        result.Member("assign", new JsFunction((FunctionBody)ObjectAssign, null, "assign", 2));
        result.Member("is", new JsFunction((FunctionBody)(static args => BoxBoolean(SameValue(FunctionArgument(args, 0), FunctionArgument(args, 1)))), null, "is", 2));
        result.Member("getOwnPropertyDescriptor", new JsFunction((FunctionBody)(static args => ObjectOwnDescriptor(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "getOwnPropertyDescriptor", 2));
        result.Member("getOwnPropertyDescriptors", new JsFunction((FunctionBody)(static args => ObjectOwnDescriptors(FunctionArgument(args, 0))), null, "getOwnPropertyDescriptors", 1));
        result.Member("getOwnPropertySymbols", new JsFunction((FunctionBody)(static args => SymbolPropertyKeys(ToObject(FunctionArgument(args, 0)).OwnPropertyNames())), null, "getOwnPropertySymbols", 1));
        result.Member("getOwnPropertyNames", new JsFunction((FunctionBody)(static args => StringPropertyKeys(ToObject(FunctionArgument(args, 0)).OwnPropertyNames())), null, "getOwnPropertyNames", 1));
        result.Member("keys", GetObjectKeysFunction()); result.Member("values", GetObjectValuesFunction()); result.Member("entries", GetObjectEntriesFunction());
        result.Member("hasOwn", new JsFunction((FunctionBody)(static args => BoxBoolean(HasOwn(FunctionArgument(args, 0), FunctionArgument(args, 1)))), null, "hasOwn", 2));
        result.Member("getPrototypeOf", new JsFunction((FunctionBody)(static args => ToObject(FunctionArgument(args, 0)).PropertyPrototype), null, "getPrototypeOf", 1));
        result.Member("setPrototypeOf", new JsFunction((FunctionBody)(static args => SetObjectPrototype(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "setPrototypeOf", 2)); return result;
    }
    static class RuntimeNamespaces
    {
        internal static readonly TsObject Reflect = CreateReflectNamespace();
        internal static readonly TsObject Json = CreateJsonNamespace();
        internal static readonly TsObject Console = new ConsoleNamespace();
        internal static readonly TsObject Math = new MathNamespace();
        internal static readonly TsObject Performance = new PerformanceNamespace();
        internal static readonly JsFunction Object = ObjectNamespace();
        internal static readonly JsFunction Number = new NumberNamespace();
        internal static readonly JsFunction BigInt = new BigIntFunction();
        internal static readonly JsFunction String = new PrimitiveConstructorFunction("String", true);
        internal static readonly JsFunction Boolean = new PrimitiveConstructorFunction("Boolean", false);
        internal static readonly JsFunction Array = new ArrayConstructorFunction();
        internal static readonly JsFunction Promise = new PromiseConstructorFunction();
        internal static readonly JsFunction Date = new DateConstructor();
        internal static readonly JsFunction RegExp = new RegExpConstructor();
        internal static readonly JsFunction Map = new CollectionConstructor(false);
        internal static readonly JsFunction Set = new CollectionConstructor(true);
        internal static readonly JsFunction Symbol = new SymbolFunction();
        internal static readonly JsFunction Iterator = new IteratorConstructor();
        internal static readonly JsFunction WeakRef = new WeakRefConstructor();
        internal static readonly JsFunction WeakMap = new WeakCollectionConstructor(false);
        internal static readonly JsFunction WeakSet = new WeakCollectionConstructor(true);
        internal static readonly JsFunction QueueMicrotask = CreateQueueMicrotask();
    }
    internal static JsFunction GetObjectConstructor() => RuntimeNamespaces.Object;
    public static void ConsoleLog(object[] args) => CallMethod(RuntimeNamespaces.Console.Get("log"), RuntimeNamespaces.Console, args);
    public static double PerformanceNow() => CallMethodNumber0(RuntimeNamespaces.Performance.Get("now"), RuntimeNamespaces.Performance).GetValueOrDefault(double.NaN);
    public static object BuiltinNamespace(string name) => name == "BigInt" ? RuntimeNamespaces.BigInt : name == "Symbol" ? RuntimeNamespaces.Symbol : name == "Iterator" ? RuntimeNamespaces.Iterator : name == "__importMeta" ? ImportMetaFunction.Value : name == "WeakRef" ? RuntimeNamespaces.WeakRef : name == "__templateText" ? TemplateTextFunction.Value : name == "__templateObject" ? TemplateObjectFunction.Value : name == "Date" ? RuntimeNamespaces.Date : name == "RegExp" ? RuntimeNamespaces.RegExp : name == "Reflect" ? RuntimeNamespaces.Reflect : name == "JSON" ? RuntimeNamespaces.Json : name == "escape" ? LegacyEscapes.Escape : name == "unescape" ? LegacyEscapes.Unescape : name == "encodeURI" ? UriFunctions.Encode : name == "encodeURIComponent" ? UriFunctions.EncodeComponent : name == "decodeURI" ? UriFunctions.Decode : name == "decodeURIComponent" ? UriFunctions.DecodeComponent : name == "isNaN" ? GlobalNumberPredicates.IsNaN : name == "isFinite" ? GlobalNumberPredicates.IsFinite : name == "parseInt" ? NumberParsers.Integer : name == "parseFloat" ? NumberParsers.Float : name == "WeakMap" ? RuntimeNamespaces.WeakMap : name == "WeakSet" ? RuntimeNamespaces.WeakSet : name == "Map" ? RuntimeNamespaces.Map : name == "Set" ? RuntimeNamespaces.Set : name == "Promise" ? RuntimeNamespaces.Promise : name == "queueMicrotask" ? RuntimeNamespaces.QueueMicrotask : name == "Array" ? RuntimeNamespaces.Array : name == "Math" ? RuntimeNamespaces.Math : name == "console" ? RuntimeNamespaces.Console : name == "performance" ? RuntimeNamespaces.Performance : name == "Object" ? RuntimeNamespaces.Object : name == "Number" ? RuntimeNamespaces.Number : name == "String" ? RuntimeNamespaces.String : name == "Boolean" ? RuntimeNamespaces.Boolean : Undefined;
}
