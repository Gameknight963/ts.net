// Dynamic objects need a prototype on normal construction paths. Store it
// directly; fixed-layout objects use the table only when their prototype changes.
public partial class JsDynamicObject : JsDescriptorObject
{
    public override bool TrySetNumberProperty(string key, double? value) => GetType() == typeof(JsDynamicObject) && TrySetStoredNumber(key, value);
    public override double? GetNumberProperty(string key) => GetType() == typeof(JsDynamicObject)
        ? GetStoredNumberWithReceiver(key, this) : base.GetNumberProperty(key);
    public override double? GetNumberWithReceiver(string key, object receiver) => GetType() == typeof(JsDynamicObject)
        ? GetStoredNumberWithReceiver(key, receiver) : base.GetNumberWithReceiver(key, receiver);
    private TsObject prototype = Js.DefaultClassObjectPrototype;
    internal override TsObject PropertyPrototype => IsModuleNamespace ? null : prototype;
    internal override void SetPrototype(TsObject value)
    {
        if (CanSetPrototype(value)) prototype = value;
    }
}
