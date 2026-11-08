using System.Reflection;

try
{
    const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    var capture = typeof(Js).GetMethod("CaptureHostEvents", flags)!;
    object Inbox() => capture.Invoke(null, null)!;
    void Register(object inbox, Action action, bool referenced) => inbox.GetType().GetMethod("Register", flags)!.Invoke(inbox, new object[] { action, referenced });
    void Unregister(object inbox, Action action) => inbox.GetType().GetMethod("Unregister", flags)!.Invoke(inbox, new object[] { action });
    void Post(object inbox, Action action) => inbox.GetType().GetMethod("Post", flags)!.Invoke(inbox, new object[] { action });
    int entryThread = Environment.CurrentManagedThreadId, completed = 0;
    bool canceled = false, timeout = false;
    int status = Js.Run(() => {
        var inbox = Inbox(); Action cancel = () => canceled = true;
        Register(inbox, cancel, true);
        var constructor = (TsObject)Js.BuiltinNamespace("AbortSignal");
        object signal = Js.CallMethod(constructor.Get("timeout"), constructor, new object[] { 5d });
        Js.CallMethod(Js.Get(signal, "addEventListener"), signal, new object[] {
            "abort", new JsFunction((FunctionBody)(_ => { timeout = true; return Js.Undefined; }), null, "", 0)
        });
        _ = Task.Run(async () => {
            await Task.Delay(75);
            Post(inbox, () => {
                if (Environment.CurrentManagedThreadId != entryThread) throw new Exception("Host callback ran on a worker");
                completed++; Unregister(inbox, cancel);
            });
        });
    });
    if (status != 0 || completed != 1 || canceled || !timeout) throw new Exception("Referenced host work or unreferenced timeout did not run");
    Console.WriteLine("PASS referenced host work keeps the entry thread alive and runs abort timers");

    status = Js.Run(() => {
        var inbox = Inbox();
        for (int index = 0; index < 16; index++) {
            int job = index;
            Action cancel = () => throw new Exception("Completed host work was canceled: " + job);
            Register(inbox, cancel, true);
            int delay = index % 5;
            _ = Task.Run(async () => {
                await Task.Delay(delay);
                Post(inbox, () => { completed++; Unregister(inbox, cancel); });
            });
        }
    });
    if (status != 0 || completed != 17) throw new Exception("Host work reference count lost completions");
    Console.WriteLine("PASS concurrent host work completes before shutdown");

    canceled = false;
    status = Js.Run(() => Register(Inbox(), () => canceled = true, false));
    if (status != 0 || !canceled) throw new Exception("Unreferenced work kept the loop alive or escaped cleanup");
    Console.WriteLine("PASS unreferenced host work does not keep the loop alive");

    canceled = false;
    var previous = Console.Error;
    try {
        Console.SetError(new StringWriter());
        status = Js.Run(() => { Register(Inbox(), () => canceled = true, true); throw new Exception("entry failure"); });
    } finally { Console.SetError(previous); }
    if (status == 0 || !canceled) throw new Exception("Failed entry did not clean up referenced work");
    Console.WriteLine("PASS failed entry cancels pending host work");
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
