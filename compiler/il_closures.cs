namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static int il_enclosing_context(int entry, IlBackend b)
    {
        int current = entry;
        while (current != 0)
        {
            if (b.context_types.at(current) != 0) { return (int)current; }
            current = (int)b.node_parents.at(current);
        }
        return 0;
    }
    internal static int il_binding_scope(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c); int owner = scalar_owner(entry, b.node_parents, c);
        if ((n.kind == NodeKind.Variable && n.fourth != 0) || (n.kind == NodeKind.Binding && n.name != 0 && is_text(n.name, text_var, c))) { return owner; }
        int current = (int)b.node_parents.at(entry);
        while (current != 0)
        {
            Node scope = get_node(current, c);
            if (scope.kind == NodeKind.Function || scope.kind == NodeKind.For || scope.kind == NodeKind.ForOf || (scope.kind == NodeKind.Switch && !scope.constant) || (scope.kind == NodeKind.Block && scope.name != 0)) { return current; }
            current = (int)b.node_parents.at(current);
        }
        return owner;
    }
    internal static bool il_parameter_binding(int entry, Compiler c, IlBackend b)
    {
        int current = (int)b.node_parents.at(entry);
        while (current != 0)
        {
            Node n = get_node(current, c);
            if (n.kind == NodeKind.Function) { return true; }
            if (n.kind == NodeKind.Block) { return false; }
            current = (int)b.node_parents.at(current);
        }
        return false;
    }
    internal static void il_prepare_closures(Compiler c, IlBackend b)
    {
        Vector<int> declarations = zero_ids(c.symbol_store.count() + 1);
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if ((n.kind == NodeKind.Variable || n.kind == NodeKind.Binding) && n.symbol != 0)
            {
                int previous = (int)declarations.at(n.symbol);
                if (previous == 0 || !il_parameter_binding(previous, c, b)) { declarations.at_mut(n.symbol) = (int)i; }
            }
        }
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if (n.kind != NodeKind.Name || n.symbol == 0 || declarations.at(n.symbol) == 0) { continue; }
            int declaration = (int)declarations.at(n.symbol); int owner = scalar_owner(declaration, b.node_parents, c);
            if (owner == scalar_owner(i, b.node_parents, c)) { continue; }
            int scope = il_binding_scope(declaration, c, b); if (scope == 0) { continue; }
            b.capture_scopes.at_mut(n.symbol) = (int)scope; b.context_types.at_mut(scope) = -1;
            Node binding = get_node(declaration, c);
            if ((binding.kind == NodeKind.Variable && binding.fourth != 0) || (binding.kind == NodeKind.Binding && binding.name != 0 && is_text(binding.name, text_var, c))) { b.capture_vars.at_mut(n.symbol) = 1; }
            b.raw_slots.at_mut(n.symbol) = 0; b.written_symbols.at_mut(n.symbol) = 1; b.scalar_roots.at_mut(n.symbol) = 0;
        }
        // Only scopes with captured bindings receive a heap frame. Parent fields
        // retain the nearest outer frame, so unrelated locals stay in CLR locals.
        int object_type = b.RuntimeToken(RuntimeMember.ObjectType);
        for (int i = 1; i < b.context_types.count(); i++)
        {
            if (b.context_types.at(i) == 0) { continue; }
            Vector<byte> name = numbered_name('E', (int)i); b.context_types.at_mut(i) = b.plan.type(name.view(), object_type, 0x101);
        }
        for (int i = 1; i < b.context_types.count(); i++)
        {
            int type = b.context_types.at(i); if (type == 0) { continue; }
            int parent = il_enclosing_context((int)b.node_parents.at(i), b); b.context_parents.at_mut(i) = parent;
            if (parent != 0) { int parent_typeLocal = b.context_types.at((int)parent); b.context_parent_fields.at_mut(i) = b.plan.field(type, 0x26, parent_typeLocal, "parent"); }
            for (int symbol = 1; symbol < b.capture_scopes.count(); symbol++)
            {
                if (b.capture_scopes.at(symbol) != (int)i) { continue; }
                int storage = b.type(get_symbol(symbol, c).type, c); b.capture_types.at_mut(symbol) = storage;
                Vector<byte> name = numbered_name('v', (int)symbol); Vector<byte> flag = numbered_name('i', (int)symbol);
                b.capture_fields.at_mut(symbol) = b.plan.field(type, 6, storage, name.view()); b.capture_flags.at_mut(symbol) = b.plan.field(type, 6, -2, flag.view());
            }
            int parent_type = parent == 0 ? 0 : b.context_types.at((int)parent);
            int constructor = parent == 0 ? b.plan.method0(5, type, 0x1886, -1, ".ctor") : b.plan.method1(5, type, 0x1886, -1, parent_type, ".ctor");
            b.context_constructors.at_mut(i) = constructor; b.begin(constructor); b.emit(0xfe09, 0); b.call(RuntimeMember.ObjectConstructor);
            if (parent != 0) { b.emit(0xfe09, 0); b.emit(0xfe09, 1); { int operand = b.context_parent_fields.at(i); b.emit(0x7d, operand); } }
            for (int symbol = 1; symbol < b.capture_scopes.count(); symbol++)
            {
                if (b.capture_scopes.at(symbol) != (int)i) { continue; }
                b.emit(0xfe09, 0); b.empty(get_symbol(symbol, c).type, c);
                { int operand = b.capture_fields.at(symbol); b.emit(0x7d, operand); }
                if (b.capture_vars.at(symbol) != 0) { b.emit(0xfe09, 0); b.emit(0x20, 1); { int operand = b.capture_flags.at(symbol); b.emit(0x7d, operand); } }
            }
            b.op(0x2a);
        }
        int function = c.functions;
        while (function != 0) { Node n = get_node(function, c); b.function_contexts.at_mut(n.symbol) = il_enclosing_context((int)b.node_parents.at(function), b); function = n.next; }
    }
    internal static void il_context_enter(int scope, Compiler c, IlBackend b)
    {
        int type = b.context_types.at(scope); if (type == 0) { return; }
        int parent = b.context_parents.at(scope); if (parent != 0) { b.load_context(parent); }
        int constructor = b.context_constructors.at(scope); b.emit(0x73, constructor); int slot = b.local(type); b.emit(0xfe0e, slot); b.context_scope = (int)scope; b.context_slot = slot;
    }
    internal static // A for-let update runs in a fresh binding environment. Keep the local slot
                    // stable across the back edge while closures retain the previous frame.
