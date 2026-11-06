namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static // Reserve for a stable array shared by the loop, never a fresh array per
                    // iteration. Otherwise a capacity hint could turn linear allocation quadratic.
bool il_push_capacity_hint(int entry, Compiler c, IlBackend b)
    {
        if (b.push_bound < 0) { return false; }
        Node receiver = get_node(get_node(get_node(entry, c).left, c).left, c);
        if (receiver.kind != NodeKind.Name || b.written_symbols.at(receiver.symbol) != 0) { return false; }
        int loop = (int)b.node_parents.at(entry);
        while (loop != 0 && get_node(loop, c).kind != NodeKind.For)
        {
            NodeKind kind = get_node(loop, c).kind;
            if ((kind == NodeKind.ForOf || kind == NodeKind.ForIn) || kind == NodeKind.While || kind == NodeKind.Do || kind == NodeKind.Function) { return false; }
            loop = (int)b.node_parents.at(loop);
        }
        if (loop == 0) { return false; }
        bool declared = false;
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if ((n.kind == NodeKind.Variable || n.kind == NodeKind.Binding) && n.symbol == receiver.symbol)
            {
                declared = true; if (il_inside(i, loop, b)) { return false; }
            }
        }
        return declared;
    }
    internal static bool il_number_array(int type, Compiler c)
    {
        return get_type(type, c).kind == TypeKind.Array && get_type(get_type(type, c).element, c).kind == TypeKind.Number;
    }
    internal static bool il_number_index(int entry, Compiler c)
    {
        Node n = get_node(entry, c);
        return n.kind == NodeKind.Index && il_number_array(get_node(n.left, c).type, c) && get_type(get_node(n.right, c).type, c).kind == TypeKind.Number;
    }
    internal static bool il_direct_index(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind != NodeKind.Index || get_type(get_node(n.left, c).type, c).kind != TypeKind.Array || get_type(get_node(n.right, c).type, c).kind != TypeKind.Number) { return false; }
        return il_nonnegative_integer(n.right, c, b);
    }
    internal static bool il_direct_array_index(int entry, Compiler c, IlBackend b)
    {
        return il_number_index(entry, c) && il_direct_index(entry, c, b);
    }
    internal static void il_array_literal(int entry, int type, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c); int count = 0; int value = n.children;
        bool spread = false;
        while (value != 0) { count++; if (get_node(value, c).kind == NodeKind.Spread) { spread = true; } value = get_node(value, c).next; }
        bool numeric = il_number_array(type, c);
        bool tagged = b.tagged_literals.at(entry) != 0 && get_type(type, c).kind == TypeKind.Array && get_type(get_type(type, c).element, c).kind == TypeKind.Any;
        if (tagged && !spread && b.reversible_literals.at(entry) == 0)
        {
            b.emit(0x20, count); b.emit(0x73, b.RuntimeToken(RuntimeMember.TaggedConstructor));
            if (count != 0) { b.op(0x25); b.plan.number(b.method, (double)count); b.call(RuntimeMember.ArrayReserveCapacity); }
            value = n.children; int indexLocal = 0;
            while (value != 0)
            {
                Node item = get_node(value, c);
                if (!(item.kind == NodeKind.Undefined && item.constant))
                {
                    b.op(0x25); b.emit(0x20, indexLocal);
                    if (get_type(item.type, c).kind == TypeKind.Number) { il_expression(value, c, b); b.call(RuntimeMember.ArraySetTaggedNumberAt); }
                    else { il_boxed(value, c, b); b.call(RuntimeMember.ArrayIndexSetAt); }
                }
                indexLocal++; value = item.next;
            }
            return;
        }
        if (!numeric && !spread)
        {
            b.emit(0x20, count); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType)); value = n.children; int indexLocal = 0;
            while (value != 0)
            {
                Node item = get_node(value, c); b.op(0x25); b.emit(0x20, indexLocal++);
                if (item.kind == NodeKind.Undefined && item.constant) { b.call(RuntimeMember.ArrayHoleValue); }
                else { il_boxed(value, c, b); }
                b.op(0xa2); value = item.next;
            }
            b.emit(0x73, b.RuntimeToken(b.reversible_literals.at(entry) != 0 ? RuntimeMember.ReversibleBufferConstructor : tagged ? RuntimeMember.TaggedBufferConstructor : RuntimeMember.ArrayBufferConstructor)); return;
        }
        b.emit(0x20, spread ? 0 : count); b.emit(0x73, b.RuntimeToken(numeric ? RuntimeMember.NumberArrayConstructor : b.reversible_literals.at(entry) != 0 ? RuntimeMember.ReversibleConstructor : tagged ? RuntimeMember.TaggedConstructor : RuntimeMember.ArrayConstructor));
        value = n.children; int index = 0;
        while (value != 0)
        {
            Node item = get_node(value, c);
            if (spread)
            {
                b.op(0x25);
                if (item.kind == NodeKind.Spread)
                {
                    il_expression(item.left, c, b); int extra = 0; int tail = item.next;
                    while (tail != 0) { if (get_node(tail, c).kind != NodeKind.Spread) { extra++; } tail = get_node(tail, c).next; }
                    b.emit(0x20, extra); b.call(RuntimeMember.ArraySpread);
                }
                else if (item.kind == NodeKind.Undefined && item.constant) { b.call(RuntimeMember.ArrayHole); }
                else if (numeric) { il_as(value, get_type(type, c).element, c, b); b.call(RuntimeMember.NumberPush); b.op(0x26); }
                else { il_boxed(value, c, b); b.call(RuntimeMember.ArrayAppend); }
                value = item.next; continue;
            }
            if (!(item.kind == NodeKind.Undefined && item.constant))
            {
                b.op(0x25); b.emit(0x20, index); il_as(value, get_type(type, c).element, c, b); b.call(RuntimeMember.NumberIndexSetAt);
            }
            index++; value = item.next;
        }
    }
    internal static bool il_array_search(int entry, Compiler c)
    {
        Node callee = get_node(get_node(entry, c).left, c);
        return callee.kind == NodeKind.Property && (is_text(callee.name, text_includes, c) || is_text(callee.name, text_index_of, c));
    }
    internal static void il_array_search_result(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c); Node callee = get_node(n.left, c);
        bool includes = is_text(callee.name, text_includes, c);
        if (includes) { b.plan.number(b.method, -1); b.op(0xfe02); b.call(RuntimeMember.WrapBoolean); }
        else { b.call(RuntimeMember.WrapNumber); }
        if (get_type(n.type, c).kind == TypeKind.Any) { b.call(includes ? RuntimeMember.BoxBoolean : RuntimeMember.BoxNumber); }
    }
    internal static bool il_array_call(int entry, Compiler c)
    {
        Node n = get_node(entry, c); Node callee = get_node(n.left, c);
        if (callee.kind != NodeKind.Property || (!is_text(callee.name, text_push, c) && !is_text(callee.name, text_pop, c) && !is_text(callee.name, text_slice, c) && !il_array_search(entry, c) && !is_text(callee.name, text_reverse, c))) { return false; }
        TypeKind owner = get_type(get_node(callee.left, c).type, c).kind;
        return owner == TypeKind.Array || owner == TypeKind.Any || (owner == TypeKind.String && is_text(callee.name, text_slice, c));
    }
    internal static void il_emit_array_call(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c); Node callee = get_node(n.left, c); int owner = get_node(callee.left, c).type;
        il_expression(callee.left, c, b);
        if (is_text(callee.name, text_reverse, c))
        {
            b.op(0x25); b.call(RuntimeMember.ArrayReverseBuiltin); int builtin = b.local(-2); b.emit(0xfe0e, builtin);
            int argument = n.children;
            while (argument != 0) { il_expression(argument, c, b); if (get_type(get_node(argument, c).type, c).kind != TypeKind.Void) { b.op(0x26); } argument = get_node(argument, c).next; }
            b.emit(0xfe0c, builtin); b.call(RuntimeMember.ArrayReverse); return;
        }
        if (il_array_search(entry, c))
        {
            int argument = n.children; bool numeric = argument != 0 && get_type(get_node(argument, c).type, c).kind == TypeKind.Number;
            if (argument != 0) { if (numeric) { il_expression(argument, c, b); } else { il_boxed(argument, c, b); } argument = get_node(argument, c).next; }
            else { b.call(RuntimeMember.Undefined); }
            if (argument != 0) { il_boxed(argument, c, b); argument = get_node(argument, c).next; }
            else { b.call(RuntimeMember.Undefined); }
            while (argument != 0) { il_expression(argument, c, b); if (get_type(get_node(argument, c).type, c).kind != TypeKind.Void) { b.op(0x26); } argument = get_node(argument, c).next; }
            b.emit(0x20, is_text(callee.name, text_includes, c) ? 1 : 0);
            b.call(numeric ? RuntimeMember.ArraySearchNumber : RuntimeMember.ArraySearch); il_array_search_result(entry, c, b); return;
        }
        if (is_text(callee.name, text_slice, c))
        {
            int argument = n.children;
            for (int i = 0; i < 2; i++)
            {
                if (argument != 0) { il_boxed(argument, c, b); argument = get_node(argument, c).next; }
                else { b.call(RuntimeMember.Undefined); }
            }
            while (argument != 0) { il_expression(argument, c, b); if (get_type(get_node(argument, c).type, c).kind != TypeKind.Void) { b.op(0x26); } argument = get_node(argument, c).next; }
            b.call(RuntimeMember.Slice); return;
        }
        if (is_text(callee.name, text_pop, c))
        {
            if (il_number_array(owner, c)) { b.call(RuntimeMember.NumberPop); }
            else { b.call(RuntimeMember.ArrayPop); b.from_object(n.type, c); }
            return;
        }
        if (il_number_array(owner, c) && n.children != 0 && get_node(n.children, c).next == 0)
        {
            il_as(n.children, get_type(owner, c).element, c, b); b.call(RuntimeMember.NumberPush);
        }
        else if (n.children != 0 && get_node(n.children, c).next == 0)
        {
            il_boxed(n.children, c, b);
            if (il_push_capacity_hint(entry, c, b)) { b.emit(0xfe0c, b.push_bound); b.call(RuntimeMember.ArrayPushOneReserved); }
            else { b.call(RuntimeMember.ArrayPushOne); }
        }
        else
        {
            int count = 0; int argument = n.children;
            while (argument != 0) { count++; argument = get_node(argument, c).next; }
            b.emit(0x20, count); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType)); argument = n.children; int index = 0;
            while (argument != 0)
            {
                b.op(0x25); b.emit(0x20, index++); il_boxed(argument, c, b); b.op(0xa2); argument = get_node(argument, c).next;
            }
            b.call(RuntimeMember.ArrayPush);
        }
        b.call(RuntimeMember.WrapNumber);
        if (get_type(n.type, c).kind != TypeKind.Number) { b.call(RuntimeMember.BoxNumber); }
    }
}
