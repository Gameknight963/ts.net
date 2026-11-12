namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    internal static void il_missing_spread_property(int type, int name, int result, Compiler c, IlBackend b)
    {
        if (b.spread_types.at(type) == 0) return;
        int present = b.label();
        b.emit(0xfe09, 0);
        if (name == 0) b.emit(0xfe09, 1); else il_text(name, c, b);
        b.emit(0x6f, b.RuntimeToken(RuntimeMember.HasOwnBase));
        b.emit(0x3a, present);
        b.emit(0xfe09, 0);
        if (name == 0) b.emit(0xfe09, 1); else il_text(name, c, b);
        b.call(RuntimeMember.Get);
        if (result != 0) b.from_object(result, c);
        b.op(0x2a);
        b.mark(present);
    }
}
