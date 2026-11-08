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
        Method("construct", 2, static arguments =>
        {
            if (FunctionArgument(arguments, 0) is not JsFunction target || !target.CanInitializeDerived) throw new InvalidOperationException("Reflect.construct target is not a constructor");
            object requested = arguments.Length < 3 ? target : arguments[2];
            if (requested is not JsFunction newTarget || !newTarget.CanInitializeDerived) throw new InvalidOperationException("Reflect.construct newTarget is not a constructor");
            object list = FunctionArgument(arguments, 1);
            if (list is not TsObject) throw new InvalidOperationException("Reflect.construct arguments must be an object");
            return target.ConstructWithNewTarget(JsFunctionMethod.ApplyArguments(list), newTarget);
        });
        Method("get", 2, static arguments =>
        {
            var target = ReflectTarget(FunctionArgument(arguments, 0));
            string key = PropertyKey(FunctionArgument(arguments, 1));
            return target.GetWithReceiver(key, arguments.Length < 3 ? target : arguments[2]);
        });
        Method("set", 3, static arguments =>
        {
            var target = ReflectTarget(FunctionArgument(arguments, 0));
            string key = PropertyKey(FunctionArgument(arguments, 1));
            return BoxBoolean(ReflectSet(target, key, FunctionArgument(arguments, 2), arguments.Length < 4 ? target : arguments[3]));
        });
        Method("deleteProperty", 2, static arguments => BoxBoolean(ReflectTarget(FunctionArgument(arguments, 0)).DeleteOwn(PropertyKey(FunctionArgument(arguments, 1)))));
        Method("has", 2, static arguments => BoxBoolean(ReflectTarget(FunctionArgument(arguments, 0)).HasInProperty(PropertyKey(FunctionArgument(arguments, 1)))));
        Method("isExtensible", 1, static arguments => BoxBoolean(ObjectExtensible(ReflectTarget(FunctionArgument(arguments, 0)))));
        Method("preventExtensions", 1, static arguments => { ObjectPreventExtensions(ReflectTarget(FunctionArgument(arguments, 0))); return BoxBoolean(true); });
        Method("ownKeys", 1, static arguments => ReflectPropertyKeys(ReflectTarget(FunctionArgument(arguments, 0)).OwnPropertyNames()));
        Method("getPrototypeOf", 1, static arguments => ReflectTarget(FunctionArgument(arguments, 0)).PropertyPrototype);
        Method("setPrototypeOf", 2, static arguments =>
        {
            var target = ReflectTarget(FunctionArgument(arguments, 0));
            object prototype = FunctionArgument(arguments, 1);
            if (prototype != null && prototype is not TsObject) throw new InvalidOperationException("Prototype must be an object or null");
            return BoxBoolean(target.TrySetPrototype((TsObject)prototype));
        });
        Method("getOwnPropertyDescriptor", 2, static arguments => ReflectTarget(FunctionArgument(arguments, 0)).OwnPropertyDescriptor(PropertyKey(FunctionArgument(arguments, 1))));
        Method("defineProperty", 3, static arguments => ReflectDefineProperty(FunctionArgument(arguments, 0), FunctionArgument(arguments, 1), FunctionArgument(arguments, 2)));
        return result;
    }
}
