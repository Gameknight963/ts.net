namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> fixed_object_layouts = [];
}

internal static partial class CompilerFunctions
{
    internal static int il_fixed_object_layout(int entry, Vector<int> declarations, HashSet<int> repeated,
        Dictionary<int, int> layouts, Compiler c, IlBackend b)
    {
        HashSet<int> visited = [];
        Node declaration;
        for (;;)
        {
            declaration = get_node(entry, c);
            if (layouts.TryGetValue(declaration.symbol, out int known)) return finish(known);
            if (!visited.Add(declaration.symbol) || repeated.Contains(declaration.symbol) ||
                declaration.kind != NodeKind.Variable || declaration.left == 0 || declaration.fourth != 0 ||
                b.written_symbols.at(declaration.symbol) != 0) return finish(0);
            Node source = get_node(declaration.left, c);
            if (source.kind != NodeKind.Name || source.symbol == 0) break;
            entry = declarations.at(source.symbol);
            if (entry == 0) return finish(0);
        }
        Node literal = get_node(declaration.left, c);
        if (literal.kind != NodeKind.Object || il_object_needs_entries(declaration.left, c) ||
            b.classes.at(literal.type) == 0 || get_type(literal.type, c).name != 0 ||
            b.literal_constructors.at(declaration.left) != 0) return finish(0);
        return finish(literal.type);
        int finish(int layout)
        {
            foreach (int symbol in visited) layouts[symbol] = layout;
            return layout;
        }
    }

    internal static bool il_fixed_object_fits(int entry, int target, Compiler c, IlBackend b)
    {
        Node value = get_node(entry, c);
        return value.kind == NodeKind.Name && get_type(target, c).kind == TypeKind.Shape &&
            get_type(target, c).name == 0 && b.fixed_object_layouts.TryGetValue(value.symbol, out int layout) &&
            (fits(layout, target, c) || il_assignment_fits(layout, target, c, b));
    }
}
