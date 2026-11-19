namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    // These expressions have no observable effects or reads of earlier fields,
    // so direct construction may compute each final field independently.
    static bool scalar_constructor_expression(int entry, TypeKind kind,
        List<(int Argument, int Type, int Symbol)> parameters, ref int remaining, Compiler c, IlBackend b, bool readNumericBindings = false)
    {
        if (--remaining < 0 || entry == 0) return false;
        Node value = get_node(entry, c);
        // Omitted defaults can use narrower argument storage than the name's
        // IR type. The parameter plan supplies both the proof and emitted type.
        if (value.kind == NodeKind.Name)
            return parameters.Any(parameter => parameter.Symbol == value.symbol && get_type(parameter.Type, c).kind == kind) ||
                readNumericBindings && kind == TypeKind.Number && value.symbol != 0 &&
                get_type(value.type, c).kind == TypeKind.Number;
        if (value.type == 0 || get_type(value.type, c).kind != kind) return false;
        if (kind != TypeKind.Number) return false;
        if (scalar_class_literal(entry, kind, c)) return true;
        if (value.kind == NodeKind.Call) {
            int target = pure_constructor_helper(entry, c, b);
            if (target == 0) return false;
            Node function = get_node(get_symbol(target, c).function, c);
            var bindings = new List<(int Argument, int Type, int Symbol)>();
            int argument = value.children;
            for (int parameter = function.children; parameter != 0; parameter = get_node(parameter, c).next) {
                Node binding = get_node(parameter, c);
                if (argument == 0 || binding.kind != NodeKind.Variable || binding.symbol == 0 || binding.constant ||
                    binding.left != 0 || binding.right != 0 || binding.third != 0 || binding.fourth != 0 || binding.children != 0 ||
                    get_type(binding.type, c).kind != TypeKind.Number ||
                    !scalar_constructor_expression(argument, TypeKind.Number, parameters, ref remaining, c, b, readNumericBindings)) return false;
                bindings.Add((argument, binding.type, binding.symbol));
                argument = get_node(argument, c).next;
            }
            if (argument != 0) return false;
            Node body = get_node(function.left, c);
            int statement = body.kind == NodeKind.Block ? body.children : function.left;
            if (statement == 0) return false;
            Node returned = get_node(statement, c);
            return returned.kind == NodeKind.Return && returned.next == 0 && returned.left != 0 &&
                scalar_constructor_expression(returned.left, TypeKind.Number, bindings, ref remaining, c, b);
        }

        if (value.kind is not (NodeKind.Binary or NodeKind.Unary) || value.third != 0 || value.fourth != 0 || value.children != 0) return false;
        int operation = get_token(value.token, c).text;
        bool sign = is_text(operation, text_plus, c) || is_text(operation, text_minus, c);
        if (value.kind == NodeKind.Unary)
            return sign && value.right == 0 && scalar_constructor_expression(value.left, kind, parameters, ref remaining, c, b, readNumericBindings);
        return (sign || is_text(operation, text_star, c) || is_text(operation, text_slash, c)) &&
            scalar_constructor_expression(value.left, kind, parameters, ref remaining, c, b, readNumericBindings) &&
            scalar_constructor_expression(value.right, kind, parameters, ref remaining, c, b, readNumericBindings);
    }

    // Only stable, uncaptured numeric helpers with a single arithmetic return
    // qualify. The shared expression budget also rejects recursive call cycles.
    static int pure_constructor_helper(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c);
        if (call.left == 0 || call.right != 0 || call.third != 0 || call.fourth != 0) return 0;
        Node reference = get_node(call.left, c);
        int target = il_direct_function(call.left, c, b);
        if (target == 0 || reference.kind != NodeKind.Name || reference.symbol != target ||
            b.written_symbols.at(target) != 0 || b.function_contexts.at(target) != 0) return 0;
        Node function = get_node(get_symbol(target, c).function, c);
        return function.left != 0 && function.right == 0 && function.fourth == 0 &&
            get_type(function.type, c).kind == TypeKind.Number ? target : 0;
    }

    static int il_scalar_constructor_expression(int entry, Dictionary<int, (int Slot, int Type)> parameters, Compiler c, IlBackend b)
    {
        Node value = get_node(entry, c);
        if (value.kind == NodeKind.Name) {
            var parameter = parameters[value.symbol];
            b.emit(0xfe0c, parameter.Slot); return parameter.Type;
        }
        if (scalar_class_literal(entry, TypeKind.Number, c)) { il_expression(entry, c, b); return value.type; }
        if (value.kind == NodeKind.Call) {
            int target = pure_constructor_helper(entry, c, b);
            Node function = get_node(get_symbol(target, c).function, c);
            int parameter = function.children;
            for (int argument = value.children; argument != 0; argument = get_node(argument, c).next) {
                int actual = il_scalar_constructor_expression(argument, parameters, c, b);
                b.convert(actual, get_node(parameter, c).type, c);
                parameter = get_node(parameter, c).next;
            }
            b.emit(0x28, b.function(target));
            return function.type;
        }
        il_scalar_constructor_expression(value.left, parameters, c, b); b.call(RuntimeMember.NumberValue);
        int operation = get_token(value.token, c).text;
        if (value.kind == NodeKind.Unary) {
            if (is_text(operation, text_minus, c)) b.op(0x65);
        }
        else {
            il_scalar_constructor_expression(value.right, parameters, c, b); b.call(RuntimeMember.NumberValue);
            b.op(is_text(operation, text_plus, c) ? 0x58 : is_text(operation, text_minus, c) ? 0x59 : is_text(operation, text_star, c) ? 0x5a : 0x5b);
        }
        b.call(RuntimeMember.WrapNumber);
        return value.type;
    }
}
