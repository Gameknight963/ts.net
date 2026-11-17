using System;

public abstract class JsNumericClassInstance : JsClassInstance
{
    // Used only for freshly allocated classes whose initializer and constructor
    // have been proved to consist entirely of direct field definitions.
    protected void InitializeDirectClass(object owner, int initialStates)
    {
        Owner = (JsClassFunction)owner;
        states = (uint)initialStates;
    }
    bool materialized;
    // Two bits per field: absent (0), own undefined (1), or own value (3).
    uint states;
    protected abstract int NumericFieldIndex(string key);
    protected abstract double ReadNumericField(int index);
    protected abstract void WriteNumericField(int index, double value);
    protected abstract void CopyNumericFields();
    protected virtual bool IsReferenceField(int index) => false;
    protected virtual object ReadReferenceField(int index) => throw new InvalidOperationException("Reference field reader is missing");
    protected virtual void WriteReferenceField(int index, object value) => throw new InvalidOperationException("Reference field writer is missing");
    object ReadFieldValue(int index) => IsReferenceField(index) ? ReadReferenceField(index) : ReadNumericField(index);
    bool StoreReference(int index, object value, bool create)
    {
        // The property store keeps mutable numeric cells for repeated writes.
        // Do not replace those with a fresh boxed number on every update.
        if (materialized || index < 0 || value is double || !IsReferenceField(index)) return false;
        int shift = index * 2;
        uint state = states >> shift & 3;
        if (state == 0 && (!create || states >> shift != 0)) return false;
        bool undefined = ReferenceEquals(value, Js.Undefined);
        WriteReferenceField(index, undefined ? null : value);
        states = states & ~(3u << shift) | (undefined ? 1u : 3u) << shift;
        return true;
    }

    internal bool HasDefinedClassFields(int mask) => !materialized && (states & unchecked((uint)mask)) == unchecked((uint)mask);

    protected int NumericFieldState(int index) => materialized ? -1 : (int)(states >> (index * 2) & 3);
    protected void CopyNumericField(int index, string key)
    {
        int state = NumericFieldState(index);
        if (state != 0) StoreNumericClassProperty(key, state == 1 ? Js.Undefined : ReadFieldValue(index));
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
        if (IsReferenceField(index)) {
            if (value.HasValue) return false;
            WriteReferenceField(index, null);
        }
        else if (value.HasValue) WriteNumericField(index, value.Value);
        states = states & ~(3u << shift) | (value.HasValue ? 3u : 1u) << shift;
        return true;
    }
    protected bool TrySetClassNumber(int index, double? value) => StoreNumber(index, value, false);
    protected bool TrySetClassString(int index, object value)
    {
        // Declared string values can still be null or undefined. Keep the
        // general representation for those; actual strings are always defined.
        if (value is not string) return TrySetClassValue(index, value);
        if (materialized || index < 0 || !IsReferenceField(index)) return false;
        int shift = index * 2;
        if ((states >> shift & 3) == 0) return false;
        WriteReferenceField(index, value);
        states |= 3u << shift;
        return true;
    }
    protected bool TrySetClassValue(int index, object value)
    {
        if (StoreReference(index, value, false)) return true;
        if (value is double number) return StoreNumber(index, number, false);
        return ReferenceEquals(value, Js.Undefined) && StoreNumber(index, null, false);
    }
    // Creating a field retains the same ordering checks as definition by name.
    protected bool TryDefineClassNumber(int index, double? value) => StoreNumber(index, value, true);
    protected bool TryDefineClassValue(int index, object value)
    {
        if (StoreReference(index, value, true)) return true;
        if (value is double number) return StoreNumber(index, number, true);
        return ReferenceEquals(value, Js.Undefined) && StoreNumber(index, null, true);
    }
    internal bool TryDefineNumber(string key, double? value)
    {
        if (StoreNumber(NumericFieldIndex(key), value, true)) return true;
        MaterializeProperties(); return false;
    }
    internal bool TryDefineNumericValue(string key, object value)
    {
        if (StoreReference(NumericFieldIndex(key), value, true)) return true;
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
                if (state == 3) return IsReferenceField(index) ? Js.Cast<double?>(ReadReferenceField(index), "checked conversion failed") : ReadNumericField(index);
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
                if (state == 3) return ReadFieldValue(index);
                if (state == 1) return Js.Undefined;
            }
        }
        return GetStoredWithReceiver(key, receiver);
    }
    internal bool HasNumericFieldValue(string key)
    {
        if (materialized) return false;
        int index = NumericFieldIndex(key);
        return index >= 0 && NumericFieldState(index) == 3 && (!IsReferenceField(index) || ReadReferenceField(index) is double);
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
            if (StoreReference(index, value, true)) return value;
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
