using System;

public static partial class Js
{
    sealed class ObjectPrototypeQuery(string name, int mode)
        : JsFunction((FunctionBody)(args => ObjectPrototypeCheck(mode, Undefined, args)), null, name, 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => ObjectPrototypeCheck(mode, receiver, arguments);
    }
    static object ObjectPrototypeCheck(int mode, object receiver, object[] arguments)
    {
        object argument = FunctionArgument(arguments, 0);
        if (mode == 0)
        {
            // A primitive argument returns false even when this is nullish.
            if (argument is not TsObject instance) return BoxBoolean(false);
            return BoxBoolean(PrototypeContains(instance, ToObject(receiver)));
        }
        // ToPropertyKey precedes ToObject(this), including observable coercion.
        string key = PropertyKey(argument);
        TsObject target = ToObject(receiver);
        return BoxBoolean(target.HasOwn(key) && (mode == 1 || target.ForInEnumerable(key)));
    }
}
