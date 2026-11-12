namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    internal static void il_missing_spread_write(int type, int name, int valueType, int argument, bool returnsValue, Compiler c, IlBackend b)
    {
        if (b.spread_types.at(type) == 0 || il_dynamic_index_name(name, c)) return;
        int present = b.label();
        b.emit(0xfe09, 0); il_text(name, c, b);
        b.emit(0x6f, b.RuntimeToken(RuntimeMember.HasOwnBase)); b.emit(0x3a, present);
        b.emit(0xfe09, 0); il_text(name, c, b); b.emit(0xfe09, argument);
        if (valueType != 0) b.box(valueType, c);
        b.call(RuntimeMember.TrySetMissingProperty); b.emit(0x39, present);
        if (returnsValue) b.emit(0xfe09, argument);
        b.op(0x2a);
        b.mark(present);
    }
}
