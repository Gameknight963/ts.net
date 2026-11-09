namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    internal static readonly TextBytes[] collection_key_methods = { "get", "has", "delete" };
    internal static bool il_collection_key_call(int entry, Compiler c)
    {
        Node call = get_node(entry, c);
        // Preserve the existing unboxed numeric-result ABI.
        if (get_type(call.type, c).kind == TypeKind.Number || il_call_has_spread(entry, c)) return false;
        int argument = call.children;
        if (argument == 0 || get_node(argument, c).next != 0 || get_type(get_node(argument, c).type, c).kind != TypeKind.Number) return false;
        Node target = get_node(call.left, c);
        while (target.kind == NodeKind.Assert) target = get_node(target.left, c);
        int name = target.kind == NodeKind.Property ? target.name
            : target.kind == NodeKind.Index && get_node(target.right, c).kind == NodeKind.String ? get_token(get_node(target.right, c).token, c).text : 0;
        if (name == 0) return false;
        foreach (var candidate in collection_key_methods) if (is_text(name, candidate, c)) return true;
        return false;
    }
    internal static void il_emit_collection_key_call(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c);
        int receiver = il_method_target(call.left, c, b);
        b.emit(0xfe0c, receiver); il_expression(call.children, c, b);
        b.call(RuntimeMember.CallCollectionNumberKey); il_function_result(call.type, c, b);
    }
}
