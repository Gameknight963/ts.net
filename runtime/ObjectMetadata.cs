using System.Collections.Generic;

sealed class ObjectMetadata
{
    internal JsClassFunction ClassOwner;
    internal ulong ClassFieldStates;
    internal TsObject ParentPrototype;
    internal Dictionary<string, JsDescriptorObject.Descriptor> Descriptors;
    internal bool HasInstanceProperties => ParentPrototype != null || Descriptors != null || ClassFieldStates != 0;
    internal ObjectMetadata Copy() => new() {ClassOwner = ClassOwner, ClassFieldStates = ClassFieldStates, ParentPrototype = ParentPrototype, Descriptors = Descriptors};
}

public abstract partial class TsObject
{
    // Empty class headers are shared and immutable. Copy before storing
    // metadata belonging to a particular instance.
    ObjectPropertyState WritablePropertyState()
    {
        if (propertyState is SharedClassPropertyState) propertyState = new ObjectPropertyState {Metadata = propertyState.Metadata.Copy()};
        return propertyState ??= new ObjectPropertyState();
    }
    ObjectMetadata EnsureMetadata() => WritablePropertyState().Metadata ??= new ObjectMetadata();
    protected ulong ClassFieldStates
    {
        get => propertyState is SharedClassPropertyState shared ? shared.States : propertyState?.Metadata?.ClassFieldStates ?? 0;
        set {
            if (ClassFieldStates == value) return;
            if (propertyState is SharedClassPropertyState shared)
                propertyState = shared.Metadata.ClassOwner.NumericInstanceState(value);
            else EnsureMetadata().ClassFieldStates = value;
        }
    }
    // Called only for fresh instances, before any source code can observe them.
    protected void InitializeFreshClassState(JsClassFunction owner, ulong states) =>
        propertyState = owner.NumericInstanceState(states);

    internal JsClassFunction ClassOwner
    {
        get => propertyState?.Metadata?.ClassOwner;
        set {
            if (propertyState == null || propertyState is SharedClassPropertyState) {
                ulong states = ClassFieldStates;
                propertyState = value != null ? value.NumericInstanceState(states) : states == 0 ? null :
                    new ObjectPropertyState {Metadata = new ObjectMetadata {ClassFieldStates = states}};
            }
            else if (value != null || propertyState.Metadata != null) EnsureMetadata().ClassOwner = value;
        }
    }
    internal TsObject ClassParentPrototype
    {
        get => propertyState?.Metadata?.ParentPrototype;
        set { if (value != null || propertyState?.Metadata != null) EnsureMetadata().ParentPrototype = value; }
    }
    internal Dictionary<string, JsDescriptorObject.Descriptor> PropertyDescriptors
    {
        get => propertyState?.Metadata?.Descriptors;
        set { if (value != null || propertyState?.Metadata != null) EnsureMetadata().Descriptors = value; }
    }
}

sealed class SharedClassPropertyState : ObjectPropertyState
{
    internal readonly ulong States;
    internal SharedClassPropertyState(JsClassFunction owner, ulong states = 0) {States = states; Metadata = new ObjectMetadata {ClassOwner = owner, ClassFieldStates = states};}
}
