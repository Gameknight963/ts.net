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
    Type storage = scalar;
    if (!types.Where(t => t.Name.StartsWith('O')).SelectMany(t => t.GetFields()).Any(f => f.FieldType == storage))
        throw new Exception($"Missing object field storage: {storage}");
    if (!methods.Any(m => m.ReturnType == scalar)) throw new Exception($"No nullable return: {scalar}");
    if (!methods.Any(m => m.GetParameters().Any(p => p.ParameterType == scalar))) throw new Exception($"No nullable parameter: {scalar}");
    if (!methods.Any(m => m.GetMethodBody()?.LocalVariables.Any(v => v.LocalType == scalar) == true)) throw new Exception($"No nullable local: {scalar}");
}
Console.WriteLine("PASS nullable scalar fields, parameters, returns, and locals");

// Exercise the actual fixed CLR layout. Calling preventExtensions in the
// TypeScript fixture would let the frontend choose dictionary storage instead.
var fixedType = types.First(t => t.Name.StartsWith('O') && t.BaseType?.Name == "TsObject" && t.GetField("p_6e") != null);
var record = Activator.CreateInstance(fixedType)!;
var runtime = fixedType.BaseType!.Assembly;
var js = runtime.GetType("Js")!;
object Call(string ns, string name, params object[] arguments)
{
    object owner = js.GetMethod("BuiltinNamespace")!.Invoke(null, new object[] { ns })!;
    object method = js.GetMethod("Get", new[] { typeof(object), typeof(string) })!.Invoke(null, new[] { owner, name })!;
    return js.GetMethod("CallMethod")!.Invoke(null, new object[] { method, owner, arguments })!;
}
void Require(bool condition) { if (!condition) throw new Exception("Fixed-layout extensibility check failed"); }
var set = fixedType.GetMethod("Set", new[] { typeof(string), typeof(object) })!;
set.Invoke(record, new object[] { "n", 3d });
set.Invoke(record, new object[] { "present", 17d });
var prototype = Activator.CreateInstance(runtime.GetType("JsDynamicObject")!)!;
Require(Equals(Call("Reflect", "setPrototypeOf", record, prototype), true));
Require(Equals(Call("Object", "isExtensible", record), true));
Require(ReferenceEquals(Call("Object", "preventExtensions", record), record));
Require(Equals(Call("Object", "isExtensible", record), false));
Require(Equals(Call("Reflect", "preventExtensions", record), true));
Require(Equals(Call("Reflect", "set", record, "n", 7d), true));
Require(Equals(fixedType.GetMethod("Get")!.Invoke(record, new object[] { "n" }), 7d));
Require(Equals(fixedType.GetMethod("Get")!.Invoke(record, new object[] { "present" }), 17d));
Require(Equals(Call("Reflect", "set", record, "added", 11d), false));
try { set.Invoke(record, new object[] { "added", 13d }); throw new Exception("Added a property to a non-extensible record"); }
catch (TargetInvocationException error) when (error.InnerException is InvalidOperationException) { }
Require(Equals(Call("Reflect", "setPrototypeOf", record, null!), false));
Require(Equals(Call("Reflect", "setPrototypeOf", record, prototype), true));
Require(Equals(Call("Object", "isSealed", record), false));
Require(Equals(Call("Object", "isFrozen", record), false));
Console.WriteLine("PASS fixed-layout objects reject extensions while existing fields remain writable");
