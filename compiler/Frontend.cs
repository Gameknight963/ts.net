namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;
internal sealed class IrReader
{
    public Vector<byte> bytes;
    public int position;
    public static IrReader create(TextBytes data)
    {
        Vector<byte> bytes = new Vector<byte>();
        for (int i = 0; i < data.length; i++) { bytes.push(data[i]); }
        return new IrReader { bytes = identity(bytes), position = 0 };
    }
    public byte ReadByte()
    {
        if (this.position >= this.bytes.count()) { print("tsnetc: truncated frontend IR"); exit(1); }
        return this.bytes.at(this.position++);
    }
    public int integer()
    {
        uint value = 0;
        for (int i = 0; i < 4; i++) { value |= (uint)tsnet_unsigned_byte(this.ReadByte()) << (8 * i); }
        if (value > 2147483647) { print("tsnetc: invalid frontend IR integer"); exit(1); }
        return (int)value;
    }
    public int id() { return (int)this.integer(); }
    public void text(Compiler c)
    {
        int length = this.id(); int start = c.source.length();
        for (int i = 0; i < length; i++) { c.source.append(this.ReadByte()); }
        source_text(start, length, c);
    }
}
internal static partial class CompilerFunctions
{
    internal static void import_frontend(TextBytes path, Compiler c, bool experimentalDecorators = false)
    {
        IrReader r = IrReader.create(NativeFrontend.Read(path.Decode(), experimentalDecorators));
        if (r.integer() != 0x34524954) { print("tsnetc: unsupported frontend IR version"); exit(1); }
        int count = r.id();
        for (int i = 0; i < count; i++) { r.text(c); }
        count = r.id(); c.next_type = (int)count; c.types = count == 0 ? 0 : 1;
        for (int i = 1; i <= count; i++)
        {
            int kind = r.integer(); int name = r.id(); int fields = r.id(); int element = r.id();
            Type record = new Type { kind = (TypeKind)kind, name = name, id = (int)i - 1, fields = fields, element = element, defined = true, next = i == count ? 0 : i + 1 };
            c.type_store.add(record);
        }
        count = r.id();
        for (int i = 0; i < count; i++)
        {
            int name = r.id(); int type = r.id(); int next = r.id();
            bool optional = r.integer() != 0;
            Field record = new Field { name = name, type = type, next = next, optional = optional }; c.field_store.add(record);
        }
        count = r.id();
        for (int i = 0; i < count; i++)
        {
            int text = r.id(); int line = r.integer(); int column = r.integer();
            Token record = new Token { kind = TokenKind.Name, text = text, line = line, column = column, next = 0 }; c.token_store.add(record);
        }
        count = r.id();
        for (int i = 0; i < count; i++)
        {
            int id = new_node(c);
            set_node_kind(id, (NodeKind)r.integer(), c); set_node_token(id, r.id(), c); set_node_name(id, r.id(), c); set_node_type(id, r.id(), c);
            set_node_left(id, r.id(), c); set_node_right(id, r.id(), c); set_node_third(id, r.id(), c); set_node_fourth(id, r.id(), c);
            set_node_children(id, r.id(), c); set_node_next(id, r.id(), c); set_node_symbol(id, r.id(), c); set_node_constant(id, r.integer() != 0, c);
        }
        count = r.id(); c.next_symbol = (int)count;
        for (int i = 0; i < count; i++)
        {
            int name = r.id(); int type = r.id(); int function = r.id();
            Symbol record = new Symbol { name = name, type = type, function = function, id = (int)i, scope = 0, constant = false, next = 0 }; c.symbol_store.add(record);
        }
        c.functions = r.id(); c.statements = r.id();
        if (r.position != r.bytes.count()) { print("tsnetc: trailing frontend IR data"); exit(1); }
    }
}
