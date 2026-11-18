using System.Reflection;

try {
_ = typeof(Js).Assembly;
var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
var original = Console.Out;
try { Console.SetOut(TextWriter.Null); assembly.EntryPoint!.Invoke(null, null); }
finally { Console.SetOut(original); }
var work = assembly.GetType("Program")!.GetMethods().Single(method => method.Name.StartsWith("work_f")).CreateDelegate<Func<double?, double?>>();
for (int i = 0; i < 30; i++) work(1000);
long before = GC.GetAllocatedBytesForCurrentThread();
var result = work(2000000);
long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
if (result != 5 || allocated > 8192) throw new Exception($"Class field writes changed: {result}, {allocated} bytes");
Console.WriteLine($"PASS two million reference field writes: {allocated} bytes including instance setup");
if (args.Length > 1)
{
    var methods = Assembly.LoadFile(Path.GetFullPath(args[1]));
    try {Console.SetOut(TextWriter.Null); methods.EntryPoint!.Invoke(null, null);}
    finally {Console.SetOut(original);}
    var kernel = methods.GetType("Program")!.GetMethods().Single(method => method.Name.StartsWith("work_f")).CreateDelegate<Func<double?, double?>>();
    for (int i = 0; i < 30; i++) kernel(1000);
    before = GC.GetAllocatedBytesForCurrentThread();
    result = kernel(2000000);
    allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    if (result != 1999999000000d || allocated > 8192)
        throw new Exception($"Class method arguments changed: {result}, {allocated} bytes");
    Console.WriteLine($"PASS two million calls each to numeric and string class methods: {allocated} bytes including instance setup");
    var zero = methods.GetType("Program")!.GetMethods().Single(method => method.Name.StartsWith("zeroWork_f")).CreateDelegate<Func<double?, double?>>();
    for (int i = 0; i < 30; i++) zero(1000);
    before = GC.GetAllocatedBytesForCurrentThread();
    result = zero(2000000);
    allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    if (result != 2000000d || allocated > 8192)
        throw new Exception($"Zero-argument class methods changed: {result}, {allocated} bytes");
    Console.WriteLine($"PASS two million calls each to void and string zero-argument methods: {allocated} bytes including instance setup");
    var native = new NativeMethod();
    var receiver = new JsDynamicObject();
    if (!ReferenceEquals(Js.CallMethod0(native, receiver), receiver) || native.Calls != 1)
        throw new Exception("Native zero-argument receiver dispatch changed");
    Console.WriteLine("PASS native zero-argument receiver dispatch");
}
}
catch (Exception error) {Console.Error.WriteLine(error); Environment.ExitCode = 1;}

sealed class NativeMethod : JsFunction
{
    public int Calls;
    public NativeMethod() : base((FunctionBody)(_ => throw new Exception("Receiver was lost")), null, "native", 0) { }
    public override object InvokeWithReceiver(object receiver, object[] arguments)
    {
        if (arguments.Length != 0) throw new Exception("Unexpected method arguments");
        Calls++;
        return receiver;
    }
}
