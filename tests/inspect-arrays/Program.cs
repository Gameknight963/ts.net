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
