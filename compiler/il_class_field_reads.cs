namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, List<int>> class_field_tokens = [];
    internal readonly Dictionary<(int Type, int Index), int> class_field_readers = [];
}

internal static partial class CompilerFunctions
{
    static int il_class_field_reader(int entry, Compiler c, IlBackend b)
    {
        Node property = get_node(entry, c);
        int owner = get_node(property.left, c).type;
        if (property.fourth != 0 || !il_numeric_class_eligible(owner, c, b)) return 0;
        var members = b.numeric_class_members[owner];
        for (int index = 0; index < members.Count; index++) {
            Field field = get_field(members[index], c);
            if (!same(field.name, property.name, c)) continue;
            bool numeric = get_type(field.type, c).kind == TypeKind.Number;
            if (numeric != (get_type(property.type, c).kind == TypeKind.Number)) return 0;
            var key = (owner, index);
            if (!b.class_field_readers.TryGetValue(key, out int method)) {
                method = b.plan.method1(5, b.classes.at(owner), 0x16, numeric ? -10 : -5, -5, "Read_field_" + index);
                b.class_field_readers.Add(key, method);
            }
            return method;
        }
        return 0;
    }

    static void il_class_field_reader_bodies(Compiler c, IlBackend b)
    {
        foreach (var entry in b.class_field_readers) {
            var (type, index) = entry.Key;
            Field field = get_field(b.numeric_class_members[type][index], c);
            bool numeric = get_type(field.type, c).kind == TypeKind.Number;
            int klass = b.classes.at(type);
            b.begin(entry.Value);
            int instance = b.local(klass), fallback = b.label();
            b.emit(0xfe09, 0); b.emit(0x75, klass); b.emit(0xfe0e, instance);
            b.emit(0xfe0c, instance); b.emit(0x39, fallback);
            // Only ordinary, present fields can bypass property lookup.
            b.emit(0xfe0c, instance); b.emit(0x20, index); b.call(RuntimeMember.NumericClassState);
            b.emit(0x20, 3); b.op(0xfe01); b.emit(0x39, fallback);
            b.emit(0xfe0c, instance); b.emit(0x7b, b.class_field_tokens[type][index]);
            if (numeric) b.call(RuntimeMember.WrapNumber);
            b.op(0x2a);
            b.mark(fallback); b.emit(0xfe09, 0); il_text(field.name, c, b);
            b.call(numeric ? RuntimeMember.GetNumber : RuntimeMember.DynamicGet); b.op(0x2a);
        }
    }
}
