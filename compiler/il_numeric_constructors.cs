namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    internal static bool il_numeric_constructor(int symbol, Compiler c, IlBackend b)
    {
        int entry = get_symbol(symbol, c).function;
        Node function = get_node(entry, c);
        if (get_type(function.type, c).kind is not (TypeKind.Void or TypeKind.Any or TypeKind.Unknown) || function.right != 0 || function.internal_continuation ||
            function.children == 0 || function.left == 0) return false;
        int parameter = function.children;
        if (function.fourth != 0)
        {
            Node receiver = get_node(function.fourth, c);
            if (parameter != function.fourth || receiver.constant || receiver.name != 0 || get_type(receiver.type, c).kind != TypeKind.Any) return false;
            parameter = receiver.next;
        }
        if (parameter == 0) return false;
        Node argument = get_node(parameter, c);
        if (argument.kind != NodeKind.Variable || argument.symbol == 0 || argument.constant || argument.next != 0 ||
            get_type(argument.type, c).kind != TypeKind.Number || argument.left != 0 || argument.right != 0 ||
            argument.third != 0 || argument.fourth != 0 || argument.children != 0) return false;
        // Constructors may return a replacement object. Restrict this entry to
        // bodies whose own returns are always undefined.
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node node = get_node(i, c);
            if (node.kind == NodeKind.Return && node.left != 0 && scalar_owner(i, b.node_parents, c) == entry &&
                get_node(node.left, c).kind != NodeKind.Undefined) return false;
        }
        return true;
    }

    internal static void il_numeric_constructor_method(int symbol, int type, int context, int environment, Compiler c, IlBackend b)
    {
        Node function = get_node(get_symbol(symbol, c).function, c);
        b.begin(b.plan.method1(5, type, 0xc6, -5, -10, "ConstructNumber1"));
        int receiver = b.local(-5);
        b.emit(0xfe09, 0); b.call(RuntimeMember.BeginOrdinaryConstruction); b.emit(0xfe0e, receiver);
        if (context != 0) {b.emit(0xfe09, 0);b.emit(0x7b, environment);}
        if (function.fourth != 0)
        {
            b.emit(0xfe09, 0); b.emit(0xfe0c, receiver); b.call(RuntimeMember.PrepareReceiver);
        }
        b.emit(0xfe09, 1); b.emit(0x28, b.function(symbol));
        if (get_type(function.type, c).kind != TypeKind.Void) b.op(0x26);
        b.emit(0xfe0c, receiver); b.call(RuntimeMember.ResolveThis); b.op(0x2a);
    }
}
