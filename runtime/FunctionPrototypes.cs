using System;

public partial class JsFunction
{
    private static readonly object UncreatedPrototype = new();
    private static readonly object NullPrototypeValue = new();
    private object ordinaryPrototype;
    internal void MarkOrdinaryPrototype() => ordinaryPrototype = UncreatedPrototype;
    private object ReadOrdinaryPrototype()
    {
        if (ReferenceEquals(ordinaryPrototype, UncreatedPrototype)) ordinaryPrototype = new FunctionPrototypeObject(this);
        return ReferenceEquals(ordinaryPrototype, NullPrototypeValue) ? null : ordinaryPrototype;
    }
    private sealed class FunctionPrototypeObject : TsObject
    {
        internal FunctionPrototypeObject(JsFunction owner) => DefineOwn("constructor", owner);
        public override object OwnKeys()
        {
            var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0);
            for (uint i = 0; i < keys.Length(); i++) if ((string)keys.ElementAt(i) != "constructor") result.Append(keys.ElementAt(i));
            return result;
        }
    }
}

public static partial class Js
{
    public static object MarkFunctionPrototype(object value) { ((JsFunction)value).MarkOrdinaryPrototype(); return value; }
}
