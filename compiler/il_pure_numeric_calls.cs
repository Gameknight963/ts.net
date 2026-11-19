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
                get_type(binding.type, c).kind is TypeKind.Shape or TypeKind.Array or TypeKind.Void) return false;
            parameters.Add((argument, binding.type, binding.symbol));
            argument = get_node(argument, c).next;
        }
        if (argument != 0) return false;
        Node body = get_node(function.left, c);
        int statement = body.kind == NodeKind.Block ? body.children : function.left;
        if (statement == 0) return false;
        Node returned = get_node(statement, c);
        int remaining = 64;
        if (returned.kind != NodeKind.Return || returned.next != 0 || returned.left == 0) return false;
        if (scalar_constructor_expression(returned.left, TypeKind.Number, parameters, ref remaining, c, b)) return true;
        remaining = 64;
        return il_pure_parameter_expression(returned.left, parameters, ref remaining, c);
    }

    // typeof, strict comparisons and conditional selection can inspect union
    // parameters without converting them or invoking user code.
    static bool il_pure_parameter_expression(int entry, List<(int Argument, int Type, int Symbol)> parameters, ref int remaining, Compiler c)
    {
        if (entry == 0 || --remaining < 0) return false;
        Node value = get_node(entry, c);
        if (value.kind == NodeKind.Name) return parameters.Any(parameter => parameter.Symbol == value.symbol);
        if (value.kind is NodeKind.Number or NodeKind.String or NodeKind.Boolean or NodeKind.Null or NodeKind.Undefined) return true;
        if (value.fourth != 0 || value.children != 0) return false;
        if (value.kind == NodeKind.TypeOf)
            return il_pure_parameter_expression(value.left, parameters, ref remaining, c);
        if (value.kind == NodeKind.Conditional)
            return il_pure_parameter_expression(value.left, parameters, ref remaining, c) &&
                il_pure_parameter_expression(value.right, parameters, ref remaining, c) &&
                il_pure_parameter_expression(value.third, parameters, ref remaining, c);
        if (value.kind != NodeKind.Binary || value.third != 0) return false;
        int operation = get_token(value.token, c).text;
        if (!is_text(operation, text_strict_equal, c) && !is_text(operation, text_strict_not_equal, c)) return false;
        return il_pure_parameter_expression(value.left, parameters, ref remaining, c) &&
            il_pure_parameter_expression(value.right, parameters, ref remaining, c);
    }
}
