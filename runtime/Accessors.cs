using System;
using System.Collections.Generic;

internal sealed class JsAccessor
{
    JsFunction getter;
    AccessorVersion getterVersion;
    internal JsFunction Getter
    {
        get => getter;
        set {
            if (!ReferenceEquals(getter, value) && getterVersion != null) getterVersion.Token = new object();
            getter = value;
        }
    }
    internal AccessorVersion WatchGetter() => getterVersion ??= new AccessorVersion();
    internal JsFunction Setter;
    internal object Read(object receiver) => Getter is null ? Js.Undefined : Getter.InvokeWithReceiver(receiver, Array.Empty<object>());
    internal double? ReadNumber(object receiver) => Getter is null ? null
        : Getter is JsReceiverFunction function ? function.InvokeMethodNumber0(receiver)
        : Getter.SupportsNumberResult() ? Getter.InvokeNumber0()
        : Js.Cast<double?>(Getter.InvokeWithReceiver(receiver, Array.Empty<object>()), "checked conversion failed");
    internal object Write(object receiver, object value)
    {
        if (Setter is null) throw new InvalidOperationException("Property has no setter");
        Setter.InvokeWithReceiver(receiver, new[] { value }); return value;
    }
}
internal class JsAccessors : Dictionary<string, JsAccessor>
{
    object version;
    internal object WatchStructure() => version ??= new object();
    void Changed() { if (version != null) version = new object(); }
    internal int IndexedCount { get; private set; }
    internal bool HasExplicitPrototype;
    internal TsObject ExplicitPrototype;
    internal JsAccessors() { }
    internal JsAccessors(Dictionary<string, JsAccessor> source)
    {
        if (source is JsAccessors properties) { HasExplicitPrototype = properties.HasExplicitPrototype; ExplicitPrototype = properties.ExplicitPrototype; }
        foreach (var entry in source) this[entry.Key] = entry.Value;
    }
    public new JsAccessor this[string key]
    {
        get => base[key];
        set {
            if (!ContainsKey(key) && JsArray.Index(key, out _)) IndexedCount++;
            base[key] = value;
            Changed();
        }
    }
    public new bool Remove(string key)
    {
        if (!base.Remove(key)) return false;
        if (JsArray.Index(key, out _)) IndexedCount--;
        Changed();
        return true;
    }
}

public abstract partial class TsObject
{
    sealed class NonExtensibleAccessors : JsAccessors
    {
        internal bool ElementsSealed, ElementsFrozen;
        internal NonExtensibleAccessors() { }
        internal NonExtensibleAccessors(Dictionary<string, JsAccessor> source) : base(source) { }
    }
    JsAccessors accessors
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        get => propertyState?.Accessors;
        set
        {
            if (propertyState != null) propertyState.Accessors = value;
            else if (value != null) propertyState = new ObjectPropertyState {Accessors = value};
            if (value == null && propertyState is not OwnProperties) propertyState = null;
        }
    }
    protected bool StoredExtensionsAllowed => accessors is not NonExtensibleAccessors;
    protected bool StoredArrayElementsSealed => accessors is NonExtensibleAccessors {ElementsSealed: true};
    protected bool StoredArrayElementsFrozen => accessors is NonExtensibleAccessors {ElementsFrozen: true};
    protected void SealStoredArrayElements(bool frozen)
    {
        PreventStoredExtensions();
        var state = (NonExtensibleAccessors)accessors;
        state.ElementsSealed = true;
        state.ElementsFrozen |= frozen;
    }
    internal void PreventStoredExtensions()
    {
        if (accessors is not NonExtensibleAccessors)
            accessors = accessors == null ? new NonExtensibleAccessors() : new NonExtensibleAccessors(accessors);
    }
    internal bool HasIndexedAccessors => accessors is {IndexedCount: > 0};
    internal bool HasOwnAccessors => accessors is {Count: > 0};
    internal IEnumerable<string> OwnAccessorKeys => accessors is { } own ? own.Keys : Array.Empty<string>();
    internal bool HasPropertyOverrides => propertyState != null;
    internal bool TryOwnAccessor(string key, out JsAccessor accessor)
    {
        JsAccessors own = accessors;
        accessor = null; return own is not null && own.TryGetValue(key, out accessor);
    }
    public void DefineAccessor(string key, object function, int mode) => DefineAccessor(key, function, mode, true);
    internal void DefineAccessor(string key, object function, int mode, bool inferName)
    {
        if (this is JsNumericClassInstance numeric) numeric.MaterializeProperties();
        if (!IsExtensible && !HasOwn(key) && (extra == null || !extra.ContainsKey(key))) throw new InvalidOperationException("Object is not extensible");
        if (!TryOwnAccessor(key, out var accessor))
        {
            accessor = new JsAccessor();
            (extra ??= new OwnProperties())[key] = Js.Undefined;
            (accessors ??= new())[key] = accessor;
        }
        var callable = (JsFunction)function;
        if (inferName) callable.InferPropertyName((mode == 1 ? "get " : "set ") + Js.PropertyFunctionName(key));
        if (mode == 1) accessor.Getter = callable; else accessor.Setter = callable;
    }
    public virtual object GetWithReceiver(string key, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Read(receiver) : HasOwn(key) ? Get(key) : InheritedValue(key, receiver);
    public virtual double? GetNumberWithReceiver(string key, object receiver) => Js.Cast<double?>(GetWithReceiver(key, receiver), "checked conversion failed");
    internal bool HasOwnNumberData(string key) => this is JsNumericClassInstance numeric && numeric.HasNumericFieldValue(key) ||
        !TryOwnAccessor(key, out _) && extra != null && extra.HasNumberValue(key);
    protected object GetStoredWithReceiver(string key, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.Read(receiver)
        : extra != null && extra.TryGetValue(key, out var value) ? value : InheritedValue(key, receiver);
    protected double? GetStoredNumberWithReceiver(string key, object receiver) => TryOwnAccessor(key, out var accessor) ? accessor.ReadNumber(receiver)
        : extra != null && extra.TryGetNumber(key, out var value) ? value
        : PropertyPrototype is TsObject prototype ? prototype.GetNumberWithReceiver(key, receiver) : null;
    public virtual object SetWithReceiver(string key, object value, object receiver)
    {
        if (IsModuleNamespace) throw new InvalidOperationException("Module namespace is read-only");
        if (TryOwnAccessor(key, out var accessor)) return accessor.Write(receiver, value);
        bool own = HasOwn(key);
        if (!own && PropertyPrototype is TsObject prototype) return prototype.SetWithReceiver(key, value, receiver);
        if (own && IsReadOnlyOwnProperty(key)) throw new InvalidOperationException("Inherited property is read-only");
        return ((TsObject)receiver).SetOwnValue(key, value);
    }
    public virtual object SetOwnValue(string key, object value)
    {
        if (TryOwnAccessor(key, out _)) throw new InvalidOperationException("Cannot replace accessor through assignment");
        if (IsModuleNamespace || IsReadOnlyOwnProperty(key)) throw new InvalidOperationException("Property is read-only");
        DefineOwn(key, value);
        return value;
    }
}
public static partial class Js
{
    public static bool HasOwnNumberData(object target, string key) => target is TsObject value && value.HasOwnNumberData(key);
    public static bool CanWriteOwnNumberData(object target, string key) => target is TsObject value && !value.IsModuleNamespace && value.HasOwnNumberData(key) && !value.IsReadOnlyOwnProperty(key);
    public static void DefineAccessor(object target, string key, object function, int mode) => ((TsObject)target).DefineAccessor(key, function, mode);
}
