namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static // An arithmetic leaf can be evaluated as integer arithmetic only after a loop
                    // guard proves that every input and output stays within the signed 32-bit range.
int il_integer_call_step(int entry, int accumulator, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c); if (call.kind != NodeKind.Call || get_type(call.type, c).kind != TypeKind.Number) { return 0; }
        Node callee = get_node(call.left, c);
        if (callee.kind != NodeKind.Name && callee.kind != NodeKind.FunctionValue) { return 0; }
        if (callee.kind == NodeKind.Name && (b.global_flags.at(callee.symbol) != 0 || b.capture_fields.at(callee.symbol) != 0)) { return 0; }
        int symbol = il_direct_function(call.left, c, b); if (symbol == 0 || b.function_contexts.at(symbol) != 0) { return 0; }
        if (call.children == 0) { return 0; }
        Node argument = get_node(call.children, c);
        if (argument.kind != NodeKind.Name || argument.symbol != accumulator || argument.next != 0) { return 0; }
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
    internal static bool il_integer_call_loop(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        if (counter == 0) { return false; }
        Node loop = get_node(entry, c); Node body = get_node(loop.fourth, c);
        if (body.kind == NodeKind.Block) { if (body.children == 0) { return false; } body = get_node(body.children, c); if (body.next != 0) { return false; } }
        if (body.kind != NodeKind.Expression) { return false; }
        Node assignment = get_node(body.left, c);
        if (assignment.kind != NodeKind.Assign || !is_text(get_token(assignment.token, c).text, text_assign, c)) { return false; }
        Node target = get_node(assignment.left, c);
        if (target.kind != NodeKind.Name || target.symbol == counter || get_type(target.type, c).kind != TypeKind.Number ||
            b.global_fields.at(target.symbol) != 0 || b.capture_fields.at(target.symbol) != 0 || b.slots.at(target.symbol) < 0) { return false; }
        int step = il_integer_call_step(assignment.right, target.symbol, c, b); if (step == 0) { return false; }
        Node binding = get_node(il_loop_binding(entry, c), c); int initial = (int)number(get_token(get_node(binding.left, c).token, c).text, c);
        int fallback = b.label(); int done = b.label();
        // Short loops do not amortize the range check. This also preserves empty-
        // loop behavior, including undefined and negative zero.
        b.emit(0xfe0c, bound); b.plan.number(b.method, (double)initial + 31); b.op(0xfe02); b.emit(0x39, fallback);
        b.load(target.symbol); b.emit(0xfe0c, bound); b.emit(0x20, initial); b.emit(0x20, step); b.call(RuntimeMember.GuardIntegerLoop); b.emit(0x39, fallback);
        int value = b.local(-6); int position = b.local(-6); int stop = b.local(-6);
        b.load(target.symbol); b.call(RuntimeMember.NumberValue); b.op(0x69); b.emit(0xfe0e, value);
        b.emit(0x20, initial); b.emit(0xfe0e, position); b.emit(0xfe0c, bound); b.call(RuntimeMember.LoopLimit); b.emit(0xfe0e, stop);
        int start = b.label(); int finish = b.label(); b.mark(start);
        b.emit(0xfe0c, position); b.emit(0xfe0c, stop); b.op(0xfe04); b.emit(0x39, finish);
        b.emit(0xfe0c, value); b.emit(0x20, step); b.op(0x58); b.emit(0xfe0e, value);
        b.emit(0xfe0c, position); b.emit(0x20, 1); b.op(0x58); b.emit(0xfe0e, position); b.emit(0x38, start); b.mark(finish);
        b.emit(0xfe0c, value); b.op(0x6c); b.call(RuntimeMember.WrapNumber); b.store(target.symbol); b.emit(0x38, done);
        b.mark(fallback); il_loop_code(entry, result, counter, bound, c, b); b.mark(done); return true;
    }
}
