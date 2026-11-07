namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static int il_module_owner(int entry, Compiler c, IlBackend b)
    {
        int current = entry;
        while (current != 0)
        {
            Node n = get_node(current, c);
            if (n.kind == NodeKind.Block && n.constant) { return current; }
            current = (int)b.node_parents.at(current);
        }
        return 0;
    }
    // Module initializer functions still share their top-level bindings with
    // imports and export getters; those bindings must not become closure locals.
    internal static bool il_module_binding(int entry, Compiler c, IlBackend b)
    {
        int owner = scalar_owner(entry, b.node_parents, c);
        if (owner == 0) return false;
        int body = get_node(owner, c).left;
        return body != 0 && get_node(body, c).kind == NodeKind.Block && get_node(body, c).constant && il_binding_scope(entry, c, b) == owner;
    }
    internal static void il_globals(int program, Compiler c, IlBackend b)
    {
        Vector<int> declarations = zero_ids(c.symbol_store.count() + 1);
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if ((n.kind == NodeKind.Variable || n.kind == NodeKind.Binding) && n.symbol != 0 && (scalar_owner(i, b.node_parents, c) == 0 || il_module_binding(i, c, b)) && b.capture_fields.at(n.symbol) == 0)
            {
                declarations.at_mut(n.symbol) = (int)i;
                if ((n.kind == NodeKind.Variable && n.fourth != 0) || (n.kind == NodeKind.Binding && n.name != 0 && is_text(n.name, text_var, c))) { b.global_vars.at_mut(n.symbol) = 1; }
            }
        }
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if (n.kind != NodeKind.Name || n.symbol == 0 || declarations.at(n.symbol) == 0 || b.global_fields.at(n.symbol) != 0) { continue; }
            int declaration = (int)declarations.at(n.symbol);
            if (scalar_owner(i, b.node_parents, c) == scalar_owner(declaration, b.node_parents, c) && il_module_owner(i, c, b) == il_module_owner(declaration, c, b)) { continue; }
            int type = get_symbol(n.symbol, c).type;
            if (get_type(type, c).kind == TypeKind.Void) { fail(n.token, "void-valued globals need lowering", c); }
            Vector<byte> name = numbered_name('v', (int)n.symbol); Vector<byte> flag = numbered_name('i', (int)n.symbol);
            int storage = b.type(type, c); b.global_fields.at_mut(n.symbol) = b.plan.field(program, 0x13, storage, name.view());
            if (b.global_vars.at(n.symbol) == 0) { b.global_flags.at_mut(n.symbol) = b.plan.field(program, 0x13, -2, flag.view()); }
            b.raw_slots.at_mut(n.symbol) = 0;
            if (b.global_vars.at(n.symbol) != 0 && get_type(type, c).kind == TypeKind.Number) { b.numeric_globals = true; }
            int root = (int)b.scalar_roots.at(n.symbol);
            if (root != 0)
            {
                for (int alias = 1; alias < b.scalar_roots.count(); alias++) { if (b.scalar_roots.at(alias) == (int)root) { b.scalar_roots.at_mut(alias) = 0; } }
            }
        }
        bool any = false;
        for (int symbol = 1; symbol < b.global_fields.count(); symbol++) { if (b.global_fields.at(symbol) != 0) { any = true; } }
        if (!any) { return; }
        int initializer = b.plan.method0(5, program, 0x1891, -1, ".cctor"); b.begin(initializer);
        for (int symbol = 1; symbol < b.global_fields.count(); symbol++)
        {
            int field = b.global_fields.at(symbol);
            if (field == 0) { continue; }
            b.empty(get_symbol(symbol, c).type, c); b.emit(0x80, field);
            int flag = b.global_flags.at(symbol); if (flag != 0) { b.emit(0x20, 0); b.emit(0x80, flag); }
        }
        b.op(0x2a);
    }
}
