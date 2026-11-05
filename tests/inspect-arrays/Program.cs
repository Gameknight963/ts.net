var array = new NumberArray(4);
var shared = new object();
var adopted = new JsArray(new object[] { JsArray.HoleValue(), Js.Undefined, null!, shared });
if (adopted.HasElement(0) || !adopted.HasElement(1) || !adopted.HasElement(2) || adopted.ElementAt(2) != null || !ReferenceEquals(adopted.ElementAt(3), shared)) throw new Exception("Adopted literal changed holes, null, or aliases");
adopted.SetLength(1);adopted.SetLength(4);
if (adopted.HasElement(1) || adopted.HasElement(2) || adopted.HasElement(3)) throw new Exception("Adopted literal retained truncated elements");
adopted.SetAt(2, shared);
if (!ReferenceEquals(adopted.ElementAt(2), shared)) throw new Exception("Embedded storage lost a write");
var booleans = new object[1000];Js.BoxBoolean(true);Js.BoxBoolean(false);Js.BoxBoolean(null);
long booleanStart = GC.GetAllocatedBytesForCurrentThread();
for (int i = 0; i < booleans.Length; i++) booleans[i] = Js.BoxBoolean(i % 3 == 0 ? null : i % 3 == 1);
long booleanBytes = GC.GetAllocatedBytesForCurrentThread() - booleanStart;
if (booleanBytes != 0 || !ReferenceEquals(booleans[0], Js.Undefined) || !Equals(booleans[1], true) || !Equals(booleans[2], false)) throw new Exception("Boxing booleans allocated or changed values");
Console.WriteLine("PASS adopted literal holes, null, aliases, truncation, and allocation-free boxed booleans");
foreach (JsArray source in new JsArray[] { new JsArray(4), new NumberArray(4), new TaggedArray(4) })
{
    source.SetAt(1, Js.Undefined);source.SetAt(3, -0.0);
    JsArray sliced = source.Slice(0.0, Js.Undefined);
    if (sliced.Length() != 4 || sliced.HasElement(0) || !sliced.HasElement(1) || sliced.HasElement(2) || !sliced.HasElement(3)) throw new Exception("Slice changed element presence");
    if (BitConverter.DoubleToInt64Bits((double)sliced.ElementAt(3)) != long.MinValue) throw new Exception("Slice lost negative zero");
    source.SetAt(3, 9.0);
    if (!Equals(sliced.ElementAt(3), -0.0)) throw new Exception("Slice shared its backing buffer");
}
var sparseSlice = new NumberArray(0);
sparseSlice.SetNumber(4294967294, 7);
var sparseCopy = sparseSlice.Slice(0.0, Js.Undefined);
if (sparseCopy.Length() != uint.MaxValue || !Equals(sparseCopy.ElementAt(4294967294), 7.0) || sparseCopy.HasElement(1)) throw new Exception("Sparse slice materialized or lost distant elements");
sparseSlice.SetNumber(4294967294, 9);
if (!Equals(sparseCopy.ElementAt(4294967294), 7.0)) throw new Exception("Sparse slice shared its dictionary");
Console.WriteLine("PASS slice holes, undefined, negative zero, independent buffers, and sparse length limit");
foreach (JsArray reversed in new JsArray[] { new JsArray(0), new NumberArray(0), new ReversibleArray(0), new TaggedArray(0) })
{
    reversed.Set("0", 7.0);reversed.Set("2", Js.Undefined);reversed.Set("4294967294", 9.0);reversed.Set("named", shared);
    if (!ReferenceEquals(reversed.Reverse(), reversed) || reversed.Length() != uint.MaxValue) throw new Exception("Reverse changed identity or length");
    if (!Equals(reversed.ElementAt(0), 9.0) || !Equals(reversed.ElementAt(4294967294), 7.0) || reversed.HasElement(2) || !reversed.HasElement(4294967292) || !ReferenceEquals(reversed.ElementAt(4294967292), Js.Undefined)) throw new Exception("Sparse reverse changed holes or undefined");
    if (!ReferenceEquals(reversed.Get("named"), shared)) throw new Exception("Reverse moved a named property");
    if (Js.ArraySearch(reversed, 7.0, 0.0, false) != 4294967294) throw new Exception("Searching a reversed sparse array lost its order");
    reversed.Reverse();
    if (!Equals(reversed.ElementAt(0), 7.0) || !Equals(reversed.ElementAt(4294967294), 9.0) || !reversed.HasElement(2) || reversed.HasElement(4294967292)) throw new Exception("Second sparse reverse lost original elements");
}
var denseReverse = new NumberArray(8);
for (int i = 0; i < 8; i++) denseReverse.SetNumberAt(i, i);
for (int i = 0; i < 10; i++) denseReverse.Reverse();
long reverseStart = GC.GetAllocatedBytesForCurrentThread();
for (int i = 0; i < 100; i++) denseReverse.Reverse();
if (GC.GetAllocatedBytesForCurrentThread() != reverseStart || denseReverse.GetNumberAt(0) != 0 || denseReverse.TryPacked(8) == null) throw new Exception("Dense reverse allocated or invalidated a packed buffer");
Console.WriteLine("PASS sparse reverse at the array length limit, presence, identity, and allocation-free dense reverse");
long[] nanPatterns = [0x7ff8000000000000, 0x7ff8000000000001, 0x7ff8000000000002, 0x7ff0000000000001, unchecked((long)0xfff8000000000001UL)];
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
foreach (JsArray original in new JsArray[] { new JsArray(3), new NumberArray(3) })
{
    original.SetIndex(1.0, Js.Undefined);
    if (original.HasElement(0) || !original.HasElement(1)) throw new Exception("Holes and undefined share presence");
    JsArray clone = original is NumberArray ? new NumberArray(0) : new JsArray(0);
    clone.Spread(original, 0);
    if (clone.Length() != 3 || !clone.HasElement(0) || !clone.HasElement(1) || !clone.HasElement(2)) throw new Exception("Spread retained holes");
    clone.AppendHole();
    if (clone.HasElement(3)) throw new Exception("Literal elision created an element");
}
var packedSource = new NumberArray(0);
packedSource.PushNumber(-0.0);packedSource.PushNumber(double.NaN);
var packedCopy = new NumberArray(0);
packedCopy.Spread(packedSource, 1);
packedCopy.PushNumber(9);
packedSource.SetNumberAt(0, 5);
if (packedCopy.TryPacked(3) == null || BitConverter.DoubleToInt64Bits(packedCopy.GetNumberAt(0)!.Value) != long.MinValue || !double.IsNaN(packedCopy.GetNumberAt(1)!.Value)) throw new Exception("Packed spread changed values or shared storage");
Console.WriteLine("PASS spread presence, independent buffers, NaN, and negative zero");
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

