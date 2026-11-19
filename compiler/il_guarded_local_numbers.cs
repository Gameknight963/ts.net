namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    static bool il_guarded_local_number(int entry, Compiler c, IlBackend b)
    {
        Node value = get_node(entry, c);
        if (value.kind != NodeKind.Name || value.symbol == 0 || b.written_symbols.at(value.symbol) != 0 ||
            b.global_fields.at(value.symbol) != 0 || b.capture_fields.at(value.symbol) != 0) return false;
        // No assignment or closure can change this local between the test and
        // this read. Follow only the branch which lexically contains the read.
        for (int child = entry, parent = b.node_parents.at(child); parent != 0; child = parent, parent = b.node_parents.at(parent)) {
            Node owner = get_node(parent, c);
            if (owner.kind == NodeKind.Function) return false;
            if (owner.kind is NodeKind.If or NodeKind.Conditional) {
                if (child == owner.right && il_guarded_number_name(owner.left, entry, true, c)) return true;
                if (child == owner.third && il_guarded_number_name(owner.left, entry, false, c)) return true;
            }
        }
        return false;
    }

    static bool il_loop_coercion_may_invoke(Node value, Compiler c, IlBackend b)
    {
        if (!il_coercion_may_invoke(value, c)) return false;
        return value.left != 0 && il_numeric_coercion_calls(value.left, c) && !il_guarded_local_number(value.left, c, b) ||
            value.right != 0 && il_numeric_coercion_calls(value.right, c) && !il_guarded_local_number(value.right, c, b);
    }
}
