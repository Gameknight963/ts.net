namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> object_receiver_shapes = [];
}

internal static partial class CompilerFunctions
{
    internal static void il_prepare_object_receivers(Compiler c, IlBackend b)
    {
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node literal = get_node(i, c);
            if (literal.kind != NodeKind.Object || get_type(literal.type, c).kind != TypeKind.Shape ||
                get_type(literal.type, c).name != 0 || b.classes.at(literal.type) == 0) continue;
            for (int property = literal.children; property != 0; property = get_node(property, c).next) {
                Node value = get_node(get_node(property, c).left, c);
                if (value.kind != NodeKind.FunctionValue || value.symbol == 0) continue;
                Node function = get_node(get_symbol(value.symbol, c).function, c);
                if (function.fourth == 0) continue;
                int receiver = get_node(function.fourth, c).symbol;
                if (receiver == 0) continue;
                if (b.object_receiver_shapes.TryGetValue(receiver, out int previous) && previous != literal.type)
                    b.object_receiver_shapes[receiver] = 0;
                else b.object_receiver_shapes[receiver] = literal.type;
            }
        }
    }

    internal static bool il_object_receiver_number_read(int entry, Compiler c, IlBackend b)
    {
        Node property = get_node(entry, c), receiver = get_node(property.left, c);
        if (get_type(property.type, c).kind != TypeKind.Number || receiver.kind != NodeKind.ResolveThis || receiver.constant) return false;
        Node name = get_node(receiver.left, c);
        if (name.kind != NodeKind.Name || !b.object_receiver_shapes.TryGetValue(name.symbol, out int owner) || owner == 0) return false;
        int member = field(owner, property.name, c);
        if (member == 0 || get_type(get_field(member, c).type, c).kind != TypeKind.Number || b.getter(member) == 0) return false;
        int fallback = b.label(), done = b.label(), original = b.local(-5), typed = b.local(b.classes.at(owner));
        il_boxed(property.left, c, b); b.emit(0xfe0e, original);
        b.emit(0xfe0c, original); b.emit(0x75, b.classes.at(owner)); b.emit(0xfe0e, typed);
        b.emit(0xfe0c, typed); b.emit(0x39, fallback);
        b.emit(0xfe0c, typed); il_shape_read(owner, member, b); b.emit(0x38, done);
        b.mark(fallback); b.emit(0xfe0c, original); il_text(property.name, c, b); b.call(RuntimeMember.GetNumber);
        b.mark(done); return true;
    }
}
