namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    static int il_filtered_number_sum(int entry, int binding, int type, int iterable, Compiler c, IlBackend b)
    {
        Node target = get_node(binding, c);
        if (target.kind != NodeKind.Variable || !target.constant || target.symbol == 0 ||
            get_type(type, c).kind is not (TypeKind.Any or TypeKind.Unknown) ||
            b.global_fields.at(target.symbol) != 0 || b.capture_fields.at(target.symbol) != 0) return 0;
        int Single(int id)
        {
            Node node = get_node(id, c);
            if (node.kind != NodeKind.Block) return id;
            return node.children != 0 && get_node(node.children, c).next == 0 ? node.children : 0;
        }
        int statement = Single(get_node(entry, c).fourth);
        if (statement == 0) return 0;
        Node test = get_node(statement, c);
        if (test.kind != NodeKind.If || test.third != 0) return 0;
        Node condition = get_node(test.left, c);
        if (condition.kind != NodeKind.Binary) return 0;
        int operation = get_token(condition.token, c).text;
        if (!is_text(operation, text_strict_equal, c) && !is_text(operation, text_loose_equal, c)) return 0;
        Node left = get_node(condition.left, c), right = get_node(condition.right, c);
        if (left.kind == NodeKind.String && right.kind == NodeKind.TypeOf) (left, right) = (right, left);
        if (left.kind != NodeKind.TypeOf || right.kind != NodeKind.String ||
            !is_text(get_token(right.token, c).text, text_number, c)) return 0;
        Node reference = get_node(left.left, c);
        if (reference.kind != NodeKind.Name || reference.symbol != target.symbol) return 0;
        int body = Single(test.right);
        if (body == 0 || get_node(body, c).kind != NodeKind.Expression) return 0;
        Node assignment = get_node(get_node(body, c).left, c);
        if (assignment.kind != NodeKind.Assign || !is_text(get_token(assignment.token, c).text, text_add_assign, c)) return 0;
        Node total = get_node(assignment.left, c), value = get_node(assignment.right, c);
        if (total.kind != NodeKind.Name || value.kind != NodeKind.Name || value.symbol != target.symbol ||
            total.symbol == target.symbol || total.symbol == 0 || get_type(total.type, c).kind != TypeKind.Number ||
            get_type(get_symbol(total.symbol, c).type, c).kind != TypeKind.Number ||
            b.global_fields.at(total.symbol) != 0 || b.capture_fields.at(total.symbol) != 0) return 0;
        int sum = b.local(-10), fallback = b.label(), done = b.label();
        b.emit(0xfe0c, iterable); il_expression(assignment.left, c, b); b.call(RuntimeMember.TrySumNumericArray); b.emit(0xfe0e, sum);
        b.emit(0xfe0c, sum); b.call(RuntimeMember.NumberMissing); b.emit(0x3a, fallback);
        b.emit(0xfe0c, sum); b.store(total.symbol); b.emit(0x38, done); b.mark(fallback);
        return done;
    }
}
