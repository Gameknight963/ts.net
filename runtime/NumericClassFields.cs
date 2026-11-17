using System;

public abstract class JsNumericClassInstance : JsClassInstance
{
    bool materialized;
    // Two bits per field: absent (0), own undefined (1), or own number (3).
    uint states;
    protected abstract int NumericFieldIndex(string key);
    protected abstract double ReadNumericField(int index);
    protected abstract void WriteNumericField(int index, double value);
    protected abstract void CopyNumericFields();
    protected int NumericFieldState(int index) => materialized ? -1 : (int)(states >> (index * 2) & 3);
    protected void CopyNumericField(int index, string key)
    {
        int state = NumericFieldState(index);
        if (state != 0) StoreNumericClassProperty(key, state == 1 ? Js.Undefined : ReadNumericField(index));
    }
    public void MaterializeProperties()
    {
        if (materialized) return;
        CopyNumericFields();
        materialized = true;
    }
    bool StoreNumber(int index, double? value, bool create)
    {
        if (materialized || index < 0) return false;
        int shift = index * 2;
        uint state = states >> shift & 3;
        // Copying fields in their compiled order must preserve insertion order.
        // An out-of-order creation uses the ordinary property store instead.
        if (state == 0 && (!create || states >> shift != 0)) return false;
        if (value.HasValue) WriteNumericField(index, value.Value);
        states = states & ~(3u << shift) | (value.HasValue ? 3u : 1u) << shift;
        return true;
    }
    internal bool TryDefineNumber(string key, double? value)
    {
        if (StoreNumber(NumericFieldIndex(key), value, true)) return true;
        MaterializeProperties(); return false;
    }
    internal bool TryDefineNumericValue(string key, object value)
    {
        if (value is double number) return TryDefineNumber(key, number);
        if (ReferenceEquals(value, Js.Undefined)) return TryDefineNumber(key, null);
        MaterializeProperties(); return false;
    }
    protected double? ReadNumericProperty(string key, object receiver)
    {
        if (!materialized)
        {
            int index = NumericFieldIndex(key);
            if (index >= 0)
            {
                int state = NumericFieldState(index);
                if (state == 3) return ReadNumericField(index);
                if (state == 1) return null;
            }
        }
        return GetStoredNumberWithReceiver(key, receiver);
    }
    public override double? GetNumberProperty(string key) => ReadNumericProperty(key, this);
    public override double? GetNumberWithReceiver(string key, object receiver) => ReadNumericProperty(key, receiver);
    public override object GetWithReceiver(string key, object receiver)
    {
        if (!materialized)
        {
            int index = NumericFieldIndex(key);
            if (index >= 0)
            {
                int state = NumericFieldState(index);
                if (state == 3) return ReadNumericField(index);
                if (state == 1) return Js.Undefined;
            }
        }
        return GetStoredWithReceiver(key, receiver);
    }
    internal bool HasNumericFieldValue(string key)
    {
        if (materialized) return false;
        int index = NumericFieldIndex(key);
        return index >= 0 && NumericFieldState(index) == 3;
    }
    public override bool HasOwn(string key)
    {
        if (materialized) return base.HasOwn(key);
        int index = NumericFieldIndex(key);
        return index >= 0 && NumericFieldState(index) != 0;
    }
    public override bool TrySetNumberProperty(string key, double? value) => materialized
        ? TrySetStoredNumber(key, value) : StoreNumber(NumericFieldIndex(key), value, false);
    public override object SetOwnValue(string key, object value)
    {
        if (!materialized)
        {
            int index = NumericFieldIndex(key);
            if (value is double number && StoreNumber(index, number, true) ||
                ReferenceEquals(value, Js.Undefined) && StoreNumber(index, null, true)) return value;
            MaterializeProperties();
        }
        return base.SetOwnValue(key, value);
    }
    public override bool DeleteOwn(string key) {MaterializeProperties(); return base.DeleteOwn(key);}
    public override object OwnKeys() {MaterializeProperties(); return base.OwnKeys();}
    public override double OwnKeyCount() {MaterializeProperties(); return base.OwnKeyCount();}
    internal override object ForInOwnKeys() {MaterializeProperties(); return base.ForInOwnKeys();}
    internal override object OwnPropertyNames() {MaterializeProperties(); return base.OwnPropertyNames();}
    public override object CloneForSpread() {MaterializeProperties(); return base.CloneForSpread();}
}
public abstract partial class TsObject
{
    protected void StoreNumericClassProperty(string key, object value) => (extra ??= new OwnProperties())[key] = value;
}
public static partial class Js
{
    public static void DefineClassNumberField(object target, string key, double? value)
    {
        target = ResolveThis(target);
        if (target is JsNumericClassInstance numeric && numeric.TryDefineNumber(key, value)) return;
        DefineClassField(target, key, BoxNumber(value));
    }
}
