using System.Reflection;
using System.Runtime.Loader;

string path = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) => name.Name == "Tsnet.Runtime"
    ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(path)!, "Tsnet.Runtime.dll")) : null;
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
var program = assembly.GetType("Program")!;
var output = Console.Out;
try {
    Console.SetOut(TextWriter.Null);
    if (program.GetMethod("Main")!.CreateDelegate<Func<int>>()() != 0) throw new Exception("Fixture initialization failed");
} finally { Console.SetOut(output); }
var methods = program.GetMethods(BindingFlags.Public | BindingFlags.Static);
var create = methods.Single(method => method.Name.StartsWith("createCallbackInput_f", StringComparison.Ordinal) && method.ReturnType == typeof(object))
    .CreateDelegate<Func<double?, object>>();
object small = create(64), large = create(100000);
foreach (string name in new[] { "predicateKernel", "reduceKernel" }) {
    var run = methods.Single(method => method.Name.StartsWith(name + "_f", StringComparison.Ordinal) && method.ReturnType == typeof(double?))
        .CreateDelegate<Func<object, double?>>();
    for (int i = 0; i < 100; i++) { run(small); run(large); }
    foreach (var sample in new[] { (Value: small, Length: 64), (Value: large, Length: 100000) }) {
        double expected = name == "predicateKernel" ? 6 : (double)sample.Length * (sample.Length - 1) / 2;
        long before = GC.GetAllocatedBytesForCurrentThread();
        double? actual = run(sample.Value);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        if (actual != expected || bytes > 1024)
            throw new Exception($"{name}: expected {expected} and at most 1024 bytes, got {actual} and {bytes} bytes for {sample.Length} elements");
        Console.WriteLine($"PASS {name}: {bytes} bytes for {sample.Length} elements");
    }
}
