using System.Reflection;
using System.Runtime.Loader;

string path = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) => name.Name == "Tsnet.Runtime"
    ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(path)!, "Tsnet.Runtime.dll")) : null;
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
var types = assembly.GetTypes();
var methods = types.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)).ToArray();
foreach (var method in methods)
{
    var body = method.GetMethodBody();
    if (body == null) continue;
    if (body.MaxStackSize == ushort.MaxValue) throw new Exception($"Uncomputed stack size: {method}");
    var code = body.GetILAsByteArray()!;
    // Tiny method headers encode an implicit maxstack of eight.
    if (code.Length == 7 && code[0] == 0x02 && code[1] == 0x7b && body.MaxStackSize is not (1 or 8))
        throw new Exception($"Inflated field getter stack: {method}");
}
Console.WriteLine("PASS computed method stack sizes and compact field getters");
foreach (Type scalar in new[] { typeof(double?), typeof(bool?) })
{
    if (!types.SelectMany(t => t.GetFields()).Any(f => f.FieldType == scalar)) throw new Exception($"No nullable field: {scalar}");
    if (!methods.Any(m => m.ReturnType == scalar)) throw new Exception($"No nullable return: {scalar}");
    if (!methods.Any(m => m.GetParameters().Any(p => p.ParameterType == scalar))) throw new Exception($"No nullable parameter: {scalar}");
    if (!methods.Any(m => m.GetMethodBody()?.LocalVariables.Any(v => v.LocalType == scalar) == true)) throw new Exception($"No nullable local: {scalar}");
}
Console.WriteLine("PASS nullable double/bool fields, parameters, returns, and locals in emitted metadata");
