namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static int il_flat_parameter(int function, Compiler c)
    {
        Node n = get_node(function, c); int parameter = n.children;
        if (parameter == 0) { return 0; }
        Node p = get_node(parameter, c);
        if (p.next != 0 || p.left != 0 || p.right == 0) { return 0; }
        Node pattern = get_node(p.right, c);
        if (pattern.constant ? p.third != 0 : p.third == 0) { return 0; }
        int child = pattern.children; int count = 0;
        while (child != 0)
        {
            Node binding = get_node(child, c);
            if (binding.kind != NodeKind.Binding || binding.constant || binding.left != 0 || binding.right != 0 ||
                (pattern.constant && get_node(binding.third, c).kind != NodeKind.String) || ++count > 8) { return 0; }
            child = binding.next;
        }
        return p.right;
    }
    internal static void il_rest_arguments(int argument, int type, Compiler c, IlBackend b)
    {
        int count = 0; int child = argument;
        while (child != 0) { count++; child = get_node(child, c).next; }
        bool numeric = il_number_array(type, c);
        b.emit(0x20, count); b.emit(0x73, b.RuntimeToken(numeric ? RuntimeMember.NumberArrayConstructor : RuntimeMember.ArrayConstructor));
        child = argument; int index = 0;
        while (child != 0)
        {
            b.op(0x25); b.emit(0x20, index++);
            if (numeric) { il_as(child, get_type(type, c).element, c, b); b.call(RuntimeMember.NumberIndexSetAt); }
            else { il_boxed(child, c, b); b.call(RuntimeMember.ArrayIndexSetAt); }
            child = get_node(child, c).next;
        }
    }
    internal static bool il_has_flat_calls(int symbol, Compiler c)
    {
        Node pattern = get_node(il_flat_parameter(get_symbol(symbol, c).function, c), c);
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node call = get_node(i, c);
            if (call.kind != NodeKind.Call || get_node(call.left, c).symbol != symbol) { continue; }
            if (!pattern.constant)
            {
                Vector<int> arguments = new Vector<int>();
                if (il_flat_arguments(call.children, arguments, c)) { return true; }
                continue;
            }
            if (call.children == 0) { continue; }
            Node argument = get_node(call.children, c);
            if (argument.kind != NodeKind.Object || argument.next != 0) { continue; }
            int count = 0; int property = argument.children;
            while (property != 0) { count++; property = get_node(property, c).next; }
            if (count <= 16) { return true; }
        }
        return false;
    }
    internal static bool il_flat_arguments(int first, Vector<int> arguments, Compiler c)
    {
        int child = first;
        while (child != 0)
        {
            Node n = get_node(child, c);
            if (n.kind == NodeKind.Spread)
            {
                Node source = get_node(n.left, c);
                while (source.kind == NodeKind.Assert && (get_type(source.type, c).kind == TypeKind.Array || get_type(source.type, c).kind == TypeKind.Any || get_type(source.type, c).kind == TypeKind.Unknown)) { source = get_node(source.left, c); }
                if (source.kind != NodeKind.Array || !il_flat_arguments(source.children, arguments, c)) { return false; }
            }
            else
            {
                if (arguments.count() >= 16) { return false; }
                arguments.push((int)child);
            }
            child = n.next;
        }
        return true;
    }
    internal static bool il_flat_rest_call(int entry, int pattern, int symbol, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c); Vector<int> arguments = new Vector<int>();
        if (!il_flat_arguments(call.children, arguments, c)) { return false; }
        Node parameter = get_node(get_node(get_symbol(symbol, c).function, c).children, c); int rest = get_node(parameter.third, c).type;
        int element = get_type(rest, c).kind == TypeKind.Array ? get_type(rest, c).element : rest;
        Vector<int> slots = new Vector<int>(); Vector<int> types = new Vector<int>();
        for (int i = 0; i < arguments.count(); i++)
        {
            int argument = (int)arguments.at(i);
            Node value = get_node(argument, c); int storage = element;
            if ((get_type(element, c).kind == TypeKind.Any || get_type(element, c).kind == TypeKind.Unknown) && get_type(value.type, c).kind != TypeKind.Void) { storage = value.type; }
            int slot = b.local(b.type(storage, c));
            if (get_type(value.type, c).kind == TypeKind.Void) { il_boxed(argument, c, b); }
            else { il_as(argument, storage, c, b); }
            b.emit(0xfe0e, slot); slots.push(slot); types.push((int)storage);
        }
        int child = get_node(pattern, c).children; int index = 0;
        while (child != 0)
        {
            Node binding = get_node(child, c);
            if (index < slots.count()) { b.emit(0xfe0c, slots.at(index)); b.convert((int)types.at(index), binding.type, c); }
            else { b.empty(binding.type, c); }
            index++; child = binding.next;
        }
        int method = b.binding_methods.at(symbol); b.emit(0x28, method); b.convert(get_symbol(symbol, c).type, call.type, c); return true;
    }
    internal static bool il_flat_parameter_call(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c); int symbol = get_node(call.left, c).symbol;
        if (b.binding_methods.at(symbol) == 0) { return false; }
        int pattern = il_flat_parameter(get_symbol(symbol, c).function, c);
        if (!get_node(pattern, c).constant) { return il_flat_rest_call(entry, pattern, symbol, c, b); }
        if (call.children == 0) { return false; }
        Node literal = get_node(call.children, c);
        if (literal.kind != NodeKind.Object || il_object_needs_entries(call.children, c) || literal.next != 0) { return false; }
        int property = literal.children; int count = 0;
        while (property != 0) { if (++count > 16) { return false; } property = get_node(property, c).next; }
        Vector<int> names = new Vector<int>(); Vector<int> slots = new Vector<int>(); Vector<int> types = new Vector<int>();
        property = literal.children;
        // Finish every RHS expression before entering the parameter environment.
        while (property != 0)
        {
            Node p = get_node(property, c); int type = get_field(field(literal.type, p.name, c), c).type; int slot = b.local(b.type(type, c));
            il_as(p.left, type, c, b); b.emit(0xfe0e, slot); names.push((int)p.name); slots.push(slot); types.push((int)type); property = p.next;
        }
        int function = get_symbol(symbol, c).function; int child = get_node(il_flat_parameter(function, c), c).children;
        while (child != 0)
        {
            Node binding = get_node(child, c); int key = get_token(get_node(binding.third, c).token, c).text; int found = -1;
            for (int i = 0; i < names.count(); i++) { if (same((int)names.at(i), key, c)) { found = (int)i; } }
            if (found < 0) { b.empty(binding.type, c); }
            else { b.emit(0xfe0c, slots.at((int)found)); b.convert((int)types.at((int)found), binding.type, c); }
            child = binding.next;
        }
        int method = b.binding_methods.at(symbol); b.emit(0x28, method); b.convert(get_symbol(symbol, c).type, call.type, c); return true;
    }
    internal static void il_flat_parameter_body(int function, Compiler c, IlBackend b)
    {
        Node n = get_node(function, c); int method = b.binding_methods.at(n.symbol);
        if (method == 0) { return; }
        // Each emitted method owns its locals, even when it shares the source body.
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node local = get_node(i, c);
            if ((local.kind == NodeKind.Variable || local.kind == NodeKind.Binding) && local.symbol != 0 && scalar_owner(i, b.node_parents, c) == function)
            {
                b.slots.at_mut(local.symbol) = -2147483647;
            }
        }
        b.begin(method); int child = get_node(il_flat_parameter(function, c), c).children; int index = 0;
        while (child != 0)
        {
            Node binding = get_node(child, c); b.slots.at_mut(binding.symbol) = -(++index); b.raw_slots.at_mut(binding.symbol) = 0; child = binding.next;
        }
        il_prepare_iteration_vars(function, c, b); il_statement(n.left, n.type, c, b);
        if (get_type(n.type, c).kind != TypeKind.Void) { b.empty(n.type, c); }
        b.op(0x2a);
    }
}
