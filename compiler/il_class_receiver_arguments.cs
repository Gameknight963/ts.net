namespace Tsnet.Compiler;

internal sealed partial class IlBackend { internal HashSet<int> class_receiver_functions; }

internal static partial class CompilerFunctions
{
    internal static int il_class_receiver_arguments(int symbol, Compiler c, IlBackend b)
    {
        if (b.class_receiver_functions == null)
        {
            b.class_receiver_functions = new();
            for (int i = 1; i <= c.node_store.count(); i++)
            {
                Node node = get_node(i, c);
                if (node.kind != NodeKind.ClassValue) continue;
                il_add_class_receiver_function(node.left, c, b);
                il_add_class_receiver_function(node.right, c, b);
            }
        }
        if (!b.class_receiver_functions.Contains(symbol)) return -1;
        Node function = get_node(get_symbol(symbol, c).function, c);
        if (function.fourth == 0 || function.children != function.fourth || function.right != 0 ||
            function.internal_continuation || function.left == 0) return -1;
        Node receiver = get_node(function.fourth, c);
        if (receiver.name != 0) return -1;
        if (receiver.next == 0) return 0;
        Node parameter = get_node(receiver.next, c);
        return parameter.kind == NodeKind.Variable && parameter.symbol != 0 && !parameter.constant && parameter.next == 0 &&
            get_type(parameter.type, c).kind is (TypeKind.Number or TypeKind.Any or TypeKind.Unknown) && parameter.left == 0 && parameter.right == 0 &&
            parameter.third == 0 && parameter.fourth == 0 && parameter.children == 0 ? 1 : -1;
    }

    internal static void il_add_class_receiver_function(int entry, Compiler c, IlBackend b)
    {
        if (entry == 0) return;
        Node value = get_node(entry, c);
        if (value.kind is NodeKind.Function or NodeKind.FunctionValue || value.kind == NodeKind.Name && value.symbol != 0 && get_symbol(value.symbol, c).function != 0)
            b.class_receiver_functions.Add(value.symbol);
    }

    internal static void il_class_receiver_argument_methods(int symbol, int count, int type, int context, int environment, Compiler c, IlBackend b)
    {
        Node function = get_node(get_symbol(symbol, c).function, c);
        for (int arity = 0; arity <= 1; arity++)
        {
            b.begin(arity == 0 ? b.plan.method1(5, type, 0xc6, -5, -5, "InvokeReceiver0")
                : b.plan.method2(5, type, 0xc6, -5, -5, -10, "InvokeReceiverNumber1"));
            if (context != 0) { b.emit(0xfe09, 0); b.emit(0x7b, environment); }
            b.emit(0xfe09, 0); b.emit(0xfe09, 1); b.call(RuntimeMember.PrepareReceiver);
            b.from_object(get_node(function.fourth, c).type, c);
            if (count != 0)
            {
                int parameterType = get_node(get_node(function.fourth, c).next, c).type;
                if (arity != 0)
                {
                    b.emit(0xfe09, 2);
                    if (get_type(parameterType, c).kind != TypeKind.Number) b.call(RuntimeMember.BoxNumber);
                }
                else b.empty(parameterType, c);
            }
            b.emit(0x28, b.function(symbol));
            if (get_type(function.type, c).kind == TypeKind.Void) b.call(RuntimeMember.Undefined);
            else b.box(function.type, c);
            b.op(0x2a);
        }
    }
}
