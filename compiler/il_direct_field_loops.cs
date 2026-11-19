namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    static bool il_direct_field_loop(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        if (counter == 0 || bound < 0 || b.loop_update_statements.Count != 0) return false;
        Node loop = get_node(entry, c);
        if (loop.fourth == 0) return false;
        Node body = get_node(loop.fourth, c);
        int statement = body.kind == NodeKind.Block ? body.children : loop.fourth;
        if (statement == 0) return false;
        Node expression = get_node(statement, c);
        if (expression.kind != NodeKind.Expression || expression.next != 0 || expression.left == 0) return false;
        Node update = get_node(expression.left, c);
        if (update.kind is not (NodeKind.Update or NodeKind.Assign) || update.left == 0) return false;
        Node property = get_node(update.left, c);
        if (property.kind != NodeKind.Property || property.left == 0 || property.fourth != 0) return false;
        // An eliminated object has no heap receiver to guard or write back.
        if (il_scalar_field(update.left, c, b).slot >= 0) return false;
        Node receiver = get_node(property.left, c);
        bool self = receiver.kind == NodeKind.ResolveThis && !receiver.constant && receiver.left != 0 &&
            get_node(receiver.left, c).kind == NodeKind.Name;
        if (!self && (receiver.kind != NodeKind.Name || receiver.symbol == 0 || receiver.symbol == counter ||
            b.written_symbols.at(receiver.symbol) != 0)) return false;
        int owner = il_class_field_owner(property.left, c, b);
        if (owner == 0) return false;
        int operation = get_token(update.token, c).text;
        double operand; bool subtract;
        if (update.kind == NodeKind.Update) {
            if (!is_text(operation, text_increment, c) && !is_text(operation, text_decrement, c)) return false;
            operand = 1; subtract = is_text(operation, text_decrement, c);
        } else {
            if (update.right == 0 || get_node(update.right, c).kind != NodeKind.Number ||
                !is_text(operation, text_add_assign, c) && !is_text(operation, text_subtract_assign, c)) return false;
            operand = number(get_token(get_node(update.right, c).token, c).text, c); subtract = is_text(operation, text_subtract_assign, c);
        }
        var fields = b.numeric_class_members[owner];
        int field = fields.FindIndex(item => same(get_field(item, c).name, property.name, c) && get_type(get_field(item, c).type, c).kind == TypeKind.Number);
        if (field < 0) return false;
        double initial = number(get_token(get_node(get_node(il_loop_binding(entry, c), c).left, c).token, c).text, c);
        Node limit = get_node(get_node(loop.right, c).right, c);
        if (limit.kind == NodeKind.Number && number(get_token(limit.token, c).text, c) <= initial + 31) return false;
        int guard = il_class_update_guard(owner, field, b);
        int fallback = b.label(), done = b.label(), instance = b.local(b.classes.at(owner)), value = b.local(-3);
        // Skip eager receiver reads for short and empty loops. The original
        // path also handles undefined, accessors and changed descriptors.
        b.emit(0xfe0c, bound); b.plan.number(b.method, initial + 31); b.op(0xfe02); b.emit(0x39, fallback);
        il_boxed(property.left, c, b); b.emit(0x75, b.classes.at(owner)); b.emit(0xfe0e, instance);
        b.emit(0xfe0c, instance); b.emit(0x39, fallback);
        b.emit(0xfe0c, instance); b.emit(0x28, guard); b.emit(0x39, fallback);
        int token = b.class_field_tokens[owner][field];
        b.emit(0xfe0c, instance); b.emit(0x7b, token); b.emit(0xfe0e, value);
        b.loop_update_statements[statement] = (value, operand, subtract);
        il_loop_code(entry, result, counter, bound, c, b);
        b.loop_update_statements.Remove(statement);
        b.emit(0xfe0c, instance); b.emit(0xfe0c, value); b.emit(0x7d, token);
        b.emit(0x38, done); b.mark(fallback); il_loop_code(entry, result, counter, bound, c, b); b.mark(done);
        return true;
    }
}
