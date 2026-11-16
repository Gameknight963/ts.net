namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    internal static bool il_packed_field_number(int entry, Compiler c, IlBackend b)
    {
        Node property = get_node(entry, c);
        if (property.kind != NodeKind.Property || property.fourth != 0 || get_type(property.type, c).kind != TypeKind.Number) return false;
        int owner = get_node(property.left, c).type;
        if (get_type(owner, c).kind != TypeKind.Shape || get_type(owner, c).name != 0 ||
            b.classes.at(owner) == 0 || b.spread_types.at(owner) != 0 || il_scalar_field(entry, c, b).slot >= 0) return false;
        int member = field(owner, property.name, c);
        if (member == 0 || get_field(member, c).optional || b.field_token(member) == 0 || b.getter(member) == 0 ||
            il_dynamic_index_field(member, c) || b.field_storage(member, c) != -3) return false;
        // Packed undefined is a NaN, which is already its numeric value in arithmetic.
        // Ordinary reads must still decode it, and overridden properties use the getter.
        int original = b.local(-5), fallback = b.label(), done = b.label(), klass = b.classes.at(owner);
        il_expression(property.left, c, b); b.emit(0xfe0e, original);
        b.emit(0xfe0c, original);
        b.emit(0x28, b.plan.specialize(b.RuntimeToken(RuntimeMember.IsPlainDataRecord), klass));
        b.emit(0x39, fallback);
        // Packed undefined is a NaN; arithmetic converts either to NaN.
        // Ordinary property reads still decode the nullable value.
        b.emit(0xfe0c, original); b.emit(0x74, klass); b.emit(0x7b, b.field_token(member)); b.emit(0x38, done);
        b.mark(fallback); b.emit(0xfe0c, original); il_shape_read(owner, member, b); b.call(RuntimeMember.NumberValue);
        b.mark(done); return true;
    }
}
