namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    internal static bool il_checked_property_addition(int left, int right, Compiler c, IlBackend b)
    {
        Node property = get_node(left, c);
        if (property.kind != NodeKind.Property || property.fourth != 0) return false;
        int receiver = b.local(-5), number = b.local(-10), boxed = b.local(-5), operand = b.local(-10);
        int read = b.label(), fallback = b.label(), done = b.label();
        il_boxed(property.left, c, b); b.emit(0xfe0e, receiver);
        b.emit(0xfe0c, receiver); il_text(property.name, c, b);
        b.call(RuntimeMember.TryReadOwnNumber); b.emit(0xfe0e, number);
        b.emit(0xfe0c, number); b.call(RuntimeMember.NumberHasValue); b.emit(0x3a, read);
        // Get must precede the right operand. Keep its result without coercing
        // it: valueOf/Symbol.toPrimitive run only after the right operand.
        b.emit(0xfe0c, receiver); il_text(property.name, c, b); b.call(RuntimeMember.DynamicGet); b.emit(0xfe0e, boxed);
        b.mark(read);
        il_expression(right, c, b); b.emit(0xfe0e, operand);
        b.emit(0xfe0c, number); b.call(RuntimeMember.NumberHasValue); b.emit(0x39, fallback);
        b.emit(0xfe0c, number); b.call(RuntimeMember.NumberValue);
        b.emit(0xfe0c, operand); b.call(RuntimeMember.NumberValue); b.op(0x58); b.call(RuntimeMember.WrapNumber);
        b.emit(0x38, done);
        b.mark(fallback); b.emit(0xfe0c, boxed); b.emit(0xfe0c, operand); b.call(RuntimeMember.AddNumberRightCheckedSlow);
        b.mark(done);
        return true;
    }
}
