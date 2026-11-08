namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed class DestructureAssignmentTarget
{
    internal int Entry, Receiver = -1, Key = -1, Private = -1;
    internal ScalarField Scalar;
}
internal static partial class CompilerFunctions
{
    internal static DestructureAssignmentTarget il_prepare_assignment_binding(int entry, Compiler c, IlBackend b)
    {
        Node binding = get_node(entry,c);
        if (binding.name == 0 || !is_text(binding.name,"$assignment",c) || binding.fourth == 0) return null;
        int targetEntry = get_node(binding.fourth,c).left;
        Node target = get_node(targetEntry,c);
        var saved = new DestructureAssignmentTarget {Entry=targetEntry, Scalar=il_scalar_field(targetEntry,c,b)};
        if (target.kind == NodeKind.Name || saved.Scalar.slot >= 0) return saved;
        il_boxed(target.left,c,b); saved.Receiver = b.local(-5); b.emit(0xfe0e,saved.Receiver);
        if (target.kind == NodeKind.Index) {
            // Capture the key value now; PutValue performs coercion after the read/default.
            il_boxed(target.right,c,b); saved.Key=b.local(-5); b.emit(0xfe0e,saved.Key);
        } else if (target.fourth != 0) {
            il_boxed(target.fourth,c,b); saved.Private=b.local(-5); b.emit(0xfe0e,saved.Private);
        }
        return saved;
    }
    internal static void il_store_assignment_binding(DestructureAssignmentTarget saved, int type, Compiler c, IlBackend b)
    {
        Node target=get_node(saved.Entry,c);
        int value=b.local(b.type(type,c)); b.emit(0xfe0e,value);
        if (target.kind == NodeKind.Name) {
            b.emit(0xfe0c,value); b.convert(type,get_symbol(target.symbol,c).type,c); b.store(target.symbol); return;
        }
        if (saved.Scalar.slot >= 0) {
            b.emit(0xfe0c,value); b.convert(type,saved.Scalar.type,c); b.emit(0xfe0e,saved.Scalar.slot); return;
        }
        b.emit(0xfe0c,saved.Receiver);
        if (saved.Key >= 0) b.emit(0xfe0c,saved.Key);
        else if (saved.Private >= 0) b.emit(0xfe0c,saved.Private);
        else il_text(target.name,c,b);
        b.emit(0xfe0c,value); b.box(type,c);
        b.call(saved.Key >= 0 ? RuntimeMember.IndexSet : saved.Private >= 0 ? RuntimeMember.PrivateSet : RuntimeMember.DynamicSet);
        if (saved.Key < 0) b.op(0x26);
    }
}
