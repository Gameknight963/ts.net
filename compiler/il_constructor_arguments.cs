namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    static bool il_zero_constructor(int symbol, Compiler c)
    {
        Node function = get_node(get_symbol(symbol, c).function, c);
        if (get_type(function.type, c).kind is not (TypeKind.Void or TypeKind.Any or TypeKind.Unknown) ||
            function.third == 0 || function.right != 0 || function.internal_continuation || function.left == 0) return false;
        if (function.fourth == 0) return function.children == 0;
        Node receiver = get_node(function.fourth, c);
        return function.children == function.fourth && receiver.kind == NodeKind.Variable &&
            receiver.name == 0 && receiver.next == 0 && get_type(receiver.type, c).kind == TypeKind.Any;
    }

    static void il_zero_constructor_method(int symbol, int type, int context, int environment, Compiler c, IlBackend b)
    {
        Node function = get_node(get_symbol(symbol, c).function, c);
        b.begin(b.plan.method0(5, type, 0xc6, -5, "Construct0"));
        int receiver = b.local(-5);
        b.emit(0xfe09, 0); b.call(RuntimeMember.BeginOrdinaryConstruction); b.emit(0xfe0e, receiver);
        if (context != 0) { b.emit(0xfe09, 0); b.emit(0x7b, environment); }
        if (function.fourth != 0) {
            bool home = b.object_home_functions.Contains(symbol);
            if (home) b.emit(0xfe09, 0);
            b.emit(0xfe0c, receiver);
            if (home) b.call(RuntimeMember.PrepareReceiver);
        }
        b.emit(0x28, b.function(symbol));
        if (get_type(function.type, c).kind == TypeKind.Void || !il_constructor_returns_value(get_symbol(symbol, c).function, c, b)) {
            if (get_type(function.type, c).kind != TypeKind.Void) b.op(0x26);
            b.emit(0xfe0c, receiver); b.call(RuntimeMember.ResolveThis);
        }
        else { b.emit(0xfe0c, receiver); b.call(RuntimeMember.FinishOrdinaryConstruction); }
        b.op(0x2a);
    }

    internal static int il_constructor_parameter(int symbol, Compiler c)
    {
        int entry = get_symbol(symbol, c).function;
        Node function = get_node(entry, c);
        if (get_type(function.type, c).kind is not (TypeKind.Void or TypeKind.Any or TypeKind.Unknown) || function.right != 0 || function.internal_continuation ||
            function.children == 0 || function.left == 0) return 0;
        int parameter = function.children;
        if (function.fourth != 0)
        {
            Node receiver = get_node(function.fourth, c);
            if (parameter != function.fourth || receiver.name != 0 || get_type(receiver.type, c).kind != TypeKind.Any) return 0;
            parameter = receiver.next;
        }
        if (parameter == 0) return 0;
        Node argument = get_node(parameter, c);
        if (argument.kind != NodeKind.Variable || argument.symbol == 0 || argument.constant || argument.next != 0 ||
            get_type(argument.type, c).kind == TypeKind.Void || argument.left != 0 || argument.right != 0 ||
            argument.third != 0 || argument.fourth != 0 || argument.children != 0) return 0;
        return parameter;
    }

    internal static bool il_constructor_returns_value(int entry, Compiler c, IlBackend b)
    {
        if (b.constructor_value_returns == null)
        {
            b.constructor_value_returns = new();
            // Share this scan across all constructors in the source.
            for (int i = 1; i <= c.node_store.count(); i++)
            {
                Node node = get_node(i, c);
                if (node.kind == NodeKind.Return && node.left != 0 && get_node(node.left, c).kind != NodeKind.Undefined)
                    b.constructor_value_returns.Add(scalar_owner(i, b.node_parents, c));
            }
        }
        return b.constructor_value_returns.Contains(entry);
    }

    internal static void il_constructor_methods(int symbol, int parameter, int type, int context, int environment, Compiler c, IlBackend b)
    {
        il_constructor_method(symbol, parameter, type, context, environment, false, c, b);
        if (get_type(get_node(parameter, c).type, c).kind == TypeKind.Number)
            il_constructor_method(symbol, parameter, type, context, environment, true, c, b);
    }

    internal static void il_constructor_method(int symbol, int parameter, int type, int context, int environment, bool numeric, Compiler c, IlBackend b)
    {
        Node function = get_node(get_symbol(symbol, c).function, c);
        b.begin(b.plan.method1(5, type, 0xc6, -5, numeric ? -10 : -5, numeric ? "ConstructNumber1" : "ConstructArgument"));
        int receiver = b.local(-5);
        b.emit(0xfe09, 0); b.call(RuntimeMember.BeginOrdinaryConstruction); b.emit(0xfe0e, receiver);
        if (context != 0) {b.emit(0xfe09, 0);b.emit(0x7b, environment);}
        if (function.fourth != 0)
        {
            bool home = b.object_home_functions.Contains(symbol);
            if (home) b.emit(0xfe09, 0);
            b.emit(0xfe0c, receiver);
            if (home) b.call(RuntimeMember.PrepareReceiver);
        }
        b.emit(0xfe09, 1);
        if (!numeric) b.from_object(get_node(parameter, c).type, c);
        b.emit(0x28, b.function(symbol));
        if (get_type(function.type, c).kind == TypeKind.Void || !il_constructor_returns_value(get_symbol(symbol, c).function, c, b))
        {
            if (get_type(function.type, c).kind != TypeKind.Void) b.op(0x26);
            b.emit(0xfe0c, receiver); b.call(RuntimeMember.ResolveThis);
        }
        else
        {
            b.emit(0xfe0c, receiver); b.call(RuntimeMember.FinishOrdinaryConstruction);
        }
        b.op(0x2a);
    }
}
