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
            string => "String", double => "Number", bool => "Boolean", JsArray => "Array",
            JsFunction => "Function", JsArguments => "Arguments", JsCaughtError => "Error",
            _ => "Object"
        };
        if (original is TsObject obj)
        {
            // These built-in tags normally come from Symbol.toStringTag. Symbol
            // properties are not supported yet, but their native tags are.
            for (TsObject current = obj; current != null; current = current.PropertyPrototype)
            {
                if (ReferenceEquals(current, RuntimeNamespaces.Reflect)) { tag = "Reflect"; break; }
                if (ReferenceEquals(current, RuntimeNamespaces.Json)) { tag = "JSON"; break; }
                if (ReferenceEquals(current, RuntimeNamespaces.Math)) { tag = "Math"; break; }
                if (ReferenceEquals(current, KeyedCollectionPrototype(false))) { tag = "Map"; break; }
                if (ReferenceEquals(current, KeyedCollectionPrototype(true))) { tag = "Set"; break; }
                if (ReferenceEquals(current, WeakCollectionPrototype(false))) { tag = "WeakMap"; break; }
                if (ReferenceEquals(current, WeakCollectionPrototype(true))) { tag = "WeakSet"; break; }
                if (ReferenceEquals(current, ((JsFunction)RuntimeNamespaces.Promise).Get("prototype"))) { tag = "Promise"; break; }
            }
        }
        return "[object " + tag + "]";
    }
    static string FunctionDisplayText(JsFunction function)
    {
        string name = function is JsBoundFunction ? "" : function.DisplayName;
        return "function " + name + "() { [native code] }";
    }
    sealed class ObjectStringFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("toString requires a receiver")), null, "toString", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => ObjectBrand(receiver);
    }
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
