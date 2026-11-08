using System.Reflection;
using System.Runtime.Loader;

try {
    string path=Path.GetFullPath(args[0]);
    AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(path)!,"Tsnet.Runtime.dll"));
    var assembly=AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    var methods=assembly.GetType("Program")!.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
    var make=methods.Single(method=>method.Name.StartsWith("make_f",StringComparison.Ordinal)).CreateDelegate<Func<object>>();
    var sum=methods.Single(method=>method.Name.StartsWith("sum_f",StringComparison.Ordinal)).CreateDelegate<Func<object,double?>>();
    object values=make();
    for(int run=0;run<30;run++)sum(values);
    long before=GC.GetAllocatedBytesForCurrentThread();
    double? result=sum(values);
    long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
    if(result!=52377600 || allocated!=0)throw new Exception($"Typed-array reads returned {result} and allocated {allocated} bytes");
    Console.WriteLine("PASS 102400 typed-array reads with matching result and zero allocations");
} catch(Exception error) { Console.Error.WriteLine(error); Environment.ExitCode=1; }
