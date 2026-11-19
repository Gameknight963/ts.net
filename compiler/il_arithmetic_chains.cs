namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    static bool il_arithmetic_chain(int entry, Compiler c, IlBackend b)
    {
        bool NumericSum(int item)
        {
            Node node = get_node(item, c);
            if (node.kind != NodeKind.Binary || get_type(node.type, c).kind != TypeKind.Number ||
                get_type(get_node(node.left, c).type, c).kind != TypeKind.Number ||
                get_type(get_node(node.right, c).type, c).kind != TypeKind.Number) return false;
            int op = get_token(node.token, c).text;
            return is_text(op, text_plus, c) || is_text(op, text_minus, c);
        }
        if (!NumericSum(entry) || !NumericSum(get_node(entry, c).left)) return false;
        List<int> operations = [];
        int left = entry;
        while (NumericSum(left)) { operations.Add(left); left = get_node(left, c).left; }
        // Preserve operand order and every intermediate conversion. This is
        // an iterative emission of the original tree, not reassociation.
        il_number(left, c, b);
        for (int i = operations.Count - 1; i >= 0; i--)
        {
            Node node = get_node(operations[i], c);
            il_number(node.right, c, b);
            b.op(is_text(get_token(node.token, c).text, text_plus, c) ? 0x58 : 0x59);
            b.call(RuntimeMember.WrapNumber);
            if (i != 0) b.call(RuntimeMember.NumberValue);
        }
        return true;
    }
}
