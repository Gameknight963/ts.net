using System;

public static partial class Js
{
    static string ObjectBrand(object receiver)
    {
        if (receiver == null) return "[object Null]";
        if (IsUndefined(receiver)) return "[object Undefined]";
        TsObject original = receiver as TsObject;
        if (receiver is JsBoxedPrimitive boxed) receiver = boxed.Value;
        string tag = receiver switch {
            JsSymbol => "Symbol", string => "String", double => "Number", bool => "Boolean", JsArray => "Array",
            JsWeakRef => "WeakRef", JsDate => "Date", JsRegExp => "RegExp", JsFunction => "Function", JsArguments => "Arguments", JsCaughtError => "Error",
            _ => "Object"
        };
        TsObject properties = original ?? PrimitivePrototype(receiver);
        string tagKey = PropertyKey(ToStringTagSymbol);
        if (properties != null && properties.HasProperty(tagKey)) {
            object custom = properties.GetWithReceiver(tagKey, original ?? receiver);
            string fallback = receiver is JsSymbol or JsWeakRef ? "Object" : tag;
            return "[object " + (custom is string text ? text : fallback) + "]";
        }
        if (original is TsObject obj)
        {
            // Native tags remain available where intrinsic prototypes do not
            // yet expose their own Symbol.toStringTag property.
            for (TsObject current = obj; current != null; current = current.PropertyPrototype)
            {
                if (ReferenceEquals(current, RuntimeNamespaces.Reflect)) { tag = "Reflect"; break; }
                if (ReferenceEquals(current, RuntimeNamespaces.Json)) { tag = "JSON"; break; }
                if (ReferenceEquals(current, RuntimeNamespaces.Math)) { tag = "Math"; break; }
            }
        }
        return "[object " + tag + "]";
    }
    static string FunctionDisplayText(JsFunction function)
    {
        if (function is not JsBoundFunction && function.SourceText is string source) return source;
        string name = function is JsBoundFunction ? "" : function.DisplayName;
        return "function " + name + "() { [native code] }";
    }
    sealed class ObjectStringFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("toString requires a receiver")), null, "toString", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => ObjectBrand(receiver);
    }
    static class ArrayStringMethod { internal static readonly JsFunction Value = new ArrayStringFunction(); }
    sealed class ArrayStringFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("toString requires a receiver")), null, "toString", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var obj = ToObject(receiver); object join = obj.Get("join");
            return join is JsFunction method ? method.InvokeWithReceiver(obj, Array.Empty<object>()) : ObjectBrand(obj);
        }
    }
    sealed class FunctionStringFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("toString requires a receiver")), null, "toString", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => receiver is JsFunction function ? FunctionDisplayText(function) : throw new InvalidOperationException("Function toString requires a function");
    }
    sealed class ObjectLocaleStringFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("toLocaleString requires a receiver")), null, "toLocaleString", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var obj = ToObject(receiver);
            object method = obj.GetWithReceiver("toString", receiver);
            if (method is not JsFunction function) throw new InvalidOperationException("toString is not callable");
            return function.InvokeWithReceiver(receiver, Array.Empty<object>());
        }
    }
}
