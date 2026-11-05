using System.Reflection;
_ = typeof(Js).Assembly;
var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
var methods = assembly.GetType("Program")!.GetMethods().Where(m => m.ReturnType == typeof(double?) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(double?)).ToArray();
if (methods.Length != 2) throw new Exception("Expected fresh and reassigned array kernels");
foreach (var method in methods) {
    var kernel = method.CreateDelegate<Func<double?, double?>>();
    for(int i=0;i<100;i++) kernel(10);
    long before = GC.GetAllocatedBytesForCurrentThread();
    var result = kernel(1000);
    long allocated = GC.GetAllocatedBytesForCurrentThread()-before;
    if (result != 1000 && result != 499500) throw new Exception($"Push result changed: {result}");
    if (allocated > 1048576) throw new Exception($"Fresh arrays reserved the whole loop: {allocated} bytes");
}
Console.WriteLine("PASS per-iteration arrays keep bounded allocation");
