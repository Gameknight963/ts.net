using System;

public partial class JsArray
{
    // The length slot is normally implicit. Materialize a hidden marker only
    // when it becomes read-only, keeping ordinary array layouts unchanged.
    internal bool LengthWritable => !base.HasOwn("length");
    internal void MakeLengthReadOnly() => base.DefineOwn("length", Js.Undefined);
    internal override bool IsReadOnlyOwnProperty(string key) => key == "length" && !LengthWritable || base.IsReadOnlyOwnProperty(key);
    protected void CheckLengthWritable()
    {
        if (!LengthWritable) throw new InvalidOperationException("Array length is read-only");
    }
    protected void CheckGrowth(uint index)
    {
        if (index >= Count) CheckLengthWritable();
    }
}

public static partial class Js
{
    static bool DefineArrayLength(JsArray array, PropertyDefinition definition)
    {
        bool Has(DescriptorFields field) => (definition.Fields & field) != 0;
        uint length = (uint)array.Length();
        if (Has(DescriptorFields.Value)) {
            double first = Number(definition.Value);
            length = unchecked((uint)ToInt32(first));
            if (Number(definition.Value) != length) throw new ArgumentOutOfRangeException(null, "Invalid array length");
        }
        bool writable = array is not JsTemplateArray && array.LengthWritable;
        if (definition.Accessor || Has(DescriptorFields.Configurable) && definition.Configurable || Has(DescriptorFields.Enumerable) && definition.Enumerable) return false;
        if (!writable && (Has(DescriptorFields.Writable) && definition.Writable || length != array.Length())) return false;
        if (Has(DescriptorFields.Value) && writable) array.SetLength(length);
        if (writable && Has(DescriptorFields.Writable) && !definition.Writable) array.MakeLengthReadOnly();
        return true;
    }
}
