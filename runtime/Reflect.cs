using System;

public static partial class Js
{
    static TsObject ReflectTarget(object value) => value as TsObject ?? throw new InvalidOperationException("Reflect target must be an object");
    static TsObject CreateReflectNamespace()
    {
        var result = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype };
        void Method(string name, int length, FunctionBody body) => result.DefineMethod(name, new JsFunction(body, null, name, length));
        Method("apply", 3, static arguments =>
        {
            if (FunctionArgument(arguments, 0) is not JsFunction function) throw new InvalidOperationException("Reflect.apply target is not callable");
            object list = FunctionArgument(arguments, 2);
            if (list is not TsObject) throw new InvalidOperationException("Reflect.apply arguments must be an object");
            return function.InvokeWithReceiver(FunctionArgument(arguments, 1), JsFunctionMethod.ApplyArguments(list));
        });
        Method("get", 2, static arguments =>
        {
            var target = ReflectTarget(FunctionArgument(arguments, 0));
            string key = PropertyKey(FunctionArgument(arguments, 1));
            return target.GetWithReceiver(key, arguments.Length < 3 ? target : arguments[2]);
        });
        Method("deleteProperty", 2, static arguments => BoxBoolean(ReflectTarget(FunctionArgument(arguments, 0)).DeleteOwn(PropertyKey(FunctionArgument(arguments, 1)))));
        Method("has", 2, static arguments => BoxBoolean(ReflectTarget(FunctionArgument(arguments, 0)).HasInProperty(PropertyKey(FunctionArgument(arguments, 1)))));
        Method("ownKeys", 1, static arguments => ReflectTarget(FunctionArgument(arguments, 0)).OwnPropertyNames());
        Method("getPrototypeOf", 1, static arguments => ReflectTarget(FunctionArgument(arguments, 0)).PropertyPrototype);
        Method("getOwnPropertyDescriptor", 2, static arguments => ReflectTarget(FunctionArgument(arguments, 0)).OwnPropertyDescriptor(PropertyKey(FunctionArgument(arguments, 1))));
        return result;
    }
}
