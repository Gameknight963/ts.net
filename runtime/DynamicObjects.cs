// Dynamic objects need a prototype on normal construction paths. Store it
// directly; fixed-layout objects use the table only when their prototype changes.
public partial class JsDynamicObject : JsDescriptorObject
{
    public override object OwnStringKeys() => GetType() == typeof(JsDynamicObject) && !IsModuleNamespace && !HasPropertyDescriptors
        ? StringKeysFromTextFields(System.Array.Empty<object>()) : base.OwnStringKeys();
    public override object GetWithReceiver(string key, object receiver) => GetType() == typeof(JsDynamicObject)
        ? GetStoredWithReceiver(key, receiver) : base.GetWithReceiver(key, receiver);
    public override bool TrySetNumberProperty(string key, double? value) => GetType() == typeof(JsDynamicObject) && TrySetStoredNumber(key, value);
    public override double? GetNumberProperty(string key) => GetType() == typeof(JsDynamicObject)
        ? GetStoredNumberWithReceiver(key, this) : base.GetNumberProperty(key);
    public override double? GetNumberWithReceiver(string key, object receiver) => GetType() == typeof(JsDynamicObject)
        ? GetStoredNumberWithReceiver(key, receiver) : base.GetNumberWithReceiver(key, receiver);
    private TsObject prototype = Js.DefaultClassObjectPrototype;
    // The caller must own a fresh object that has not been exposed to user code.
    internal override void InitializeFreshPrototype(TsObject value) => prototype = value;
    internal override TsObject PropertyPrototype => IsModuleNamespace ? null : prototype;
    internal override void SetPrototype(TsObject value)
    {
        if (CanSetPrototype(value)) prototype = value;
    }
}
