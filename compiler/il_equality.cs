namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
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
