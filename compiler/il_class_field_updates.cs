namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<(int Type, int Index), int> class_field_updates = [];
}

internal static partial class CompilerFunctions
{
    static int il_class_field_update(int entry, Compiler c, IlBackend b)
    {
        Node property = get_node(entry, c);
        if (property.kind != NodeKind.Property || property.fourth != 0) return 0;
        int owner = il_class_field_owner(property.left, c, b);
        if (owner == 0) return 0;
        var members = b.numeric_class_members[owner];
        for (int index = 0; index < members.Count; index++)
        {
            Field field = get_field(members[index], c);
            if (!same(field.name, property.name, c) || get_type(field.type, c).kind != TypeKind.Number) continue;
            var key = (owner, index);
            if (!b.class_field_updates.TryGetValue(key, out int method))
            {
                method = b.plan.method(5, b.classes.at(owner), 0x16, -10, new int[] {-5, -3, -2}, "Update_number_" + index);
                b.class_field_updates.Add(key, method);
            }
            return method;
        }
        return 0;
    }

    static void il_class_field_update_bodies(Compiler c, IlBackend b)
    {
        foreach (var entry in b.class_field_updates)
        {
            var (type, index) = entry.Key;
            Field field = get_field(b.numeric_class_members[type][index], c);
            int klass = b.classes.at(type), token = b.class_field_tokens[type][index];
            b.begin(entry.Value);
            int instance = b.local(klass), previous = b.local(-3), next = b.local(-3);
            int fallback = b.label(), prefix = b.label(), ready = b.label();
            b.emit(0xfe09, 0); b.emit(0x75, klass); b.emit(0xfe0e, instance);
            b.emit(0xfe0c, instance); b.emit(0x39, fallback);
            b.emit(0xfe0c, instance); b.emit(0x20, index); b.call(RuntimeMember.NumericClassState);
            b.emit(0x20, 3); b.op(0xfe01); b.emit(0x39, fallback);
            // A defined numeric data field stays defined. No user code runs
            // between this read and write, so one property-state check suffices.
            b.emit(0xfe0c, instance); b.emit(0x7b, token); b.emit(0xfe0e, previous);
            b.emit(0xfe0c, previous); b.emit(0xfe09, 1); b.op(0x58); b.emit(0xfe0e, next);
            b.emit(0xfe0c, instance); b.emit(0xfe0c, next); b.emit(0x7d, token);
            b.emit(0xfe09, 2); b.emit(0x39, prefix);
            b.emit(0xfe0c, previous); b.emit(0x38, ready);
            b.mark(prefix); b.emit(0xfe0c, next); b.mark(ready);
            b.call(RuntimeMember.WrapNumber); b.op(0x2a);
            b.mark(fallback); b.emit(0xfe09, 0); il_text(field.name, c, b);
            b.emit(0xfe09, 1); b.emit(0xfe09, 2); b.call(RuntimeMember.AddNumberProperty); b.op(0x2a);
        }
    }
}
