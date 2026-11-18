namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> loop_string_calls = [];
}

internal static partial class CompilerFunctions
{
    static bool il_string_range_loop(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        // This entire body only creates a range result and adds its length.
        // Stable locals and a literal argument leave no callbacks on the guarded
        // built-in path, so neither the method nor its inputs can change.
        if (counter == 0 || bound < 0 || b.loop_string_calls.Count != 0 || b.loop_getter_values.Count != 0) return false;
        Node loop = get_node(entry, c);
        if (loop.fourth == 0) return false;
        Node block = get_node(loop.fourth, c);
        if (block.kind != NodeKind.Block || block.children == 0) return false;
        Node value = get_node(block.children, c);
        if (value.kind != NodeKind.Variable || value.left == 0 || value.symbol == 0 || value.next == 0 ||
            get_type(value.type, c).kind != TypeKind.String || !local(value.symbol)) return false;
        int callEntry = value.left;
        Node call = get_node(callEntry, c);
        if (call.kind == NodeKind.Assert) { callEntry = call.left; if (callEntry == 0) return false; call = get_node(callEntry, c); }
        if (call.kind != NodeKind.Call || call.children == 0) return false;
        Node argument = get_node(call.children, c), method = get_node(call.left, c);
        if (argument.kind != NodeKind.Number || argument.next != 0 || method.kind is not (NodeKind.Property or NodeKind.Index) ||
            method.fourth != 0 || method.left == 0) return false;
        Node receiver = get_node(method.left, c);
        if (receiver.kind != NodeKind.Name || !local(receiver.symbol) ||
            get_type(receiver.type, c).kind is not (TypeKind.Any or TypeKind.Unknown or TypeKind.String)) return false;
        int keySymbol = 0;
        if (method.kind == NodeKind.Index) {
            if (method.right == 0) return false;
            Node key = get_node(method.right, c);
            if (key.kind == NodeKind.Name && local(key.symbol)) keySymbol = key.symbol;
            else if (key.kind != NodeKind.String) return false;
        } else if (method.name == 0) return false;
        Node addition = get_node(value.next, c);
        if (addition.kind != NodeKind.Expression || addition.next != 0 || addition.left == 0) return false;
        Node update = get_node(addition.left, c);
        if (update.kind != NodeKind.Assign || !is_text(get_token(update.token, c).text, text_add_assign, c)) return false;
        Node total = get_node(update.left, c), length = get_node(update.right, c);
        if (total.kind != NodeKind.Name || get_type(total.type, c).kind != TypeKind.Number || !local(total.symbol) ||
            length.kind != NodeKind.Property || length.fourth != 0 || length.left == 0 || !is_text(length.name, text_length, c)) return false;
        Node lengthTarget = get_node(length.left, c);
        if (lengthTarget.kind != NodeKind.Name || lengthTarget.symbol != value.symbol ||
            total.symbol == value.symbol || total.symbol == receiver.symbol || total.symbol == keySymbol ||
            counter == receiver.symbol || counter == keySymbol || value.symbol == receiver.symbol || value.symbol == keySymbol) return false;
        double initial = number(get_token(get_node(get_node(il_loop_binding(entry, c), c).left, c).token, c).text, c);
        Node limit = get_node(get_node(loop.right, c).right, c);
        if (limit.kind == NodeKind.Number && number(get_token(limit.token, c).text, c) <= initial + 31) return false;
        int fallback = b.label(), done = b.label(), text = b.local(-4), size = b.local(-3);
        b.emit(0xfe0c, bound); b.plan.number(b.method, initial + 31); b.op(0xfe02); b.emit(0x39, fallback);
        il_boxed(method.left, c, b);
        if (method.kind == NodeKind.Property) il_text(method.name, c, b); else il_boxed(method.right, c, b);
        il_number(call.children, c, b); b.call(RuntimeMember.TryStringRangeValue); b.emit(0xfe0e, text);
        b.emit(0xfe0c, text); b.emit(0x39, fallback);
        b.emit(0xfe0c, text); b.call(RuntimeMember.Length); b.op(0x6c); b.emit(0xfe0e, size);
        b.loop_string_calls[callEntry] = text; b.loop_getter_values[update.right] = size;
        il_loop_code(entry, result, counter, bound, c, b);
        b.loop_string_calls.Remove(callEntry); b.loop_getter_values.Remove(update.right);
        b.emit(0x38, done); b.mark(fallback); il_loop_code(entry, result, counter, bound, c, b); b.mark(done);
        return true;

        bool local(int symbol) => symbol != 0 && symbol != counter && b.global_fields.at(symbol) == 0 && b.capture_fields.at(symbol) == 0;
    }
}
