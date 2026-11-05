using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

string path = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) => name.Name == "Tsnet.Runtime"
    ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(path)!, "Tsnet.Runtime.dll")) : null;
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
var methods = assembly.GetType("Program")!.GetMethods(BindingFlags.Public | BindingFlags.Static);
var opcodes = typeof(OpCodes).GetFields().Where(f => f.FieldType == typeof(OpCode))
    .Select(f => (OpCode)f.GetValue(null)!).ToDictionary(op => (ushort)op.Value);
bool AllocatesObject(MethodInfo method)
{
    var bytes = method.GetMethodBody()!.GetILAsByteArray()!;
    for (int i = 0; i < bytes.Length;)
    {
        ushort code = bytes[i++];
        if (code == 0xfe) code = (ushort)(0xfe00 | bytes[i++]);
        var op = opcodes[code];
        if (op == OpCodes.Newobj && method.Module.ResolveMethod(BitConverter.ToInt32(bytes, i))!.DeclaringType!.Name.StartsWith('O')) return true;
        i += op.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => throw new Exception("Unexpected switch table"),
            _ => 4
        };
    }
    return false;
}
var scalar = methods.Single(m => m.Name == "f0");
if (AllocatesObject(scalar)) throw new Exception("Local object was not eliminated");
if (!scalar.GetMethodBody()!.LocalVariables.Any(v => v.LocalType == typeof(int))) throw new Exception("Counted loop has no integer counter");
if (!methods.Any(m => m.ReturnType.IsInterface && AllocatesObject(m))) throw new Exception("Escaping object lost its class representation");
var kernel = scalar.CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 10; i++) kernel(1000, i);
long before = GC.GetAllocatedBytesForCurrentThread();
double? result = kernel(1000, 3);
long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
if (allocated != 0 || result != 503000) throw new Exception($"Expected no allocation and preserved arithmetic, got {allocated} bytes and {result}");
Console.WriteLine("PASS local objects allocate zero bytes; escaping objects retain class construction");
