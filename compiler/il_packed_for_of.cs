namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    static int il_packed_for_of(int entry, int binding, int type, int iterable, int result, Compiler c, IlBackend b)
    {
        Node loop = get_node(entry, c), target = get_node(binding, c);
        if (get_type(type, c).kind != TypeKind.Number || target.kind is not (NodeKind.Name or NodeKind.Variable)) return 0;
        bool Local(int symbol) => symbol != 0 && b.global_fields.at(symbol) == 0 && b.capture_fields.at(symbol) == 0 &&
            get_type(get_symbol(symbol, c).type, c).kind is TypeKind.Number or TypeKind.Boolean;
        if (!Local(target.symbol)) return 0;
        int remaining = 128;
        bool Pure(int id)
        {
            if (id == 0) return true;
            if (--remaining < 0) return false;
            Node value = get_node(id, c);
            if (il_key_may_invoke(value, c) || il_coercion_may_invoke(value, c)) return false;
            switch (value.kind) {
                case NodeKind.Block:
                    for (int child = value.children; child != 0; child = get_node(child, c).next)
                        if (!Pure(child)) return false;
                    return true;
                case NodeKind.Expression: return Pure(value.left);
                case NodeKind.If: return Pure(value.left) && Pure(value.right) && Pure(value.third);
                case NodeKind.Variable: return Local(value.symbol) && Pure(value.left);
                case NodeKind.Name: return Local(value.symbol) && value.type != 0 &&
                    get_type(value.type, c).kind is TypeKind.Number or TypeKind.Boolean;
                case NodeKind.Number:
                case NodeKind.Boolean: return true;
                case NodeKind.Assign:
                case NodeKind.Update:
                    return value.left != 0 && get_node(value.left, c).kind == NodeKind.Name && Pure(value.left) && Pure(value.right);
                case NodeKind.Binary: return Pure(value.left) && Pure(value.right);
                case NodeKind.Conditional: return Pure(value.left) && Pure(value.right) && Pure(value.third);
                case NodeKind.Unary:
                    int operation = get_token(value.token, c).text;
                    return (is_text(operation, text_plus, c) || is_text(operation, text_minus, c) ||
                        is_text(operation, text_not, c)) && Pure(value.left);
                default: return false;
            }
        }
        if (!Pure(loop.fourth)) return 0;
        // Iterator lookup has already run. Only intrinsic, packed arrays pass
        // this guard; the accepted body cannot change their storage or length.
        int packed = b.local(-12), limit = b.local(-6), position = b.local(-6), valueSlot = b.local(b.type(type, c));
        int fallback = b.label(), done = b.label(), start = b.label(), stop = b.label();
        b.emit(0xfe0c, iterable); b.call(RuntimeMember.ArrayTryPackedRead); b.emit(0xfe0e, packed);
        b.emit(0xfe0c, packed); b.emit(0x39, fallback);
        b.emit(0xfe0c, iterable); b.call(RuntimeMember.ArrayLength); b.op(0x69); b.emit(0xfe0e, limit);
        b.emit(0xfe0c, packed); b.op(0x8e); b.op(0x69); b.emit(0xfe0c, limit); b.op(0xfe04); b.emit(0x3a, fallback);
        b.emit(0x20, 0); b.emit(0xfe0e, position); b.emit(0x38, stop);
        b.mark(start);
        b.emit(0xfe0c, packed); b.emit(0xfe0c, position); b.op(0x99); b.call(RuntimeMember.WrapNumber); b.emit(0xfe0e, valueSlot);
        il_iteration_store(binding, type, valueSlot, c, b);
        il_statement(loop.fourth, result, c, b);
        b.emit(0xfe0c, position); b.emit(0x20, 1); b.op(0x58); b.emit(0xfe0e, position);
        b.mark(stop); b.emit(0xfe0c, position); b.emit(0xfe0c, limit); b.op(0xfe04); b.emit(0x3a, start);
        b.emit(0x38, done); b.mark(fallback);
        return done;
    }
}
