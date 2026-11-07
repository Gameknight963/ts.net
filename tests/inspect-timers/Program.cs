using System.Runtime.CompilerServices;
static class Program {
 static JsFunction Function(string name) => (JsFunction)Js.BuiltinNamespace(name);
 static object Schedule(string name, JsFunction callback, double delay, params object[] args) => Function(name).InvokeWithReceiver(Js.Undefined, new object[] {callback, delay}.Concat(args).ToArray());
 static void Cancel(object id) => Function("clearTimeout").InvokeWithReceiver(Js.Undefined, new[] {id});
 static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
 [MethodImpl(MethodImplOptions.NoInlining)]
 static (WeakReference Reference, object Id) MakeTimer() {
  var payload = new JsDynamicObject();
  var callback = new JsFunction((FunctionBody)(_ => { GC.KeepAlive(payload); return Js.Undefined; }), null, "", 0);
  return (new WeakReference(payload), Schedule("setTimeout", callback, 60000, payload));
 }
 static void Collect() { for(int i=0;i<3;i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); } }
 static void Main() {
  Check(Js.Run(() => {
   object other = Schedule("setTimeout", new JsFunction((FunctionBody)(_ => Js.Undefined), null, "", 0), 30000);
   var created = MakeTimer(); Cancel(created.Id); Collect();
   Check(!created.Reference.IsAlive, "Cancelled timer retained callback or arguments");
   Cancel(other);
  }) == 0, "Cancellation check failed");
  var errors = new StringWriter(); var previous = Console.Error;
  try {
   Console.SetError(errors);
   Check(Js.Run(() => {
    Schedule("setInterval", new JsFunction((FunctionBody)(_ => throw new InvalidOperationException("timer failure")), null, "", 0), 1);
   }) == 1, "Timer exception did not fail entry point");
  } finally { Console.SetError(previous); }
  Check(errors.ToString().Contains("timer failure"), "Missing timer error");
  Check(Js.Run(() => {}) == 0, "Timer survived failed entry point");
  Console.WriteLine("PASS cancelled timers release callbacks and arguments; failures clear pending timers");
 }
}
