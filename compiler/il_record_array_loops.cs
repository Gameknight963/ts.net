namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, (int Buffer, int Counter, int Owner, int Member)> record_array_fields = [];
}

internal static partial class CompilerFunctions
{
    internal static bool il_record_array_field(int entry, Compiler c, IlBackend b)
    {
        if (!b.record_array_fields.TryGetValue(entry, out var read)) return false;
        b.emit(0xfe0c, read.Buffer); b.load_storage(read.Counter); b.op(0x9a);
        b.emit(0x74, b.classes.at(read.Owner)); b.emit(0x7b, b.field_token(read.Member));
        return true;
    }

    internal static bool il_record_array_loop(int entry, int result, Compiler c, IlBackend b)
    {
        if (b.loop_versions != 0 || b.record_array_fields.Count != 0) return false;
        Node loop = get_node(entry, c);
        if (loop.kind != NodeKind.For || loop.right == 0) return false;
        Node condition = get_node(loop.right, c), length = get_node(condition.right, c);
        if (length.kind != NodeKind.Property || length.fourth != 0 || !is_text(length.name, text_length, c)) return false;
        Node source = get_node(length.left, c);
        int counter = il_counted_loop(entry, c, b, true);
        if (source.kind != NodeKind.Name || source.symbol == 0 || counter == 0 || source.symbol == counter ||
            b.global_fields.at(counter) != 0 || b.capture_fields.at(counter) != 0) return false;
        Node body = get_node(loop.fourth, c);
        int statement = body.kind == NodeKind.Block ? body.children : loop.fourth;
        Node expression = get_node(statement, c), assignment = get_node(expression.left, c);
        if (expression.kind != NodeKind.Expression || expression.next != 0 || assignment.kind != NodeKind.Assign ||
            !is_text(get_token(assignment.token, c).text, text_add_assign, c)) return false;
        Node total = get_node(assignment.left, c);
        if (total.kind != NodeKind.Name || total.symbol == counter || total.symbol == source.symbol ||
            get_type(total.type, c).kind != TypeKind.Number || b.global_fields.at(total.symbol) != 0 ||
            b.capture_fields.at(total.symbol) != 0) return false;
        int owner = 0;
        var reads = new List<(int Entry, int Member)>();
        bool Pure(int item)
        {
            Node node = get_node(item, c);
            if (get_type(node.type, c).kind != TypeKind.Number) return false;
            if (node.kind == NodeKind.Number) return true;
            if (node.kind == NodeKind.Name) return node.symbol == counter;
            if (node.kind == NodeKind.Property && node.fourth == 0)
            {
                Node index = get_node(node.left, c);
                if (index.kind != NodeKind.Index || index.fourth != 0) return false;
                Node array = get_node(index.left, c), key = get_node(index.right, c);
                if (array.kind != NodeKind.Name || array.symbol != source.symbol || key.kind != NodeKind.Name || key.symbol != counter) return false;
                int shape = index.type, member = field(shape, node.name, c);
                if (get_type(shape, c).kind != TypeKind.Shape || get_type(shape, c).name != 0 || b.classes.at(shape) == 0 ||
                    b.spread_types.at(shape) != 0 || member == 0 || get_field(member, c).optional ||
                    b.field_token(member) == 0 || il_dynamic_index_field(member, c) || b.field_storage(member, c) != -3 ||
                    (owner != 0 && owner != shape)) return false;
                owner = shape; reads.Add((item, member)); return true;
            }
            int op = get_token(node.token, c).text;
            bool sign = is_text(op, text_plus, c) || is_text(op, text_minus, c);
            if (node.kind == NodeKind.Unary) return sign && Pure(node.left);
            return node.kind == NodeKind.Binary && (sign || is_text(op, text_star, c) || is_text(op, text_slash, c)) &&
                Pure(node.left) && Pure(node.right);
        }
        if (!Pure(assignment.right) || reads.Count == 0) return false;
        int buffer = b.local(-7), bound = b.local(-3), fallback = b.label(), done = b.label();
        // The probe invokes no user code. It verifies every element's exact
        // layout and lack of overrides before freezing the length and reads.
        b.load(source.symbol);
        b.emit(0x28, b.plan.specialize(b.RuntimeToken(RuntimeMember.TryPlainRecordArray), b.classes.at(owner)));
        b.emit(0xfe0e, buffer); b.emit(0xfe0c, buffer); b.emit(0x39, fallback);
        b.load(source.symbol); b.call(RuntimeMember.ArrayLength); b.emit(0xfe0e, bound);
        int saved = b.raw_slot(counter), nonnegative = b.nonnegative_ints.at(counter);
        b.raw_slots.at_mut(counter) = -6; b.nonnegative_ints.at_mut(counter) = 1; b.loop_versions++;
        foreach (var read in reads) b.record_array_fields[read.Entry] = (buffer, counter, owner, read.Member);
        il_loop_code(entry, result, counter, bound, c, b);
        foreach (var read in reads) b.record_array_fields.Remove(read.Entry);
        b.raw_slots.at_mut(counter) = saved; b.nonnegative_ints.at_mut(counter) = nonnegative;
        b.emit(0x38, done); b.mark(fallback); il_loop_body(entry, result, 0, -1, c, b); b.mark(done);
        b.loop_versions--; return true;
    }
}
