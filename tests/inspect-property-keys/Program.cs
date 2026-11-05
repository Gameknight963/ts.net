using System.Text;
using var output = new StreamWriter(args[0], false, new UTF8Encoding(false));
ulong state = 0x49583634abcdef01;
for (int i = 0; i < 20000; i++) {
    state ^= state << 13;state ^= state >> 7;state ^= state << 17;
    double value = BitConverter.Int64BitsToDouble(unchecked((long)state));
    output.WriteLine(state.ToString("X16") + "\t" + Js.PropertyKey(value));
}
Console.WriteLine("PASS generated numeric property keys for comparison with Node");
