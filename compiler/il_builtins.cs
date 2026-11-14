namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    // Keep these positions in sync with Js.BuiltinNamespaceById.
    internal static readonly TextBytes[] builtin_namespace_names =
    {
        "Promise", "Map", "Set", "Array", "Object", "Number", "String",
        "Boolean", "Math", "Reflect", "JSON", "console", "performance"
    };
    internal static bool il_number_call(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c), target = get_node(call.left, c);
        if (target.kind != NodeKind.BuiltinNamespace || !is_text(get_token(target.token, c).text, "Number", c) || call.children == 0) return false;
        Node argument = get_node(call.children, c);
        if (argument.next != 0 || argument.kind == NodeKind.Spread) return false;
        // The binding is protected. Still evaluate it before the argument.
        il_expression(call.left, c, b); b.op(0x26);
        if (get_type(argument.type, c).kind == TypeKind.Number) il_number(call.children, c, b);
        else { il_boxed(call.children, c, b); b.call(RuntimeMember.ExplicitNumber); }
        b.call(RuntimeMember.WrapNumber); return true;
    }
    internal static void il_builtin_namespace(int name, Compiler c, IlBackend b)
    {
        for (int id = 0; id < builtin_namespace_names.Length; id++)
        {
            if (!is_text(name, builtin_namespace_names[id], c)) continue;
            b.emit(0x20, id); b.call(RuntimeMember.BuiltinNamespaceById); return;
        }
        il_text(name, c, b); b.call(RuntimeMember.BuiltinNamespace);
    }
}
