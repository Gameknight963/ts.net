namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    internal static bool il_checked_index_helper(int entry, Compiler c, IlBackend b)
    {
        Node assertion = get_node(entry, c);
        if (assertion.kind != NodeKind.Assert || get_type(assertion.type, c).kind != TypeKind.Number) return false;
        Node call = get_node(assertion.left, c);
        if (call.kind != NodeKind.Call || get_type(call.type, c).kind is not (TypeKind.Any or TypeKind.Unknown)) return false;
        Node callee = get_node(call.left, c);
        if (callee.kind != NodeKind.Name || b.global_flags.at(callee.symbol) != 0 || b.capture_fields.at(callee.symbol) != 0) return false;
        int symbol = il_direct_function(call.left, c, b);
        if (symbol == 0 || b.function_contexts.at(symbol) != 0) return false;
        Node function = get_node(get_symbol(symbol, c).function, c);
        if (function.fourth != 0 || function.right != 0 || function.internal_continuation || function.children == 0 ||
            get_type(function.type, c).kind is not (TypeKind.Any or TypeKind.Unknown)) return false;
        Node first = get_node(function.children, c), second = get_node(first.next, c);
        if (first.kind != NodeKind.Variable || first.next == 0 || second.kind != NodeKind.Variable || second.next != 0 ||
            first.left != 0 || first.third != 0 || second.left != 0 || second.third != 0 ||
            get_type(first.type, c).kind != TypeKind.Array || get_type(second.type, c).kind != TypeKind.Number ||
            get_type(get_type(first.type, c).element, c).kind is not (TypeKind.Any or TypeKind.Unknown)) return false;
        Node body = get_node(function.left, c);
        if (body.kind != NodeKind.Block || body.children == 0) return false;
        Node statement = get_node(body.children, c), index = get_node(statement.left, c);
        if (statement.kind != NodeKind.Return || statement.next != 0 || index.kind != NodeKind.Index || index.fourth != 0 ||
            get_node(index.left, c).kind != NodeKind.Name || get_node(index.left, c).symbol != first.symbol ||
            get_node(index.right, c).kind != NodeKind.Name || get_node(index.right, c).symbol != second.symbol) return false;
        int argument = call.children;
        if (argument == 0 || get_node(argument, c).kind == NodeKind.Spread) return false;
        int position = get_node(argument, c).next;
        if (position == 0 || get_node(position, c).next != 0 || get_node(position, c).kind == NodeKind.Spread) return false;
        // Preserve parameter conversions and their left-to-right evaluation.
        il_as(argument, first.type, c, b);
        il_as(position, second.type, c, b);
        b.call(RuntimeMember.CheckedArrayNumberValue);
        return true;
    }
}
