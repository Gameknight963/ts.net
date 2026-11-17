using System.Reflection;

_ = typeof(Js).Assembly;
var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
var method = assembly.GetType("Program")!.GetMethods().Single(m =>
    m.ReturnType == typeof(double?) && m.GetParameters().Length == 2 &&
    m.GetParameters().All(p => p.ParameterType == typeof(double?)));
var kernel = method.CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 30; i++) kernel(1000, 0);
long before = GC.GetAllocatedBytesForCurrentThread();
var result = kernel(100000, 0);
long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
long limit = args.Length > 1 ? long.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 18410000;
if (result != 4999950000d || allocated > limit)
    throw new Exception($"Constructor result or allocation budget changed: {result}, {allocated}");
Console.WriteLine($"PASS 100000 typed constructor results and allocation budget: {allocated} bytes");
