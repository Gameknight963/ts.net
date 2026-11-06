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
if (small != 4 || large != 5000350000d || smallBytes != largeBytes)
    throw new Exception($"Optional numeric calls or reads changed results or allocate per iteration: {small}, {large}, {smallBytes}, {largeBytes}");
Console.WriteLine($"PASS optional numeric calls and property reads: {large}, {largeBytes} bytes per invocation for both loop lengths");
