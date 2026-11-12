namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    internal static int il_fresh_assign_source(int entry, Compiler c)
    {
        Node call = get_node(entry, c);
        if (call.kind != NodeKind.Call || get_type(call.type, c).kind == TypeKind.Number || il_call_has_spread(entry, c)) return 0;
        int first = call.children;
        if (first == 0 || get_node(first, c).kind != NodeKind.Object || get_node(first, c).children != 0) return 0;
        int source = get_node(first, c).next;
        if (source == 0 || get_node(source, c).next != 0 || get_type(get_node(source, c).type, c).kind != TypeKind.Shape) return 0;
        int field = get_type(get_node(source, c).type, c).fields;
        while (field != 0) { if (get_field(field, c).optional) return 0; field = get_field(field, c).next; }
        Node target = get_node(call.left, c);
        while (target.kind == NodeKind.Assert) target = get_node(target.left, c);
        int name = target.kind == NodeKind.Property ? target.name
            : target.kind == NodeKind.Index && get_node(target.right, c).kind == NodeKind.String ? get_token(get_node(target.right, c).token, c).text : 0;
        return name != 0 && is_text(name, "assign", c) ? source : 0;
    }
    internal static int il_fresh_assign_class(int source, Compiler c, IlBackend b)
    {
        Node n = get_node(source, c);
        if (n.kind == NodeKind.Name && b.spread_source_classes.at(n.symbol) != 0) return b.spread_source_classes.at(n.symbol);
        if (n.kind == NodeKind.Object && !il_object_needs_entries(source, c) && b.literal_classes.at(source) != 0) return b.literal_classes.at(source);
        return b.classes.at(n.type);
    }
    internal static void il_emit_fresh_assign(int entry, int source, int klass, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c);
        int receiver = il_method_target(call.left, c, b);
        int function = b.local(-5); b.emit(0xfe0e, function);
        il_boxed(call.children, c, b); int destination = b.local(-5); b.emit(0xfe0e, destination);
        il_boxed(source, c, b); int cached = b.local(-5); b.emit(0xfe0e, cached);
        b.emit(0xfe0c, function); b.emit(0xfe0c, cached); b.emit(0xfe0c, destination);
        int method = b.plan.specialize(b.RuntimeToken(RuntimeMember.TryCopyAssign), klass);
        int fallback = b.label(); int done = b.label(); b.emit(0x28, method); b.emit(0x39, fallback);
        b.emit(0xfe0c, destination); b.emit(0x38, done);
        b.mark(fallback); b.emit(0xfe0c, function); b.emit(0xfe0c, receiver);
        b.emit(0x20, 2); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType));
        b.op(0x25); b.emit(0x20, 0); b.emit(0xfe0c, destination); b.op(0xa2);
        b.op(0x25); b.emit(0x20, 1); b.emit(0xfe0c, cached); b.op(0xa2);
        b.call(RuntimeMember.CallMethod); b.mark(done); b.from_object(call.type, c);
    }
}
