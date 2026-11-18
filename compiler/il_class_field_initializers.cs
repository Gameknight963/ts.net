namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<(int Type, int Index, bool Numeric), int> class_field_initializers = [];
}

internal static partial class CompilerFunctions
{
    static int il_class_field_initializer(int entry, Compiler c, IlBackend b)
    {
        // Instance initializers with known field names can avoid looking up
        // that name again for every allocation.
        if (!il_numeric_class_initializer(entry, c, b)) return 0;
        Node field = get_node(entry, c);
        int owner = il_class_field_owner(field.left, c, b);
        if (owner == 0 || field.name == 0) return 0;
        var members = b.numeric_class_members[owner];
        for (int index = 0; index < members.Count; index++)
        {
            if (!same(get_field(members[index], c).name, field.name, c)) continue;
            bool numeric = get_type(get_node(field.right, c).type, c).kind == TypeKind.Number;
            var key = (owner, index, numeric);
            if (!b.class_field_initializers.TryGetValue(key, out int method))
            {
                method = b.plan.method2(5, b.classes.at(owner), 0x16, -1, -5, numeric ? -10 : -5,
                    (numeric ? "Define_number_" : "Define_value_") + index);
                b.class_field_initializers.Add(key, method);
            }
            return method;
        }
        return 0;
    }

    static void il_class_field_initializer_bodies(Compiler c, IlBackend b)
    {
        foreach (var entry in b.class_field_initializers)
        {
            var (type, index, numeric) = entry.Key;
            int klass = b.classes.at(type);
            int name = get_field(b.numeric_class_members[type][index], c).name;
            b.begin(entry.Value);
            int instance = b.local(klass), fallback = b.label();
            b.emit(0xfe09, 0); b.emit(0x75, klass); b.emit(0xfe0e, instance);
            b.emit(0xfe0c, instance); b.emit(0x39, fallback);
            if (numeric && get_type(get_field(b.numeric_class_members[type][index], c).type, c).kind == TypeKind.Number) {
                b.emit(0xfe0c, instance); b.emit(0x20, index); b.emit(0xfe09, 1);
                b.call(RuntimeMember.TryDefineClassNumberState); b.emit(0x39, fallback);
                b.emit(0xfe0c, instance); b.emit(0xfe09, 1); b.call(RuntimeMember.NumberValue);
                b.emit(0x7d, b.class_field_tokens[type][index]);
            }
            else {
                // Reference slots retain runtime handling of numeric values.
                b.emit(0xfe0c, instance); b.emit(0x20, index); b.emit(0xfe09, 1);
                b.call(numeric ? RuntimeMember.TryDefineClassNumber : RuntimeMember.TryDefineClassValue);
                b.emit(0x39, fallback);
            }
            b.op(0x2a);
            b.mark(fallback); b.emit(0xfe09, 0); il_text(name, c, b); b.emit(0xfe09, 1);
            b.call(numeric ? RuntimeMember.DefineClassNumberField : RuntimeMember.DefineClassField);
            b.op(0x2a);
        }
    }
}