void il_context_clone(int scope, Compiler c, IlBackend b)
    {
        if (b.context_types.at(scope) == 0) { return; }
        int destination = b.context_slot; int type = b.context_types.at(scope); int previous = b.local(type);
        b.emit(0xfe0c, destination); b.emit(0xfe0e, previous); il_context_enter(scope, c, b);
        for (int symbol = 1; symbol < b.capture_scopes.count(); symbol++)
        {
            if (b.capture_scopes.at(symbol) != (int)scope) { continue; }
            int field = b.capture_fields.at(symbol); int flag = b.capture_flags.at(symbol);
            b.emit(0xfe0c, b.context_slot); b.emit(0xfe0c, previous); b.emit(0x7b, field); b.emit(0x7d, field);
            b.emit(0xfe0c, b.context_slot); b.emit(0xfe0c, previous); b.emit(0x7b, flag); b.emit(0x7d, flag);
        }
        b.emit(0xfe0c, b.context_slot); b.emit(0xfe0e, destination); b.context_slot = destination;
    }
    internal static bool il_owns_context(int function, Compiler c, IlBackend b)
    {
        for (int i = 1; i < b.context_types.count(); i++)
        {
            if (b.context_types.at(i) != 0 && scalar_owner(i, b.node_parents, c) == function) { return true; }
        }
        return false;
    }
}
