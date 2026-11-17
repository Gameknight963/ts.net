namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> raw_shape_readers = [];
}

internal static partial class CompilerFunctions
{
    internal static void il_prepare_raw_property_reads(Compiler c, IlBackend b)
    {
        HashSet<int> requested = [];
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node property = get_node(i, c);
            if (property.kind != NodeKind.Property || property.left == 0 || property.type == 0 ||
                property.fourth != 0 || get_type(property.type, c).kind != TypeKind.Number) continue;
            int owner = get_node(property.left, c).type;
            if (owner != 0 && get_type(owner, c).kind == TypeKind.Shape && get_type(owner, c).name == 0)
                requested.Add(field(owner, property.name, c));
        }
        Dictionary<int, List<(int Class, int Field)>> candidates = [];
        for (int type = c.types; type != 0; type = get_type(type, c).next)
        {
            Type record = get_type(type, c);
            // These records are sealed and read their own CLR fields.
            // Mutable layouts and descriptor-backed objects use fallback.
            if (record.kind != TypeKind.Shape || record.name != 0 || b.classes.at(type) == 0 ||
                b.layout_needed.at(type) != 0 || b.spread_types.at(type) != 0 || il_has_dynamic_indices(type, c)) continue;
            for (int stored = record.fields; stored != 0; stored = get_field(stored, c).next)
            {
                Field property = get_field(stored, c);
                if (get_type(property.type, c).kind != TypeKind.Number || b.field_storage(stored, c) != -3 ||
                    il_dynamic_index_field(stored, c)) continue;
                if (!candidates.TryGetValue(property.name, out var layouts)) candidates[property.name] = layouts = [];
                // Keep readers small enough to inline; every other layout has fallback.
                if (layouts.Count < 2) layouts.Add((b.classes.at(type), b.field_token(stored)));
            }
        }
        for (int owner = c.types; owner != 0; owner = get_type(owner, c).next)
        {
            Type shape = get_type(owner, c);
            if (shape.kind != TypeKind.Shape || shape.name != 0) continue;
            for (int member = shape.fields; member != 0; member = get_field(member, c).next)
            {
                Field property = get_field(member, c);
                if (get_type(property.type, c).kind != TypeKind.Number || !requested.Contains(member)) continue;
                if (!candidates.TryGetValue(property.name, out var layouts)) continue;
                int method = b.plan.method1(5, b.interface_type(owner), 0x16, -3, -5,
                    field_name(property.name, "ReadNumber_p_", c).view());
                b.raw_shape_readers[member] = method; b.begin(method);
                foreach (var layout in layouts)
                {
                    int next = b.label(), instance = b.local(layout.Class);
                    b.emit(0xfe09, 0); b.emit(0x75, layout.Class); b.emit(0xfe0e, instance);
                    b.emit(0xfe0c, instance); b.emit(0x39, next);
                    // Both missing numbers and numeric NaNs are NaN in arithmetic.
                    b.emit(0xfe0c, instance); b.emit(0x7b, layout.Field); b.op(0x2a);
                    b.mark(next);
                }
                b.emit(0xfe09, 0); il_shape_read(owner, member, b);
                b.call(RuntimeMember.NumberValue); b.op(0x2a);
            }
        }
    }

    internal static bool il_raw_property_number(int entry, Compiler c, IlBackend b)
    {
        Node property = get_node(entry, c);
        if (property.kind == NodeKind.Assert && get_type(property.type, c).kind == TypeKind.Number &&
            get_type(get_node(property.left, c).type, c).kind == TypeKind.Number)
            return il_raw_property_number(property.left, c, b);
        if (property.kind != NodeKind.Property || property.fourth != 0 ||
            get_type(property.type, c).kind != TypeKind.Number || il_scalar_field(entry, c, b).slot >= 0) return false;
        int owner = get_node(property.left, c).type;
        if (get_type(owner, c).kind != TypeKind.Shape || get_type(owner, c).name != 0) return false;
        int member = field(owner, property.name, c);
        if (!b.raw_shape_readers.TryGetValue(member, out int method)) return false;
        il_expression(property.left, c, b); b.emit(0x28, method); return true;
    }
}
