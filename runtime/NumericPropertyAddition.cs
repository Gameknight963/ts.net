using System.Runtime.CompilerServices;

public abstract partial class TsObject
{
    // Null requests ordinary Get, including for an undefined numeric field.
    public virtual double? TryReadOwnNumber(string key) => null;

    internal bool TryReadDynamicNumber(string key, out double? value)
    {
        value = null;
        // Unknown CLR overrides and descriptor-backed properties retain Get.
        if (this is not JsCreatedObject && GetType() != typeof(JsDynamicObject)) return false;
        if (((JsDescriptorObject)this).HasPropertyDescriptors) return false;
        OwnProperties properties = extra;
        if (properties == null || properties.Accessors is { } accessors && accessors.ContainsKey(key)) return false;
        return properties.TryReadNumber(key, out value);
    }
}

public static partial class Js
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? TryReadOwnNumber(object receiver, string key) =>
        receiver is TsObject obj ? obj.TryReadOwnNumber(key) : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NumberHasValue(double? value) => value.HasValue;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double? AddNumberRightCheckedSlow(object left, double? right) =>
        AddNumberRightChecked(left, right);

    public static double? AddPropertyToNumber(double? left, object receiver, string key) =>
        receiver is TsObject obj && obj.TryReadDynamicNumber(key, out double? right)
            ? NumberValue(left) + NumberValue(right) : AddToNumber(left, Get(receiver, key));
}
