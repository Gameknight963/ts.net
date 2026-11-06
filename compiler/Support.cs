using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
namespace Tsnet.Compiler;

internal readonly struct TextBytes(byte[] bytes)
{
    public int length => bytes.Length;
    public byte this[int index] => bytes[index];
    public string Decode() => Encoding.UTF8.GetString(bytes.AsSpan(0, bytes.Length > 0 && bytes[^1] == 0 ? bytes.Length - 1 : bytes.Length));
    public static implicit operator TextBytes(byte[] bytes) => new(bytes);
    public static implicit operator TextBytes(string text) => new(Encoding.UTF8.GetBytes(text + "\0"));
}
internal sealed class Vector<T>
{
    readonly List<T> items = [];
    public void push(T value) => items.Add(value);
    public void push(char value) => items.Add((T)(object)(byte)value);
    public int count() => items.Count;
    public T at(int index) => items[index];
    public ref T at_mut(int index) => ref CollectionsMarshal.AsSpan(items)[index];
    public void reserve(int count) => items.EnsureCapacity(count);
    public T[] view() => items.ToArray();
}
internal sealed class Store<T>
{
    readonly List<T> items = [];
    public int count() => items.Count;
    public int add(T value) { items.Add(value); return items.Count; }
    public T get(int id) => items[id - 1];
    public ref T at_mut(int id) => ref CollectionsMarshal.AsSpan(items)[id - 1];
    public void put(int id, T value) => items[id - 1] = value;
}
internal sealed class Source
{
    readonly List<byte> bytes = [];
    public static Source read(TextBytes path)
    {
        byte[] data = File.ReadAllBytes(path.Decode());
        if (data.Length > 4 * 1024 * 1024 || data.Contains((byte)0)) throw new CompilationException("NUL in source or input exceeds 4 MiB limit");
        Source source = new(); source.bytes.AddRange(data); return source;
    }
    public int length() => bytes.Count;
    public void append(byte value) => bytes.Add(value);
    public byte peek(int index) => index >= bytes.Count ? (byte)0 : bytes[index];
}
internal sealed class Output
{
    readonly Stream stream;
    internal Output(Stream stream) { this.stream = stream; }
    public static Output console() => new(Console.OpenStandardOutput());
    public void put(byte value) => stream.WriteByte(value);
    public void put(char value) => stream.WriteByte((byte)value);
    public void integer(int value) => stream.Write(Encoding.UTF8.GetBytes(value.ToString(CultureInfo.InvariantCulture)));
    public void bytes(TextBytes value) => stream.Write(Encoding.UTF8.GetBytes(value.Decode()));
}
internal sealed class CompilationException(string message) : Exception(message);
internal static partial class CompilerFunctions
{
    internal static T identity<T>(T value) => value;
    internal static int tsnet_unsigned_byte(byte value) => value;
    internal static uint tsnet_int_bits(int value) => unchecked((uint)value);
    internal static ulong tsnet_double_bits(double value) => BitConverter.DoubleToUInt64Bits(value);
    internal static byte tsnet_low_byte(ulong value) => unchecked((byte)value);
    internal static double native_number(TextBytes value) => double.Parse(value.Decode(), CultureInfo.InvariantCulture);
    internal static void print(string value) => Console.WriteLine(value);
    internal static void print(TextBytes value) => Console.WriteLine(value.Decode());
    internal static void exit(int code) => throw new CompilationException("Compilation failed");
}
