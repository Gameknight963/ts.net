using System;

public static partial class Js
{
    static class LegacyDecoratorFunctions
    {
        internal static readonly JsFunction Parameter = new((FunctionBody)(static args =>
        {
            object index = FunctionArgument(args, 0), decorator = FunctionArgument(args, 1);
            if (decorator is not JsFunction function) throw new InvalidOperationException("Parameter decorator is not callable");
            return new JsFunction((FunctionBody)(arguments =>
            {
                function.InvokeWithReceiver(Undefined, new[] { FunctionArgument(arguments, 0), FunctionArgument(arguments, 1), index });
                return Undefined;
            }), null, "", 2);
        }), null, "", 2);

        internal static readonly JsFunction Class = new((FunctionBody)(static args =>
        {
            object value = FunctionArgument(args, 0);
            var decorators = (JsArray)FunctionArgument(args, 1);
            for (long i = (long)decorators.Length() - 1; i >= 0; i--)
            {
                if (decorators.ElementAt((uint)i) is not JsFunction decorator) throw new InvalidOperationException("Class decorator is not callable");
                object result = decorator.InvokeWithReceiver(Undefined, new[] { value });
                if (Truthy(result)) value = result;
            }
            return value;
        }), null, "", 2);

        internal static readonly JsFunction Member = new((FunctionBody)(static args =>
        {
            var home = (JsClassFunction)args[0];
            string key = PropertyKey(args[1]);
            object publicKey = symbolKeys.TryGetValue(key, out var symbol) ? symbol : key;
            int flags = (int)Number(args[2]);
            bool isStatic = (flags & 1) != 0, field = (flags & 2) != 0;
            TsObject target = isStatic ? home : home.Prototype;
            object descriptor = field ? Undefined : target.OwnPropertyDescriptor(key);
            var previous = descriptor as TsObject;
            bool writable = previous != null && Truthy(previous.Get("writable"));
            bool enumerable = previous != null && Truthy(previous.Get("enumerable"));
            bool configurable = previous != null && Truthy(previous.Get("configurable"));
            var decorators = (JsArray)args[3];
            for (long i = (long)decorators.Length() - 1; i >= 0; i--)
            {
                if (decorators.ElementAt((uint)i) is not JsFunction decorator) throw new InvalidOperationException("Member decorator is not callable");
                object result = decorator.InvokeWithReceiver(Undefined, new[] { (object)target, publicKey, descriptor });
                if (Truthy(result)) descriptor = result;
            }
            if (Truthy(descriptor))
            {
                if (descriptor is not TsObject data) throw new InvalidOperationException("Decorator property descriptor must be an object");
                bool accessor = data.HasProperty("get") || data.HasProperty("set");
                if (accessor && (data.HasProperty("value") || data.HasProperty("writable"))) throw new InvalidOperationException("Decorator descriptor mixes data and accessors");
                if ((data.HasProperty("enumerable") ? Truthy(data.Get("enumerable")) : enumerable)
                    || !(data.HasProperty("configurable") ? Truthy(data.Get("configurable")) : configurable)
                    || !accessor && !(data.HasProperty("writable") ? Truthy(data.Get("writable")) : writable))
                    throw new NotSupportedException("Legacy decorator changes to property attributes are not supported");
                target.DeleteOwn(key);
                if (accessor)
                {
                    object getter = data.Get("get"), setter = data.Get("set");
                    if (!IsUndefined(getter)) DefineClassMethod(home, key, getter, isStatic, 1);
                    if (!IsUndefined(setter)) DefineClassMethod(home, key, setter, isStatic, 2);
                }
                else DefineClassMethod(home, key, data.Get("value"), isStatic, 0);
            }
            return Undefined;
        }), null, "", 4);
    }
}
