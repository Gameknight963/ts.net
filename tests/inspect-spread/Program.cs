using System.Reflection;
_ = typeof(Js).Assembly;
var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
var kernel = assembly.GetType("Program")!.GetMethod("f0")!.CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 100; i++) kernel(1000, i);
long before = GC.GetAllocatedBytesForCurrentThread();
var small = kernel(1, 3);
long smallBytes = GC.GetAllocatedBytesForCurrentThread() - before;
before = GC.GetAllocatedBytesForCurrentThread();
var large = kernel(100000, 3);
long largeBytes = GC.GetAllocatedBytesForCurrentThread() - before;
if (small != 3 || large != 10000200000 || smallBytes != largeBytes || largeBytes > 64)
    throw new Exception($"Spread arithmetic or allocation changed: {small}, {large}, {smallBytes}, {largeBytes}");
Console.WriteLine("PASS local spread copies allocate no objects per iteration");
