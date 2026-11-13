using System;
using System.Runtime.CompilerServices;

// Tokens carry no getter reference, so deleting a getter can release its closure.
internal sealed class AccessorVersion
{
    internal object Token;
    // This cell owns no getter, so its initial token can safely reference itself.
    internal AccessorVersion() {Token = this;}
}

public abstract partial class TsObject
{
    internal object WatchAccessorStructure() => accessors?.WatchStructure();
}

public sealed partial class JsClassFunction
{
    struct SimpleGetter
    {
        internal string Name;
        internal Type ExpectedType;
        internal object MapToken;
        internal AccessorVersion GetterVersion;
        internal object GetterToken;
        internal bool Valid;
    }
    SimpleGetter[] simpleGetters;

    internal void RegisterSimpleGetter(int slot, string name, JsFunction function)
    {
        // Shared runtime function classes can carry different delegate bodies.
        if (function.GetType().Assembly == typeof(Js).Assembly) return;
        if (simpleGetters == null || slot >= simpleGetters.Length)
            Array.Resize(ref simpleGetters, Math.Max(slot + 1, (simpleGetters?.Length ?? 0) * 2));
        simpleGetters[slot].Name = name;
        simpleGetters[slot].ExpectedType = function.GetType();
        RefreshSimpleGetter(slot, Prototype.WatchAccessorStructure());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool MatchesSimpleGetter(JsClassInstance receiver, int slot)
    {
        if (Parent != null || simpleGetters == null || (uint)slot >= (uint)simpleGetters.Length ||
            Prototype.GetType() != receiver.GetType() || !ReferenceEquals(receiver.InheritedPrototype, Prototype)) return false;
        ref SimpleGetter entry = ref simpleGetters[slot];
        if (entry.ExpectedType == null || receiver.HasOwn(entry.Name)) return false;
        object token = Prototype.WatchAccessorStructure();
        if (!ReferenceEquals(entry.MapToken, token) || entry.GetterVersion != null && !ReferenceEquals(entry.GetterToken, entry.GetterVersion.Token))
            return RefreshSimpleGetter(slot, token);
        return entry.Valid;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    bool RefreshSimpleGetter(int slot, object mapToken)
    {
        ref SimpleGetter entry = ref simpleGetters[slot];
        entry.MapToken = mapToken;
        if (!Prototype.TryOwnAccessor(entry.Name, out var accessor)) {
            entry.GetterVersion = null; entry.GetterToken = null; return entry.Valid = false;
        }
        entry.GetterVersion = accessor.WatchGetter();
        entry.GetterToken = entry.GetterVersion.Token;
        return entry.Valid = accessor.Getter != null && accessor.Getter.GetType() == entry.ExpectedType;
    }
}

public static partial class Js
{
    // Only source getters whose body is identical for every function instance qualify.
    public static void DefineSimpleClassGetter(object target, string key, object method, int slot)
    {
        DefineClassMethod(target, key, method, false, 1);
        ((JsClassFunction)target).RegisterSimpleGetter(slot, key, (JsFunction)method);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MatchesSimpleClassGetter(object receiver, int slot) => receiver is JsClassInstance instance &&
        instance.Owner != null && instance.Owner.MatchesSimpleGetter(instance, slot);
}
