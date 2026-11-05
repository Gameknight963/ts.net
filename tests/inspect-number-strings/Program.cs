int count = 0;
foreach (string line in File.ReadLines(args[0])) {
    int separator = line.IndexOf('\t');
    byte[] bytes = Convert.FromHexString(line[..separator]);
    char[] units = new char[bytes.Length / 2];
    for (int i=0; i<units.Length; i++) units[i] = (char)(bytes[i*2] | bytes[i*2+1] << 8);
    string text = new string(units);
    double actual = Js.Number(text);
    string bits = double.IsNaN(actual) ? "nan" : BitConverter.DoubleToUInt64Bits(actual).ToString("x16");
    if (bits != line[(separator + 1)..]) throw new Exception($"Numeric conversion differs for {line[..separator]}: {bits}");
    count++;
}
Console.WriteLine($"PASS {count} numeric strings, including radix rounding and every UTF-16 whitespace candidate");
