using System;
using System.Runtime.CompilerServices;

internal sealed class JsConstructorFrame(JsClassFunction home, object allocated)
{
    internal readonly JsClassFunction Home = home;
    internal readonly object Allocated = allocated;
    internal object Value;
    internal bool Initialized;
    internal int Calls;
    internal JsFunction AfterSuper;
}
internal sealed class JsSuperReference(JsClassFunction home, object receiver, bool isStatic) : TsObject
{
    internal readonly object Receiver = Js.ResolveThis(receiver);
    TsObject Target => isStatic ? home.Parent : home.Prototype.ParentPrototype;
    public override object Get(string key) => Target is null ? throw new InvalidOperationException("Cannot read super property of null") : Target.GetWithReceiver(key,Receiver);
    public override object Set(string key, object value) => Target is null ? throw new InvalidOperationException("Cannot set super property of null") : Target.SetWithReceiver(key,value,Receiver);
}
public static partial class Js
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object ResolveThis(object receiver)
    {
        if (receiver is not JsConstructorFrame frame) return receiver;
        return frame.Initialized ? frame.Value : throw MakeThrown(new JsCaughtError("ReferenceError", "Must call super constructor before accessing this"));
    }
    internal static object SuperReceiver(object receiver) => receiver is JsSuperReference reference ? reference.Receiver : receiver;
    public static object SuperBase(object home, object receiver, bool isStatic) => new JsSuperReference((JsClassFunction)home, receiver, isStatic);
    public static void SuperInitializer(object receiver, object callback) => ((JsConstructorFrame)receiver).AfterSuper = (JsFunction)callback;
    public static object SuperCall(object receiver, object arguments)
    {
        var frame = (JsConstructorFrame)receiver;
        if (frame.Home.Parent is null) throw new InvalidOperationException("Super constructor is not callable");
        int length = checked((int)Iteration.Length(arguments)); var values = new object[length];
        for (int i = 0; i < length; i++) values[i] = Iteration.Value(arguments, i);
        object allocated = frame.Calls++ == 0 ? frame.Allocated : ((JsClassInstance)frame.Allocated).Owner.Allocate();
        object result = frame.Home.Parent.InitializeDerived(allocated, values);
        if (frame.Initialized) throw MakeThrown(new JsCaughtError("ReferenceError", "Super constructor may only be called once"));
        frame.Value = result; frame.Initialized = true;
        frame.Home.InitializeFields(result);
        frame.AfterSuper?.Invoke(Array.Empty<object>());
        return result;
    }
}
