namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    // Keep these positions in sync with Js.BuiltinNamespaceById.
    internal static readonly TextBytes[] builtin_namespace_names =
    {
        "Promise", "Map", "Set", "Array", "Object", "Number", "String",
        "Boolean", "Math", "Reflect", "JSON", "console", "performance"
    };
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
