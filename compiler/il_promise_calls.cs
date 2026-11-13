namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    internal static bool il_promise_static_number_call(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c), target = get_node(call.left, c);
        if (target.kind != NodeKind.Property || target.fourth != 0 || call.children == 0 || il_call_has_spread(entry, c)) return false;
        Node owner = get_node(target.left, c), argument = get_node(call.children, c);
        if (owner.kind != NodeKind.BuiltinNamespace || !is_text(get_token(owner.token, c).text, "Promise", c) ||
            argument.next != 0 || get_type(argument.type, c).kind != TypeKind.Number ||
            !(is_text(target.name, "resolve", c) || is_text(target.name, "reject", c))) return false;
        int receiver = il_method_target(call.left, c, b);
        b.emit(0xfe0c, receiver); il_as(call.children, argument.type, c, b);
        b.call(RuntimeMember.CallPromiseStaticNumber); il_function_result(call.type, c, b); return true;
    }
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
