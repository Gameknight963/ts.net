namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    internal static bool il_checked_array_loop(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        if (counter == 0 || bound < 0 || b.loop_getter_values.Count != 0 || b.loop_getter_bodies.Count != 0 ||
            b.loop_getter_choices.Count != 0 || b.global_fields.at(counter) != 0 || b.capture_fields.at(counter) != 0) return false;
        Node loop = get_node(entry, c), body = get_node(loop.fourth, c);
        int statement = body.kind == NodeKind.Block ? body.children : loop.fourth;
        Node expression = get_node(statement, c);
        if (expression.kind != NodeKind.Expression || expression.next != 0) return false;
        Node assignment = get_node(expression.left, c);
        if (assignment.kind != NodeKind.Assign || !is_text(get_token(assignment.token, c).text, text_add_assign, c)) return false;
        Node total = get_node(assignment.left, c);
        if (total.kind != NodeKind.Name || total.symbol == counter || get_type(total.type, c).kind != TypeKind.Number ||
            b.global_fields.at(total.symbol) != 0 || b.capture_fields.at(total.symbol) != 0 ||
            !il_checked_index_helper_call(assignment.right, c, b, out var match)) return false;
        Node receiver = get_node(match.Receiver, c), index = get_node(match.Index, c);
        if (receiver.kind != NodeKind.Name || receiver.symbol == 0 || receiver.symbol == counter || receiver.symbol == total.symbol ||
            index.kind != NodeKind.Number) return false;
        double position = number(get_token(index.token, c).text, c);
        if (!double.IsFinite(position) || position < 0 || position > int.MaxValue || position != Math.Truncate(position)) return false;
        int binding = il_loop_binding(entry, c);
        double initial = number(get_token(get_node(get_node(binding, c).left, c).token, c).text, c);
        Node limit = get_node(get_node(loop.right, c).right, c);
        if (limit.kind == NodeKind.Number && number(get_token(limit.token, c).text, c) <= initial + 31) return false;

        int ordinary = b.label(), done = b.label(), probe = b.local(-10), value = b.local(-3);
        b.emit(0xfe0c, bound); b.plan.number(b.method, initial + 31); b.op(0xfe02); b.emit(0x39, ordinary);
        b.load(total.symbol); b.op(0x26);
        il_buffer_binding_guard(receiver.symbol, ordinary, b);
        il_boxed(match.Receiver, c, b); b.emit(0x20, (int)position); b.call(RuntimeMember.TryReadOwnArrayNumber); b.emit(0xfe0e, probe);
        b.emit(0xfe0c, probe); b.call(RuntimeMember.NumberMissing); b.emit(0x3a, ordinary);
        b.emit(0xfe0c, probe); b.call(RuntimeMember.NumberValue); b.emit(0xfe0e, value);
        // A successful probe reads ordinary own data. The helper and remaining
        // loop body cannot call user code, write the array, or change its binding.
        b.loop_getter_values[assignment.right] = value;
        il_loop_code(entry, result, counter, bound, c, b);
        b.loop_getter_values.Remove(assignment.right);
        b.emit(0x38, done); b.mark(ordinary);
        il_loop_code(entry, result, counter, bound, c, b); b.mark(done);
        return true;
    }
}
