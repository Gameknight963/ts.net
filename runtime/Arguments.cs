using System;
using System.Globalization;

// Strict-mode arguments do not alias parameter bindings. The object is created
// on its first read, and retained by the receiver captured by nested arrows.
internal sealed class JsArgumentsReceiver(object receiver, object[] values)
{
    internal readonly object Receiver = receiver;
    JsArguments arguments;
    internal JsArguments Arguments => arguments ??= new JsArguments(values);
    // Once exposed, length may be reassigned, deleted, or replaced by a getter.
    internal double? Length => arguments == null ? values.Length - 1 : Js.Cast<double?>(arguments.Get("length"), "checked conversion failed");
}

internal sealed class JsArguments : JsDynamicObject
{
    object[] values;
    bool expanded;
    object lengthValue;
    bool lengthAssigned;
    bool iteratorHidden = true;
    internal JsArguments(object[] source)
    {
        // The generated adapter consumes its hidden receiver and parameters
        // before running user code. Keep an independent snapshot of user values.
        values = new object[source.Length - 1];
        Array.Copy(source, 1, values, 0, values.Length);
        lengthValue = (double)values.Length;
        DefineOwn("length", lengthValue);
        DefineOwn("callee", Js.Undefined);
        DefineOwn(Js.ArgumentsIteratorKey, Js.ArrayMethodValue("values"));
    }
    void ExpandProperties()
    {
        if (expanded) return;
        for (int index = 0; index < values.Length; index++)
            base.Define(new(index.ToString(CultureInfo.InvariantCulture), values[index], null, null, false, true, true, true));
        base.Define(new("length", lengthValue, null, null, false, true, false, true));
        base.Define(new("callee", Js.Undefined, CalleeThrower, CalleeThrower, true, false, false, false));
        if (base.HasOwn(Js.ArgumentsIteratorKey))
            base.Define(new(Js.ArgumentsIteratorKey, base.Get(Js.ArgumentsIteratorKey), null, null, false, true, !iteratorHidden, true));
        expanded = true;
        values = null;
        lengthValue = null;
    }
    internal override void Define(Descriptor descriptor) { ExpandProperties(); base.Define(descriptor); }
    static JsFunction CalleeThrower => Js.StrictFunctionThrower;
    internal override object LookupOwnAccessor(string key, bool setter) => key == "callee" ? CalleeThrower : base.LookupOwnAccessor(key, setter);
    internal override object OwnPropertyDescriptor(string key) => key == "callee"
        ? Js.AccessorDescriptor(CalleeThrower, CalleeThrower, false, false) : base.OwnPropertyDescriptor(key);
    static Exception CalleeError() => new InvalidOperationException("Strict arguments.callee cannot be accessed");
    internal override bool IsReadOnlyOwnProperty(string key) => key == "callee" || base.IsReadOnlyOwnProperty(key);
    public override bool HasOwn(string key) => expanded ? base.HasOwn(key) : key is "length" or "callee" || JsArray.Index(key, out uint index) && index < values.Length || base.HasOwn(key);
    internal bool TryReadIndex(double index, out object value)
    {
        if (!expanded && index >= 0 && index < values.Length && index == (int)index) {
            value = values[(int)index];
            return true;
        }
        value = null;
        return false;
    }
    public override object Get(string key)
    {
        if (expanded) return base.Get(key);
        if (key == "callee") throw CalleeError();
        if (key == "length") return lengthValue;
        return JsArray.Index(key, out uint index) && index < values.Length ? values[index] : base.Get(key);
    }
    public override object SetOwnValue(string key, object value) => expanded ? base.SetOwnValue(key, value) : key is "length" or "callee" || JsArray.Index(key, out uint index) && index < values.Length ? Set(key, value) : base.SetOwnValue(key, value);
    public override object Set(string key, object value)
    {
        if (expanded) return base.Set(key, value);
        if (key == "callee") throw CalleeError();
        if (key == "length") { lengthValue = value; lengthAssigned = true; return value; }
        if (JsArray.Index(key, out uint index) && index < values.Length) { values[index] = value; return value; }
        return base.Set(key, value);
    }
    public override object SetWithReceiver(string key, object value, object receiver) => key == "callee" ? throw CalleeError() : base.SetWithReceiver(key, value, receiver);
    internal override object OwnPropertyNames() => expanded ? base.ForInOwnKeys() : AllOwnKeys(true);
    internal override object ForInOwnKeys() => expanded ? base.ForInOwnKeys() : Js.ForInNames(OwnKeys(), "length", "callee");
    internal override bool ForInEnumerable(string key) => expanded ? base.ForInEnumerable(key) : key is not ("length" or "callee") && (!iteratorHidden || key != Js.ArgumentsIteratorKey);
    public override bool DeleteOwn(string key) { ExpandProperties(); return base.DeleteOwn(key); }
    public override double OwnKeyCount() => expanded ? base.OwnKeyCount() : values.Length + ExtraKeyCount - 2;
    public override object OwnKeys()
    {
        if (expanded) return base.OwnKeys();
        var names = (JsArray)AllOwnKeys(false);
        var result = new JsArray(0);
        for (uint index = 0; index < names.Length(); index++) if ((string)names.ElementAt(index) is not ("length" or "callee") && (!iteratorHidden || (string)names.ElementAt(index) != Js.ArgumentsIteratorKey)) result.Append(names.ElementAt(index));
        return result;
    }
    object AllOwnKeys(bool hidden)
    {
        var keys = new object[values.Length + (hidden ? 2 : 0)];
        for (int index = 0; index < values.Length; index++) keys[index] = index.ToString(CultureInfo.InvariantCulture);
        if (hidden) { keys[^2] = "length"; keys[^1] = "callee"; }
        return KeysFromFixed(keys);
    }
    internal double IterationLength()
    {
        double length = expanded ? Js.Number(Get("length")) : lengthAssigned ? Js.Number(lengthValue) : values.Length;
        return double.IsNaN(length) || length <= 0 ? 0 : Math.Min(9007199254740991d, Math.Floor(length));
    }
}

public static partial class Js
{
    internal static string ArgumentsIteratorKey => PropertyKey(IteratorSymbol);
    public static object Arguments(object receiver) => ((JsArgumentsReceiver)receiver).Arguments;
    public static double? ArgumentsLength(object receiver) => ((JsArgumentsReceiver)receiver).Length;
    public static object EnableArguments(object value) { ((JsReceiverFunction)value).EnableArguments(); return value; }
    internal static object ArgumentsReceiver(object receiver) => receiver is JsArgumentsReceiver arguments ? arguments.Receiver : receiver;
}
