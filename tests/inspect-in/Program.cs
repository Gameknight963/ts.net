using System.Reflection;

_ = typeof(Js).Assembly;
var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
var method = assembly.GetType("Program")!.GetMethods().Single(m =>
    m.ReturnType == typeof(double?) && m.GetParameters().Length == 2 &&
    m.GetParameters().All(p => p.ParameterType == typeof(double?)));
var kernel = method.CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 30; i++) kernel(1000, 0);
long before = GC.GetAllocatedBytesForCurrentThread();
var small = kernel(1, 0);
long smallBytes = GC.GetAllocatedBytesForCurrentThread() - before;
before = GC.GetAllocatedBytesForCurrentThread();
var large = kernel(100000, 0);
long largeBytes = GC.GetAllocatedBytesForCurrentThread() - before;
if (small != 2 || large != 175000 || smallBytes != largeBytes)
    throw new Exception($"Membership results or loop allocations changed: {small}, {large}, {smallBytes}, {largeBytes}");
Console.WriteLine($"PASS numeric array and string object membership: {large}, {largeBytes} bytes per invocation for both loop lengths");
