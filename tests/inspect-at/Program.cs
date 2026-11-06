using System.Reflection;
var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
var method = assembly.GetType("Program")!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
    .Single(m => m.ReturnType == typeof(double?) && m.GetParameters().Length == 2);
var kernel = method.CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 200; i++) kernel(10000, 0);
long start = GC.GetAllocatedBytesForCurrentThread();
var result = kernel(1000000, 0);
long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
if (result != 499999500000.0 || bytes >= 1024)
    throw new Exception($"at loop result/allocation mismatch: {result}, {bytes} bytes");
for (int code = 0; code <= char.MaxValue; code++) {
    string text = new((char)code, 1);
    object value = Js.CallAtIndex(Js.StringAtFunction(), text, 0);
    if (value is not string actual || actual != text) throw new Exception("UTF-16 code unit mismatch");
}
Console.WriteLine($"PASS at UTF-16 indexing and numeric loop allocations ({bytes} bytes)");
