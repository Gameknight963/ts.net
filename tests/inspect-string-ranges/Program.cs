using System.Reflection;
var assembly=Assembly.LoadFile(Path.GetFullPath(args[0]));
var methods=assembly.GetType("Program")!.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)
    .Where(m=>m.ReturnType==typeof(double?) && m.GetParameters().Length==2).OrderBy(m=>m.MetadataToken).ToArray();
if(methods.Length!=4)throw new Exception("Expected four string kernels");
for(int index=0;index<methods.Length;index++){
    var kernel=methods[index].CreateDelegate<Func<double?,double?,double?>>();
    for(int i=0;i<200;i++)kernel(10000,0);
    int count=index==2?10000:1000000;
    long start=GC.GetAllocatedBytesForCurrentThread();var result=kernel(count,0);long bytes=GC.GetAllocatedBytesForCurrentThread()-start;
    double expected=0;for(int i=0;i<count;i++)expected+=index==0?'a'+(i&15):index==1?16:index==2?'a'+(i&7):(i%2==0?'a'+((i&31)/2):0);
    if(result!=expected || bytes>(index==2?count*40+1024:0))throw new Exception($"Range {index}: result {result}, allocation {bytes}");
    Console.WriteLine($"PASS string range kernel {index}: {bytes} bytes for {count} calls");
}
