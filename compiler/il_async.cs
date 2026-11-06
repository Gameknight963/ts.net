namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    // Both the callable and its receiver were captured before arguments awaited.
    internal static void il_saved_call(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c); il_boxed(n.left, c, b);
        if (n.right != 0) il_boxed(n.right, c, b);
        if (il_call_has_spread(entry, c))
        {
            b.emit(0x20, 0); b.emit(0x73, b.RuntimeToken(RuntimeMember.ArrayConstructor)); int arguments = b.local(-5); b.emit(0xfe0e, arguments);
            int child = n.children;
            while (child != 0) { il_append_call_argument(child, arguments, 0, c, b); child = get_node(child, c).next; }
            b.emit(0xfe0c, arguments); b.call(n.right != 0 ? RuntimeMember.CallMethodArguments : RuntimeMember.CallFunctionArguments); il_function_result(n.type, c, b); return;
        }
        int count = 0, argument = n.children;
        while (argument != 0) { count++; argument = get_node(argument, c).next; }
        b.emit(0x20, count); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType)); argument = n.children; int index = 0;
        while (argument != 0) { b.op(0x25); b.emit(0x20, index++); il_boxed(argument, c, b); b.op(0xa2); argument = get_node(argument, c).next; }
        b.call(n.right != 0 ? RuntimeMember.CallMethod : RuntimeMember.CallFunction); il_function_result(n.type, c, b);
    }
}
