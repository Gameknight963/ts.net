namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    internal int bigint_literal_type;
    internal readonly Dictionary<int, int> bigint_literals = [];
}

internal static partial class CompilerFunctions
{
    internal static int il_bigint_literal_text(int entry, Compiler c)
    {
        if (entry == 0) return 0;
        Node call = get_node(entry, c);
        if (call.kind != NodeKind.Call || !call.constant || call.children == 0) return 0;
        Node target = get_node(call.left, c), literal = get_node(call.children, c);
        return target.kind == NodeKind.BuiltinNamespace && is_text(get_token(target.token, c).text, "BigInt", c) &&
            literal.kind == NodeKind.String && literal.next == 0 ? get_token(literal.token, c).text : 0;
    }
    internal static bool il_bigint_literal(int entry, Compiler c, IlBackend b)
    {
        int text = il_bigint_literal_text(entry, c);
        if (text == 0) return false;
        if (!b.bigint_literals.TryGetValue(text, out int cache))
        {
            if (b.bigint_literal_type == 0)
                b.bigint_literal_type = b.plan.type("BigIntLiterals", b.RuntimeToken(RuntimeMember.ObjectType), 0x181);
            cache = b.plan.field(b.bigint_literal_type, 0x13, -5, numbered_name('b', b.bigint_literals.Count).view());
            b.bigint_literals[text] = cache;
        }
        int ready = b.label();
        b.emit(0x7e, cache); b.op(0x25); b.emit(0x3a, ready); b.op(0x26);
        il_text(text, c, b); b.call(RuntimeMember.BigIntLiteral);
        b.op(0x25); b.emit(0x80, cache); b.mark(ready);
        return true;
    }
}
