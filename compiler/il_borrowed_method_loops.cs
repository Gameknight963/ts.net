namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    internal static bool il_borrowed_method_loop(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        if (counter == 0 || bound < 0 || b.loop_getter_bodies.Count != 0 || b.loop_getter_values.Count != 0 ||
            b.loop_getter_choices.Count != 0 || b.global_fields.at(counter) != 0 || b.capture_fields.at(counter) != 0) return false;
        Node loop = get_node(entry, c);
        if (loop.fourth == 0) return false;
        Node body = get_node(loop.fourth, c);
        int statement = body.kind == NodeKind.Block ? body.children : loop.fourth;
        if (statement == 0) return false;
        Node expression = get_node(statement, c);
        if (expression.kind != NodeKind.Expression || expression.left == 0 || expression.next != 0) return false;
        Node assignment = get_node(expression.left, c);
        if (assignment.kind != NodeKind.Assign || !is_text(get_token(assignment.token, c).text, text_add_assign, c)) return false;
        Node total = get_node(assignment.left, c), call = get_node(assignment.right, c);
        if (total.kind != NodeKind.Name || total.symbol == counter || get_type(total.type, c).kind != TypeKind.Number ||
            b.global_fields.at(total.symbol) != 0 || b.capture_fields.at(total.symbol) != 0 || call.kind != NodeKind.Call ||
            get_type(call.type, c).kind != TypeKind.Number) return false;
        int parameter = counter, ignored = 0, ignoredType = 0, helperFirst = 0;
        Node callee = get_node(call.left, c);
        if (callee.kind == NodeKind.Name)
        {
            int helper = il_direct_function(call.left, c, b);
            if (helper == 0 || b.function_contexts.at(helper) != 0 || b.global_flags.at(callee.symbol) != 0) return false;
            Node function = get_node(get_symbol(helper, c).function, c);
            if (function.fourth != 0 || function.right != 0 || function.internal_continuation || function.children == 0 || function.left == 0) return false;
            Node first = get_node(function.children, c);
            if (first.kind != NodeKind.Variable || first.next == 0) return false;
            Node second = get_node(first.next, c);
            bool plain(Node p) => p.kind == NodeKind.Variable && p.symbol != 0 && !p.constant &&
                p.left == 0 && p.right == 0 && p.third == 0 && p.fourth == 0 && p.children == 0;
            if (!plain(first) || first.next == 0 || !plain(second) || second.next != 0 ||
                get_type(first.type, c).kind is not (TypeKind.Shape or TypeKind.Any) || get_type(second.type, c).kind != TypeKind.Number) return false;
            Node forwardedBody = get_node(function.left, c);
            int returnedEntry = forwardedBody.kind == NodeKind.Block ? forwardedBody.children : function.left;
            if (returnedEntry == 0) return false;
            Node returned = get_node(returnedEntry, c);
            if (returned.kind != NodeKind.Return || returned.next != 0) return false;
            Node forwarded = get_node(returned.left, c);
            if (forwarded.kind != NodeKind.Call || get_type(forwarded.type, c).kind != TypeKind.Number) return false;
            if (call.children == 0) return false;
            Node originalFirst = get_node(call.children, c);
            if (originalFirst.next == 0) return false;
            Node originalSecond = get_node(originalFirst.next, c);
            if (originalFirst.kind != NodeKind.Name || originalFirst.symbol == counter || originalFirst.symbol == total.symbol ||
                originalFirst.next == 0 || originalSecond.kind != NodeKind.Name || originalSecond.symbol != counter || originalSecond.next != 0) return false;
            ignored = call.children; ignoredType = first.type; parameter = second.symbol; helperFirst = first.symbol;
            call = forwarded; callee = get_node(call.left, c);
        }
        if (callee.kind != NodeKind.Property || callee.fourth != 0 || !is_text(callee.name, "call", c)) return false;
        Node functionName = get_node(callee.left, c);
        if (functionName.kind != NodeKind.Name || functionName.symbol == 0 || functionName.symbol == counter || functionName.symbol == total.symbol || functionName.symbol == helperFirst) return false;
        if (call.children == 0) return false;
        Node receiver = get_node(call.children, c);
        if (receiver.next == 0) return false;
        Node argument = get_node(receiver.next, c);
        int receiverEntry = call.children;
        if (receiver.kind == NodeKind.Assert && get_type(receiver.type, c).kind == TypeKind.Any)
        {
            receiverEntry = receiver.left; receiver = get_node(receiverEntry, c);
        }
        if (receiver.kind != NodeKind.Name || receiver.symbol == 0 || receiver.symbol == counter || receiver.symbol == total.symbol || receiver.symbol == helperFirst || receiver.symbol == parameter ||
            argument.kind != NodeKind.Name || argument.symbol != parameter || argument.next != 0) return false;
        // An as-any call argument can erase the expression's shape. The
        // declared symbol still identifies a candidate layout for the guard.
        int literalType(int symbol, int fallback)
        {
            int found = 0;
            for (int i = 1; i <= c.node_store.count(); i++) {
                Node variable = get_node(i, c);
                if (variable.kind != NodeKind.Variable || variable.symbol != symbol || variable.left == 0) continue;
                if (found != 0) return fallback;
                Node value = get_node(variable.left, c);
                if (value.kind != NodeKind.Object) return fallback;
                found = value.type;
            }
            return found != 0 ? found : fallback;
        }
        int owner = literalType(receiver.symbol, get_type(receiver.type, c).kind == TypeKind.Any ? get_symbol(receiver.symbol, c).type : receiver.type);
        if (get_type(owner, c).kind != TypeKind.Shape || get_type(owner, c).name != 0 || b.classes.at(owner) == 0 || b.spread_types.at(owner) != 0) return false;
        int initializer = 0;
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node variable = get_node(i, c);
            if (variable.kind != NodeKind.Variable || variable.symbol != functionName.symbol || variable.left == 0) continue;
            if (initializer != 0) return false;
            initializer = variable.left;
        }
        if (initializer == 0) return false;
        Node source = get_node(initializer, c);
        if (source.kind != NodeKind.Property || source.fourth != 0) return false;
        int methodOwner = get_node(source.left, c).type;
        Node sourceOwner = get_node(source.left, c);
        if (sourceOwner.kind == NodeKind.Name) methodOwner = literalType(sourceOwner.symbol, methodOwner);
        if (!b.simple_class_methods.TryGetValue(methodOwner, out var methods)) return false;
        SimpleClassMethod method = default;
        foreach (var candidate in methods) if (same(candidate.Name, source.name, c)) {method = candidate;break;}
        if (method.Function == 0 || !b.function_class_types.TryGetValue(method.Function, out int functionType)) return false;
        var names = new List<int>();
        void collect(int item)
        {
            Node node = get_node(item, c);
            if (node.kind == NodeKind.Property) {if (!names.Any(name => same(name, node.name, c))) names.Add(node.name);return;}
            if (node.left != 0) collect(node.left);
            if (node.right != 0) collect(node.right);
        }
        collect(method.Expression);
        foreach (int name in names)
        {
            int member = field(owner, name, c);
            if (member == 0 || get_field(member, c).optional || b.field_token(member) == 0 ||
                il_dynamic_index_field(member, c) || b.field_storage(member, c) != -3) return false;
        }
        int binding = il_loop_binding(entry, c);
        double initial = number(get_token(get_node(get_node(binding, c).left, c).token, c).text, c);
        Node limit = get_node(get_node(loop.right, c).right, c);
        if (limit.kind == NodeKind.Number && number(get_token(limit.token, c).text, c) <= initial + 31) return false;
        int ordinary = b.label(), done = b.label(), functionSlot = b.local(-5), instance = b.local(b.classes.at(owner));
        b.emit(0xfe0c, bound); b.plan.number(b.method, initial + 31); b.op(0xfe02); b.emit(0x39, ordinary);
        Token location = get_token(expression.token, c);
        if (location.file != 0 && location.line > 0) {
            Vector<byte> file = new();
            for (int i = 0; i < text_size(location.file, c); i++) file.push(text_byte(location.file, i, c));
            b.plan.source_point(b.method, file.view(), location.line, Math.Clamp(location.column, 1, 65534));
        }
        b.load(total.symbol); b.op(0x26);
        if (ignored != 0) {
            il_boxed(ignored, c, b);
            Type ignoredShape = get_type(ignoredType, c);
            if (ignoredShape.kind == TypeKind.Shape && (ignoredShape.name != 0 || ignoredShape.fields != 0)) {
                // Repeating a structural conversion can execute proxy traps.
                // Only skip it when the CLR type already satisfies the check.
                int expected = ignoredShape.name != 0 ? b.RuntimeToken(RuntimeMember.TsObjectType) : b.interfaces.at(ignoredType);
                b.emit(0x75, expected); b.emit(0x39, ordinary);
            } else b.op(0x26);
        }
        il_buffer_binding_guard(functionName.symbol, ordinary, b);
        il_boxed(callee.left, c, b); b.emit(0xfe0e, functionSlot);
        b.emit(0xfe0c, functionSlot); b.emit(0x75, functionType); b.emit(0x39, ordinary);
        b.emit(0xfe0c, functionSlot); b.call(RuntimeMember.HasBuiltinFunctionCall); b.emit(0x39, ordinary);
        il_buffer_binding_guard(receiver.symbol, ordinary, b);
        il_boxed(receiverEntry, c, b); b.emit(0x75, b.classes.at(owner)); b.emit(0xfe0e, instance);
        b.emit(0xfe0c, instance); b.emit(0x28, b.plan.specialize(b.RuntimeToken(RuntimeMember.IsPlainDataRecord), b.classes.at(owner))); b.emit(0x39, ordinary);
        var fields = new List<(int Name, int Slot)>();
        foreach (int name in names)
        {
            int slot = b.local(-3); fields.Add((name, slot));
            b.emit(0xfe0c, instance); b.load_field(field(owner, name, c), c); b.call(RuntimeMember.NumberValue); b.emit(0xfe0e, slot);
        }
        b.loop_getter_bodies[assignment.right] = new(method.Expression, fields, method.Parameter, counter);
        il_loop_code(entry, result, counter, bound, c, b);
        b.loop_getter_bodies.Remove(assignment.right); b.emit(0x38, done); b.mark(ordinary);
        il_loop_code(entry, result, counter, bound, c, b); b.mark(done); return true;
    }
}
