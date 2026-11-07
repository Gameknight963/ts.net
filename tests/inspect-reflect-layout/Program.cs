using System.Reflection;
using System.Runtime.Loader;

string path=Path.GetFullPath(args[0]);
var context=new AssemblyLoadContext("reflect-layout",true);
context.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(path)!,"Tsnet.Runtime.dll"));
var assembly=context.LoadFromAssemblyPath(path);
var kernel=assembly.GetType("Program")!.GetMethods(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)
    .Single(method=>method.Name.StartsWith("work_f",StringComparison.Ordinal)&&method.GetParameters().Length==0&&method.ReturnType==typeof(double?));
for(int i=0;i<5;i++)kernel.Invoke(null,null);
long before=GC.GetAllocatedBytesForCurrentThread();
object? result=kernel.Invoke(null,null);
long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
if(result is not double value||value!=10000000000d)throw new Exception("Object loop result changed");
if(allocated>1024)throw new Exception($"Read-only Reflect alias caused object-loop allocations: {allocated} bytes");
Console.WriteLine($"PASS read-only Reflect alias: 100000 object-loop iterations, {allocated} bytes including reflection call");
