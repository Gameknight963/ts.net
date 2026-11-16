namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    internal readonly HashSet<int> nullable_number_fields = [];
}

internal static partial class CompilerFunctions
{
    internal static void il_choose_number_field_storage(Compiler c, IlBackend b)
    {
        var writtenShapes = new HashSet<int>();
        var repeatedLayouts = new HashSet<int>();
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if (n.kind == NodeKind.Object)
            {
                for (int parent = b.node_parents.at(i); parent != 0; parent = b.node_parents.at(parent))
                {
                    Node owner = get_node(parent, c);
                    if (owner.kind == NodeKind.Function) break;
                    if (owner.kind is NodeKind.For or NodeKind.ForOf or NodeKind.ForIn or NodeKind.While or NodeKind.Do)
                    {
                        repeatedLayouts.Add(n.type);
                        break;
                    }
                }
            }
            if (n.kind is not (NodeKind.Assign or NodeKind.Update or NodeKind.ForOf)) continue;
            Node target = get_node(n.left, c);
            while (target.kind == NodeKind.Assert) target = get_node(target.left, c);
            if (target.kind is not (NodeKind.Property or NodeKind.Index) || target.fourth != 0 || get_type(target.type, c).kind != TypeKind.Number) continue;
            int receiver = get_node(target.left, c).type;
            if (get_type(receiver, c).kind == TypeKind.Shape) writtenShapes.Add(receiver);
        }
        // This selects a layout, not a correctness proof: compact fields still
        // support later dynamic writes. Mutable layouts favor ordinary CLR
        // nullable fields. Layouts created repeatedly in loops, including
        // compatible spread sources, keep their smaller storage.
        for (int type = c.types; type != 0; type = get_type(type, c).next)
        {
            if (get_type(type, c).kind != TypeKind.Shape) continue;
            bool repeated = false;
            foreach (int layout in repeatedLayouts)
                if (type == layout || il_same_spread_fields(type, layout, c)) { repeated = true; break; }
            if (repeated) continue;
            bool mutable = false;
            foreach (int target in writtenShapes)
                if (type == target || fits(type, target, c)) { mutable = true; break; }
            if (!mutable) continue;
            for (int member = get_type(type, c).fields; member != 0; member = get_field(member, c).next)
                if (get_type(get_field(member, c).type, c).kind == TypeKind.Number) b.nullable_number_fields.Add(member);
        }
    }
}
