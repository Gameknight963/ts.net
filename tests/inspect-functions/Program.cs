using System.Reflection;

_ = typeof(Js).Assembly;
var functions = Assembly.LoadFile(Path.GetFullPath(args[0]));
var types = functions.GetTypes().Where(t => t.BaseType == typeof(JsFunction)).ToArray();
foreach (int arity in new[] { 0, 1, 2 }) {
    if (!types.Any(t => t.IsSealed && t.GetMethod("InvokeNumber" + arity)!.DeclaringType == t))
        throw new Exception($"Missing generated numeric callback class for arity {arity}");
}
var assembly = Assembly.LoadFile(Path.GetFullPath(args[1]));
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
    throw new Exception($"Numeric callbacks changed results or allocated per call: {small}, {large}, {smallBytes}, {largeBytes}");
Console.WriteLine("PASS generated numeric callback overrides and no allocation per repeated call");
