var sparse = new NumberArray(0);
sparse.SetNumber(4294967294, 17);
if (Iteration.Length(sparse) != uint.MaxValue || Iteration.Number(sparse, -2) != 17) throw new Exception("Lost unsigned iteration position");
var generic = new JsArray(0);
generic.Set("4294967294", 19.0);
if (!Equals(Iteration.Value(generic, -2), 19.0)) throw new Exception("Generic array lost unsigned position");
string text = "\ud83e\udd80x\ud800y\udc00";
string[] expected = ["\ud83e\udd80", "x", "\ud800", "y", "\udc00"];
int position = 0;
foreach (string value in expected)
{
    if (!Equals(Iteration.Value(text, position), value)) throw new Exception("Incorrect Unicode iteration");
    position += Iteration.Step(text, position);
}
if (position != text.Length) throw new Exception("Incorrect Unicode cursor");
text = "alphabet";
Iteration.Value(text, 0);
long before = GC.GetAllocatedBytesForCurrentThread();
int length = 0;
for (int i = 0; i < 1000; i++) length += ((string)Iteration.Value(text, i % text.Length)).Length;
long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
if (length != 1000 || allocated != 0) throw new Exception($"ASCII traversal allocated {allocated} bytes");
Console.WriteLine("PASS unsigned array positions, Unicode code points, and allocation-free ASCII characters");
