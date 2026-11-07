using System;
using System.Runtime.CompilerServices;

static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static (TsObject Owner, WeakReference Symbol) Create()
    {
        var factory = (JsFunction)Js.BuiltinNamespace("Symbol");
        object symbol = factory.InvokeWithReceiver(Js.Undefined, new object[] { "temporary" });
        var owner = new JsDynamicObject();
        owner.DefineOwn(Js.PropertyKey(symbol), 42d);
        return (owner, new WeakReference(symbol));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CheckLive(TsObject owner, WeakReference witness)
    {
        if (!witness.IsAlive) throw new Exception("Object lost its symbol key");
        var constructor = (TsObject)Js.BuiltinNamespace("Object");
        var symbols = (JsArray)Js.CallMethod(constructor.Get("getOwnPropertySymbols"), constructor, new object[] { owner });
        if (symbols.Length() != 1 || !ReferenceEquals(symbols.ElementAt(0), witness.Target))
            throw new Exception("Reflection lost symbol identity");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Drop(ref TsObject owner) { owner = null; }
    static void Collect() { for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); } }
    static void Main()
    {
        var pair = Create();
        Collect(); CheckLive(pair.Owner, pair.Symbol);
        Drop(ref pair.Owner);
        Collect();
        if (pair.Symbol.IsAlive) throw new Exception("Symbol key table retained an unreachable symbol");
        Console.WriteLine("PASS objects retain symbol keys; unreachable symbol keys are collectible");
    }
}
