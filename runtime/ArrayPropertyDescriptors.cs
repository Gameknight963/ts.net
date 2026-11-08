using System;

public partial class JsArray
{
    sealed class NamedProperty(JsArray owner, JsDescriptorObject.Descriptor descriptor)
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
    sealed class NamedGetter(NamedProperty property) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Property requires a receiver")), null, "", 0)
    {
        internal readonly NamedProperty Property = property;
        public override object InvokeWithReceiver(object receiver, object[] arguments) => Property.Read(receiver);
    }
    sealed class NamedSetter(NamedProperty property) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Property requires a receiver")), null, "", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => property.Write(receiver, Js.FunctionArgument(arguments, 0));
    }
    bool TryNamedProperty(string key, out NamedProperty property)
    {
        property = null;
        if (!TryOwnAccessor(key, out var accessor) || accessor.Getter is not NamedGetter getter) return false;
        property = getter.Property;
        return true;
    }
    internal virtual void DefineNamedProperty(JsDescriptorObject.Descriptor descriptor)
    {
        var property = new NamedProperty(this, descriptor);
        DefineOwn(descriptor.Key, Js.Undefined);
        DefineAccessor(descriptor.Key, new NamedGetter(property), 1, false);
        if (descriptor.Accessor ? descriptor.Setter != null : descriptor.Writable)
            DefineAccessor(descriptor.Key, new NamedSetter(property), 2, false);
    }
    bool TrySetNamedData(string key, object value)
    {
        if (!TryNamedProperty(key, out var property) || property.Descriptor.Accessor) return false;
        if (!property.Descriptor.Writable) throw new InvalidOperationException("Property is read-only");
        property.Value = value;
        return true;
    }
    bool DeleteNamedProperty(string key)
    {
        if (TryNamedProperty(key, out var property)) {
            if (!property.Descriptor.Configurable) return false;
            DefineOwn(key, Js.Undefined);
        }
        return base.DeleteOwn(key);
    }
    internal override object OwnPropertyDescriptor(string key)
    {
        if (!TryNamedProperty(key, out var property)) return base.OwnPropertyDescriptor(key);
        var descriptor = property.Descriptor;
        return descriptor.Accessor ? Js.AccessorDescriptor(descriptor.Getter, descriptor.Setter, descriptor.Enumerable, descriptor.Configurable)
            : Js.DataDescriptor(property.Value, descriptor.Writable, descriptor.Enumerable, descriptor.Configurable);
    }
    internal override object LookupOwnAccessor(string key, bool setter)
    {
        if (!TryNamedProperty(key, out var property)) return base.LookupOwnAccessor(key, setter);
        return property.Descriptor.Accessor ? (setter ? property.Descriptor.Setter : property.Descriptor.Getter) ?? Js.Undefined : Js.Undefined;
    }
    bool NamedPropertyEnumerable(string key) => !TryNamedProperty(key, out var property) || property.Descriptor.Enumerable;
}
