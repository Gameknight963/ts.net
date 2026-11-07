using System.Reflection;
_ = typeof(Js).Assembly;
var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
var kernel = assembly.GetType("Program")!.GetMethods().Single(method => method.Name.EndsWith("_f0", StringComparison.Ordinal)).CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 100; i++) kernel(1000, i);
long before = GC.GetAllocatedBytesForCurrentThread();
var small = kernel(1, 3);
long smallBytes = GC.GetAllocatedBytesForCurrentThread() - before;
before = GC.GetAllocatedBytesForCurrentThread();
var large = kernel(100000, 3);
long largeBytes = GC.GetAllocatedBytesForCurrentThread() - before;
if (small != 3 || large != 10000200000 || smallBytes != largeBytes || largeBytes > 64)
    throw new Exception($"Spread arithmetic or allocation changed: {small}, {large}, {smallBytes}, {largeBytes}");
Console.WriteLine("PASS local spread copies allocate no objects per iteration");

foreach (long bits in new long[] {0x7ff8000000000001, 0x7ff8000000000002, 0x7ff0000000000001, unchecked((long)0xfff8000000000001)}) {
    double? value = Js.DecodeNumberField(Js.EncodeNumberField(BitConverter.Int64BitsToDouble(bits)));
    if (!value.HasValue || !double.IsNaN(value.Value)) throw new Exception("A numeric NaN became undefined");
}
if (Js.DecodeNumberField(Js.EncodeNumberField(null)).HasValue) throw new Exception("Undefined became a number");
Console.WriteLine("PASS numeric NaN payloads remain distinct from undefined in fields");
