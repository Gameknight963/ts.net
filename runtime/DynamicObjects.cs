// Dynamic objects need a prototype on normal construction paths. Store it
// directly; fixed-layout objects use the table only when their prototype changes.
public partial class JsDynamicObject : TsObject
{
    private TsObject prototype = Js.DefaultClassObjectPrototype;
    internal bool Extensible = true;
    internal override bool IsExtensible => Extensible;
    internal override TsObject PropertyPrototype => IsModuleNamespace ? null : prototype;
    internal override void SetPrototype(TsObject value)
    {
        if (CanSetPrototype(value)) prototype = value;
    }
}
