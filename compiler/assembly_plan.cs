namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed class AssemblyPlan
{
    public Vector<byte> bytes;
    public int next;
    public static AssemblyPlan create()
    {
        AssemblyPlan result = new AssemblyPlan { bytes = new Vector<byte>(), next = 1 };
        result.integer(0x314c4954);
        result.text("App");
        return identity(result);
    }
    public void integer(int value)
    {
        uint bits = tsnet_int_bits(value);
        for (int i = 0; i < 4; i++) { this.bytes.push(tsnet_low_byte(bits >> (i * 8))); }
    }
    public void raw_text(TextBytes value)
    {
        this.integer((int)value.length);
        for (int i = 0; i < value.length; i++) { this.bytes.push(value[i]); }
    }
    public void text(TextBytes value)
    {
        int length = value.length;
        if (length != 0 && value[length - 1] == 0) { length--; }
        this.integer((int)length);
        for (int i = 0; i < length; i++) { this.bytes.push(value[i]); }
    }
    public int command(int code)
    {
        int id = this.next++;
        this.integer(code); this.integer(id);
        return id;
    }
    public int reference(TextBytes assembly, TextBytes space, TextBytes name)
    {
        int id = this.command(1);
        this.text(assembly); this.text(space); this.text(name);
        return id;
    }
    public int type(TextBytes name, int parent, int attributes)
    {
        int id = this.command(2);
        this.integer(attributes); this.text(name); this.integer(parent);
        return id;
    }
    public void implements(int owner, int target) { this.integer(3); this.integer(owner); this.integer(target); }
    public int field(int owner, int attributes, int type, TextBytes name)
    {
        int id = this.command(4);
        this.integer(owner); this.integer(attributes); this.integer(type); this.text(name);
        return id;
    }
    public int method(int command, int owner, int attributes, int result, int[] parameters, TextBytes name)
    {
        int id = this.command(command);
        this.integer(owner); this.integer(attributes); this.integer(result);
        this.integer((int)parameters.Length);
        for (int i = 0; i < parameters.Length; i++) { this.integer(parameters[i]); }
        this.text(name);
        return id;
    }
    public int method0(int command, int owner, int attributes, int result, TextBytes name)
    {
        Vector<int> parameters = new Vector<int>();
        return this.method(command, owner, attributes, result, parameters.view(), name);
    }
    public int method1(int command, int owner, int attributes, int result, int parameter, TextBytes name)
    {
        int[] parameters = { parameter };
        return this.method(command, owner, attributes, result, parameters, name);
    }
    public int method2(int command, int owner, int attributes, int result, int first, int second, TextBytes name)
    {
        int[] parameters = { first, second };
        return this.method(command, owner, attributes, result, parameters, name);
    }
    public int specialize(int method, int type)
    {
        int id = this.command(12); this.integer(method); this.integer(type); return id;
    }
    public void local(int method, int type) { this.integer(7); this.integer(method); this.integer(type); }
    public void instruction(int method, int code, int operand)
    {
        this.integer(8); this.integer(method); this.integer(code); this.integer(operand);
    }
    public void number(int method, double value)
    {
        this.integer(9); this.integer(method);
        ulong bits = tsnet_double_bits(value);
        for (int i = 0; i < 8; i++) { this.bytes.push(tsnet_low_byte(bits >> (i * 8))); }
    }
    public void StringLiteral(int method, TextBytes value) { this.integer(10); this.integer(method); this.text(value); }
    public void raw_string(int method, TextBytes value) { this.integer(10); this.integer(method); this.raw_text(value); }
    public void catch_region(int method, int start, int end, int handler, int handlerEnd, int type)
    {
        this.integer(13);this.integer(method);this.integer(start);this.integer(end);this.integer(handler);this.integer(handlerEnd);this.integer(type);
    }
    public void function_source(int method, int constructor, TextBytes source) { integer(14); integer(method); integer(constructor); raw_text(source); }
    public void source_point(int method, TextBytes file, int line, int column)
    {
        integer(15); integer(method); raw_text(file); integer(line); integer(column);
    }
    public void finish(int entry, TextBytes path)
    {
        this.integer(11); this.integer(entry);
        using var stream = new MemoryStream(this.bytes.view());
        var image = new global::AssemblyPlan(new BinaryReader(stream)).Build();
        var destination = path.Decode();
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temporary, image); File.Move(temporary, destination, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

internal static partial class CompilerFunctions
{
    internal static void name_number(int number, Vector<byte> result)
    {
        if (number >= 10) { name_number(number / 10, result); }
        result.push((byte)('0' + number % 10));
    }
    internal static Vector<byte> numbered_name(char prefix, int number)
    {
        Vector<byte> result = new Vector<byte>(); result.push(prefix); name_number(number, result); return identity(result);
    }
    internal static Vector<byte> field_name(int name, TextBytes prefix, Compiler c)
    {
        Vector<byte> result = new Vector<byte>();
        for (int i = 0; i + 1 < prefix.length; i++) { result.push(prefix[i]); }
        for (int i = 0; i < text_size(name, c); i++)
        {
            int value = tsnet_unsigned_byte(text_byte(name, i, c));
            int high = value / 16; int low = value % 16;
            result.push((byte)(high < 10 ? '0' + high : 'a' + high - 10));
            result.push((byte)(low < 10 ? '0' + low : 'a' + low - 10));
        }
        return identity(result);
    }
}
