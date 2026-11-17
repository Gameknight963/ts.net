namespace Tsnet.Compiler;

internal enum ClassFieldWriteForm { Value, Number, String }

internal sealed partial class IlBackend
{
    internal readonly Dictionary<(int Type, int Index, ClassFieldWriteForm Form), int> class_field_writers = [];
    internal Dictionary<int, int> class_field_receivers;
}

internal static partial class CompilerFunctions
{
    static void il_class_field_writer_bodies(Compiler c, IlBackend b)
    {
        // Declare helpers when a write uses them, then emit their bodies after
        // the program. Unused fields do not need either form of the helper.
        foreach (var entry in b.class_field_writers)
        {
            var (type, index, form) = entry.Key;
            bool numeric = form == ClassFieldWriteForm.Number;
            int klass = b.classes.at(type);
            int name = get_field(b.numeric_class_members[type][index], c).name;
            b.begin(entry.Value);
            int instance = b.local(klass), fallback = b.label();
            b.emit(0xfe09, 0); b.emit(0x75, klass); b.emit(0xfe0e, instance);
            b.emit(0xfe0c, instance); b.emit(0x39, fallback);
            // The runtime guard rejects missing or materialized fields. Those
            // writes must still observe descriptors and inherited setters.
            b.emit(0xfe0c, instance); b.emit(0x20, index); b.emit(0xfe09, 1);
            RuntimeMember store = form switch {
                ClassFieldWriteForm.Number => RuntimeMember.TrySetClassNumber,
                ClassFieldWriteForm.String => RuntimeMember.TrySetClassString,
                _ => RuntimeMember.TrySetClassValue
            };
            b.call(store);
            b.emit(0x39, fallback); b.op(0x2a);
            b.mark(fallback); b.emit(0xfe09, 0); il_text(name, c, b); b.emit(0xfe09, 1);
            b.call(numeric ? RuntimeMember.DynamicSetNumber : RuntimeMember.DynamicSet);
            if (!numeric) b.op(0x26);
            b.op(0x2a);
        }
    }

    static void il_class_field_receiver(int entry, int type, Compiler c, IlBackend b)
    {
        int function = scalar_field_function(entry, c);
        if (function == 0 || get_node(function, c).fourth == 0) return;
        int symbol = get_node(get_node(function, c).fourth, c).symbol;
        if (symbol != 0) b.class_field_receivers[symbol] = type;
    }

    static int il_class_field_owner(int receiverEntry, Compiler c, IlBackend b)
    {
        int owner = get_node(receiverEntry, c).type;
        if (!il_numeric_class_eligible(owner, c, b))
        {
            if (b.class_field_receivers == null)
            {
                b.class_field_receivers = [];
                for (int i = 1; i <= c.node_store.count(); i++)
                {
                    var node = get_node(i, c);
                    if (node.kind != NodeKind.ClassValue || node.fourth == 0) continue;
                    int type = get_node(node.fourth, c).type;
                    if (!il_numeric_class_eligible(type, c, b)) continue;
                    il_class_field_receiver(node.left, type, c, b);
                    il_class_field_receiver(node.right, type, c, b);
                    for (int child = node.children; child != 0; child = get_node(child, c).next)
                        if (!get_node(child, c).constant) il_class_field_receiver(get_node(child, c).left, type, c, b);
                }
            }
            var receiver = get_node(receiverEntry, c);
            while (receiver.kind is NodeKind.Assert or NodeKind.ResolveThis && receiver.left != 0) receiver = get_node(receiver.left, c);
            if (receiver.kind != NodeKind.Name || !b.class_field_receivers.TryGetValue(receiver.symbol, out owner)) return 0;
        }
        return owner;
    }

    static int il_class_field_writer(int target, int valueType, bool stringValue, Compiler c, IlBackend b)
    {
        var property = get_node(target, c);
        if (property.kind != NodeKind.Property || property.fourth != 0) return 0;
        int owner = il_class_field_owner(property.left, c, b);
        if (owner == 0) return 0;
        var members = b.numeric_class_members[owner];
        for (int index = 0; index < members.Count; index++)
        {
            if (!same(get_field(members[index], c).name, property.name, c)) continue;
            bool numeric = get_type(valueType, c).kind == TypeKind.Number;
            ClassFieldWriteForm form = numeric ? ClassFieldWriteForm.Number :
                stringValue || get_type(valueType, c).kind == TypeKind.String ? ClassFieldWriteForm.String : ClassFieldWriteForm.Value;
            var key = (owner, index, form);
            if (!b.class_field_writers.TryGetValue(key, out int method))
            {
                string prefix = form switch {
                    ClassFieldWriteForm.Number => "Set_number_",
                    ClassFieldWriteForm.String => "Set_string_",
                    _ => "Set_value_"
                };
                method = b.plan.method2(5, b.classes.at(owner), 0x16, -1, -5, numeric ? -10 : -5, prefix + index);
                b.class_field_writers.Add(key, method);
            }
            return method;
        }
        return 0;
    }
}
