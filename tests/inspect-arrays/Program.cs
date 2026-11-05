var array = new NumberArray(4);
long[] nanPatterns = [0x7ff8000000000000, 0x7ff8000000000001, 0x7ff0000000000001, unchecked((long)0xfff8000000000001UL)];
foreach (long bits in nanPatterns)
{
    double value = BitConverter.Int64BitsToDouble(bits);
    array.SetNumberAt(0, value);
    double? result = array.GetNumberAt(0);
    if (!result.HasValue || !double.IsNaN(result.Value)) throw new Exception("Numeric NaN became undefined");
}
if (array.GetNumberAt(1).HasValue || array.GetNumberAt(100).HasValue) throw new Exception("Array hole became a defined number");
array.SetNumberAt(0, -0.0);
if (BitConverter.DoubleToInt64Bits(array.GetNumberAt(0)!.Value) != long.MinValue) throw new Exception("Lost negative zero");
array.SetNumber(4294967294, 7);
if (array.Length() != uint.MaxValue || array.GetNumber(4294967294) != 7) throw new Exception("Sparse index failed");
array.SetLength(1);
if (array.GetNumber(4294967294).HasValue) throw new Exception("Shrinking retained sparse elements");
Console.WriteLine("PASS array NaN payloads, undefined, negative zero, and sparse truncation");
array.SetLength(0);
array.PushNumber(1);array.PushNumber(2);array.PushNumber(3);
if (array.TryPacked(3) == null || array.TryPacked(4) != null) throw new Exception("Invalid packed range");
array.ReservePushes(100);
if (array.Length() != 3 || array.TryPacked(3) == null) throw new Exception("Reserving changed array semantics");
array.SetNumberAt(1, null);
if (array.TryPacked(2) != null || array.TryPacked(1) == null) throw new Exception("Undefined did not invalidate the packed range");
array.SetLength(0);array.SetLength(3);
if (array.TryPacked(1) != null) throw new Exception("Regrowing exposed removed elements");
Console.WriteLine("PASS packed range invalidation and capacity reservation");
var large = new NumberArray(0);
for (int i = 0; i < 1048577; i++) large.PushNumber(i);
if (large.TryPacked(1048577) == null || large.GetNumberAt(1048576) != 1048576) throw new Exception("Sequential growth unexpectedly became sparse");
Console.WriteLine("PASS sequential dense growth beyond one million elements");
if (args.Length != 0)
{
    var assembly = System.Reflection.Assembly.LoadFrom(System.IO.Path.GetFullPath(args[0]));
    var method = assembly.GetType("Program")!.GetMethod("f0")!;
    if (!method.GetMethodBody()!.LocalVariables.Any(v => v.LocalType == typeof(double[]))) throw new Exception("Packed loop has no guarded buffer");
    var kernel = method.CreateDelegate<Func<object, double?, double?>>();
    var packed = new NumberArray(8);
    for (int i = 0; i < 8; i++) packed.SetNumberAt(i, i);
    for (int i = 0; i < 10; i++) kernel(packed, 8);
    long before = GC.GetAllocatedBytesForCurrentThread();
    var result = kernel(packed, 8);
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    if (allocated != 0 || !result.HasValue) throw new Exception("Packed traversal allocated or lost its result");
    var payloadMethod = assembly.GetType("Program")!.GetMethods().Single(m =>
        m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(double?) && m.GetParameters()[2].ParameterType == typeof(double?));
    var writePayload = payloadMethod.CreateDelegate<Action<object, double?, double?>>();
    writePayload(packed, 8, BitConverter.Int64BitsToDouble(0x7ff8000000000001));
    if (!packed.GetNumberAt(0).HasValue || !double.IsNaN(packed.GetNumberAt(0)!.Value)) throw new Exception("Packed write turned numeric NaN into undefined");
    Console.WriteLine("PASS guarded packed buffer in emitted IL and allocation-free traversal");
}
