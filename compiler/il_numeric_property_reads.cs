namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    internal static void il_numeric_property_reads(int type, int klass, Compiler c, IlBackend b)
    {
        bool eligible(int member) => get_type(get_field(member, c).type, c).kind == TypeKind.Number && !il_dynamic_index_field(member, c);
        int number = 0;
        for (int member = get_type(type, c).fields; member != 0; member = get_field(member, c).next)
            if (eligible(member)) number = get_field(member, c).type;
        if (number == 0) return;

        b.begin(b.plan.method1(5, klass, 0xc6, -10, -4, "GetNumberProperty"));
        il_missing_spread_property(type, 0, number, c, b);
        for (int member = get_type(type, c).fields; member != 0; member = get_field(member, c).next)
        {
            if (!eligible(member)) continue;
            int next = b.label();
            b.emit(0xfe09, 1); il_text(get_field(member, c).name, c, b);
            b.call(RuntimeMember.StringEqual); b.emit(0x39, next);
            b.emit(0xfe09, 0); b.load_field(member, c); b.op(0x2a);
            b.mark(next);
        }
        b.emit(0xfe09, 0); b.emit(0xfe09, 1);
        b.call(RuntimeMember.GetNumberProperty); b.op(0x2a);
    }
}
