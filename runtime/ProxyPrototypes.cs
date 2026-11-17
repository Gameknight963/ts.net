using System;
public static partial class Js
{
    sealed partial class ProxyState
    {
        internal TsObject GetPrototype()
        {
            var original = Target; var trap = Trap("getPrototypeOf");
            if (trap.Method == null) return original.PropertyPrototype;
            object value = trap.Invoke(original);
            if (value != null && value is not TsObject) throw new InvalidOperationException("Proxy prototype must be an object or null");
            if (!ObjectExtensible(original) && !ReferenceEquals(value, original.PropertyPrototype)) throw new InvalidOperationException("Proxy cannot change a sealed target's prototype");
            return value as TsObject;
        }
        internal bool SetPrototype(TsObject prototype)
        {
            var original = Target; var trap = Trap("setPrototypeOf");
            if (trap.Method == null) return original.TrySetPrototype(prototype);
            if (!Truthy(trap.Invoke(original, prototype))) return false;
            if (!ObjectExtensible(original) && !ReferenceEquals(prototype, original.PropertyPrototype)) throw new InvalidOperationException("Proxy cannot change a sealed target's prototype");
            return true;
        }
        internal bool Extensible()
        {
            var original = Target; var trap = Trap("isExtensible");
            if (trap.Method == null) return ObjectExtensible(original);
            bool result = Truthy(trap.Invoke(original));
            if (result != ObjectExtensible(original)) throw new InvalidOperationException("Proxy extensibility must match its target");
            return result;
        }
        internal bool PreventExtensions()
        {
            var original = Target; var trap = Trap("preventExtensions");
            if (trap.Method == null) return Truthy(ReflectPreventExtensions(original));
            if (!Truthy(trap.Invoke(original))) return false;
            if (ObjectExtensible(original)) throw new InvalidOperationException("Proxy target remains extensible");
            return true;
        }
    }
}
