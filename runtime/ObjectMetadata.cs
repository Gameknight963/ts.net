using System.Collections.Generic;

sealed class ObjectMetadata
{
    internal TsObject ParentPrototype;
    internal Dictionary<string, JsDescriptorObject.Descriptor> Descriptors;
}

public abstract partial class TsObject
{
    // Empty class headers are shared and immutable. Copy before storing
    // metadata belonging to a particular instance.
    ObjectPropertyState WritablePropertyState()
    {
        if (propertyState is SharedClassPropertyState) propertyState = new ObjectPropertyState {ClassOwner = propertyState.ClassOwner};
        return propertyState ??= new ObjectPropertyState();
    }
    ObjectMetadata EnsureMetadata() => WritablePropertyState().Metadata ??= new ObjectMetadata();
    internal JsClassFunction ClassOwner
    {
        get => propertyState?.ClassOwner;
        set {
            if (propertyState == null || propertyState is SharedClassPropertyState) propertyState = value?.InstanceState;
            else propertyState.ClassOwner = value;
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
    internal SharedClassPropertyState(JsClassFunction owner) {ClassOwner = owner;}
}
