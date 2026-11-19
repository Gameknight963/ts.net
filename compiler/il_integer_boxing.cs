namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    static bool il_box_integer(int entry, Compiler c, IlBackend b)
    {
        Node node = get_node(entry, c);
        if (get_type(node.type, c).kind != TypeKind.Number) return false;
        bool integer = node.kind == NodeKind.Name && b.raw_slot(node.symbol) == -6;
        if (integer)
        {
            integer = false;
            for (int parent = entry; parent != 0; parent = b.node_parents.at(parent))
            {
                Node loop = get_node(parent, c);
                if (loop.kind != NodeKind.For || get_node(il_loop_binding(parent, c), c).symbol != node.symbol) continue;
                Node bound = get_node(get_node(loop.right, c).right, c);
                double limit = bound.kind == NodeKind.Number ? number(get_token(bound.token, c).text, c) : double.NaN;
                integer = limit > 0 && limit <= 256;
                break;
            }
        }
        if (node.kind == NodeKind.Number && il_int_result(entry, c))
        {
            double value = number(get_token(node.token, c).text, c);
            integer = value >= 0 && value < 256 && (value != 0 || !double.IsNegative(value));
        }
        if (node.kind == NodeKind.Unary)
            integer = get_type(get_node(node.left, c).type, c).kind == TypeKind.Number &&
                is_text(get_token(node.token, c).text, text_bit_not, c);
        if (node.kind == NodeKind.Binary)
        {
            int opcode = il_bitwise_code(get_token(node.token, c).text, c);
            // Unsigned right shifts can produce values above int.MaxValue.
            integer = opcode != 0 && opcode != 0x64 &&
                (get_type(get_node(node.left, c).type, c).kind == TypeKind.Number ||
                 get_type(get_node(node.right, c).type, c).kind == TypeKind.Number);
        }
        if (!integer) return false;
        il_integer(entry, c, b);
        b.call(RuntimeMember.BoxInteger);
        return true;
    }
}
