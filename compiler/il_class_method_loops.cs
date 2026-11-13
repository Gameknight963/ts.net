namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    internal static bool il_class_method_loop(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        if (counter == 0 || bound < 0 || b.loop_getter_values.Count != 0 || b.loop_getter_bodies.Count != 0 ||
            b.global_fields.at(counter) != 0 || b.capture_fields.at(counter) != 0) return false;
        Node loop = get_node(entry, c), block = get_node(loop.fourth, c);
        int statement = block.kind == NodeKind.Block ? block.children : loop.fourth;
        if (statement == 0) return false;
        Node expression = get_node(statement, c);
        if (expression.kind != NodeKind.Expression || expression.next != 0) return false;
        Node assignment = get_node(expression.left, c);
        if (assignment.kind != NodeKind.Assign || !is_text(get_token(assignment.token, c).text, "+=", c)) return false;
        Node total = get_node(assignment.left, c), call = get_node(assignment.right, c);
        if (total.kind != NodeKind.Name || total.symbol == counter || get_type(total.type, c).kind != TypeKind.Number ||
            b.global_fields.at(total.symbol) != 0 || b.capture_fields.at(total.symbol) != 0 || call.kind != NodeKind.Call || get_type(call.type, c).kind != TypeKind.Number) return false;
        Node callee = get_node(call.left, c);
        if (callee.kind != NodeKind.Name || callee.symbol == 0 || get_symbol(callee.symbol, c).function == 0 || il_direct_function(call.left, c, b) == 0) return false;
        Node helper = get_node(get_symbol(callee.symbol, c).function, c);
        if (helper.fourth != 0 || helper.right != 0 || helper.constant || get_type(helper.type, c).kind != TypeKind.Number || helper.children == 0) return false;
        bool ordinary(Node p) => p.symbol != 0 && !p.constant && p.left == 0 && p.right == 0 && p.third == 0 && p.fourth == 0 && p.children == 0;
        Node first = get_node(helper.children, c);
        if (!ordinary(first) || first.next == 0 || get_type(first.type, c).kind is not (TypeKind.Shape or TypeKind.Any)) return false;
        Node second = get_node(first.next, c);
        if (!ordinary(second) || second.next != 0 || get_type(second.type, c).kind != TypeKind.Number) return false;
        Node helperBody = get_node(helper.left, c);
        Node returned = get_node(helperBody.kind == NodeKind.Block ? helperBody.children : helper.left, c);
        if (returned.kind != NodeKind.Return || returned.next != 0) return false;
        Node forwarded = get_node(returned.left, c);
        if (forwarded.kind != NodeKind.Call || forwarded.children == 0) return false;
        Node member = get_node(forwarded.left, c), forwardedArgument = get_node(forwarded.children, c);
        if (member.kind != NodeKind.Property || member.fourth != 0 || member.name == 0 || forwardedArgument.kind != NodeKind.Name ||
            forwardedArgument.symbol != second.symbol || forwardedArgument.next != 0) return false;
        Node target = get_node(member.left, c);
        if (target.kind != NodeKind.Name || target.symbol != first.symbol || call.children == 0) return false;
        Node receiver = get_node(call.children, c);
        if (receiver.kind != NodeKind.Name || receiver.symbol == 0 || receiver.next == 0 || receiver.symbol == total.symbol || b.global_vars.at(receiver.symbol) != 0) return false;
        Node argument = get_node(receiver.next, c);
        if (argument.kind != NodeKind.Name || argument.symbol != counter || argument.next != 0) return false;
        int type = receiver.type;
        if (get_type(type, c).kind != TypeKind.Shape || get_type(type, c).name == 0 || !b.simple_class_methods.TryGetValue(type, out var methods)) return false;
        SimpleClassMethod method = default;
        foreach (var candidate in methods) if (same(candidate.Name, member.name, c)) { method = candidate; break; }
        if (method.Function == 0 || !b.function_class_types.TryGetValue(method.Function, out int functionType)) return false;
        int binding = il_loop_binding(entry, c);
        double initial = number(get_token(get_node(get_node(binding, c).left, c).token, c).text, c);
        Node limit = get_node(get_node(loop.right, c).right, c);
        if (limit.kind == NodeKind.Number && number(get_token(limit.token, c).text, c) <= initial + 31) return false;
        var names = new List<int>();
        void collect(int node)
        {
            Node value = get_node(node, c);
            if (value.kind == NodeKind.Property) { if (!names.Any(name => same(name, value.name, c))) names.Add(value.name); return; }
            if (value.left != 0) collect(value.left);
            if (value.right != 0) collect(value.right);
        }
        collect(method.Expression);
        int ordinaryLoop = b.label(), done = b.label(), instance = b.local(b.classes.at(type));
        b.emit(0xfe0c, bound); b.plan.number(b.method, initial + 31); b.op(0xfe02); b.emit(0x39, ordinaryLoop);
        Token location = get_token(expression.token, c);
        if (location.file != 0 && location.line > 0)
        {
            Vector<byte> file = new();
            for (int i = 0; i < text_size(location.file, c); i++) file.push(text_byte(location.file, i, c));
            b.plan.source_point(b.method, file.view(), location.line, Math.Clamp(location.column, 1, 65534));
        }
        b.load(total.symbol); b.op(0x26);
        il_boxed(call.children, c, b); b.emit(0x75, b.classes.at(type)); b.emit(0xfe0e, instance);
        b.emit(0xfe0c, instance); b.emit(0x39, ordinaryLoop);
        // Read only ordinary prototype data: an accessor must run on every call.
        b.emit(0xfe0c, instance); il_text(member.name, c, b); b.call(RuntimeMember.GetClassMethodData);
        b.emit(0x75, functionType); b.emit(0x39, ordinaryLoop);
        foreach (int name in names)
        {
            b.emit(0xfe0c, instance); il_text(name, c, b); b.call(RuntimeMember.HasOwnNumberData); b.emit(0x39, ordinaryLoop);
        }
        var fields = new List<(int Name, int Slot)>();
        foreach (int name in names)
        {
            int slot = b.local(-3); fields.Add((name, slot));
            b.emit(0xfe0c, instance); il_text(name, c, b); b.call(RuntimeMember.GetNumber); b.call(RuntimeMember.NumberValue); b.emit(0xfe0e, slot);
        }
        // The helper forwards its arguments, and the guarded method is numeric
        // arithmetic only. No user callback can change the method or these fields.
        b.loop_getter_bodies[assignment.right] = new(method.Expression, fields, method.Parameter, counter);
        il_loop_code(entry, result, counter, bound, c, b);
        b.loop_getter_bodies.Remove(assignment.right);
        b.emit(0x38, done); b.mark(ordinaryLoop);
        il_loop_code(entry, result, counter, bound, c, b); b.mark(done);
        return true;
    }
}
