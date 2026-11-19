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
        b.emit(0x74, b.classes.at(read.Owner));
        if (read.Member == 0) return true; // The guarded element binding itself.
        int token = il_numeric_class_eligible(read.Owner, c, b)
            ? b.class_field_tokens[read.Owner][b.numeric_class_members[read.Owner].IndexOf(read.Member)]
            : b.field_token(read.Member);
        b.emit(0x7b, token);
        if (get_type(get_node(entry, c).type, c).kind != TypeKind.Number)
            b.from_object(get_node(entry, c).type, c);
        return true;
    }

    internal static bool il_record_array_loop(int entry, int result, Compiler c, IlBackend b)
    {
        if (b.loop_versions != 0 || b.record_array_fields.Count != 0) return false;
        Node loop = get_node(entry, c);
        if (loop.kind != NodeKind.For || loop.right == 0 || loop.fourth == 0) return false;
        Node condition = get_node(loop.right, c);
        if (condition.kind != NodeKind.Binary || condition.right == 0) return false;
        Node length = get_node(condition.right, c);
        if (length.kind != NodeKind.Property || length.fourth != 0 || !is_text(length.name, text_length, c)) return false;
        Node source = get_node(length.left, c);
        int counter = il_counted_loop(entry, c, b, true);
        if (source.kind != NodeKind.Name || source.symbol == 0 || counter == 0 || source.symbol == counter ||
            b.global_fields.at(counter) != 0 || b.capture_fields.at(counter) != 0) return false;
        Node body = get_node(loop.fourth, c);
        int statement = body.kind == NodeKind.Block ? body.children : loop.fourth;
        if (statement == 0) return false;
        int alias = 0, aliasValue = 0;
        Node expression = get_node(statement, c);
        if (expression.kind == NodeKind.Variable) {
            if (expression.symbol == 0 || expression.left == 0 || expression.right != 0 || expression.third != 0 ||
                expression.children != 0 || expression.next == 0 || expression.symbol == source.symbol ||
                expression.symbol == counter || b.global_fields.at(expression.symbol) != 0 ||
                b.capture_fields.at(expression.symbol) != 0 || expression.type != get_node(expression.left, c).type) return false;
            alias = expression.symbol; aliasValue = expression.left;
            expression = get_node(expression.next, c);
        }
        if (expression.kind != NodeKind.Expression || expression.left == 0 || expression.next != 0) return false;
        Node assignment = get_node(expression.left, c);
        if (assignment.kind != NodeKind.Assign ||
            !is_text(get_token(assignment.token, c).text, text_add_assign, c)) return false;
        Node total = get_node(assignment.left, c);
        if (total.kind != NodeKind.Name || total.symbol == counter || total.symbol == source.symbol || total.symbol == alias ||
            get_type(total.type, c).kind != TypeKind.Number || b.global_fields.at(total.symbol) != 0 ||
            b.capture_fields.at(total.symbol) != 0) return false;
        if (aliasValue != 0) {
            Node index = get_node(aliasValue, c);
            int shape = index.type;
            if (index.kind == NodeKind.Assert && index.left != 0) {
                Node operand = get_node(index.left, c);
                if (operand.type != shape && get_type(operand.type, c).kind is not (TypeKind.Any or TypeKind.Unknown)) return false;
                index = operand;
            }
            if (index.kind != NodeKind.Index || index.fourth != 0) return false;
            Node array = get_node(index.left, c), key = get_node(index.right, c);
            if (array.kind != NodeKind.Name || array.symbol != source.symbol ||
                key.kind != NodeKind.Name || key.symbol != counter) return false;
        }
        int owner = 0;
        var reads = new List<(int Entry, int Member)>();
        bool Pure(int item)
        {
            Node node = get_node(item, c);
            TypeKind kind = get_type(node.type, c).kind;
            if (kind is not (TypeKind.Number or TypeKind.String or TypeKind.Boolean)) return false;
            if (node.kind is NodeKind.Number or NodeKind.String or NodeKind.Boolean) return true;
            if (node.kind == NodeKind.Name) return kind == TypeKind.Number && node.symbol == counter;
            if (node.kind == NodeKind.Conditional)
                return get_type(get_node(node.left, c).type, c).kind == TypeKind.Boolean &&
                    Pure(node.left) && Pure(node.right) && Pure(node.third);
            if (node.kind == NodeKind.Property && node.fourth == 0)
            {
                // String length cannot invoke user code. The emitted checked
                // read still rejects invalid dynamically assigned values.
                if (kind == TypeKind.Number && is_text(node.name, text_length, c) &&
                    get_type(get_node(node.left, c).type, c).kind == TypeKind.String)
                    return Pure(node.left);
                Node index = get_node(node.left, c);
                int shape = index.type;
                if (index.kind == NodeKind.Name && index.symbol == alias) index = get_node(aliasValue, c);
                // A single assertion is redundant when the exact-layout probe
                // succeeds. Keep its target type and leave fallback checks intact.
                if (index.kind == NodeKind.Assert && index.left != 0) {
                    Node operand = get_node(index.left, c);
                    // An element already typed differently may have its own
                    // checked conversion, which the target-layout probe cannot prove.
                    if (operand.type != shape && get_type(operand.type, c).kind is not (TypeKind.Any or TypeKind.Unknown)) return false;
                    index = operand;
                }
                if (index.kind != NodeKind.Index || index.fourth != 0) return false;
                Node array = get_node(index.left, c), key = get_node(index.right, c);
                if (array.kind != NodeKind.Name || array.symbol != source.symbol || key.kind != NodeKind.Name || key.symbol != counter) return false;
                int member = field(shape, node.name, c);
                if (get_type(shape, c).kind != TypeKind.Shape || b.classes.at(shape) == 0 || member == 0 ||
                    get_field(member, c).optional || (owner != 0 && owner != shape)) return false;
                if (il_numeric_class_eligible(shape, c, b)) {
                    if (!b.numeric_class_members[shape].Contains(member) || get_type(get_field(member, c).type, c).kind != kind) return false;
                }
                else if (get_type(shape, c).name != 0 || b.spread_types.at(shape) != 0 || b.field_token(member) == 0 ||
                    il_dynamic_index_field(member, c) || kind != TypeKind.Number || b.field_storage(member, c) != -3) return false;
                owner = shape; reads.Add((item, member)); return true;
            }
            if (kind != TypeKind.Number || node.kind is not (NodeKind.Binary or NodeKind.Unary)) return false;
            int op = get_token(node.token, c).text;
            bool sign = is_text(op, text_plus, c) || is_text(op, text_minus, c);
            if (get_type(get_node(node.left, c).type, c).kind != TypeKind.Number) return false;
            if (node.kind == NodeKind.Unary) return sign && Pure(node.left);
            return node.kind == NodeKind.Binary && (sign || is_text(op, text_star, c) || is_text(op, text_slash, c)) &&
                get_type(get_node(node.right, c).type, c).kind == TypeKind.Number && Pure(node.left) && Pure(node.right);
        }
        if (!Pure(assignment.right) || reads.Count == 0 ||
            aliasValue != 0 && get_node(aliasValue, c).type != owner) return false;
        int buffer = b.local(-7), bound = b.local(-3), fallback = b.label(), done = b.label();
        // The probe invokes no user code. It verifies every element's exact
        // layout and lack of overrides before freezing the length and reads.
        b.load(source.symbol);
        if (il_numeric_class_eligible(owner, c, b)) {
            int lowMask = 0, highMask = 0;
            foreach (var read in reads) {
                int index = b.numeric_class_members[owner].IndexOf(read.Member);
                if (index < 16) lowMask |= 3 << (index * 2);
                else highMask |= 3 << ((index - 16) * 2);
            }
            b.emit(0x20, lowMask);
            if (highMask != 0) b.emit(0x20, highMask);
            b.emit(0x28, b.plan.specialize(b.RuntimeToken(highMask != 0 ? RuntimeMember.TryClassArrayWide : RuntimeMember.TryClassArray), b.classes.at(owner)));
        }
        else b.emit(0x28, b.plan.specialize(b.RuntimeToken(RuntimeMember.TryPlainRecordArray), b.classes.at(owner)));
        b.emit(0xfe0e, buffer); b.emit(0xfe0c, buffer); b.emit(0x39, fallback);
        b.load(source.symbol); b.call(RuntimeMember.ArrayLength); b.emit(0xfe0e, bound);
        int saved = b.raw_slot(counter), nonnegative = b.nonnegative_ints.at(counter);
        b.raw_slots.at_mut(counter) = -6; b.nonnegative_ints.at_mut(counter) = 1; b.loop_versions++;
        foreach (var read in reads) b.record_array_fields[read.Entry] = (buffer, counter, owner, read.Member);
        if (aliasValue != 0) b.record_array_fields[aliasValue] = (buffer, counter, owner, 0);
        il_loop_code(entry, result, counter, bound, c, b);
        foreach (var read in reads) b.record_array_fields.Remove(read.Entry);
        if (aliasValue != 0) b.record_array_fields.Remove(aliasValue);
        b.raw_slots.at_mut(counter) = saved; b.nonnegative_ints.at_mut(counter) = nonnegative;
        b.emit(0x38, done); b.mark(fallback); il_loop_body(entry, result, 0, -1, c, b); b.mark(done);
        b.loop_versions--; return true;
    }
}
