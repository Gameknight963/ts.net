namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static void il_prepare_enumeration(Compiler c, IlBackend b)
    {
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            NodeKind kind = get_node(i, c).kind;
            if (kind == NodeKind.ObjectValuesFunction) { b.enumerate_values = true; }
            if (kind == NodeKind.ObjectEntriesFunction) { b.enumerate_entries = true; }
        }
    }
    internal static bool il_enumeration_index(int name, ref double value, Compiler c)
    {
        int count = text_size(name, c); value = 0;
        if (count == 0 || count > 10 || (count > 1 && text_byte(name, 0, c) == '0')) { return false; }
        for (int i = 0; i < count; i++)
        {
            byte ch = text_byte(name, i, c); if (ch < '0' || ch > '9') { return false; }
            value = value * 10 + (double)(ch - '0');
        }
        return value < 4294967295.0;
    }
    internal static void il_sort_enumeration(Vector<int> fields, Compiler c)
    {
        for (int i = 0; i < fields.count(); i++)
        {
            int best = i; double index = 0; bool numeric = il_enumeration_index(get_field(fields.at(i), c).name, ref index, c);
            for (int j = i + 1; j < fields.count(); j++)
            {
                double candidate = 0; bool next = il_enumeration_index(get_field(fields.at(j), c).name, ref candidate, c);
                if (next && (!numeric || candidate < index)) { best = j; index = candidate; numeric = true; }
            }
            if (best == i) { continue; }
            int chosen = fields.at(best);
            for (int j = best; j > i; j--) { int previous = fields.at(j - 1); fields.at_mut(j) = previous; }
            fields.at_mut(i) = chosen;
        }
    }
    internal static void il_enumeration_projection(int klass, int mode, Vector<int> fields, Compiler c, IlBackend b)
    {
        int method = 0; RuntimeMember fallback = RuntimeMember.OwnValuesBase;
        if (mode == 0) { method = b.plan.method0(5, klass, 0xc6, -5, "OwnValues"); }
        else if (mode == 1) { method = b.plan.method0(5, klass, 0xc6, -5, "OwnEntries"); fallback = RuntimeMember.OwnEntriesBase; }
        else { method = b.plan.method0(5, klass, 0xc6, -5, "OwnNumberValues"); fallback = RuntimeMember.OwnNumberValuesBase; }
        b.begin(method); int slow = b.label(); int generic = b.RuntimeToken(RuntimeMember.IsExactSpread); int exact = b.plan.specialize(generic, klass);
        b.emit(0xfe09, 0); b.emit(0x28, exact); b.emit(0x39, slow);
        b.emit(0xfe09, 0); b.emit(0x20, 0); b.call(RuntimeMember.KeyCountWithFixed); b.plan.number(b.method, 0); b.op(0xfe01); b.emit(0x39, slow);
        if (fields.count() == 0)
        {
            b.emit(0x20, 0); b.emit(0x73, b.RuntimeToken(mode == 2 ? RuntimeMember.NumberArrayConstructor : RuntimeMember.ArrayConstructor)); b.op(0x2a);
        }
        else
        {
            b.emit(0x20, (int)fields.count()); b.emit(0x8d, b.RuntimeToken(mode == 2 ? RuntimeMember.DoubleType : RuntimeMember.ObjectType));
            for (int i = 0; i < fields.count(); i++)
            {
                int member = fields.at(i); b.op(0x25); b.emit(0x20, (int)i);
                if (mode == 1)
                {
                    b.emit(0x20, 2); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType));
                    b.op(0x25); b.emit(0x20, 0); il_text(get_field(member, c).name, c, b); b.op(0xa2);
                    b.op(0x25); b.emit(0x20, 1);
                }
                b.emit(0xfe09, 0);
                if (mode == 2) { b.emit(0x7b, b.field_token(member)); }
                else { b.load_field(member, c); b.box(get_field(member, c).type, c); }
                if (mode == 1) { b.op(0xa2); b.emit(0x73, b.RuntimeToken(RuntimeMember.ArrayBufferConstructor)); }
                b.op(mode == 2 ? 0xa1 : 0xa2);
            }
            if (mode == 2) { b.call(RuntimeMember.NumberValuesFromFields); }
            else { b.emit(0x73, b.RuntimeToken(RuntimeMember.ArrayBufferConstructor)); }
            b.op(0x2a);
        }
        b.mark(slow); b.emit(0xfe09, 0); b.call(fallback); b.op(0x2a);
    }
    internal static void il_enumeration_shape(int type, int klass, int literal, Compiler c, IlBackend b)
    {
        if (!b.enumerate_values && !b.enumerate_entries) { return; }
        Vector<int> fields = new Vector<int>();
        if (literal == 0)
        {
            int member = get_type(type, c).fields;
            while (member != 0) { if (fields.count() >= 16) { return; } fields.push(member); member = get_field(member, c).next; }
        }
        else
        {
            int child = get_node(literal, c).children;
            while (child != 0)
            {
                int member = field(type, get_node(child, c).name, c); if (member == 0 || fields.count() >= 16) { return; }
                fields.push(member); child = get_node(child, c).next;
            }
        }
        il_sort_enumeration(fields, c); bool numbers = true;
        for (int i = 0; i < fields.count(); i++) { if (get_type(get_field(fields.at(i), c).type, c).kind != TypeKind.Number) { numbers = false; } }
        if (b.enumerate_values)
        {
            il_enumeration_projection(klass, 0, fields, c, b);
            if (numbers) { il_enumeration_projection(klass, 2, fields, c, b); }
        }
        if (b.enumerate_entries) { il_enumeration_projection(klass, 1, fields, c, b); }
    }
}
