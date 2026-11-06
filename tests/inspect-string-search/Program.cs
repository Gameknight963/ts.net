using System.Reflection;
var assembly=Assembly.LoadFile(Path.GetFullPath(args[0]));
var methods=assembly.GetType("Program")!.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)
    .Where(m=>m.ReturnType==typeof(double?) && m.GetParameters().Length==2).OrderBy(m=>m.MetadataToken).ToArray();
if(methods.Length!=3)throw new Exception("Expected three string search kernels");
const string text="alpha beta needle delta alpha beta needle delta";
for(int index=0;index<methods.Length;index++) {
    var kernel=methods[index].CreateDelegate<Func<double?,double?,double?>>();
    for(int i=0;i<200;i++)kernel(10000,0);
    long start=GC.GetAllocatedBytesForCurrentThread();var result=kernel(1000000,0);long bytes=GC.GetAllocatedBytesForCurrentThread()-start;
    double expected=0;
    for(int i=0;i<1000000;i++){
        string needle=(i&1)!=0?"needle":"missing";int position=i&31;
        int found=index==2?text[..Math.Min(text.Length,position+needle.Length)].LastIndexOf(needle,StringComparison.Ordinal):text.IndexOf(needle,position,StringComparison.Ordinal);
        expected+=index==1?(found>=0?1:0):found;
    }
    if(result!=expected || bytes!=0)throw new Exception($"Search {index}: result {result}, allocation {bytes}");
    Console.WriteLine($"PASS string search kernel {index}: zero allocations for one million calls");
}
