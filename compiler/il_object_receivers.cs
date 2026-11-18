namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> object_receiver_shapes = [];
    internal readonly Dictionary<int, (int Owner, int Member)> object_receiver_fields = [];
}

internal static partial class CompilerFunctions
{
    internal static void il_prepare_object_receivers(Compiler c, IlBackend b)
    {
        // Existing one-field interfaces also describe borrowed receivers with
        // a different concrete layout. Their readers retain dynamic fallback.
        for (int type = c.types; type != 0; type = get_type(type, c).next) {
            Type shape = get_type(type, c);
            int member = shape.fields;
            if (shape.kind != TypeKind.Shape || shape.name != 0 || member == 0 ||
                get_field(member, c).next != 0 || get_type(get_field(member, c).type, c).kind != TypeKind.Number ||
                !b.shape_readers.ContainsKey(member)) continue;
            b.object_receiver_fields.TryAdd(get_field(member, c).name, (type, member));
        }
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

    internal static bool il_object_receiver_number_read(int entry, Compiler c, IlBackend b, int receiverSlot = -1)
    {
        Node property = get_node(entry, c), receiver = get_node(property.left, c);
        if (get_type(property.type, c).kind != TypeKind.Number || receiver.kind != NodeKind.ResolveThis || receiver.constant) return false;
        Node name = get_node(receiver.left, c);
        if (name.kind != NodeKind.Name || !b.object_receiver_shapes.TryGetValue(name.symbol, out int owner) || owner == 0) return false;
        int member = field(owner, property.name, c);
        if (member == 0 || get_type(get_field(member, c).type, c).kind != TypeKind.Number || b.getter(member) == 0) return false;
        void LoadReceiver() {
            if (receiverSlot >= 0) b.emit(0xfe0c, receiverSlot);
            else il_boxed(property.left, c, b);
        }
        if (b.object_receiver_fields.TryGetValue(property.name, out var narrow) && fits(owner, narrow.Owner, c)) {
            LoadReceiver();
            il_shape_read(narrow.Owner, narrow.Member, b);
            return true;
        }
        int fallback = b.label(), done = b.label(), original = b.local(-5), typed = b.local(b.classes.at(owner));
        LoadReceiver(); b.emit(0xfe0e, original);
        b.emit(0xfe0c, original); b.emit(0x75, b.classes.at(owner)); b.emit(0xfe0e, typed);
        b.emit(0xfe0c, typed); b.emit(0x39, fallback);
        b.emit(0xfe0c, typed); il_shape_read(owner, member, b); b.emit(0x38, done);
        b.mark(fallback); b.emit(0xfe0c, original); il_text(property.name, c, b); b.call(RuntimeMember.GetNumber);
        b.mark(done); return true;
    }
}
