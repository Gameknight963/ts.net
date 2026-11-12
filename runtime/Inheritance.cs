using System;
using System.Runtime.CompilerServices;

internal sealed class JsConstructionReceiver(object receiver, object target)
{
    internal readonly object Receiver = receiver;
    internal readonly object Target = target;
}

internal sealed class JsConstructorFrame(JsClassFunction home, object allocated, JsFunction newTarget)
{
    internal readonly JsClassFunction Home = home;
    internal readonly JsFunction NewTarget = newTarget;
    internal readonly object Allocated = allocated;
    internal object Value;
    internal bool Initialized;
    internal int Calls;
    internal JsFunction AfterSuper;
}
internal sealed class JsObjectMethodReceiver(TsObject home, object receiver)
{
    internal readonly TsObject Home = home;
    internal readonly object Receiver = receiver;
}
internal sealed class JsSuperReference(TsObject home, object receiver, bool isStatic) : TsObject
{
    internal readonly object Receiver = Js.ResolveThis(receiver);
    TsObject Target => home is JsClassFunction klass ? isStatic ? klass.Parent : klass.Prototype.ParentPrototype : home.PropertyPrototype;
    public override object Get(string key) => Target is null ? throw new InvalidOperationException("Cannot read super property of null") : Target.GetWithReceiver(key,Receiver);
    public override object Set(string key, object value) => Target is null ? throw new InvalidOperationException("Cannot set super property of null") : Target.SetWithReceiver(key,value,Receiver);
}
public static partial class Js
{
    // Hidden receiver wrappers are not TsObjects; ordinary objects need no unwrapping.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object ResolveThis(object receiver) => receiver is TsObject ? receiver : ResolveWrappedThis(receiver);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object ResolveWrappedThis(object receiver)
    {
        receiver = ArgumentsReceiver(receiver);
        if (receiver is JsObjectMethodReceiver method) return ResolveThis(method.Receiver);
        if (receiver is JsConstructionReceiver construction) return ResolveThis(construction.Receiver);
        if (receiver is not JsConstructorFrame frame) return receiver;
        return frame.Initialized ? frame.Value : throw MakeThrown(new JsCaughtError("ReferenceError", "Must call super constructor before accessing this"));
    }
    public static object NewTarget(object receiver)
    {
        receiver = ArgumentsReceiver(receiver);
        return receiver is JsConstructionReceiver construction ? construction.Target : receiver is JsConstructorFrame frame ? frame.NewTarget : Undefined;
    }
    public static object EnableNewTarget(object value) { ((JsFunction)value).UsesNewTarget = true; return value; }
    internal static object SuperReceiver(object receiver) => receiver is JsSuperReference reference ? reference.Receiver : receiver;
    public static object SuperBase(object home, object receiver, bool isStatic)
    {
        home = ArgumentsReceiver(home);
        return new JsSuperReference(home is JsObjectMethodReceiver method ? method.Home : (TsObject)home, receiver, isStatic);
    }
    public static object MarkObjectHome(object function, object home) { ((JsFunction)function).SetObjectHome((TsObject)home); return function; }
    public static void SuperInitializer(object receiver, object callback) => ((JsConstructorFrame)ArgumentsReceiver(receiver)).AfterSuper = (JsFunction)callback;
    public static object SuperCall(object receiver, object arguments)
    {
        var frame = (JsConstructorFrame)ArgumentsReceiver(receiver);
        if (frame.Home.Parent is null) throw new InvalidOperationException("Super constructor is not callable");
        int length = checked((int)Iteration.Length(arguments)); var values = new object[length];
        for (int i = 0; i < length; i++) values[i] = Iteration.Value(arguments, i);
        object allocated = frame.Calls++ == 0 ? frame.Allocated : ((JsClassInstance)frame.Allocated).Owner.Allocate();
        if (!ReferenceEquals(allocated, frame.Allocated)) ((TsObject)allocated).SetPrototype(((TsObject)frame.Allocated).PropertyPrototype);
        object result = frame.Home.Parent.InitializeDerived(allocated, values, frame.NewTarget);
        if (frame.Initialized) throw MakeThrown(new JsCaughtError("ReferenceError", "Super constructor may only be called once"));
        frame.Value = result; frame.Initialized = true;
        frame.Home.InitializeFields(result);
        frame.AfterSuper?.Invoke(Array.Empty<object>());
        return result;
    }
}
