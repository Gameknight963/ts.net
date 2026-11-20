namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal int addition_value_type, addition_from_value, addition_from_number, addition_read_property;
    internal int addition_add, addition_add_property, addition_checked, addition_to_number;
}

internal static partial class CompilerFunctions
{
    static bool il_any_addition(int entry, Compiler c)
    {
        Node node = get_node(entry, c);
        return node.kind == NodeKind.Binary && get_type(node.type, c).kind == TypeKind.Any &&
            is_text(get_token(node.token, c).text, text_plus, c);
    }
    static bool il_addition_chain(int entry, Compiler c)
    {
        // Bound emitted code size; other expressions keep ordinary addition.
        int count = 0;
        bool Visit(int item) {
            if (!il_any_addition(item, c)) return true;
            if (++count > 16) return false;
            Node node = get_node(item, c);
            return Visit(node.left) && Visit(node.right);
        }
        return Visit(entry) && count >= 2;
    }
    static void il_addition_runtime(IlBackend b)
    {
        if (b.addition_value_type != 0) return;
        int value = b.addition_value_type = b.plan.reference("Tsnet.Runtime", "", "JsAdditionValue", valueType: true);
        int js = b.RuntimeToken(RuntimeMember.JsType);
        b.addition_from_value = b.plan.method1(6, js, 0, value, -5, "AdditionFromValue");
        b.addition_from_number = b.plan.method1(6, js, 0, value, -10, "AdditionFromNumber");
        b.addition_read_property = b.plan.method2(6, js, 0, value, -5, -4, "ReadAdditionProperty");
        b.addition_add = b.plan.method2(6, js, 0, value, value, value, "AddAdditionValues");
        b.addition_add_property = b.plan.method(6, js, 0, value, [value, -5, -4], "AddAdditionProperty");
        b.addition_checked = b.plan.method1(6, js, 0, -10, value, "CheckedAdditionNumber");
        b.addition_to_number = b.plan.method2(6, js, 0, -10, -10, value, "AddAdditionToNumber");
    }
    static void il_addition_value(int entry, Compiler c, IlBackend b)
    {
        il_addition_runtime(b);
        Node node = get_node(entry, c);
        if (il_any_addition(entry, c)) {
            // Retain the original tree and coercion order, including parenthesized sums.
            il_addition_value(node.left, c, b);
            Node right = get_node(node.right, c);
            if (right.kind == NodeKind.Property && right.fourth == 0 &&
                get_type(right.type, c).kind is TypeKind.Any or TypeKind.Unknown) {
                il_boxed(right.left, c, b); il_text(right.name, c, b); b.emit(0x28, b.addition_add_property);
            }
            else { il_addition_value(node.right, c, b); b.emit(0x28, b.addition_add); }
        }
        else if (get_type(node.type, c).kind == TypeKind.Number) {
            il_expression(entry, c, b); b.emit(0x28, b.addition_from_number);
        }
        else if (node.kind == NodeKind.Property && node.fourth == 0 &&
            get_type(node.type, c).kind is TypeKind.Any or TypeKind.Unknown) {
            il_boxed(node.left, c, b); il_text(node.name, c, b); b.emit(0x28, b.addition_read_property);
        }
        else { il_boxed(entry, c, b); b.emit(0x28, b.addition_from_value); }
    }
}
