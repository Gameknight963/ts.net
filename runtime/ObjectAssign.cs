using System;
using System.Threading;

public static partial class Js
{
    sealed class ObjectAssignFunction() : JsFunction((FunctionBody)ObjectAssign, null, "assign", 2) { }
    // The compiler supplies an exact generated data class without optional fields.
    // Its own key list is stable while it has no property overrides.
    static class AssignSourceKeys<T>
    {
        static string[] keys;
        internal static string[] Get(TsObject source)
        {
            var result = Volatile.Read(ref keys);
            if (result != null) return result;
            var names = (JsArray)source.OwnPropertyNames();
            result = new string[(int)names.Length()];
            for (int index = 0; index < result.Length; index++) result[index] = (string)names.ElementAt((uint)index);
            return Interlocked.CompareExchange(ref keys, result, null) ?? result;
        }
    }
    static bool AssignCopyAllowed<T>(TsObject source)
    {
        if (source.HasPropertyOverrides || DefaultClassObjectPrototype.PropertyPrototype != null) return false;
        foreach (string key in AssignSourceKeys<T>.Get(source))
            if (DefaultClassObjectPrototype.TryOwnAccessor(key, out _) || DefaultClassObjectPrototype.IsReadOnlyOwnProperty(key)) return false;
        return true;
    }
    public static bool TryCopyAssign<T>(object target, object source, object destination)
    {
        if (target is not ObjectAssignFunction || source is not TsObject value || value.GetType() != typeof(T) || !AssignCopyAllowed<T>(value)) return false;
        foreach (string key in AssignSourceKeys<T>.Get(value)) ((TsObject)destination).DefineOwn(key, value.Get(key));
        return true;
    }
    static object ObjectAssign(object[] arguments)
    {
        var target = ToObject(FunctionArgument(arguments, 0));
        for (int i = 1; i < arguments.Length; i++)
        {
            object input = arguments[i];
            if (IsNullish(input)) continue;
            var source = ToObject(input);
            var keys = (JsArray)source.OwnPropertyNames();
            for (uint index = 0; index < keys.Length(); index++)
            {
                string key = (string)keys.ElementAt(index);
                if (source.HasOwn(key) && source.ForInEnumerable(key))
                    target.Set(key, source.Get(key));
            }
        }
        return target;
    }
}
