namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> shape_readers = [];
    internal readonly Dictionary<int, int> shape_writers = [];
}

internal static partial class CompilerFunctions
{
    internal static void il_structural_objects(Compiler c, IlBackend b)
    {
        var checks = new List<(int Type, int Method, int Nominal)>();
        for (int type = c.types; type != 0; type = get_type(type, c).next)
        {
            Type shape = get_type(type, c);
            if (shape.kind != TypeKind.Shape || shape.name != 0 || shape.fields == 0) continue;
            int method = b.plan.method2(5, b.interface_type(type), 0x16, -5, -5, -4, "Check");
            checks.Add((type, method, b.cast(type)));
            b.casts.at_mut(type) = method;
        }
        foreach (var check in checks)
        {
            b.begin(check.Method);
            int good = b.label(), bad = b.label();
            b.emit(0xfe09, 0); b.emit(0x75, b.interface_type(check.Type)); b.emit(0x3a, good);
            for (int member = get_type(check.Type, c).fields; member != 0; member = get_field(member, c).next)
            {
                Field field = get_field(member, c);
                if (field.optional) continue;
                b.emit(0xfe09, 0); il_text(field.name, c, b);
                b.call(RuntimeMember.HasStructuralProperty); b.emit(0x39, bad);
            }
            b.mark(good); b.emit(0xfe09, 0); b.op(0x2a);
            b.mark(bad); b.emit(0xfe09, 0); b.emit(0xfe09, 1); b.emit(0x28, check.Nominal); b.op(0x2a);
        }
        foreach (var check in checks)
        {
            for (int member = get_type(check.Type, c).fields; member != 0; member = get_field(member, c).next)
            {
                Field field = get_field(member, c); int storage = b.type(field.type, c);
                int reader = b.plan.method1(5, b.interface_type(check.Type), 0x16, storage, -5, field_name(field.name, "Read_p_", c).view());
                b.shape_readers[member] = reader; b.begin(reader);
                int typed = b.label();
                b.emit(0xfe09, 0); b.emit(0x75, b.interface_type(check.Type)); b.op(0x25); b.emit(0x3a, typed); b.op(0x26);
                b.emit(0xfe09, 0); il_text(field.name, c, b);
                if (get_type(field.type, c).kind == TypeKind.Number) b.call(RuntimeMember.GetNumber);
                else { b.call(RuntimeMember.DynamicGet); b.from_object(field.type, c); }
                b.op(0x2a);
                b.mark(typed); b.emit(0x6f, b.getter(member)); b.op(0x2a);

                int writer = b.plan.method2(5, b.interface_type(check.Type), 0x16, -1, -5, storage, field_name(field.name, "Write_p_", c).view());
                b.shape_writers[member] = writer; b.begin(writer); typed = b.label();
                b.emit(0xfe09, 0); b.emit(0x75, b.interface_type(check.Type)); b.op(0x25); b.emit(0x3a, typed); b.op(0x26);
                b.emit(0xfe09, 0); il_text(field.name, c, b); b.emit(0xfe09, 1); b.box(field.type, c);
                b.call(RuntimeMember.DynamicSet); b.op(0x26); b.op(0x2a);
                b.mark(typed); b.emit(0xfe09, 1); b.emit(0x6f, b.setter(member)); b.op(0x2a);
            }
        }
    }
    internal static void il_shape_read(int owner, int member, IlBackend b)
    {
        if (b.shape_readers.TryGetValue(member, out int method)) b.emit(0x28, method);
        else { b.emit(0x74, b.interface_type(owner)); b.emit(0x6f, b.getter(member)); }
    }
}
