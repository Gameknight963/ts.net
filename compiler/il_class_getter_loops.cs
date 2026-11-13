namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> loop_getter_values = [];
}

internal static partial class CompilerFunctions
{
    internal static bool il_class_getter_loop(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        if (counter == 0 || bound < 0 || b.loop_getter_values.Count != 0) return false;
        Node loop = get_node(entry, c), body = get_node(loop.fourth, c);
        int statement = body.kind == NodeKind.Block ? body.children : loop.fourth;
        if (statement == 0) return false;
        Node expression = get_node(statement, c);
        if (expression.kind != NodeKind.Expression || expression.next != 0) return false;
        Node assignment = get_node(expression.left, c);
        if (assignment.kind != NodeKind.Assign || !is_text(get_token(assignment.token, c).text, "+=", c)) return false;
        Node total = get_node(assignment.left, c), property = get_node(assignment.right, c);
        if (total.kind != NodeKind.Name || total.symbol == counter || get_type(total.type, c).kind != TypeKind.Number ||
            b.global_fields.at(total.symbol) != 0 || b.capture_fields.at(total.symbol) != 0 ||
            property.kind != NodeKind.Property || property.fourth != 0 || get_type(property.type, c).kind != TypeKind.Number) return false;
        Node receiver = get_node(property.left, c);
        if (receiver.kind != NodeKind.Name || receiver.symbol == 0 || receiver.symbol == total.symbol || b.global_vars.at(receiver.symbol) != 0) return false;
        int type = receiver.type;
        if (get_type(type, c).kind != TypeKind.Shape || get_type(type, c).name == 0 ||
            !il_simple_class_getter(type, property.name, c, b, out var getter)) return false;
        int klass = b.classes.at(type);
        if (klass == 0) return false;
        int binding = il_loop_binding(entry, c);
        double initial = number(get_token(get_node(get_node(binding, c).left, c).token, c).text, c);
        Node limit = get_node(get_node(loop.right, c).right, c);
        if (limit.kind == NodeKind.Number && number(get_token(limit.token, c).text, c) <= initial + 31) return false;
        var fields = new List<int>();
        void collect(int node)
        {
            Node value = get_node(node, c);
            if (value.kind == NodeKind.Property) { if (!fields.Any(name => same(name, value.name, c))) fields.Add(value.name); return; }
            if (value.left != 0) collect(value.left);
            if (value.right != 0) collect(value.right);
        }
        collect(getter.Expression);
        int ordinary = b.label(), done = b.label(), saved = b.local(-3), instance = b.local(klass);
        // Keep short loops on the ordinary path. This also prevents evaluating
        // the receiver or accumulator binding for empty loops.
        b.emit(0xfe0c, bound); b.plan.number(b.method, initial + 31); b.op(0xfe02); b.emit(0x39, ordinary);
        Token location = get_token(expression.token, c);
        if (location.file != 0 && location.line > 0)
        {
            Vector<byte> file = new();
            for (int i = 0; i < text_size(location.file, c); i++) file.push(text_byte(location.file, i, c));
            b.plan.source_point(b.method, file.view(), location.line, Math.Clamp(location.column, 1, 65534));
        }
        // A compound assignment reads its local accumulator before its RHS.
        b.load(total.symbol); b.op(0x26);
        il_boxed(property.left, c, b); b.emit(0x75, klass); b.emit(0xfe0e, instance);
        b.emit(0xfe0c, instance); b.emit(0x39, ordinary);
        b.emit(0xfe0c, instance); b.emit(0x20, getter.Slot); b.call(RuntimeMember.MatchesSimpleClassGetter); b.emit(0x39, ordinary);
        foreach (int name in fields)
        {
            b.emit(0xfe0c, instance); il_text(name, c, b); b.call(RuntimeMember.HasOwnNumberData); b.emit(0x39, ordinary);
        }
        // Only numeric arithmetic remains in this body. With no writes or calls,
        // the guarded getter and its own data fields cannot change during it.
        il_number(assignment.right, c, b); b.emit(0xfe0e, saved);
        b.loop_getter_values[assignment.right] = saved;
        il_loop_code(entry, result, counter, bound, c, b);
        b.loop_getter_values.Remove(assignment.right);
        b.emit(0x38, done); b.mark(ordinary);
        il_loop_code(entry, result, counter, bound, c, b); b.mark(done);
        return true;
    }
}
