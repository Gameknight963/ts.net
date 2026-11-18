using System.Collections.Generic;

sealed class ObjectMetadata
{
    internal JsClassFunction ClassOwner;
    internal TsObject ParentPrototype;
    internal Dictionary<string, JsDescriptorObject.Descriptor> Descriptors;
    internal bool HasInstanceProperties => ParentPrototype != null || Descriptors != null;
    internal ObjectMetadata Copy() => new() {ClassOwner = ClassOwner, ParentPrototype = ParentPrototype, Descriptors = Descriptors};
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
    internal JsClassFunction ClassOwner
    {
        get => propertyState?.Metadata?.ClassOwner;
        set {
            if (propertyState == null || propertyState is SharedClassPropertyState) propertyState = value?.InstanceState;
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
    internal SharedClassPropertyState(JsClassFunction owner) {Metadata = new ObjectMetadata {ClassOwner = owner};}
}
