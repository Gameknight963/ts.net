namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> promise_handler_caches = [];
}

internal static partial class CompilerFunctions
{
    internal static int il_reusable_promise_handler(int entry, Compiler c, IlBackend b)
    {
        Node value = get_node(entry, c);
        if (value.kind != NodeKind.FunctionValue || value.symbol == 0 || b.function_contexts.at(value.symbol) != 0) return 0;
        int definition = get_symbol(value.symbol, c).function;
        if (definition == 0) return 0;
        Node function = get_node(definition, c);
        // Only noncapturing arrows: no receiver, self identity, prototype,
        // suspension state or compiler continuation can escape this proof.
        return function.fourth == 0 && function.third == 0 && function.right == 0 &&
            !function.constant && !function.internal_continuation ? value.symbol : 0;
    }
    internal static void il_prepare_promise_handlers(int program, Compiler c, IlBackend b)
    {
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node call = get_node(i, c);
            if (call.kind != NodeKind.Call || call.type == 0 || !il_promise_then_call(i, c)) continue;
            int symbol = il_reusable_promise_handler(call.children, c, b);
            if (symbol == 0 || b.promise_handler_caches.ContainsKey(symbol)) continue;
            b.promise_handler_caches[symbol] = b.plan.field(program, 0x13, -5, numbered_name('h', get_symbol(symbol, c).id).view());
        }
    }
    internal static bool il_promise_static_number_call(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c), target = get_node(call.left, c);
        if (target.kind != NodeKind.Property || target.fourth != 0 || call.children == 0 || il_call_has_spread(entry, c)) return false;
        Node owner = get_node(target.left, c), argument = get_node(call.children, c);
        if (owner.kind != NodeKind.BuiltinNamespace || !is_text(get_token(owner.token, c).text, "Promise", c) ||
            argument.next != 0 || get_type(argument.type, c).kind != TypeKind.Number ||
            !(is_text(target.name, "resolve", c) || is_text(target.name, "reject", c))) return false;
        int receiver = il_method_target(call.left, c, b);
        b.emit(0xfe0c, receiver); il_as(call.children, argument.type, c, b);
        b.call(RuntimeMember.CallPromiseStaticNumber); il_function_result(call.type, c, b); return true;
    }
    internal static readonly TextBytes promise_then_name = "then";
    internal static bool il_promise_then_call(int entry, Compiler c)
    {
        Node call = get_node(entry, c);
        int argument = call.children;
        if (argument == 0 || get_node(argument, c).next != 0 || il_call_has_spread(entry, c)) return false;
        if (get_type(call.type, c).kind == TypeKind.Number && get_type(get_node(argument, c).type, c).kind == TypeKind.Number) return false;
        Node target = get_node(call.left, c);
        while (target.kind == NodeKind.Assert) target = get_node(target.left, c);
        int name = target.kind == NodeKind.Property ? target.name
            : target.kind == NodeKind.Index && get_node(target.right, c).kind == NodeKind.String ? get_token(get_node(target.right, c).token, c).text : 0;
        return name != 0 && is_text(name, promise_then_name, c);
    }
    internal static void il_emit_promise_then_call(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c);
        int receiver = il_method_target(call.left, c, b);
        int symbol = il_reusable_promise_handler(call.children, c, b);
        if (symbol != 0 && b.promise_handler_caches.TryGetValue(symbol, out int cache))
        {
            int target = b.local(-5), fresh = b.label(), ready = b.label();
            b.op(0x25); b.emit(0xfe0e, target); b.emit(0xfe0c, receiver);
            b.emit(0xfe0c, target); b.emit(0xfe0c, receiver); b.call(RuntimeMember.CanReusePromiseHandler);
            b.emit(0x39, fresh);
            b.emit(0x7e, cache); b.op(0x25); b.emit(0x3a, ready); b.op(0x26);
            il_boxed(call.children, c, b); b.op(0x25); b.emit(0x80, cache); b.emit(0x38, ready);
            b.mark(fresh); il_boxed(call.children, c, b);
            b.mark(ready);
        }
        else { b.emit(0xfe0c, receiver); il_boxed(call.children, c, b); }
        b.call(RuntimeMember.CallPromiseThen); il_function_result(call.type, c, b);
    }
}
