using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

try
{
    string path = Path.GetFullPath(args[0]);
    AssemblyLoadContext.Default.Resolving += (_, name) => name.Name == "Tsnet.Runtime"
        ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(path)!, "Tsnet.Runtime.dll")) : null;
    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    var methods = assembly.GetType("Program")!.GetMethods(BindingFlags.Public | BindingFlags.Static);
    var opcodes = typeof(OpCodes).GetFields().Where(f => f.FieldType == typeof(OpCode))
        .Select(f => (OpCode)f.GetValue(null)!).ToDictionary(op => (ushort)op.Value);
    bool Constructs(MethodInfo method)
    {
        var bytes = method.GetMethodBody()!.GetILAsByteArray()!;
        for (int position = 0; position < bytes.Length;)
        {
            ushort code = bytes[position++];
            if (code == 0xfe) code = (ushort)(0xfe00 | bytes[position++]);
            var opcode = opcodes[code];
            if (opcode == OpCodes.Call && method.Module.ResolveMethod(BitConverter.ToInt32(bytes, position)) is MethodInfo target &&
                target.DeclaringType!.Name == "Js" && target.Name is "Construct" or "ConstructArguments") return true;
            position += opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(bytes, position),
                _ => 4
            };
        }
        return false;
    }
    var kernel = methods.Single(method => method.Name.StartsWith("kernel_f", StringComparison.Ordinal));
    var escape = methods.Single(method => method.Name.StartsWith("escape_f", StringComparison.Ordinal));
    if (Constructs(kernel) || !Constructs(escape)) throw new Exception("Local class construction was not eliminated or escaping construction was lost");
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
    if (bytesAllocated != 0 || checksum != 251000) throw new Exception($"Local classes allocated {bytesAllocated} bytes or produced {checksum}");
    Console.WriteLine("PASS local classes allocate zero bytes; escaping classes retain construction");

    return 0;
}
catch (Exception error) {Console.Error.WriteLine(error.Message);return 1;}
