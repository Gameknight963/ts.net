namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    internal readonly HashSet<int> reference_bindings = [];
}

internal static partial class CompilerFunctions
{
    internal static bool il_reference_value(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind is NodeKind.Object or NodeKind.Array or NodeKind.FunctionValue or NodeKind.Null or NodeKind.Undefined) return true;
        if (n.kind == NodeKind.Name) return b.reference_bindings.Contains(n.symbol) ||
            n.symbol != 0 && get_symbol(n.symbol, c).function != 0 && b.written_symbols.at(n.symbol) == 0;
        // Named function expressions use a factory to establish their self binding.
        // Recognize only an immediate function with one function-valued binding
        // followed by returning that binding. No general call-result inference.
        if (n.kind != NodeKind.Call || n.children != 0 || get_type(n.type, c).kind is not (TypeKind.Any or TypeKind.Unknown)) return false;
        Node callee = get_node(n.left, c);
        if (callee.kind != NodeKind.FunctionValue || callee.symbol == 0) return false;
        Node function = get_node(get_symbol(callee.symbol, c).function, c);
        if (function.children != 0 || function.right != 0 || function.fourth != 0) return false;
        Node body = get_node(function.left, c);
        if (body.kind != NodeKind.Block || body.children == 0) return false;
        Node binding = get_node(body.children, c);
        if (binding.kind != NodeKind.Variable || binding.symbol == 0 || b.written_symbols.at(binding.symbol) != 0 ||
            get_node(binding.left, c).kind != NodeKind.FunctionValue || binding.next == 0) return false;
        Node returned = get_node(binding.next, c), value = get_node(returned.left, c);
        return returned.kind == NodeKind.Return && returned.next == 0 && value.kind == NodeKind.Name && value.symbol == binding.symbol;
    }

    internal static void il_prepare_reference_bindings(Compiler c, IlBackend b)
    {
        bool changed;
        do {
            changed = false;
            for (int i = 1; i <= c.node_store.count(); i++) {
                Node n = get_node(i, c);
                if (n.kind != NodeKind.Variable || n.symbol == 0 || n.left == 0 || b.written_symbols.at(n.symbol) != 0 ||
                    il_parameter_binding(i, c, b) || b.reference_bindings.Contains(n.symbol)) continue;
                if (il_reference_value(n.left, c, b)) { b.reference_bindings.Add(n.symbol); changed = true; }
            }
        } while (changed);
    }

    internal static bool il_is_loose_equality(int op, Compiler c) =>
        is_text(op, text_loose_equal, c) || is_text(op, text_loose_not_equal, c);

    internal static bool il_loose_equality(Node n, Compiler c, IlBackend b)
    {
        int op = get_token(n.token, c).text;
        if (!il_is_loose_equality(op, c)) return false;
        Node left = get_node(n.left, c), right = get_node(n.right, c);
        TypeKind lhs = get_type(left.type, c).kind, rhs = get_type(right.type, c).kind;
        // These ABIs contain a single primitive type, with optional undefined.
        if (lhs == rhs && (lhs == TypeKind.Number || lhs == TypeKind.Boolean || lhs == TypeKind.String)) return false;
        int value = left.kind is NodeKind.Null or NodeKind.Undefined ? n.right :
            right.kind is NodeKind.Null or NodeKind.Undefined ? n.left : 0;
        if (value != 0)
        {
            TypeKind kind = get_type(get_node(value, c).type, c).kind;
            if (il_direct_array_index(value, c, b) && il_packed_slot(value, c, b) < 0)
            {
                Node read = get_node(value, c); il_expression(read.left, c, b); il_integer(read.right, c, b);
                b.call(RuntimeMember.NumberAtIsNullish);
            }
            else if (kind is TypeKind.Number or TypeKind.Boolean) { il_expression(value, c, b); b.missing(get_node(value, c).type, c); }
            else { il_boxed(value, c, b); b.call(RuntimeMember.IsNullish); }
        }
        else if (lhs == TypeKind.Number)
        {
            il_expression(n.left, c, b); il_boxed(n.right, c, b); b.call(RuntimeMember.LooseEqualNumber);
        }
        else if (rhs == TypeKind.Number)
        {
            // Preserve expression order without boxing the numeric operand.
            il_boxed(n.left, c, b); int saved = b.local(-5); b.emit(0xfe0e, saved);
            il_expression(n.right, c, b); b.emit(0xfe0c, saved); b.call(RuntimeMember.LooseEqualNumber);
        }
        else
        {
            il_boxed(n.left, c, b); il_boxed(n.right, c, b); b.call(RuntimeMember.LooseEqual);
        }
        if (is_text(op, text_loose_not_equal, c)) il_invert(b);
        b.call(RuntimeMember.WrapBoolean);
        return true;
    }
}
