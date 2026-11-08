using System;
using System.Runtime.CompilerServices;

static class Program
{
    static readonly JsFunction Define = (JsFunction)((TsObject)Js.BuiltinNamespace("Object")).Get("defineProperty");
    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference<object> Replace(TsObject target, string key = "value")
    {
        object original = new();
        var attributes = new JsDynamicObject();
        attributes.DefineOwn("value", original);
        attributes.DefineOwn("writable", Js.BoxBoolean(true));
        attributes.DefineOwn("configurable", Js.BoxBoolean(true));
        Define.Invoke(new object[] {target, key, attributes});
        target.Set(key, new object());
        return new WeakReference<object>(original);
    }
    static void Main()
    {
        try {
            foreach (TsObject owner in new TsObject[] {new JsDynamicObject(), new JsClassInstance(), new JsArray(0)}) {
                var previous = Replace(owner);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                if (previous.TryGetTarget(out _)) throw new Exception("Descriptor retained overwritten value on " + owner.GetType().Name);
                if (Js.IsUndefined(owner.Get("value"))) throw new Exception("Replacement value disappeared");
                GC.KeepAlive(owner);
            }
            var array = new JsArray(0);
            var indexed = Replace(array, "0");
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            if (indexed.TryGetTarget(out _)) throw new Exception("Indexed descriptor retained overwritten value");
            GC.KeepAlive(array);
            Console.WriteLine("PASS descriptors release overwritten values while owners remain alive");
        } catch (Exception error) {Console.Error.WriteLine(error); Environment.ExitCode = 1;}
    }
}
