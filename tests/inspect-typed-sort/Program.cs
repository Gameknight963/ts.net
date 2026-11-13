using System.Reflection;
using System.Runtime.Loader;

try
{
    string path=Path.GetFullPath(args[0]);
    AssemblyLoadContext.Default.Resolving+=(_,name)=>AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(path)!,name.Name+".dll"));
    var assembly=AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    var methods=assembly.GetType("Program")!.GetMethods(BindingFlags.Public|BindingFlags.Static);
    var kernel=methods.Single(method=>method.Name.StartsWith("kernel_f",StringComparison.Ordinal)).CreateDelegate<Func<object,object>>();
    var make=methods.Single(method=>method.Name.StartsWith("make_f",StringComparison.Ordinal)).CreateDelegate<Func<double?,object>>();
    var checksum=methods.Single(method=>method.Name.StartsWith("checksum_f",StringComparison.Ordinal)).CreateDelegate<Func<object,double?>>();
    var output=Console.Out;
    try {
        Console.SetOut(TextWriter.Null);var entry=assembly.EntryPoint!;
        object? result=entry.Invoke(null,entry.GetParameters().Length==0?null:[Array.Empty<string>()]);
        if(result is int code && code!=0)throw new Exception("Fixture initialization failed");
    } finally {Console.SetOut(output);}
    for(int kind=0;kind<6;kind++)
    {
        object value=make(kind);
        for(int i=0;i<1000;i++)kernel(value);
        long before=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<100;i++)kernel(value);
        long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        if(checksum(value)!=(kind>=4?0:30))throw new Exception($"Incorrect sorted values for kind {kind}");
        if(kind==2 ? allocated==0 : allocated!=0)throw new Exception($"Unexpected allocation {allocated} for kind {kind}");
    }
    Console.WriteLine("PASS integer typed-array sorting without allocation, offset views, BigInt values, and shared-buffer fallback");
    return 0;
}
catch(Exception error){Console.Error.WriteLine(error.Message);return 1;}
