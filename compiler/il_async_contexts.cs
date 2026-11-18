namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> async_context_caches = [];
}

internal static partial class CompilerFunctions
{
    static void il_prepare_async_contexts(Compiler c, IlBackend b)
    {
        // Only cache frames on a resumption cycle. One-shot await chains would
        // otherwise pay for extra fields and branches without reusing anything.
        var bindings = new Dictionary<int, int>();
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node node = get_node(i, c);
            if (node.kind == NodeKind.Variable && node.symbol != 0 && node.left != 0)
                bindings[node.symbol] = node.left;
        }
        int Target(int entry) {
            var seen = new HashSet<int>();
            while (entry != 0 && seen.Add(entry)) {
                Node node = get_node(entry, c);
                if (node.kind == NodeKind.FunctionValue) return get_symbol(node.symbol, c).function;
                if (node.kind == NodeKind.AsyncResume) { entry = node.left; continue; }
                if (node.kind == NodeKind.Name && bindings.TryGetValue(node.symbol, out int value)) { entry = value; continue; }
                break;
            }
            return 0;
        }
        var edges = new Dictionary<int, HashSet<int>>();
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node node = get_node(i, c);
            int target = node.kind == NodeKind.Return ? Target(node.left)
                : node.kind == NodeKind.AsyncAwait ? Target(node.right) : 0;
            int owner = scalar_owner(i, b.node_parents, c);
            if (target == 0 || owner == 0 || !get_node(owner, c).internal_continuation) continue;
            if (!edges.TryGetValue(owner, out var successors)) edges[owner] = successors = [];
            successors.Add(target);
        }
        bool IsRepeated(int scope) {
            var pending = new Stack<int>();
            var seen = new HashSet<int>();
            if (edges.TryGetValue(scope, out var first)) foreach (int next in first) pending.Push(next);
            while (pending.TryPop(out int current)) {
                if (current == scope) return true;
                if (seen.Add(current) && edges.TryGetValue(current, out var next))
                    foreach (int target in next) pending.Push(target);
            }
            return false;
        }
        var visible = new HashSet<int>();
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node node = get_node(i, c);
            if (node.kind != NodeKind.Name || node.symbol == 0) continue;
            int scope = b.capture_scopes.at(node.symbol);
            if (scope == 0) continue;
            int owner = scalar_owner(i, b.node_parents, c);
            if (owner == 0 || !get_node(owner, c).internal_continuation) visible.Add(scope);
        }
        for (int scope = 1; scope < b.context_types.count(); scope++) {
            if (b.context_types.at(scope) == 0 || visible.Contains(scope) || !IsRepeated(scope)) continue;
            Node function = get_node(scope, c);
            if (function.kind != NodeKind.Function || !function.internal_continuation || function.children != 0 || function.fourth != 0) continue;
            int parent = b.context_parents.at(scope);
            if (parent == 0) continue;
            int root = scalar_owner(b.node_parents.at(scope), b.node_parents, c);
            while (root != 0 && get_node(root, c).internal_continuation) root = scalar_owner(b.node_parents.at(root), b.node_parents, c);
            if (root == 0 || get_node(root, c).right == 0 || scalar_owner(parent, b.node_parents, c) != root) continue;
            Node marker = get_node(get_node(root, c).right, c);
            if (is_text(marker.name, "generator", c) || is_text(marker.name, "async generator", c)) continue;
            bool repeated = false;
            for (int current = parent; current != root && current != 0; current = b.node_parents.at(current)) {
                if (get_node(current, c).kind is NodeKind.For or NodeKind.While or NodeKind.Do or NodeKind.ForOf or NodeKind.ForIn) { repeated = true; break; }
            }
            if (repeated) continue;
            bool valuesOnly = true;
            for (int symbol = 1; symbol < b.capture_scopes.count(); symbol++) {
                if (b.capture_scopes.at(symbol) == scope && get_type(get_symbol(symbol, c).type, c).kind is not (TypeKind.Number or TypeKind.Boolean)) { valuesOnly = false; break; }
            }
            if (!valuesOnly) continue;
            // The parent belongs to this async activation. These value bindings
            // are observed only by private continuations, which run sequentially.
            Vector<byte> name = numbered_name('r', scope);
            b.async_context_caches[scope] = b.plan.field(b.context_types.at(parent), 6, b.context_types.at(scope), name.view());
        }
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node node = get_node(i, c);
            if (node.kind != NodeKind.AsyncAwait || node.right == 0) continue;
            Node value = get_node(node.right, c);
            if (value.kind != NodeKind.FunctionValue || value.symbol == 0 || b.yield_continuation_caches.at(value.symbol) != 0) continue;
            int context = b.function_contexts.at(value.symbol);
            if (!b.async_context_caches.ContainsKey(context) || !get_node(get_symbol(value.symbol, c).function, c).internal_continuation) continue;
            Vector<byte> name = numbered_name('y', value.symbol);
            b.yield_continuation_caches.at_mut(value.symbol) = b.plan.field(b.context_types.at(context), 6, b.RuntimeToken(RuntimeMember.FunctionType), name.view());
        }
    }

    static bool il_async_context_enter(int scope, Compiler c, IlBackend b)
    {
        if (!b.async_context_caches.TryGetValue(scope, out int cache)) return false;
        int parent = b.context_parents.at(scope), type = b.context_types.at(scope), slot = b.local(type);
        int reuse = b.label(), ready = b.label();
        b.load_context(parent); b.emit(0x7b, cache); b.emit(0xfe0e, slot);
        b.emit(0xfe0c, slot); b.emit(0x3a, reuse);
        b.load_context(parent); b.load_context(parent); b.emit(0x73, b.context_constructors.at(scope));
        b.op(0x25); b.emit(0xfe0e, slot); b.emit(0x7d, cache); b.emit(0x38, ready);
        b.mark(reuse);
        // Re-entry has the same values and initialization flags as a fresh frame.
        // Callback cache fields are compiler-owned and deliberately survive.
        for (int symbol = 1; symbol < b.capture_scopes.count(); symbol++) {
            if (b.capture_scopes.at(symbol) != scope) continue;
            b.emit(0xfe0c, slot); b.empty(get_symbol(symbol, c).type, c); b.emit(0x7d, b.capture_fields.at(symbol));
            b.emit(0xfe0c, slot); b.emit(0x20, b.capture_vars.at(symbol) != 0 ? 1 : 0); b.emit(0x7d, b.capture_flags.at(symbol));
        }
        b.mark(ready); b.context_scope = scope; b.context_slot = slot;
        return true;
    }
}
