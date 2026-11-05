using System.Reflection;

// Use the same runtime assembly as the emitted program and warm its caches.
_ = typeof(Js).Assembly;
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
var method = assembly.GetType("Program")!.GetMethods().Single(m =>
    m.ReturnType == typeof(double?) && m.GetParameters().Length == 2 &&
    m.GetParameters().All(p => p.ParameterType == typeof(double?)));
var kernel = method.CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 30; i++) kernel(1000, i);
long before = GC.GetAllocatedBytesForCurrentThread();
var result = kernel(100000, 3);
long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
if (result != 100003 || allocated != 0)
    throw new Exception($"Stored-predicate loop changed its result or allocated: {result}, {allocated} bytes");
Console.WriteLine("PASS stored-predicate alias loop returns the expected result without allocation");
