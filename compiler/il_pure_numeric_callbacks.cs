namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    // A single arithmetic return cannot change array storage or prototypes.
    static bool il_pure_numeric_callback(int symbol, Compiler c, IlBackend b)
    {
        Node function = get_node(get_symbol(symbol, c).function, c);
        if (function.left == 0 || function.right != 0 || function.fourth != 0) return false;
        var parameters = new List<(int Argument, int Type, int Symbol)>();
        for (int parameter = function.children; parameter != 0; parameter = get_node(parameter, c).next) {
            Node binding = get_node(parameter, c);
            if (binding.kind != NodeKind.Variable || binding.symbol == 0 || binding.constant || binding.left != 0 ||
                binding.right != 0 || binding.third != 0 || binding.fourth != 0 || binding.children != 0 ||
                get_type(binding.type, c).kind != TypeKind.Number || parameters.Count == 2) return false;
            parameters.Add((0, binding.type, binding.symbol));
        }
        // Reading typed numeric bindings, including captures, cannot execute
        // accessors or change the source. Keep property reads excluded.
        var names = new HashSet<int>(parameters.Select(parameter => parameter.Symbol));
        foreach (int entry in b.FunctionNodes(get_symbol(symbol, c).function, c)) {
            Node reference = get_node(entry, c);
            if (reference.kind == NodeKind.Name && reference.symbol != 0 &&
                get_type(reference.type, c).kind == TypeKind.Number && names.Add(reference.symbol))
                parameters.Add((0, reference.type, reference.symbol));
        }
        Node body = get_node(function.left, c);
        int statement = body.kind == NodeKind.Block ? body.children : function.left;
        if (statement == 0) return false;
        Node returned = get_node(statement, c);
        if (returned.kind != NodeKind.Return || returned.next != 0 || returned.left == 0) return false;
        Node value = get_node(returned.left, c);
        int remaining = 32;
        if (get_type(value.type, c).kind == TypeKind.Number)
            return scalar_constructor_expression(returned.left, TypeKind.Number, parameters, ref remaining, c, b);
        if (get_type(value.type, c).kind != TypeKind.Boolean || value.kind != NodeKind.Binary ||
            value.third != 0 || value.fourth != 0 || value.children != 0) return false;
        int operation = get_token(value.token, c).text;
        bool comparison = is_text(operation, text_less, c) || is_text(operation, text_less_equal, c) ||
            is_text(operation, text_greater, c) || is_text(operation, text_greater_equal, c) ||
            is_text(operation, text_strict_equal, c) || is_text(operation, text_strict_not_equal, c) ||
            is_text(operation, text_loose_equal, c) || is_text(operation, text_loose_not_equal, c);
        return comparison && scalar_constructor_expression(value.left, TypeKind.Number, parameters, ref remaining, c, b) &&
            scalar_constructor_expression(value.right, TypeKind.Number, parameters, ref remaining, c, b);
    }
}
