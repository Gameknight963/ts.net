using System;
using System.Globalization;

// Strict-mode arguments do not alias parameter bindings. The object is created
// on its first read, and retained by the receiver captured by nested arrows.
internal sealed class JsArgumentsReceiver(object receiver, object[] values)
{
    internal readonly object Receiver = receiver;
    JsArguments arguments;
    internal JsArguments Arguments => arguments ??= new JsArguments(values);
}

internal sealed class JsArguments : JsDynamicObject
{
    readonly object[] values;
    object assignedLength;
    bool lengthAssigned;
    internal JsArguments(object[] source)
    {
        // The generated adapter consumes its hidden receiver and parameters
        // before running user code. Keep an independent snapshot of user values.
        values = new object[source.Length - 1];
        Array.Copy(source, 1, values, 0, values.Length);
    }
    static readonly JsFunction CalleeThrower = new((FunctionBody)(static _ => throw CalleeError()), null, "", 0);
    internal override object LookupOwnAccessor(string key, bool setter) => key == "callee" ? CalleeThrower : base.LookupOwnAccessor(key, setter);
    internal override object OwnPropertyDescriptor(string key) => key == "callee"
        ? Js.AccessorDescriptor(CalleeThrower, CalleeThrower, false, false) : base.OwnPropertyDescriptor(key);
    static Exception CalleeError() => new InvalidOperationException("Strict arguments.callee cannot be accessed");
    internal override bool IsReadOnlyOwnProperty(string key) => key == "callee" || base.IsReadOnlyOwnProperty(key);
    public override bool HasOwn(string key) => key is "length" or "callee" || JsArray.Index(key, out uint index) && index < values.Length || base.HasOwn(key);
    public override object Get(string key)
    {
        if (key == "callee") throw CalleeError();
        if (key == "length") return lengthAssigned ? assignedLength : (double)values.Length;
        return JsArray.Index(key, out uint index) && index < values.Length ? values[index] : base.Get(key);
    }
    public override object SetOwnValue(string key, object value) => key is "length" or "callee" || JsArray.Index(key, out uint index) && index < values.Length ? Set(key, value) : base.SetOwnValue(key, value);
    public override object Set(string key, object value)
    {
        if (key == "callee") throw CalleeError();
        if (key == "length") { assignedLength = value; lengthAssigned = true; return value; }
        if (JsArray.Index(key, out uint index) && index < values.Length) { values[index] = value; return value; }
        return base.Set(key, value);
    }
    public override object SetWithReceiver(string key, object value, object receiver) => key == "callee" ? throw CalleeError() : base.SetWithReceiver(key, value, receiver);
    internal override object OwnPropertyNames() => Js.MergeOwnNames(this, OwnKeys(), "length", "callee");
    internal override object ForInOwnKeys() => Js.ForInNames(OwnKeys(), "length", "callee");
    internal override bool ForInEnumerable(string key) => key is not ("length" or "callee");
    public override double OwnKeyCount() => values.Length + ExtraKeyCount;
    public override object OwnKeys()
    {
        var keys = new object[values.Length];
        for (int i = 0; i < keys.Length; i++) keys[i] = i.ToString(CultureInfo.InvariantCulture);
        return KeysFromFixed(keys);
    }
    internal double IterationLength()
    {
        double length = lengthAssigned ? Js.Number(assignedLength) : values.Length;
        return double.IsNaN(length) || length <= 0 ? 0 : Math.Min(9007199254740991d, Math.Floor(length));
    }
}

public static partial class Js
{
    public static object Arguments(object receiver) => ((JsArgumentsReceiver)receiver).Arguments;
    public static object EnableArguments(object value) { ((JsReceiverFunction)value).EnableArguments(); return value; }
    internal static object ArgumentsReceiver(object receiver) => receiver is JsArgumentsReceiver arguments ? arguments.Receiver : receiver;
}
