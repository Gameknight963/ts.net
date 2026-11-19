namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, (int Text, int Counter, RuntimeMember Reader)> string_code_calls = [];
}

internal static partial class CompilerFunctions
{
    static bool il_string_code_loop(int entry, int result, Compiler c, IlBackend b)
    {
        if (b.loop_versions != 0 || b.string_code_calls.Count != 0) return false;
        Node loop = get_node(entry, c);
        if (loop.kind != NodeKind.For || loop.right == 0 || loop.fourth == 0) return false;
        Node condition = get_node(loop.right, c);
        if (condition.kind != NodeKind.Binary || condition.right == 0) return false;
        Node length = get_node(condition.right, c);
        if (length.kind != NodeKind.Property || length.left == 0 || length.fourth != 0 || !is_text(length.name, text_length, c)) return false;
        Node receiver = get_node(length.left, c);
        if (receiver.kind != NodeKind.Name || get_type(receiver.type, c).kind != TypeKind.String ||
            b.written_symbols.at(receiver.symbol) != 0) return false;
        int counter = il_counted_loop(entry, c, b, true);
        if (counter == 0 || receiver.symbol == counter) return false;
        var calls = new List<(int Entry, RuntimeMember Reader)>();
        int methods = 0;
        foreach (int i in b.LoopNodes(loop.fourth, c)) {
            Node n = get_node(i, c);
            if (il_key_may_invoke(n, c) || il_loop_coercion_may_invoke(n, c, b)) return false;
            if (n.kind is not (NodeKind.Block or NodeKind.Expression or NodeKind.If or NodeKind.Variable or NodeKind.Name or
                NodeKind.Number or NodeKind.Boolean or NodeKind.String or NodeKind.Null or NodeKind.Undefined or NodeKind.TypeOf or
                NodeKind.Unary or NodeKind.Binary or NodeKind.Conditional or NodeKind.Assign or NodeKind.Update or
                NodeKind.Call or NodeKind.Property or NodeKind.Assert or NodeKind.Nullish or NodeKind.Break or NodeKind.Continue or NodeKind.Return)) return false;
            if (n.kind == NodeKind.Unary && is_text(get_token(n.token, c).text, text_delete, c)) return false;
            if (n.kind == NodeKind.Assert && (n.left == 0 || get_type(n.type, c).kind != TypeKind.Number ||
                get_node(n.left, c).kind != NodeKind.Call)) return false;
            if (n.kind == NodeKind.Call) {
                if (n.left == 0 || n.children == 0) return false;
                Node method = get_node(n.left, c), argument = get_node(n.children, c);
                if (method.kind != NodeKind.Property || method.fourth != 0 ||
                    !is_text(method.name, text_char_code_at, c) && !is_text(method.name, text_code_point_at, c) ||
                    get_node(method.left, c).kind != NodeKind.Name || get_node(method.left, c).symbol != receiver.symbol ||
                    argument.kind != NodeKind.Name || argument.symbol != counter || argument.next != 0 ||
                    n.right != 0 || n.third != 0 || n.fourth != 0 || get_type(n.type, c).kind != TypeKind.Number) return false;
                calls.Add((i, is_text(method.name, text_code_point_at, c) ? RuntimeMember.ReadStringCodePoint : RuntimeMember.ReadStringCodeUnit));
            }
            if (n.kind == NodeKind.Property) {
                if (n.fourth != 0 || n.left == 0 || get_node(n.left, c).kind != NodeKind.Name ||
                    get_node(n.left, c).symbol != receiver.symbol) return false;
                if (is_text(n.name, text_char_code_at, c)) methods |= 1;
                else if (is_text(n.name, text_code_point_at, c)) methods |= 2;
                else if (!is_text(n.name, text_length, c)) return false;
            }
            if (n.kind is NodeKind.Assign or NodeKind.Update) {
                Node target = get_node(n.left, c);
                if (target.kind != NodeKind.Name || target.symbol == receiver.symbol || target.symbol == counter) return false;
            }
            if (n.kind == NodeKind.Variable && (n.symbol == receiver.symbol || n.symbol == counter ||
                get_type(n.type, c).kind is not (TypeKind.Number or TypeKind.Boolean or TypeKind.String))) return false;
        }
        if (calls.Count == 0) return false;
        int fallback = b.label(), done = b.label(), text = b.local(-4), bound = b.local(-3);
        il_boxed(length.left, c, b);
        b.call((methods & 1) != 0 ? RuntimeMember.TryStringCodeUnitLoop : RuntimeMember.TryStringCodePointLoop); b.emit(0xfe0e, text);
        b.emit(0xfe0c, text); b.emit(0x39, fallback);
        // Guard properties read without calling them too: a getter on the
        // other character method could otherwise change the cached method.
        if (methods == 3) {
            b.emit(0xfe0c, text); b.call(RuntimeMember.TryStringCodePointLoop); b.emit(0xfe0e, text);
            b.emit(0xfe0c, text); b.emit(0x39, fallback);
        }
        b.emit(0xfe0c, text); b.call(RuntimeMember.Length); b.op(0x6c); b.emit(0xfe0e, bound);
        int previous = b.raw_slot(counter), nonnegative = b.nonnegative_ints.at(counter);
        b.raw_slots.at_mut(counter) = -6; b.nonnegative_ints.at_mut(counter) = 1; b.loop_versions++;
        foreach (var call in calls) b.string_code_calls[call.Entry] = (text, counter, call.Reader);
        il_loop_code(entry, result, counter, bound, c, b);
        foreach (var call in calls) b.string_code_calls.Remove(call.Entry);
        b.raw_slots.at_mut(counter) = previous; b.nonnegative_ints.at_mut(counter) = nonnegative;
        b.emit(0x38, done); b.mark(fallback); il_loop_code(entry, result, 0, -1, c, b); b.mark(done);
        b.loop_versions--;
        return true;
    }
}
