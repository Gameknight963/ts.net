namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    internal static bool il_has_object_rest(int entry, Compiler c)
    {
        for (int child = get_node(entry, c).children; child != 0; child = get_node(child, c).next)
            if (get_node(child, c).kind == NodeKind.Binding && get_node(child, c).constant) return true;
        return false;
    }
    internal static void il_object_rest_bindings(int entry, int source, Compiler c, IlBackend b)
    {
        Node pattern = get_node(entry, c); int count = 0;
        for (int child = pattern.children; child != 0; child = get_node(child, c).next) if (!get_node(child, c).constant) count++;
        b.emit(0xfe0c, source); b.call(RuntimeMember.RequireObject);
        b.emit(0x20, count); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType)); int excluded = b.local(-7); b.emit(0xfe0e, excluded);
        int index = 0;
        for (int child = pattern.children; child != 0; child = get_node(child, c).next)
        {
            Node binding = get_node(child, c);
            if (binding.constant)
            {
                b.emit(0x73, b.spread_any_constructor); int destination = b.local(-5); b.emit(0xfe0e, destination);
                b.emit(0xfe0c, destination); b.emit(0xfe0c, source); b.emit(0xfe0c, excluded); b.call(RuntimeMember.CopyObjectRest);
                b.emit(0xfe0c, destination); b.from_object(binding.type, c);
            }
            else
            {
                // Coerce each key once, before reading its value or running defaults.
                il_boxed(binding.third, c, b); b.call(RuntimeMember.PropertyKey); int key = b.local(-4); b.emit(0xfe0e, key);
                b.emit(0xfe0c, excluded); b.emit(0x20, index++); b.emit(0xfe0c, key); b.op(0xa2);
                b.emit(0xfe0c, source); b.emit(0xfe0c, key); b.call(RuntimeMember.IndexGet); b.from_object(binding.type, c);
            }
            il_finish_binding(child, c, b);
        }
    }
}
