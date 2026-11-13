namespace Tsnet.Compiler;

internal readonly record struct LoopGetterBody(int Expression, List<(int Name, int Slot)> Fields);

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> loop_getter_values = [];
    internal readonly Dictionary<int, LoopGetterBody> loop_getter_bodies = [];
    internal readonly Dictionary<int, int> loop_field_writes = [];
}

internal static partial class CompilerFunctions
{
    internal static bool il_loop_getter_read(int entry, Compiler c, IlBackend b)
    {
        if (b.loop_getter_values.TryGetValue(entry, out int saved)) { b.emit(0xfe0c, saved); return true; }
        if (!b.loop_getter_bodies.TryGetValue(entry, out var body)) return false;
        il_simple_getter_number(body.Expression, c, b, fields: body.Fields); return true;
    }
    internal static bool il_loop_field_write(int entry, Compiler c, IlBackend b)
    {
        if (!b.loop_field_writes.TryGetValue(entry, out int slot)) return false;
        il_number(get_node(entry, c).right, c, b); b.op(0x25); b.emit(0xfe0e, slot); b.call(RuntimeMember.WrapNumber);
        return true;
    }
    internal static bool il_class_getter_loop(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        if (counter == 0 || bound < 0 || b.loop_getter_values.Count != 0 || b.loop_getter_bodies.Count != 0 ||
            b.global_fields.at(counter) != 0 || b.capture_fields.at(counter) != 0) return false;
        Node loop = get_node(entry, c), body = get_node(loop.fourth, c);
        int statement = body.kind == NodeKind.Block ? body.children : loop.fourth;
        if (statement == 0) return false;
        Node expression = get_node(statement, c);
        if (expression.kind != NodeKind.Expression) return false;
        int writeEntry = 0;
        Node write = default;
        if (expression.next != 0)
        {
            writeEntry = expression.left; write = get_node(writeEntry, c);
            expression = get_node(expression.next, c);
        }
        if (expression.kind != NodeKind.Expression || expression.next != 0) return false;
        Node assignment = get_node(expression.left, c);
        if (assignment.kind != NodeKind.Assign || !is_text(get_token(assignment.token, c).text, "+=", c)) return false;
        Node total = get_node(assignment.left, c), property = get_node(assignment.right, c);
        if (total.kind != NodeKind.Name || total.symbol == counter || get_type(total.type, c).kind != TypeKind.Number ||
            b.global_fields.at(total.symbol) != 0 || b.capture_fields.at(total.symbol) != 0 ||
            property.kind != NodeKind.Property || property.fourth != 0 || get_type(property.type, c).kind != TypeKind.Number) return false;
        Node receiver = get_node(property.left, c);
        if (receiver.kind != NodeKind.Name || receiver.symbol == 0 || receiver.symbol == total.symbol || b.global_vars.at(receiver.symbol) != 0) return false;
        int writtenName = 0;
        if (writeEntry != 0)
        {
            if (write.kind != NodeKind.Assign || !is_text(get_token(write.token, c).text, text_assign, c) || get_type(write.type, c).kind != TypeKind.Number) return false;
            Node destination = get_node(write.left, c), supplied = get_node(write.right, c);
            if (destination.kind != NodeKind.Property || destination.name == 0 || destination.fourth != 0 || get_type(destination.type, c).kind != TypeKind.Number ||
                supplied.kind != NodeKind.Name || supplied.symbol != counter) return false;
            Node owner = get_node(destination.left, c);
            if (owner.kind != NodeKind.Name || owner.symbol != receiver.symbol) return false;
            writtenName = destination.name;
        }
        int type = receiver.type;
        if (get_type(type, c).kind != TypeKind.Shape || get_type(type, c).name == 0 ||
            !il_simple_class_getter(type, property.name, c, b, out var getter)) return false;
        int klass = b.classes.at(type);
        if (klass == 0) return false;
        int binding = il_loop_binding(entry, c);
        double initial = number(get_token(get_node(get_node(binding, c).left, c).token, c).text, c);
        Node limit = get_node(get_node(loop.right, c).right, c);
        if (limit.kind == NodeKind.Number && number(get_token(limit.token, c).text, c) <= initial + 31) return false;
        var fields = new List<int>();
        void collect(int node)
        {
            Node value = get_node(node, c);
            if (value.kind == NodeKind.Property) { if (!fields.Any(name => same(name, value.name, c))) fields.Add(value.name); return; }
            if (value.left != 0) collect(value.left);
            if (value.right != 0) collect(value.right);
        }
        collect(getter.Expression);
        if (writeEntry != 0 && !fields.Any(name => same(name, writtenName, c))) fields.Add(writtenName);
        int ordinary = b.label(), done = b.label(), saved = b.local(-3), instance = b.local(klass);
        // Keep short loops on the ordinary path. This also prevents evaluating
        // the receiver or accumulator binding for empty loops.
        b.emit(0xfe0c, bound); b.plan.number(b.method, initial + 31); b.op(0xfe02); b.emit(0x39, ordinary);
        Token location = get_token(writeEntry == 0 ? expression.token : get_node(statement, c).token, c);
        if (location.file != 0 && location.line > 0)
        {
            Vector<byte> file = new();
            for (int i = 0; i < text_size(location.file, c); i++) file.push(text_byte(location.file, i, c));
            b.plan.source_point(b.method, file.view(), location.line, Math.Clamp(location.column, 1, 65534));
        }
        // A compound assignment reads its local accumulator before its RHS.
        if (writeEntry == 0) { b.load(total.symbol); b.op(0x26); }
        il_boxed(property.left, c, b); b.emit(0x75, klass); b.emit(0xfe0e, instance);
        b.emit(0xfe0c, instance); b.emit(0x39, ordinary);
        b.emit(0xfe0c, instance); b.emit(0x20, getter.Slot); b.call(RuntimeMember.MatchesSimpleClassGetter); b.emit(0x39, ordinary);
        if (writeEntry != 0)
        {
            b.emit(0xfe0c, instance); il_text(writtenName, c, b); b.call(RuntimeMember.CanWriteOwnNumberData); b.emit(0x39, ordinary);
        }
        foreach (int name in fields)
        {
            b.emit(0xfe0c, instance); il_text(name, c, b); b.call(RuntimeMember.HasOwnNumberData); b.emit(0x39, ordinary);
        }
        int writtenSlot = -1;
        if (writeEntry == 0)
        {
            // No body writes or calls can change these guarded data fields.
            il_number(assignment.right, c, b); b.emit(0xfe0e, saved);
            b.loop_getter_values[assignment.right] = saved;
        }
        else
        {
            var slots = new List<(int Name, int Slot)>();
            foreach (int name in fields)
            {
                int slot = b.local(-3); slots.Add((name, slot));
                if (same(name, writtenName, c)) { writtenSlot = slot; continue; }
                b.emit(0xfe0c, instance); il_text(name, c, b);
                b.call(RuntimeMember.GetNumber); b.call(RuntimeMember.NumberValue); b.emit(0xfe0e, slot);
            }
            // The first statement assigns the counter to this writable own field.
            // No callback or escaping exit can observe intermediate stores.
            b.loop_field_writes[writeEntry] = writtenSlot;
            b.loop_getter_bodies[assignment.right] = new(getter.Expression, slots);
        }
        il_loop_code(entry, result, counter, bound, c, b);
        b.loop_getter_values.Remove(assignment.right);
        if (writeEntry != 0)
        {
            b.loop_field_writes.Remove(writeEntry); b.loop_getter_bodies.Remove(assignment.right);
            b.emit(0xfe0c, instance); il_text(writtenName, c, b); b.emit(0xfe0c, writtenSlot); b.call(RuntimeMember.WrapNumber);
            b.call(RuntimeMember.DynamicSetNumber);
        }
        b.emit(0x38, done); b.mark(ordinary);
        il_loop_code(entry, result, counter, bound, c, b); b.mark(done);
        return true;
    }
}
