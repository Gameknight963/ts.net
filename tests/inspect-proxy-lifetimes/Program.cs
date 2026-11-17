using System.Reflection;
using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static (object Proxy, object Revoke, WeakReference Target, WeakReference Handler) Create(Func<object, object, object> make, bool callable)
    {
        object target = callable ? new JsFunction((FunctionBody)(static _ => Js.Undefined), null, "", 0) : new JsDynamicObject();
        object handler = new JsDynamicObject();
        object pair = make(target, handler);
        return (Js.Get(pair, "proxy"), Js.Get(pair, "revoke"), new WeakReference(target), new WeakReference(handler));
    }

    static void Collect()
    {
        for (int i = 0; i < 4; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
    }

    static void Check(Func<object, object, object> make, bool callable)
    {
        var pair = Create(make, callable);
        Collect();
        if (!pair.Target.IsAlive || !pair.Handler.IsAlive) throw new Exception("Live proxy lost its target or handler");
        Js.CallMethod(pair.Revoke, Js.Undefined, Array.Empty<object>());
        Collect();
        if (pair.Target.IsAlive || pair.Handler.IsAlive) throw new Exception("Revoked proxy retains its target or handler");
        GC.KeepAlive(pair.Proxy);
        GC.KeepAlive(pair.Revoke);
    }

    static void Main(string[] args)
    {
        _ = typeof(Js).Assembly;
        var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
        var make = assembly.GetType("Program")!.GetMethods().Single(method => method.Name.StartsWith("make_f")).CreateDelegate<Func<object, object, object>>();
        Check(make, false);
        Check(make, true);
        Console.WriteLine("PASS live proxies retain targets and handlers; revoked object and function proxies release both");
    }
}
