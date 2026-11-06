namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static // Contextual typing may reuse a shape whose field order differs from the
                    // literal. Keep key order in a shared generated subclass, not in each object.
void il_prepare_key_layouts(Compiler c, IlBackend b)
    {
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c); if (n.kind != NodeKind.Object || get_type(n.type, c).kind != TypeKind.Shape || il_object_needs_entries(i, c)) { continue; }
            int member = get_type(n.type, c).fields; int property = n.children; bool different = false;
            while (member != 0 && property != 0)
            {
                Node p = get_node(property, c);
                if (p.kind != NodeKind.Property || !same(p.name, get_field(member, c).name, c)) { different = true; }
                member = get_field(member, c).next; property = p.next;
            }
            if (member != 0 || property != 0) { different = true; }
            if (different) { b.layout_needed.at_mut(n.type) = 1; b.literal_constructors.at_mut(i) = -1; }
        }
    }
    internal static void il_key_layouts(Compiler c, IlBackend b)
    {
        Vector<int> templates = new Vector<int>();
        for (int i = 1; i < b.literal_constructors.count(); i++)
        {
            if (b.literal_constructors.at(i) != -1) { continue; }
            Node n = get_node(i, c); int shared = 0;
            for (int j = 0; j < templates.count(); j++)
            {
                Node previous = get_node((int)templates.at(j), c); if (previous.type != n.type) { continue; }
                int left = n.children; int right = previous.children;
                while (left != 0 && right != 0 && get_node(left, c).name == get_node(right, c).name) { left = get_node(left, c).next; right = get_node(right, c).next; }
                if (left == 0 && right == 0) { shared = b.literal_constructors.at((int)templates.at(j)); int shared_class = b.literal_classes.at((int)templates.at(j)); b.literal_classes.at_mut(i) = shared_class; break; }
            }
            if (shared != 0) { b.literal_constructors.at_mut(i) = shared; continue; }
            templates.push((int)i); Vector<byte> name = numbered_name('K', (int)i); int parent = b.classes.at(n.type);
            int type = b.plan.type(name.view(), parent, 0x101); int constructor = b.plan.method0(5, type, 0x1886, -1, ".ctor");
            b.literal_classes.at_mut(i) = type; b.literal_constructors.at_mut(i) = constructor; b.begin(constructor); b.emit(0xfe09, 0); b.emit(0x28, b.constructor(n.type)); b.op(0x2a);
            b.begin(b.plan.method0(5, type, 0xc6, -5, "OwnKeys")); b.emit(0xfe09, 0);
            int count = 0; bool text_keys = true; int child = n.children; while (child != 0) { count++; int nameLocal = get_node(child, c).name; if (text_size(nameLocal, c) > 0 && text_byte(nameLocal, 0, c) >= '0' && text_byte(nameLocal, 0, c) <= '9') { text_keys = false; } child = get_node(child, c).next; }
            b.emit(0x20, count); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType)); int index = 0; child = n.children;
            while (child != 0) { Node p = get_node(child, c); b.op(0x25); b.emit(0x20, index++); il_text(p.name, c, b); b.op(0xa2); child = p.next; }
            b.call(text_keys ? RuntimeMember.KeysFromTextFields : RuntimeMember.KeysFromFixed); b.op(0x2a); il_enumeration_shape(n.type, type, i, c, b);
        }
    }
    internal static void il_object_keys_call(int entry, bool count, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c); int argument = call.children;
        if (argument == 0) { b.call(RuntimeMember.Undefined); }
        else { il_boxed(argument, c, b); argument = get_node(argument, c).next; }
        int source = b.local(-5); b.emit(0xfe0e, source);
        while (argument != 0) { il_expression(argument, c, b); if (get_type(get_node(argument, c).type, c).kind != TypeKind.Void) { b.op(0x26); } argument = get_node(argument, c).next; }
        b.emit(0xfe0c, source); b.call(count ? RuntimeMember.ObjectKeyCount : RuntimeMember.ObjectKeys);
        if (count) { b.call(RuntimeMember.WrapNumber); }
    }
    internal static void il_object_enumeration_call(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c); int argument = call.children;
        if (argument == 0) { b.call(RuntimeMember.Undefined); }
        else { il_boxed(argument, c, b); argument = get_node(argument, c).next; }
        int source = b.local(-5); b.emit(0xfe0e, source);
        while (argument != 0) { il_expression(argument, c, b); if (get_type(get_node(argument, c).type, c).kind != TypeKind.Void) { b.op(0x26); } argument = get_node(argument, c).next; }
        b.emit(0xfe0c, source);
        if (get_node(call.left, c).kind == NodeKind.ObjectEntriesFunction) { b.call(RuntimeMember.ObjectEntries); }
        else { b.call(il_number_array(call.type, c) ? RuntimeMember.ObjectNumberValues : RuntimeMember.ObjectValues); }
    }
}
