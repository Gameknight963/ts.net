using System;

sealed partial class OwnProperties
{
    bool AddNumber(ref object slot, int flag, double delta, bool postfix, out double result)
    {
        double previous;
        if (slot is NumberCell cell) {
            previous = cell.Value;
            cell.Value = previous + delta; cell.Boxed = null;
            result = postfix ? previous : cell.Value;
            return true;
        }
        if (slot is double value) previous = value;
        else if (ReferenceEquals(slot, Js.Undefined)) previous = double.NaN;
        else { result = 0; return false; }
        double next = previous + delta;
        if (flag == 0) slot = new NumberCell(next);
        else StoreNumber(ref slot, next, flag);
        result = postfix ? previous : next;
        return true;
    }
    internal bool TryAddNumber(string key, double delta, bool postfix, out double result)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key == key0) return AddNumber(ref value0, 1, delta, postfix, out result);
        if (key == key1) return AddNumber(ref value1, 2, delta, postfix, out result);
        if (key == key2) return AddNumber(ref value2, 4, delta, postfix, out result);
        if (key == key3) return AddNumber(ref value3, 8, delta, postfix, out result);
        if (tail != null) {
            if (!tail.TryGetValue(key, out object stored)) { result = 0; return false; }
            object previous = stored;
            if (!AddNumber(ref stored, 0, delta, postfix, out result)) return false;
            if (!ReferenceEquals(previous, stored)) tail[key] = stored;
            return true;
        }
        if (tailOrder is SingleProperty single && key == single.Key)
            return AddNumber(ref single.Value, 0, delta, postfix, out result);
        result = 0; return false;
    }
}

public abstract partial class TsObject
{
    internal bool TryAddStoredNumber(string key, double delta, bool postfix, out double result)
    {
        result = 0;
        if (GetType() != typeof(JsDynamicObject) && GetType() != typeof(JsClassInstance) && this is not JsCreatedObject)
            return this is JsNumericClassInstance numeric && numeric.TryAddNumberField(key, delta, postfix, out result);
        return !IsModuleNamespace && !((JsDescriptorObject)this).HasPropertyDescriptors &&
            !TryOwnAccessor(key, out _) && extra != null && extra.TryAddNumber(key, delta, postfix, out result);
    }
}

public static partial class Js
{
    public static double? AddNumberProperty(object receiver, string key, double delta, bool postfix)
    {
        if (receiver is TsObject obj && obj.TryAddStoredNumber(key, delta, postfix, out double result)) return result;
        double previous = NumberValue(GetNumber(receiver, key));
        double next = previous + delta;
        SetNumber(receiver, key, next);
        return postfix ? previous : next;
    }
    public static double? AddNumberIndex(object receiver, object key, double delta, bool postfix)
    {
        if (IsNullish(receiver)) throw new InvalidOperationException("Cannot read property of null or undefined");
        if (key is string text) return AddNumberProperty(receiver, text, delta, postfix);
        // Object keys may convert differently between the read and the write.
        double previous = NumberValue(IndexGetNumberValue(receiver, key));
        double next = previous + delta;
        IndexSetNumberValue(receiver, key, next);
        return postfix ? previous : next;
    }
}
