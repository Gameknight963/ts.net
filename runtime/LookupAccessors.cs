using System;

public abstract partial class TsObject
{
    internal virtual object LookupOwnAccessor(string key, bool setter)
    {
        // Namespace bindings are exposed as data descriptors, and inspection
        // must still throw for an uninitialized binding.
        if (IsModuleNamespace) { _ = Get(key); return Js.Undefined; }
        if (!TryOwnAccessor(key, out var accessor)) return Js.Undefined;
        JsFunction function = setter ? accessor.Setter : accessor.Getter;
        return function is null ? Js.Undefined : function;
    }
}

public static partial class Js
{
    sealed class LookupAccessorFunction(bool setter) : JsFunction(
        (FunctionBody)(static _ => throw new InvalidOperationException("Accessor lookup requires a receiver")), null, setter ? "__lookupSetter__" : "__lookupGetter__", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            TsObject current = ToObject(receiver);
            string key = PropertyKey(FunctionArgument(arguments, 0));
            while (current != null)
            {
                if (current.HasOwn(key)) return current.LookupOwnAccessor(key, setter);
                current = current.PropertyPrototype;
            }
            return Undefined;
        }
    }
}
