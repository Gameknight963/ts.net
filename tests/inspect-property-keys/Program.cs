using System.Text;
using var output = new StreamWriter(args[0], false, new UTF8Encoding(false));
ulong state = 0x49583634abcdef01;
double[] boundaries = {
    int.MinValue - 1d, int.MinValue - 0.5, int.MinValue, int.MinValue + 0.5,
    -300, -1, -0d, 0, 1, 9, 10, 99, 100, 255, 256, 299, 300,
    int.MaxValue - 0.5, int.MaxValue, int.MaxValue + 0.5, int.MaxValue + 1d,
    double.NaN, double.NegativeInfinity, double.PositiveInfinity
};
for (int i = 0; i < 20000; i++) {
    state ^= state << 13;state ^= state >> 7;state ^= state << 17;
    ulong bits = i < boundaries.Length ? unchecked((ulong)BitConverter.DoubleToInt64Bits(boundaries[i])) : state;
    double value = BitConverter.Int64BitsToDouble(unchecked((long)bits));
    output.WriteLine(bits.ToString("X16") + "\t" + Js.PropertyKey(value) + "\t" + Js.ToInt32(value));
}
Console.WriteLine("PASS generated numeric property keys for comparison with Node");
