using System.Text;

static string Decode(byte[] bytes)
{
    using var stream = new MemoryStream();
    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
    {
        writer.Write(bytes.Length);writer.Write(bytes);
    }
    stream.Position = 0;
    return AssemblyPlan.Text(new BinaryReader(stream));
}
foreach (string text in new[] { "", "ASCII", "雪🦀", "a\0b" })
    if (Decode(Encoding.UTF8.GetBytes(text)) != text) throw new Exception("Changed valid Unicode");
for (int code = 0xd800; code <= 0xdfff; code++)
{
    byte[] bytes = [0x61, (byte)(0xe0 | (code >> 12)), (byte)(0x80 | ((code >> 6) & 63)), (byte)(0x80 | (code & 63)), 0x62];
    if (Decode(bytes) != "a" + (char)code + "b") throw new Exception("Lost surrogate code unit");
}
byte[][] invalid = [[0x80], [0xc0, 0x80], [0xed, 0xa0], [0xed, 0xa0, 0x00], [0xf4, 0x90, 0x80, 0x80], [0x80, 0xed, 0xa0, 0x80], [0xed, 0xa0, 0x80, 0xff]];
foreach (byte[] bytes in invalid)
{
    try { Decode(bytes); }
    catch (DecoderFallbackException) { continue; }
    throw new Exception("Accepted malformed string bytes");
}
Console.WriteLine("PASS Unicode, every surrogate code unit, and malformed string rejection");
