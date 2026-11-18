namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, (int Function, int Receiver)> loop_update_statements = [];
}

internal static partial class CompilerFunctions
{
    // No user code may run between the method guard and the last call. Restrict
    // the body to a numeric own-field update; the original method still runs.
    static bool il_class_update_loop(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        if (counter == 0 || bound < 0 || b.loop_update_statements.Count != 0) return false;
        Node loop = get_node(entry, c), block = get_node(loop.fourth, c);
        int statement = block.kind == NodeKind.Block ? block.children : loop.fourth;
        Node expression = get_node(statement, c);
        if (expression.kind != NodeKind.Expression || expression.next != 0) return false;
        Node call = get_node(expression.left, c);
        if (call.kind != NodeKind.Call) return false;
        int receiverEntry = 0, name = 0, helperSymbol = 0;
        Node member = get_node(call.left, c);
        if (member.kind == NodeKind.Property && member.fourth == 0 && call.children == 0) {
            receiverEntry = member.left; name = member.name;
        } else {
            helperSymbol = il_direct_function(call.left, c, b);
            if (helperSymbol == 0 || member.kind != NodeKind.Name || member.symbol != helperSymbol ||
                b.function_contexts.at(helperSymbol) != 0 || call.children == 0 || get_node(call.children, c).next != 0) return false;
            Node helper = get_node(get_symbol(helperSymbol, c).function, c);
            if (helper.fourth != 0 || helper.right != 0 || helper.constant || helper.children == 0) return false;
            Node parameter = get_node(helper.children, c);
            if (!ordinary(parameter) || parameter.next != 0 ||
                parameter.type != get_node(call.children, c).type && get_type(parameter.type, c).kind != TypeKind.Any) return false;
            Node body = get_node(helper.left, c);
            Node forwarded = get_node(body.kind == NodeKind.Block ? body.children : helper.left, c);
            if (forwarded.next != 0 || forwarded.kind is not (NodeKind.Expression or NodeKind.Return)) return false;
            Node invocation = get_node(forwarded.left, c);
            member = get_node(invocation.left, c);
            if (invocation.kind != NodeKind.Call || invocation.children != 0 || member.kind != NodeKind.Property || member.fourth != 0) return false;
            Node target = get_node(member.left, c);
            if (target.kind != NodeKind.Name || target.symbol != parameter.symbol) return false;
            receiverEntry = call.children; name = member.name;
        }
        Node receiver = get_node(receiverEntry, c);
        if (name == 0 || receiver.kind != NodeKind.Name || receiver.symbol == 0 || receiver.symbol == counter ||
            b.global_fields.at(receiver.symbol) != 0 || b.capture_fields.at(receiver.symbol) != 0 ||
            get_type(receiver.type, c).name == 0 || !il_numeric_class_eligible(receiver.type, c, b)) return false;

        int source = 0;
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node candidate = get_node(i, c);
            if (candidate.kind != NodeKind.ClassValue || candidate.fourth == 0 || get_node(candidate.fourth, c).type != receiver.type) continue;
            if (source != 0 || candidate.third != 0) return false;
            source = i;
        }
        if (source == 0) return false;
        int functionSymbol = 0, fieldName = 0;
        for (int item = get_node(source, c).children; item != 0; item = get_node(item, c).next) {
            Node method = get_node(item, c);
            if (!same(method.name, name, c)) continue;
            if (functionSymbol != 0 || method.constant || method.right != 0 || method.third != 0 || method.fourth != 0) return false;
            int functionEntry = scalar_field_function(method.left, c);
            if (functionEntry == 0) return false;
            Node function = get_node(functionEntry, c);
            if (function.fourth == 0 || function.right != 0 || function.constant || function.children != function.fourth ||
                b.function_contexts.at(function.symbol) != 0 || b.object_home_functions.Contains(function.symbol) ||
                !b.function_class_types.ContainsKey(function.symbol)) return false;
            Node self = get_node(function.fourth, c);
            if (!ordinary(self) || self.next != 0 || self.name != 0) return false;
            Node body = get_node(function.left, c);
            Node action = get_node(body.kind == NodeKind.Block ? body.children : function.left, c);
            if (action.kind != NodeKind.Expression || action.next != 0) return false;
            Node update = get_node(action.left, c), property = get_node(update.left, c);
            if (property.kind != NodeKind.Property || property.fourth != 0 || get_type(property.type, c).kind != TypeKind.Number) return false;
            Node target = get_node(property.left, c);
            if (target.kind == NodeKind.ResolveThis) target = get_node(target.left, c);
            if (target.kind != NodeKind.Name || target.symbol != self.symbol) return false;
            int operation = get_token(update.token, c).text;
            if (update.kind == NodeKind.Update) {
                if (!is_text(operation, text_increment, c) && !is_text(operation, text_decrement, c)) return false;
            } else if (update.kind != NodeKind.Assign || get_node(update.right, c).kind != NodeKind.Number ||
                !is_text(operation, text_add_assign, c) && !is_text(operation, text_subtract_assign, c)) return false;
            functionSymbol = function.symbol; fieldName = property.name;
        }
        if (functionSymbol == 0) return false;
        var fields = b.numeric_class_members[receiver.type];
        int fieldIndex = fields.FindIndex(field => same(get_field(field, c).name, fieldName, c) && get_type(get_field(field, c).type, c).kind == TypeKind.Number);
        if (fieldIndex < 0) return false;
        double initial = number(get_token(get_node(get_node(il_loop_binding(entry, c), c).left, c).token, c).text, c);
        Node limit = get_node(get_node(loop.right, c).right, c);
        if (limit.kind == NodeKind.Number && number(get_token(limit.token, c).text, c) <= initial + 31) return false;

        int fallback = b.label(), done = b.label(), instance = b.local(b.classes.at(receiver.type));
        b.emit(0xfe0c, bound); b.plan.number(b.method, initial + 31); b.op(0xfe02); b.emit(0x39, fallback);
        if (helperSymbol != 0 && b.global_flags.at(helperSymbol) != 0) b.check_global(helperSymbol);
        il_boxed(receiverEntry, c, b); b.emit(0x75, b.classes.at(receiver.type)); b.emit(0xfe0e, instance);
        b.emit(0xfe0c, instance); b.emit(0x39, fallback);
        b.emit(0xfe0c, instance); il_text(name, c, b); b.call(RuntimeMember.GetClassMethodData);
        b.emit(0x75, b.function_class_types[functionSymbol]); b.emit(0x39, fallback);
        b.emit(0xfe0c, instance); il_text(fieldName, c, b); b.call(RuntimeMember.HasOwnNumberData); b.emit(0x39, fallback);
        b.loop_update_statements[statement] = (functionSymbol, instance);
        il_loop_code(entry, result, counter, bound, c, b);
        b.loop_update_statements.Remove(statement);
        b.emit(0x38, done); b.mark(fallback);
        il_loop_code(entry, result, counter, bound, c, b); b.mark(done);
        return true;

        static bool ordinary(Node value) => value.symbol != 0 && !value.constant && value.left == 0 && value.right == 0 &&
            value.third == 0 && value.fourth == 0 && value.children == 0;
    }
}
