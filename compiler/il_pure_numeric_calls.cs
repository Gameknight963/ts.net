namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    // Prove the callee independently of its arguments. The loop's effect scan
    // still checks every argument expression, including indexed reads.
    static bool il_pure_numeric_call(int entry, Compiler c, IlBackend b)
    {
        int target = pure_constructor_helper(entry, c, b);
        if (target == 0) return false;
        Node call = get_node(entry, c), function = get_node(get_symbol(target, c).function, c);
        var parameters = new List<(int Argument, int Type, int Symbol)>();
        int argument = call.children;
        for (int parameter = function.children; parameter != 0; parameter = get_node(parameter, c).next) {
            Node binding = get_node(parameter, c);
            if (argument == 0 || binding.kind != NodeKind.Variable || binding.symbol == 0 || binding.constant ||
                binding.left != 0 || binding.right != 0 || binding.third != 0 || binding.fourth != 0 || binding.children != 0 ||
                get_type(binding.type, c).kind != TypeKind.Number || get_type(get_node(argument, c).type, c).kind != TypeKind.Number) return false;
            parameters.Add((argument, binding.type, binding.symbol));
            argument = get_node(argument, c).next;
        }
        if (argument != 0) return false;
        Node body = get_node(function.left, c);
        int statement = body.kind == NodeKind.Block ? body.children : function.left;
        if (statement == 0) return false;
        Node returned = get_node(statement, c);
        int remaining = 64;
        return returned.kind == NodeKind.Return && returned.next == 0 && returned.left != 0 &&
            scalar_constructor_expression(returned.left, TypeKind.Number, parameters, ref remaining, c, b);
    }
}
