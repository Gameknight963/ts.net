using System;
using System.Runtime.CompilerServices;

public abstract partial class TsObject
{
    // Enum-specific prototype state must not enlarge every application object.
    private sealed class EnumState { internal TsObject Prototype = Js.EnumObjectPrototype; }
    private static readonly ConditionalWeakTable<TsObject, EnumState> enumStates = new();
    internal void InitializeEnum() => enumStates.Add(this, new EnumState());
    private bool EnumHasInherited(string key) => enumStates.TryGetValue(this, out var state) && state.Prototype != null && (key == "__proto__" || state.Prototype.HasProperty(key));
    private object EnumInherited(string key) => !enumStates.TryGetValue(this, out var state) || state.Prototype == null ? Js.Undefined : key == "__proto__" ? state.Prototype : state.Prototype.GetWithReceiver(key, this);
    private bool SetEnumPrototype(string key, object value)
    {
        if (key != "__proto__" || HasOwn(key) || !enumStates.TryGetValue(this, out var state) || state.Prototype == null) return false;
        if (value == null || value is TsObject)
        {
            for (var ancestor = value as TsObject; ancestor != null;)
            {
                if (ReferenceEquals(ancestor, this)) throw new InvalidOperationException("Cyclic prototype value");
                ancestor = enumStates.TryGetValue(ancestor, out var parent) ? parent.Prototype : null;
            }
            state.Prototype = value as TsObject;
        }
        return true;
    }
}

public static partial class Js
{
    private sealed class EnumPrototypeObject : TsObject { }
    internal static readonly TsObject EnumObjectPrototype = new EnumPrototypeObject();
    public static void InitializeEnum(object value) => ((TsObject)value).InitializeEnum();
}
