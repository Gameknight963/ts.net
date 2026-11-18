namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    // These expressions cannot invoke user code or inspect earlier field writes,
    // so direct construction may compute each final field independently.
    static bool scalar_constructor_expression(int entry, TypeKind kind,
        List<(int Argument, int Type, int Symbol)> parameters, ref int remaining, Compiler c)
    {
        if (--remaining < 0 || entry == 0) return false;
        Node value = get_node(entry, c);
        // Omitted defaults can use narrower argument storage than the name's
        // IR type. The parameter plan supplies both the proof and emitted type.
        if (value.kind == NodeKind.Name)
            return parameters.Any(parameter => parameter.Symbol == value.symbol && get_type(parameter.Type, c).kind == kind);
        if (value.type == 0 || get_type(value.type, c).kind != kind) return false;
        if (kind != TypeKind.Number) return false;
        if (scalar_class_literal(entry, kind, c)) return true;
        if (value.kind is not (NodeKind.Binary or NodeKind.Unary) || value.third != 0 || value.fourth != 0 || value.children != 0) return false;
        int operation = get_token(value.token, c).text;
        bool sign = is_text(operation, text_plus, c) || is_text(operation, text_minus, c);
        if (value.kind == NodeKind.Unary)
            return sign && value.right == 0 && scalar_constructor_expression(value.left, kind, parameters, ref remaining, c);
        return (sign || is_text(operation, text_star, c) || is_text(operation, text_slash, c)) &&
            scalar_constructor_expression(value.left, kind, parameters, ref remaining, c) &&
            scalar_constructor_expression(value.right, kind, parameters, ref remaining, c);
    }

    static int il_scalar_constructor_expression(int entry, Dictionary<int, (int Slot, int Type)> parameters, Compiler c, IlBackend b)
    {
        Node value = get_node(entry, c);
        if (value.kind == NodeKind.Name) {
            var parameter = parameters[value.symbol];
            b.emit(0xfe0c, parameter.Slot); return parameter.Type;
        }
        if (scalar_class_literal(entry, TypeKind.Number, c)) { il_expression(entry, c, b); return value.type; }
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
