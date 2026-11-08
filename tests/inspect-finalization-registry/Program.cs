using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

static class Program
{
    static object Call(TsObject registry, string name, params object[] args) => Js.CallMethod(registry.Get(name), registry, args);
    static TsObject Create(JsFunction callback) => (TsObject)((JsFunction)Js.BuiltinNamespace("FinalizationRegistry")).Construct(new object[]{callback});
    static void Check(bool condition, string message) {if(!condition) throw new Exception(message);}
    static void Collect() {for(int i=0;i<3;i++){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();}}
    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference Target, WeakReference Held) Register(TsObject registry, object token = null, bool symbol = false)
    {
        object target = symbol ? ((JsFunction)Js.BuiltinNamespace("Symbol")).InvokeWithReceiver(Js.Undefined, new object[]{"target"}) : new JsDynamicObject(); var held = new JsDynamicObject();
        Call(registry, "register", target, held, token ?? Js.Undefined);
        return (new WeakReference(target), new WeakReference(held));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference RegisterToken(TsObject registry, object target)
    {
        var token = new JsDynamicObject(); Call(registry, "register", target, 1d, token); return new WeakReference(token);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference Registry, WeakReference Held) Abandon(object target)
    {
        var registry = Create(new JsFunction((FunctionBody)(static _ => Js.Undefined), null, "cleanup", 1));
        var held = new JsDynamicObject(); Call(registry, "register", target, held);
        return (new WeakReference(registry), new WeakReference(held));
    }
    static void Main()
    {
        try {
            int thread = Environment.CurrentManagedThreadId, delivered = 0;
            var order = new List<string>();
            var registry = Create(new JsFunction((FunctionBody)(args => {
                Check(Environment.CurrentManagedThreadId == thread, "Cleanup ran on a different thread");
                Check(args.Length == 1 && args[0] is TsObject, "Cleanup held value changed");
                delivered++; order.Add("cleanup"); return Js.Undefined;
            }), null, "cleanup", 1));
            var witnesses = Register(registry); Collect();
            Check(!witnesses.Target.IsAlive, "Registry retained its target");
            Check(witnesses.Held.IsAlive, "Registry released its held value before cleanup");
            int status = Js.Run(() => {
                Check(delivered == 0, "Cleanup ran during an ordinary call");
                var microtask = new JsFunction((FunctionBody)(_ => {order.Add("microtask");return Js.Undefined;}), null, "job", 0);
                ((JsFunction)Js.BuiltinNamespace("queueMicrotask")).InvokeWithReceiver(Js.Undefined, new object[]{microtask});
            });
            Check(status == 0 && delivered == 1 && string.Join(",", order) == "microtask,cleanup", "Cleanup did not follow microtasks");
            Collect(); Check(!witnesses.Held.IsAlive, "Cleanup retained the delivered held value");
            var symbol = Register(registry, symbol: true); Collect();
            Check(!symbol.Target.IsAlive, "Registry retained a symbol target");
            Check(Js.Run(() => {}) == 0 && delivered == 2, "Collected symbol was not delivered");
            var token = new JsDynamicObject();
            var removedFirst = Register(registry, token); var removedSecond = Register(registry, token);
            Check((bool)Call(registry, "unregister", token), "Unregister did not remove matching registrations");
            Check(!(bool)Call(registry, "unregister", token), "Unregister removed a registration twice");
            Collect(); Check(!removedFirst.Held.IsAlive && !removedSecond.Held.IsAlive, "Unregister retained held values");
            Check(Js.Run(() => {}) == 0 && delivered == 2, "Unregistered cells were delivered");
            var liveTarget = new JsDynamicObject();
            var weakToken = RegisterToken(registry, liveTarget); Collect();
            Check(!weakToken.IsAlive, "Registry retained an unregister token");
            var abandoned = Abandon(liveTarget); Collect();
            Check(!abandoned.Registry.IsAlive && !abandoned.Held.IsAlive, "Unused registry or held value remained rooted");
            GC.KeepAlive(liveTarget); GC.KeepAlive(registry);
            Console.WriteLine("PASS finalization cleanup ordering, entry thread, held values, unregister groups, weak tokens, and registry lifetimes");
        } catch(Exception error) {Console.Error.WriteLine(error);Environment.ExitCode=1;}
    }
}
