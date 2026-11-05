using System.Reflection;

_ = typeof(Js).Assembly;
var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
var environments = assembly.GetTypes().Where(t => t.Name.StartsWith("E") && t.IsSealed).ToArray();
if (environments.Length == 0 || !environments.SelectMany(t => t.GetFields()).Any(f => f.FieldType == typeof(double?)))
    throw new Exception("Captured numbers must use nullable value fields");
var method = assembly.GetType("Program")!.GetMethods().Single(m =>
    m.ReturnType == typeof(double?) && m.GetParameters().Length == 2 &&
    m.GetParameters().All(p => p.ParameterType == typeof(double?)));
var kernel = method.CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 30; i++) kernel(1000, i);
long before = GC.GetAllocatedBytesForCurrentThread();
var small = kernel(1, 3);
long smallBytes = GC.GetAllocatedBytesForCurrentThread() - before;
before = GC.GetAllocatedBytesForCurrentThread();
var large = kernel(100000, 3);
long largeBytes = GC.GetAllocatedBytesForCurrentThread() - before;
if (small != 4 || large != 100003 || largeBytes != smallBytes)
    throw new Exception($"Captured callback changed results or allocated per call: {small}, {large}, {smallBytes}, {largeBytes}");
Console.WriteLine("PASS typed captured-number fields and no allocation per repeated callback");
