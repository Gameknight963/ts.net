using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

static class Program
{
    static object Deref(TsObject reference) => Js.CallMethod(reference.Get("deref"), reference, Array.Empty<object>());
    [MethodImpl(MethodImplOptions.NoInlining)]
    static (TsObject Reference, WeakReference Witness) Create()
    {
        var target = new JsDynamicObject();
        return ((TsObject)((JsFunction)Js.BuiltinNamespace("WeakRef")).Construct(new object[] { target }), new WeakReference(target));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static (TsObject Reference, WeakReference Witness, List<object> Holder) CreateHeld()
    {
        var pair = Create(); return (pair.Reference, pair.Witness, new List<object> { pair.Witness.Target });
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RetainAndDrop(TsObject reference, List<object> holder) { Deref(reference); holder.Clear(); }
    static void Collect() { for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); } }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Main()
    {
        (TsObject Reference, WeakReference Witness) created = default;
        Check(Js.Run(() => { created = Create(); Collect(); Check(created.Witness.IsAlive, "Construction must retain its target for the current job"); }) == 0, "Construction job failed");
        Collect(); Check(!created.Witness.IsAlive, "WeakRef retained its target after the job");
        Check(Js.IsUndefined(Deref(created.Reference)), "Collected targets must return undefined");
        var live = CreateHeld();
        Check(Js.Run(() => { }) == 0, "Initial job failed");
        Check(Js.Run(() => { RetainAndDrop(live.Reference, live.Holder); Collect(); Check(live.Witness.IsAlive, "deref must retain its result for the current job"); }) == 0, "deref job failed");
        Collect(); Check(!live.Witness.IsAlive, "deref retained its target after the job");
        Check(Js.IsUndefined(Deref(live.Reference)), "Collected deref target must return undefined");
        GC.KeepAlive(created.Reference); GC.KeepAlive(live.Reference);
        Console.WriteLine("PASS WeakRef targets survive their job and are collectible afterward");
    }
}
