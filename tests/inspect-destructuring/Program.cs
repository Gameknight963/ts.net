var assembly = System.Reflection.Assembly.LoadFrom(Path.GetFullPath(args[0]));
var kernel = assembly.GetType("Program")!.GetMethod("f0")!.CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 20; i++) kernel(1000, 3);
long before = GC.GetAllocatedBytesForCurrentThread();
double? result = kernel(1000, 3);
long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
if (result != 1002500 || bytes != 0) throw new Exception($"Literal destructuring returned {result} and allocated {bytes} bytes");
Console.WriteLine("PASS literal destructuring allocates no arrays or numeric boxes");
