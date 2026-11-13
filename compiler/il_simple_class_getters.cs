namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal readonly record struct SimpleClassGetter(int Name, int Slot, int Expression, int FieldName);
internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, List<SimpleClassGetter>> simple_class_getters = [];
    internal readonly Dictionary<int, int> simple_getter_slots = [];
    internal readonly Dictionary<(int Type, int Name), int> simple_getter_readers = [];
}

internal static partial class CompilerFunctions
{
    internal static void il_prepare_simple_class_getters(Compiler c, IlBackend b)
    {
        var classes = new Dictionary<int, List<int>>();
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node node = get_node(i, c);
            if (node.kind != NodeKind.ClassValue || node.fourth == 0) continue;
            int type = get_node(node.fourth, c).type;
            if (!classes.TryGetValue(type, out var list)) classes[type] = list = [];
            list.Add(i);
        }
        foreach (var pair in classes) {
            if (pair.Value.Count != 1) continue;
            Node klass = get_node(pair.Value[0], c);
            if (klass.third != 0) continue;
            var data = new List<int>();
            int fields = scalar_field_function(klass.left, c);
            if (fields != 0) {
                Node body = get_node(get_node(fields, c).left, c);
                for (int item = body.children; item != 0; item = get_node(item, c).next) {
                    Node field = get_node(item, c);
                    if (field.kind == NodeKind.ClassField && field.name != 0 && field.third == 0 && field.fourth == 0)
                        data.Add(field.name);
                }
            }
            var getters = new List<SimpleClassGetter>();
            for (int item = klass.children; item != 0; item = get_node(item, c).next) {
                Node method = get_node(item, c);
                if (method.constant || method.name == 0 || method.right != 0 || method.fourth != 0 ||
                    method.third == 0 || !get_node(method.third, c).constant) continue;
                int member = field(pair.Key, method.name, c), entry = scalar_field_function(method.left, c);
                if (member == 0 || get_type(get_field(member, c).type, c).kind != TypeKind.Number || entry == 0) continue;
                Node function = get_node(entry, c);
                if (get_type(function.type, c).kind != TypeKind.Number || function.left == 0) continue;
                Node receiver = function.fourth == 0 ? default : get_node(function.fourth, c);
                if (function.children != function.fourth || receiver.next != 0 || receiver.name != 0 || receiver.constant) continue;
                Node body = get_node(function.left, c);
                int statement = body.kind == NodeKind.Block ? body.children : function.left;
                Node returned = get_node(statement, c);
                if (returned.kind != NodeKind.Return || returned.left == 0 || returned.next != 0) continue;
                Node value = get_node(returned.left, c);
                int fieldName = 0;
                if (!scalar_class_literal(returned.left, TypeKind.Number, c)) {
                    if (value.kind != NodeKind.Property || value.fourth != 0 || value.left == 0 ||
                        get_type(value.type, c).kind != TypeKind.Number || same(value.name, method.name, c) ||
                        !data.Any(name => same(name, value.name, c))) continue;
                    Node owner = get_node(value.left, c);
                    if (owner.kind == NodeKind.ResolveThis && !owner.constant) owner = get_node(owner.left, c);
                    if (function.fourth == 0 || owner.kind != NodeKind.Name || owner.symbol != receiver.symbol) continue;
                    fieldName = value.name;
                }
                var getter = new SimpleClassGetter(method.name, getters.Count, returned.left, fieldName);
                getters.Add(getter); b.simple_getter_slots[item] = getter.Slot;
            }
            if (getters.Count != 0) b.simple_class_getters[pair.Key] = getters;
        }
    }

    internal static bool il_simple_class_getter(int type, int name, Compiler c, IlBackend b, out SimpleClassGetter getter)
    {
        if (b.simple_class_getters.TryGetValue(type, out var getters))
            foreach (var item in getters) if (same(item.Name, name, c)) { getter = item; return true; }
        getter = default; return false;
    }

    internal static void il_simple_class_getter_reader(int type, int member, Compiler c, IlBackend b)
    {
        int name = get_field(member, c).name;
        if (b.simple_getter_readers.ContainsKey((type, name))) return;
        int reader = b.plan.method1(5, b.interface_type(type), 0x16, -10, -5, field_name(name, "Read_simple_", c).view());
        b.simple_getter_readers[(type, name)] = reader; b.begin(reader);
        int typed = b.label();
        b.emit(0xfe09, 0); b.emit(0x75, b.interface_type(type)); b.op(0x25); b.emit(0x3a, typed); b.op(0x26);
        b.emit(0xfe09, 0); il_text(name, c, b); b.call(RuntimeMember.GetNumber); b.op(0x2a);
        b.mark(typed); b.emit(0x6f, b.getter(member)); b.op(0x2a);
    }

    internal static void il_simple_class_getter_body(SimpleClassGetter getter, Compiler c, IlBackend b)
    {
        if (getter.FieldName == 0) { il_number(getter.Expression, c, b); b.call(RuntimeMember.WrapNumber); }
        else {
            b.emit(0xfe09, 0); il_text(getter.FieldName, c, b); b.emit(0xfe09, 0);
            b.call(RuntimeMember.GetStoredNumberWithReceiver);
        }
        b.op(0x2a);
    }
}
