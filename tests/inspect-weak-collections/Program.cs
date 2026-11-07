using System;
using System.Runtime.CompilerServices;

static class Program
{
    static object Invoke(TsObject owner, string name, params object[] arguments) => Js.CallMethod(owner.Get(name), owner, arguments);
    static TsObject Create(string name) => (TsObject)((JsFunction)Js.BuiltinNamespace(name)).Construct(Array.Empty<object>());
    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference Key, WeakReference Value) InsertCycle(TsObject map)
    {
        var key = new JsDynamicObject(); var value = new JsDynamicObject(); value.DefineOwn("key", key);
        Invoke(map, "set", key, value); return (new(key), new(value));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference InsertSetKey(TsObject set)
    {
        var key = new JsDynamicObject(); Invoke(set, "add", key); return new(key);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference InsertValue(TsObject map, TsObject key)
    {
        var value = new JsDynamicObject(); Invoke(map, "set", key, value); return new(value);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference Key, WeakReference Value) InsertSymbolCycle(TsObject map)
    {
        var factory = (JsFunction)Js.BuiltinNamespace("Symbol");
        object key = factory.InvokeWithReceiver(Js.Undefined, new object[] { "weak" });
        var value = new JsDynamicObject(); value.DefineOwn("key", key);
        Invoke(map, "set", key, value); return (new(key), new(value));
    }
    static void Collect()
    {
        for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Main()
    {
        var map = Create("WeakMap"); var set = Create("WeakSet");
        var symbolCycle = InsertSymbolCycle(map);
        var cycle = InsertCycle(map); var setKey = InsertSetKey(set);
        var liveKey = new JsDynamicObject(); var liveValue = InsertValue(map, liveKey);
        var deletedKey = new JsDynamicObject(); var deletedValue = InsertValue(map, deletedKey);
        Invoke(map, "delete", deletedKey);
        Collect();
        Check(!symbolCycle.Key.IsAlive && !symbolCycle.Value.IsAlive, "WeakMap retained an unreachable symbol/value cycle");
        Check(!cycle.Key.IsAlive && !cycle.Value.IsAlive, "WeakMap retained an unreachable key/value cycle");
        Check(!setKey.IsAlive, "WeakSet retained an unreachable key");
        Check(liveValue.IsAlive && ReferenceEquals(Invoke(map, "get", liveKey), liveValue.Target), "WeakMap lost a value whose key is live");
        Check(!deletedValue.IsAlive, "WeakMap retained a deleted value");
        GC.KeepAlive(map); GC.KeepAlive(set); GC.KeepAlive(liveKey); GC.KeepAlive(deletedKey);
        Console.WriteLine("PASS weak collections release unreachable keys, cycles and deleted values; live keys retain values");
    }
}
