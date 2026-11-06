using System;

public static partial class Js
{
    sealed class PromiseWithResolversFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("withResolvers requires a constructor")), null, "withResolvers", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var capability = NewPromiseCapability(receiver);
            var result = new JsDynamicObject();
            result.DefineOwn("promise", capability.Promise);
            result.DefineOwn("resolve", capability.Resolve);
            result.DefineOwn("reject", capability.Reject);
            return result;
        }
    }
    sealed class PromiseTryFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("try requires a constructor")), null, "try", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var capability = NewPromiseCapability(receiver);
            object result;
            try
            {
                object callback = FunctionArgument(arguments, 0);
                var values = new object[Math.Max(0, arguments.Length - 1)];
                if (values.Length != 0) Array.Copy(arguments, 1, values, 0, values.Length);
                result = CallFunction(callback, values);
            }
            catch (Exception error) { capability.Complete(CaughtValue(error), true); return capability.Promise; }
            capability.Complete(result, false);
            return capability.Promise;
        }
    }
}
