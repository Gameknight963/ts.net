namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal HashSet<int> class_receiver_functions;
    internal readonly HashSet<int> object_home_functions = [];
}

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
                if (node.kind is not (NodeKind.ClassValue or NodeKind.Object)) continue;
                if (node.kind == NodeKind.ClassValue) {
                    il_add_class_receiver_function(node.left, c, b);
                    il_add_class_receiver_function(node.right, c, b);
                }
                for (int member = node.children; member != 0; member = get_node(member, c).next) {
                    Node property = get_node(member, c);
                    bool home = node.kind == NodeKind.Object && property.fourth != 0 &&
                        get_node(property.fourth, c).kind != NodeKind.Undefined;
                    il_add_class_receiver_function(property.left, c, b, home);
                }
            }
        }
        if (!b.class_receiver_functions.Contains(symbol)) return -1;
        Node function = get_node(get_symbol(symbol, c).function, c);
        if (function.fourth == 0 || function.children != function.fourth || function.right != 0 ||
            function.internal_continuation || function.left == 0) return -1;
        Node receiver = get_node(function.fourth, c);
        if (receiver.name != 0) return -1;
        int count = 0; bool numbers = true;
        for (int entry = receiver.next; entry != 0; entry = get_node(entry, c).next) {
            Node parameter = get_node(entry, c);
            if (++count > 2 || parameter.kind != NodeKind.Variable || parameter.symbol == 0 || parameter.constant ||
                get_type(parameter.type, c).kind == TypeKind.Void || parameter.left != 0 || parameter.right != 0 ||
                parameter.third != 0 || parameter.fourth != 0 || parameter.children != 0) return -1;
            numbers &= get_type(parameter.type, c).kind == TypeKind.Number;
        }
        return count == 2 && !numbers ? -1 : count;
    }

    internal static void il_add_class_receiver_function(int entry, Compiler c, IlBackend b, bool home = false)
    {
        if (entry == 0) return;
        Node value = get_node(entry, c);
        if (value.kind is NodeKind.Function or NodeKind.FunctionValue || value.kind == NodeKind.Name && value.symbol != 0 && get_symbol(value.symbol, c).function != 0)
        {
            b.class_receiver_functions.Add(value.symbol);
            if (home) b.object_home_functions.Add(value.symbol);
        }
    }

    internal static void il_method_array_arguments(int symbol, int type, int context, int environment, Compiler c, IlBackend b)
    {
        Node function = get_node(get_symbol(symbol, c).function, c);
        b.begin(b.plan.method2(5, type, 0xc6, -5, -5, -7, "InvokeWithReceiver"));
        if (context != 0) { b.emit(0xfe09, 0); b.emit(0x7b, environment); }
        b.emit(0xfe09, 1); b.from_object(get_node(function.fourth, c).type, c);
        int index = 0;
        for (int entry = get_node(function.fourth, c).next; entry != 0; entry = get_node(entry, c).next) {
            b.emit(0xfe09, 2); b.emit(0x20, index++); b.call(RuntimeMember.FunctionArgument);
            b.from_object(get_node(entry, c).type, c);
        }
        b.emit(0x28, b.function(symbol));
        if (get_type(function.type, c).kind == TypeKind.Void) b.call(RuntimeMember.Undefined);
        else b.box(function.type, c);
        b.op(0x2a);
    }

    internal static void il_class_receiver_argument_methods(int symbol, int count, int type, int context, int environment, Compiler c, IlBackend b)
    {
        Node function = get_node(get_symbol(symbol, c).function, c);
        // Extra numeric arguments are ignored only when arguments is unobservable.
        for (int form = 0; form <= 3; form++)
        {
            int method;
            if (form == 3) {
                Vector<int> parameters = new(); parameters.push(-5); parameters.push(-10); parameters.push(-10);
                method = b.plan.method(5, type, 0xc6, -5, parameters.view(), "InvokeReceiverNumber2");
            } else method = form == 0 ? b.plan.method1(5, type, 0xc6, -5, -5, "InvokeReceiver0")
                : form == 1 ? b.plan.method2(5, type, 0xc6, -5, -5, -10, "InvokeReceiverNumber1")
                : b.plan.method2(5, type, 0xc6, -5, -5, -5, "InvokeReceiverArgument1");
            b.begin(method);
            if (context != 0) { b.emit(0xfe09, 0); b.emit(0x7b, environment); }
            // Only object methods using super receive a home-object wrapper.
            bool home = b.object_home_functions.Contains(symbol);
            if (home) b.emit(0xfe09, 0);
            b.emit(0xfe09, 1);
            if (home) b.call(RuntimeMember.PrepareReceiver);
            b.from_object(get_node(function.fourth, c).type, c);
            int parameter = get_node(function.fourth, c).next;
            for (int i = 0; i < count; i++, parameter = get_node(parameter, c).next)
            {
                int parameterType = get_node(parameter, c).type;
                if (form == 3 || form != 0 && i == 0)
                {
                    b.emit(0xfe09, i + 2);
                    if (form is 1 or 3 && get_type(parameterType, c).kind != TypeKind.Number) {b.call(RuntimeMember.BoxNumber);b.from_object(parameterType,c);}
                    else if(form == 2) b.from_object(parameterType,c);
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
