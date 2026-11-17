using System.Reflection;
using System.Runtime.Loader;

try
{
    string path = Path.GetFullPath(args[0]);
    AssemblyLoadContext.Default.Resolving += (_, name) => name.Name == "Tsnet.Runtime"
        ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(path)!, "Tsnet.Runtime.dll")) : null;
    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    var methods = assembly.GetType("Program")!.GetMethods(BindingFlags.Public | BindingFlags.Static);
    var kernel = methods.Single(method => method.Name.StartsWith("kernel_f", StringComparison.Ordinal));
    var escape = methods.Single(method => method.Name.StartsWith("escape_f", StringComparison.Ordinal));
    var output = Console.Out;
    try
    {
        Console.SetOut(TextWriter.Null);
        var entry = assembly.EntryPoint!;
        object? result = entry.Invoke(null, entry.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
        if (result is int code && code != 0) throw new Exception("Class fixture initialization failed");
    }
    finally { Console.SetOut(output); }
    var run = kernel.CreateDelegate<Func<double?, double?>>();
    for (int i = 0; i < 100; i++) run(1000);
    long before = GC.GetAllocatedBytesForCurrentThread();
    double? checksum = run(1000);
    long bytesAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
    double expected = args.Length > 1 ? double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 251000;
    if (bytesAllocated != 0 || checksum != expected) throw new Exception($"Local classes allocated {bytesAllocated} bytes or produced {checksum}");
    var create = escape.CreateDelegate<Func<object>>();
    for (int i = 0; i < 100; i++) create();
    var escaped = new object[16];
    before = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < escaped.Length; i++) escaped[i] = create();
    long escapingBytes = GC.GetAllocatedBytesForCurrentThread() - before;
    if (escaped.Any(value => value is null || value.GetType().IsValueType) ||
        escaped.Distinct(ReferenceEqualityComparer.Instance).Count() != escaped.Length)
        throw new Exception("Escaping classes lost their reference identity");
    var runtime = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(path)!, "Tsnet.Runtime.dll"));
    var js = runtime.GetType("Js")!;
    var get = js.GetMethod("Get", [typeof(object), typeof(string)])!;
    var instanceOf = js.GetMethod("InstanceOf", [typeof(object), typeof(object)])!;
    object constructor = get.Invoke(null, [escaped[0], "constructor"])!;
    foreach (object value in escaped)
        if (!(bool)instanceOf.Invoke(null, [value, constructor])!) throw new Exception("Escaping class lost its prototype");
    Console.WriteLine($"PASS local classes allocate zero bytes; escaped classes retain identity and prototypes ({escapingBytes} bytes)");

    return 0;
}
catch (Exception error) {Console.Error.WriteLine(error.Message);return 1;}
