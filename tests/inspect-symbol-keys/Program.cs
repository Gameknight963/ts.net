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
        var factory = (JsFunction)Js.BuiltinNamespace("Symbol");
        string symbolKey = Js.PropertyKey(factory.InvokeWithReceiver(Js.Undefined, new object[] { "json" }));
        string textKey = new string(symbolKey.ToCharArray());
        var ordinary = new JsDynamicObject(); ordinary.DefineOwn(textKey, 1d);
        var symbolic = new JsDynamicObject(); symbolic.DefineOwn(symbolKey, 2d);
        var json = (TsObject)Js.BuiltinNamespace("JSON");
        object stringify = json.Get("stringify");
        string quoted = (string)Js.CallMethod(stringify, json, new object[] { textKey });
        string actual = (string)Js.CallMethod(stringify, json, new object[] { new JsArray(new object[] { ordinary, symbolic }) });
        if (actual != "[{" + quoted + ":1},{}]")
        {
            Console.Error.WriteLine("JSON key snapshots confused a string key with an equal-text symbol key");
            Environment.ExitCode = 1; return;
        }
        Console.WriteLine("PASS JSON key snapshots distinguish symbol identity from string contents");
    }
}
