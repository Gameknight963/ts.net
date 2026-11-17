namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> fixed_object_layouts = [];
}

internal static partial class CompilerFunctions
{
    internal static int il_fixed_object_layout(int entry, Compiler c, IlBackend b)
    {
        Node declaration = get_node(entry, c);
        if (declaration.kind != NodeKind.Variable || declaration.left == 0 || declaration.fourth != 0 ||
            b.written_symbols.at(declaration.symbol) != 0) return 0;
        Node literal = get_node(declaration.left, c);
        if (literal.kind != NodeKind.Object || il_object_needs_entries(declaration.left, c) ||
            b.classes.at(literal.type) == 0 || get_type(literal.type, c).name != 0 ||
            b.literal_constructors.at(declaration.left) != 0) return 0;
        return literal.type;
    }

    internal static bool il_fixed_object_fits(int entry, int target, Compiler c, IlBackend b)
    {
        Node value = get_node(entry, c);
        return value.kind == NodeKind.Name && get_type(target, c).kind == TypeKind.Shape &&
            get_type(target, c).name == 0 && b.fixed_object_layouts.TryGetValue(value.symbol, out int layout) &&
            (fits(layout, target, c) || il_assignment_fits(layout, target, c, b));
    }
}
