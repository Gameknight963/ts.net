namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    internal static readonly TextBytes promise_then_name = "then";
    internal static bool il_promise_then_call(int entry, Compiler c)
    {
        Node call = get_node(entry, c);
        int argument = call.children;
        if (argument == 0 || get_node(argument, c).next != 0 || il_call_has_spread(entry, c)) return false;
        if (get_type(call.type, c).kind == TypeKind.Number && get_type(get_node(argument, c).type, c).kind == TypeKind.Number) return false;
        Node target = get_node(call.left, c);
        while (target.kind == NodeKind.Assert) target = get_node(target.left, c);
        int name = target.kind == NodeKind.Property ? target.name
            : target.kind == NodeKind.Index && get_node(target.right, c).kind == NodeKind.String ? get_token(get_node(target.right, c).token, c).text : 0;
        return name != 0 && is_text(name, promise_then_name, c);
    }
    internal static void il_emit_promise_then_call(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c);
        int receiver = il_method_target(call.left, c, b);
        b.emit(0xfe0c, receiver); il_boxed(call.children, c, b);
        b.call(RuntimeMember.CallPromiseThen); il_function_result(call.type, c, b);
    }
}
