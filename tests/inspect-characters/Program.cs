using System.Reflection;
var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
var method = assembly.GetType("Program")!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
    .Single(m => m.ReturnType == typeof(double?) && m.GetParameters().Length == 2);
var kernel = method.CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 200; i++) kernel(10000, 0);
long start = GC.GetAllocatedBytesForCurrentThread();
var result = kernel(1000000, 0);
long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
double expected = 0; for (int i = 0; i < 1000000; i++) expected += "abcdefghijklmnopqrstuvwxyz"[i % 26];
if (result != expected || bytes >= 1024) throw new Exception($"Character result/allocation mismatch: {result}, {bytes} bytes");
object code = Js.Get("", "charCodeAt"), point = Js.Get("", "codePointAt"), character = Js.Get("", "charAt");
for (int value = 0; value <= char.MaxValue; value++) {
    string text = new((char)value, 1);
    if (Js.CallMethodNumber1(code, text, 0) != value || Js.CallMethodNumber1(point, text, 0) != value ||
        Js.CallMethodIndex(character, text, 0) is not string actual || actual != text)
        throw new Exception("UTF-16 code unit mismatch");
}
for (int high = 0xD800; high <= 0xDBFF; high++) {
    foreach (char low in new[]{'\uDC00', '\uDD13', '\uDFFF'}) {
        string text = new(new[]{(char)high, low});
        if (Js.CallMethodNumber1(point, text, 0) != char.ConvertToUtf32((char)high, low) || Js.CallMethodNumber1(point, text, 1) != low)
            throw new Exception("Surrogate pair mismatch");
    }
}
Console.WriteLine($"PASS character code units, surrogate pairs, and loop allocations ({bytes} bytes)");
