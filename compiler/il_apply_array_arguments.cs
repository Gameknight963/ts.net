namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> apply_array_functions = [];
    internal int apply_array_parameter;
}

internal static partial class CompilerFunctions
{
    // Only a single return expression, with no observable array identity,
    // mutation, captured parameters, receiver, arguments or suspension.
    internal static void il_prepare_apply_array_function(int entry, int program, Compiler c, IlBackend b)
    {
        Node function = get_node(entry, c);
        if (b.function_values_needed.at(function.symbol) == 0 || function.internal_continuation || function.fourth != 0 || function.right != 0 ||
            b.function_contexts.at(function.symbol) != 0 || il_owns_context(entry, c, b) ||
            il_numeric_value_function(function.symbol, c) != 2) return;
        Node first = get_node(function.children, c), array = get_node(first.next, c);
        if (first.left != 0 || array.left != 0 || b.capture_fields.at(array.symbol) != 0 ||
            get_type(first.type, c).kind is not (TypeKind.Any or TypeKind.Unknown or TypeKind.Shape) ||
            get_type(array.type, c).kind != TypeKind.Array ||
            get_type(get_type(array.type, c).element, c).kind != TypeKind.Number) return;
        Node body = get_node(function.left, c);
        if (body.kind == NodeKind.Block) {
            if (body.children == 0 || get_node(body.children, c).next != 0) return;
            body = get_node(body.children, c);
        }
        if (body.kind != NodeKind.Return || body.left == 0) return;
        int reads = 0;
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node node = get_node(i, c);
            if (node.kind != NodeKind.Name || node.symbol != array.symbol) continue;
            if (scalar_owner(i, b.node_parents, c) != entry) return;
            int indexEntry = b.node_parents.at(i);
            Node index = get_node(indexEntry, c);
            if (index.kind != NodeKind.Index || index.left != i || index.type == 0 ||
                get_type(index.type, c).kind != TypeKind.Number) return;
            Node key = get_node(index.right, c);
            if (!scalar_array_index_literal(key) ||
                number(get_token(key.token, c).text, c) != 0) return;
            Node use = get_node(b.node_parents.at(indexEntry), c);
            if (use.kind is not (NodeKind.Binary or NodeKind.Unary or NodeKind.Return or NodeKind.Assert or
                NodeKind.TypeOf or NodeKind.Conditional or NodeKind.Nullish or NodeKind.Sequence) ||
                use.kind == NodeKind.Unary && is_text(get_token(use.token, c).text, text_delete, c)) return;
            reads++;
        }
        if (reads == 0) return;
        Vector<int> parameters = new(); parameters.push(b.type(first.type, c)); parameters.push(-10);
        b.apply_array_functions[function.symbol] = b.plan.method(5, program, 0x16, -10, parameters.view(), $"apply_array_f{get_symbol(function.symbol, c).id}");
    }
    internal static void il_apply_array_override(int symbol, int type, Compiler c, IlBackend b)
    {
        if (!b.apply_array_functions.TryGetValue(symbol, out int method)) return;
        Node function = get_node(get_symbol(symbol, c).function, c);
        Vector<int> parameters = new(); parameters.push(-5); parameters.push(-5); parameters.push(-10);
        b.begin(b.plan.method(5, type, 0xc6, -10, parameters.view(), "InvokeValueNumberArray"));
        b.emit(0xfe09, 2); b.from_object(get_node(function.children, c).type, c);
        b.emit(0xfe09, 3); b.emit(0x28, method); b.op(0x2a);
    }
    internal static void il_apply_array_body(int entry, Compiler c, IlBackend b)
    {
        Node function = get_node(entry, c);
        if (!b.apply_array_functions.TryGetValue(function.symbol, out int method)) return;
        Node first = get_node(function.children, c), array = get_node(first.next, c);
        b.begin(method); b.slots.at_mut(first.symbol) = -1; b.slots.at_mut(array.symbol) = -2;
        b.raw_slots.at_mut(first.symbol) = 0; b.raw_slots.at_mut(array.symbol) = 0;
        b.apply_array_parameter = array.symbol;
        il_statement(function.left, function.type, c, b);
        b.apply_array_parameter = 0;
        b.empty(function.type, c); b.op(0x2a);
    }
    internal static bool il_apply_array_read(int entry, Compiler c, IlBackend b)
    {
        if (b.apply_array_parameter == 0) return false;
        Node index = get_node(entry, c), receiver = get_node(index.left, c);
        if (index.kind != NodeKind.Index || receiver.kind != NodeKind.Name || receiver.symbol != b.apply_array_parameter) return false;
        b.load_storage(receiver.symbol); return true;
    }
}
