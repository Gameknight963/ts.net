namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    static bool il_integer_same_location(int first, int second, Compiler c, IlBackend b)
    {
        Node left = get_node(first, c), right = get_node(second, c);
        if (left.kind == NodeKind.Name && right.kind == NodeKind.Name) return left.symbol == right.symbol;
        ScalarField leftField = il_scalar_field(first, c, b), rightField = il_scalar_field(second, c, b);
        return leftField.slot >= 0 && leftField.slot == rightField.slot &&
            get_type(leftField.type, c).kind == TypeKind.Number && get_type(rightField.type, c).kind == TypeKind.Number;
    }

    static int il_integer_literal_step(int entry, bool subtract, Compiler c)
    {
        if (get_node(entry, c).kind != NodeKind.Number || !il_int_result(entry, c)) return 0;
        long value = (int)number(get_token(get_node(entry, c).token, c).text, c);
        if (subtract) value = -value;
        return value >= int.MinValue && value <= int.MaxValue ? (int)value : 0;
    }

    static int il_integer_counter_step(int entry, Compiler c, IlBackend b)
    {
        Node update = get_node(entry, c);
        int operation = get_token(update.token, c).text;
        if (update.kind == NodeKind.Update)
            return is_text(operation, text_increment, c) ? 1 : is_text(operation, text_decrement, c) ? -1 : 0;
        if (update.kind != NodeKind.Assign) return 0;
        if (is_text(operation, text_add_assign, c) || is_text(operation, text_subtract_assign, c))
            return il_integer_literal_step(update.right, is_text(operation, text_subtract_assign, c), c);
        if (!is_text(operation, text_assign, c)) return 0;
        Node value = get_node(update.right, c);
        if (value.kind == NodeKind.Call) return il_integer_call_step(update.right, update.left, c, b);
        if (value.kind != NodeKind.Binary) return 0;
        operation = get_token(value.token, c).text;
        if (il_integer_same_location(update.left, value.left, c, b) &&
            (is_text(operation, text_plus, c) || is_text(operation, text_minus, c)))
            return il_integer_literal_step(value.right, is_text(operation, text_minus, c), c);
        if (is_text(operation, text_plus, c) && il_integer_same_location(update.left, value.right, c, b))
            return il_integer_literal_step(value.left, false, c);
        return 0;
    }
    // An arithmetic leaf can use integers after the loop guard proves every
    // input and output stays within the signed 32-bit range.
    internal static int il_integer_call_step(int entry, int accumulator, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c); if (call.kind != NodeKind.Call || get_type(call.type, c).kind != TypeKind.Number) { return 0; }
        Node callee = get_node(call.left, c);
        if (callee.kind != NodeKind.Name && callee.kind != NodeKind.FunctionValue) { return 0; }
        if (callee.kind == NodeKind.Name && (b.global_flags.at(callee.symbol) != 0 || b.capture_fields.at(callee.symbol) != 0)) { return 0; }
        int symbol = il_direct_function(call.left, c, b); if (symbol == 0 || b.function_contexts.at(symbol) != 0) { return 0; }
        if (call.children == 0) { return 0; }
        Node argument = get_node(call.children, c);
        if (!il_integer_same_location(call.children, accumulator, c, b) || argument.next != 0) { return 0; }
        Node function = get_node(get_symbol(symbol, c).function, c); if (function.children == 0) { return 0; }
        Node parameter = get_node(function.children, c);
        if (parameter.kind != NodeKind.Variable || parameter.next != 0 || parameter.left != 0 || parameter.third != 0 ||
            get_type(parameter.type, c).kind != TypeKind.Number || get_type(function.type, c).kind != TypeKind.Number) { return 0; }
        Node body = get_node(function.left, c);
        if (body.kind == NodeKind.Block) { if (body.children == 0) { return 0; } body = get_node(body.children, c); if (body.next != 0) { return 0; } }
        if (body.kind != NodeKind.Return) { return 0; }
        Node expression = get_node(body.left, c); if (expression.kind != NodeKind.Binary) { return 0; }
        Node left = get_node(expression.left, c); Node right = get_node(expression.right, c);
        if (left.kind != NodeKind.Name || left.symbol != parameter.symbol || right.kind != NodeKind.Number || !il_int_result(expression.right, c)) { return 0; }
        double literal = number(get_token(right.token, c).text, c); if (literal <= 0 || literal > 2147483647) { return 0; }
        int op = get_token(expression.token, c).text;
        if (is_text(op, text_plus, c)) { return (int)literal; }
        if (is_text(op, text_minus, c)) { return -(int)literal; }
        return 0;
    }
    internal static bool il_integer_counter_loop(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        if (counter == 0) { return false; }
        Node loop = get_node(entry, c); Node body = get_node(loop.fourth, c);
        if (body.kind == NodeKind.Block) { if (body.children == 0) { return false; } body = get_node(body.children, c); if (body.next != 0) { return false; } }
        if (body.kind != NodeKind.Expression) { return false; }
        Node assignment = get_node(body.left, c);
        if (assignment.kind is not (NodeKind.Assign or NodeKind.Update)) return false;
        Node target = get_node(assignment.left, c);
        if (get_type(target.type, c).kind != TypeKind.Number) return false;
        ScalarField scalar = il_scalar_field(assignment.left, c, b);
        bool local = target.kind == NodeKind.Name;
        // Heap properties may invoke getters or setters; only fields already
        // replaced by local slots can use the same range proof as a variable.
        if (local) {
            if (target.symbol == counter || get_type(get_symbol(target.symbol, c).type, c).kind != TypeKind.Number ||
                b.global_fields.at(target.symbol) != 0 || b.capture_fields.at(target.symbol) != 0 || b.slots.at(target.symbol) < 0) return false;
        } else if (scalar.slot < 0 || get_type(scalar.type, c).kind != TypeKind.Number) return false;
        // Nonzero monotone steps let the endpoints bound every intermediate value.
        // Zero steps retain the ordinary path, including signed-zero behavior.
        int step = il_integer_counter_step(body.left, c, b); if (step == 0) return false;
        void LoadValue() {if (local) b.load(target.symbol); else b.emit(0xfe0c, scalar.slot);}
        void StoreValue() {if (local) b.store(target.symbol); else b.emit(0xfe0e, scalar.slot);}
        Node binding = get_node(il_loop_binding(entry, c), c); int initial = (int)number(get_token(get_node(binding.left, c).token, c).text, c);
        int fallback = b.label(); int done = b.label();
        // Short loops do not amortize the range check. This also preserves empty-
        // loop behavior, including undefined and negative zero.
        b.emit(0xfe0c, bound); b.plan.number(b.method, (double)initial + 31); b.op(0xfe02); b.emit(0x39, fallback);
        LoadValue(); b.emit(0xfe0c, bound); b.emit(0x20, initial); b.emit(0x20, step); b.call(RuntimeMember.GuardIntegerLoop); b.emit(0x39, fallback);
        int value = b.local(-6); int position = b.local(-6); int stop = b.local(-6);
        LoadValue(); b.call(RuntimeMember.NumberValue); b.op(0x69); b.emit(0xfe0e, value);
        b.emit(0x20, initial); b.emit(0xfe0e, position); b.emit(0xfe0c, bound); b.call(RuntimeMember.LoopLimit); b.emit(0xfe0e, stop);
        int start = b.label(); int finish = b.label(); b.mark(start);
        b.emit(0xfe0c, position); b.emit(0xfe0c, stop); b.op(0xfe04); b.emit(0x39, finish);
        b.emit(0xfe0c, value); b.emit(0x20, step); b.op(0x58); b.emit(0xfe0e, value);
        b.emit(0xfe0c, position); b.emit(0x20, 1); b.op(0x58); b.emit(0xfe0e, position); b.emit(0x38, start); b.mark(finish);
        b.emit(0xfe0c, value); b.op(0x6c); b.call(RuntimeMember.WrapNumber); StoreValue(); b.emit(0x38, done);
        b.mark(fallback); il_loop_code(entry, result, counter, bound, c, b); b.mark(done); return true;
    }
}
