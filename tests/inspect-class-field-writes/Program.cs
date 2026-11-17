using System.Reflection;

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
