using System;

public partial class JsArray
{
    internal bool CanUseElementStorage => !HasIndexedAccessors && ReferenceEquals(PropertyPrototype, Js.ArrayPrototype) && Js.ArraySlicePrototypesHaveNoIndices();
    sealed class DefinedProperty(JsArray owner, JsDescriptorObject.Descriptor descriptor)
    {
        internal readonly JsArray Owner = owner;
        internal readonly JsDescriptorObject.Descriptor Descriptor = descriptor with {Value = Js.Undefined};
        internal object Value = descriptor.Value;
        internal object Read(object receiver) => Descriptor.Accessor
            ? Descriptor.Getter == null ? Js.Undefined : Descriptor.Getter.InvokeWithReceiver(receiver, Array.Empty<object>()) : Value;
        internal object Write(object receiver, object value)
        {
            if (Descriptor.Accessor) Descriptor.Setter.InvokeWithReceiver(receiver, new[] {value});
            else if (ReferenceEquals(receiver, Owner)) Value = value;
            else if (receiver is TsObject target) target.SetOwnValue(Descriptor.Key, value);
            else throw new InvalidOperationException("Property receiver must be an object");
            return value;
        }
    }
    sealed class PropertyGetter(DefinedProperty property) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Property requires a receiver")), null, "", 0)
    {
        internal readonly DefinedProperty Property = property;
        public override object InvokeWithReceiver(object receiver, object[] arguments) => Property.Read(receiver);
    }
    sealed class PropertySetter(DefinedProperty property) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Property requires a receiver")), null, "", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => property.Write(receiver, Js.FunctionArgument(arguments, 0));
    }
    bool TryDefinedProperty(string key, out DefinedProperty property)
    {
        property = null;
        if (!TryOwnAccessor(key, out var accessor) || accessor.Getter is not PropertyGetter getter) return false;
        property = getter.Property;
        return true;
    }
    internal virtual void DefineArrayProperty(JsDescriptorObject.Descriptor descriptor)
    {
        if (this is JsTemplateArray) return;
        if (Index(descriptor.Key, out uint index)) {
            if (!descriptor.Accessor && descriptor.Writable && descriptor.Enumerable && descriptor.Configurable) {
                if (HasOwnAccessors && base.HasOwn(descriptor.Key)) DeleteDefinedProperty(descriptor.Key);
                Write(index, descriptor.Value);
                return;
            }
            if (index >= Count) Resize(index + 1);
            DeleteElement(index);
        }
        var property = new DefinedProperty(this, descriptor);
        DefineOwn(descriptor.Key, Js.Undefined);
        DefineAccessor(descriptor.Key, new PropertyGetter(property), 1, false);
        if (descriptor.Accessor ? descriptor.Setter != null : descriptor.Writable)
            DefineAccessor(descriptor.Key, new PropertySetter(property), 2, false);
    }
    bool TrySetDefinedData(string key, object value)
    {
        if (!TryDefinedProperty(key, out var property) || property.Descriptor.Accessor) return false;
        if (!property.Descriptor.Writable) throw new InvalidOperationException("Property is read-only");
        property.Value = value;
        return true;
    }
    bool DeleteDefinedProperty(string key)
    {
        if (TryDefinedProperty(key, out var property)) {
            if (!property.Descriptor.Configurable) return false;
            DefineOwn(key, Js.Undefined);
        }
        return base.DeleteOwn(key);
    }
    protected bool HasDefinedElement(uint index) => HasIndexedAccessors && TryOwnAccessor(index.ToString(System.Globalization.CultureInfo.InvariantCulture), out _);
    protected bool TryReadDefinedElement(uint index, out object value)
    {
        value = null;
        if (!HasIndexedAccessors || !TryOwnAccessor(index.ToString(System.Globalization.CultureInfo.InvariantCulture), out var accessor)) return false;
        value = accessor.Read(this);
        return true;
    }
    protected bool TryWriteDefinedElement(uint index, object value)
    {
        if (!HasIndexedAccessors || !TryOwnAccessor(index.ToString(System.Globalization.CultureInfo.InvariantCulture), out var accessor)) return false;
        accessor.Write(this, value);
        return true;
    }
    protected bool TryWriteDefinedNumber(uint index, double? value)
    {
        if (!HasIndexedAccessors || !TryOwnAccessor(index.ToString(System.Globalization.CultureInfo.InvariantCulture), out var accessor)) return false;
        accessor.Write(this, Js.BoxNumber(value));
        return true;
    }
    internal override object OwnPropertyDescriptor(string key)
    {
        if (!TryDefinedProperty(key, out var property)) return base.OwnPropertyDescriptor(key);
        var descriptor = property.Descriptor;
        return descriptor.Accessor ? Js.AccessorDescriptor(descriptor.Getter, descriptor.Setter, descriptor.Enumerable, descriptor.Configurable)
            : Js.DataDescriptor(property.Value, descriptor.Writable, descriptor.Enumerable, descriptor.Configurable);
    }
    internal override object LookupOwnAccessor(string key, bool setter)
    {
        if (!TryDefinedProperty(key, out var property)) return base.LookupOwnAccessor(key, setter);
        return property.Descriptor.Accessor ? (setter ? property.Descriptor.Setter : property.Descriptor.Getter) ?? Js.Undefined : Js.Undefined;
    }
    bool DefinedPropertyEnumerable(string key) => !TryDefinedProperty(key, out var property) || property.Descriptor.Enumerable;
}