var tagged = new TaggedArray(new object[] { JsArray.HoleValue(), Js.Undefined, null!, shared, -0.0, double.NaN });
if (tagged.HasElement(0) || !tagged.HasElement(1) || !tagged.HasElement(2)) throw new Exception("Tagged literal lost element presence");
var tags = tagged.TryTags(6) ?? throw new Exception("Tagged literal has no type buffer");
if (tags[0] != 0 || tags[1] == 1 || tags[4] != 1 || tags[5] != 1) throw new Exception("Tagged literal misclassified a value");
for (int i = 0; i < 100; i++) { tagged.SetNumberAt(4, i);tagged.SetReferenceAt(3, shared); }
long taggedStart = GC.GetAllocatedBytesForCurrentThread();
for (int i = 0; i < 1000; i++) { tagged.SetNumberAt(4, i);tagged.SetReferenceAt(3, shared); }
if (GC.GetAllocatedBytesForCurrentThread() != taggedStart || tags[4] != 1) throw new Exception("Tagged writes allocated");
tagged.SetNumberAt(4, -0.0);
if (BitConverter.DoubleToInt64Bits((double)tagged.ElementAt(4)) != long.MinValue || !double.IsNaN((double)tagged.ElementAt(5))) throw new Exception("Tagged materialization lost numeric payloads");
if (tagged.ElementAt(2) != null || !ReferenceEquals(tagged.ElementAt(3), shared) || !ReferenceEquals(tagged.ElementAt(1), Js.Undefined)) throw new Exception("Tagged materialization lost references");
if (tagged.TryTags(6) != null) throw new Exception("Materialized tags remained available");
tagged.SetNumberAt(4, 42);tagged.SetReferenceAt(3, null!);
if (!Equals(tagged.ElementAt(4), 42.0) || tagged.ElementAt(3) != null) throw new Exception("Materialized writes failed");
var truncatedTags = new TaggedArray(new object[] { 1.0, "text", 3.0 });
truncatedTags.SetLength(1);truncatedTags.SetLength(3);
if (truncatedTags.IsNumberAt(1) || truncatedTags.IsNumberAt(2) || truncatedTags.HasElement(2)) throw new Exception("Tagged truncation retained values");
Console.WriteLine("PASS tagged holes, payloads, allocation-free writes, materialization, and truncation");

if (args.Length > 1)
{
    var context = new System.Runtime.Loader.AssemblyLoadContext("tagged-selection", isCollectible: true);
    context.Resolving += (_, name) => name.Name == typeof(Js).Assembly.GetName().Name ? typeof(Js).Assembly : null;
    var emitted = context.LoadFromAssemblyPath(Path.GetFullPath(args[1])).GetType("Program")!;
    var arrays = emitted.GetMethods().Where(m => m.ReturnType == typeof(object) && m.GetParameters().Length == 0)
        .Select(m => m.Invoke(null, null)).OfType<JsArray>().ToArray();
    if (arrays.Length != 2 || arrays.Count(a => a is TaggedArray) != 1 || arrays.Count(a => a.GetType() == typeof(JsArray)) != 1)
        throw new Exception("Storage selection lost the tag-scan optimization or changed payload-only arrays");
    if (!emitted.GetMethods().Any(m => m.GetMethodBody()?.LocalVariables.Any(v => v.LocalType == typeof(byte[])) == true))
        throw new Exception("Tag scans have no guarded byte buffer in emitted IL");
    context.Unload();
    Console.WriteLine("PASS selective tagged storage and guarded byte-buffer traversal in emitted IL");
}
