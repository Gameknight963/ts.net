namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static void il_finish_binding(int entry, Compiler c, IlBackend b, DestructureAssignmentTarget assignment = null)
    {
        Node binding = get_node(entry, c);
        if (binding.left != 0)
        {
            int defined = b.label(); b.op(0x25); b.missing(binding.type, c); b.emit(0x39, defined); b.op(0x26);
            il_as(binding.left, binding.type, c, b); b.mark(defined);
        }
        if (binding.right != 0)
        {
            b.box(binding.type, c); int nested = b.local(-5); b.emit(0xfe0e, nested); il_binding_pattern(binding.right, nested, c, b);
        }
        else
        {
            // Exported namespace bindings write their property immediately,
            // so later defaults can read an earlier destructured export.
            if (assignment != null) { il_store_assignment_binding(assignment,binding.type,c,b); return; }
            if (binding.fourth != 0)
            {
                Node target = get_node(binding.fourth, c);
                int value = b.local(b.type(binding.type, c)); b.emit(0xfe0e, value);
                il_boxed(target.left, c, b); il_text(target.name, c, b);
                b.emit(0xfe0c, value); b.box(binding.type, c); b.call(RuntimeMember.DynamicSet); b.op(0x26);
                return;
            }
            if (b.global_fields.at(binding.symbol) != 0 || b.capture_fields.at(binding.symbol) != 0) { b.initialize(binding.symbol); return; }
            if (b.iteration_vars.at(binding.symbol) == 0) { b.slots.at_mut(binding.symbol) = b.local(b.type(binding.type, c)); }
            b.store(binding.symbol);
        }
    }
    internal static bool il_literal_bindings(int entry, Compiler c, IlBackend b)
    {
        Node pattern = get_node(entry, c); Node literal = get_node(pattern.left, c);
        if (pattern.constant) { return il_literal_object_bindings(entry, c, b); }
        if (literal.kind != NodeKind.Array) { return false; }
        int child = pattern.children;
        while (child != 0)
        {
            Node binding = get_node(child, c);
            if (binding.kind == NodeKind.Binding && (binding.constant || binding.right != 0)) { return false; }
            child = binding.next;
        }
        child = literal.children; int count = 0;
        while (child != 0)
        {
            Node item = get_node(child,c);
            if (item.kind == NodeKind.Undefined && item.constant) return false;
            if (get_node(child, c).kind == NodeKind.Spread || ++count > 16) { return false; }
            child = get_node(child, c).next;
        }
        int element = get_type(literal.type, c).element; Vector<int> slots = new Vector<int>(); Vector<int> types = new Vector<int>(); child = literal.children;
        // Save all values first: later RHS expressions can mutate earlier bindings.
        while (child != 0)
        {
            Node value = get_node(child, c); int storage = element;
            if (get_type(element, c).kind == TypeKind.Any || get_type(element, c).kind == TypeKind.Unknown) { storage = value.type; }
            int slot = b.local(b.type(storage, c));
            if (value.kind == NodeKind.Undefined && value.constant) { b.empty(storage, c); }
            else { il_as(child, storage, c, b); }
            b.emit(0xfe0e, slot); slots.push(slot); types.push((int)storage); child = value.next;
        }
        bool mayThrow = false; int bindingIndex = 0;
        for (int item = pattern.children; item != 0; item = get_node(item, c).next, bindingIndex++)
        {
            Node binding = get_node(item, c);
            if (binding.kind == NodeKind.Binding && (binding.left != 0 || binding.fourth != 0 || bindingIndex < types.count() && types.at(bindingIndex) != binding.type)) mayThrow = true;
        }
        int closeable = b.local(-2), position = b.local(-6);
        b.emit(0x20, 1); b.emit(0xfe0e, closeable); b.emit(0x20, 0); b.emit(0xfe0e, position);
        int start = b.label(), end = b.label(), handler = b.label(), handlerEnd = b.label(), normal = b.label(); b.mark(start);
        child = pattern.children; int index = 0;
        while (child != 0)
        {
            Node binding = get_node(child, c);
            b.emit(0x20, index < slots.count() ? 1 : 0); b.emit(0xfe0e, closeable);
            b.emit(0x20, index + 1); b.emit(0xfe0e, position);
            if (binding.kind == NodeKind.Binding)
            {
                if (index < slots.count()) { b.emit(0xfe0c, slots.at(index)); b.convert((int)types.at(index), binding.type, c); }
                else { b.empty(binding.type, c); }
                il_finish_binding(child, c, b);
            }
            index++; child = binding.next;
        }
        if (mayThrow)
        {
            b.emit(0xdd, normal); b.mark(end); b.mark(handler);
            int exception = b.RuntimeToken(RuntimeMember.ExceptionType), error = b.local(exception); b.emit(0xfe0e, error);
            il_close_literal_iterator(literal, slots, types, closeable, position, true, c, b);
            b.emit(0xfe0c, error); b.op(0x7a); b.mark(handlerEnd);
            b.plan.catch_region(b.method, start, end, handler, handlerEnd, exception);
        }
        b.mark(normal); il_close_literal_iterator(literal, slots, types, closeable, position, false, c, b);
        return true;
    }
    internal static void il_close_literal_iterator(Node literal, Vector<int> slots, Vector<int> types, int closeable, int position, bool suppress, Compiler c, IlBackend b)
    {
        int skip = b.label(); b.emit(0xfe0c, closeable); b.emit(0x39, skip);
        b.call(RuntimeMember.SequenceIteratorHasReturn); b.emit(0x39, skip);
        // Saved RHS values let the uncommon cleanup path reconstruct the source
        // without repeating expressions. Defaults may install return dynamically.
        b.emit(0x20, 0); b.emit(0x73, b.RuntimeToken(RuntimeMember.ArrayConstructor)); int source = b.local(-5); b.emit(0xfe0e, source);
        int child = literal.children, index = 0;
        while (child != 0)
        {
            Node value = get_node(child, c); b.emit(0xfe0c, source);
            if (value.kind == NodeKind.Undefined && value.constant) b.call(RuntimeMember.ArrayHole);
            else { b.emit(0xfe0c, slots.at(index)); b.box(types.at(index), c); b.call(RuntimeMember.ArrayAppend); }
            index++; child = value.next;
        }
        il_close_iteration(source, closeable, suppress, b, position); b.mark(skip);
    }
    internal static bool il_literal_object_bindings(int entry, Compiler c, IlBackend b)
    {
        Node pattern = get_node(entry, c); Node literal = get_node(pattern.left, c);
        if (literal.kind != NodeKind.Object) { return false; }
        if (il_object_needs_entries(pattern.left, c)) { return false; }
        int child = pattern.children;
        while (child != 0)
        {
            Node binding = get_node(child, c);
            if (binding.constant || binding.right != 0 || get_node(binding.third, c).kind != NodeKind.String) { return false; }
            child = binding.next;
        }
        child = literal.children; int count = 0;
        while (child != 0) { if (++count > 16) { return false; } child = get_node(child, c).next; }
        Vector<int> names = new Vector<int>(); Vector<int> slots = new Vector<int>(); Vector<int> types = new Vector<int>();
        child = literal.children;
        while (child != 0)
        {
            Node property = get_node(child, c); int member = field(literal.type, property.name, c); int type = get_field(member, c).type;
            int slot = b.local(b.type(type, c)); il_as(property.left, type, c, b); b.emit(0xfe0e, slot);
            names.push((int)property.name); slots.push(slot); types.push((int)type); child = property.next;
        }
        child = pattern.children;
        while (child != 0)
        {
            Node binding = get_node(child, c); int key = get_token(get_node(binding.third, c).token, c).text; int found = -1;
            for (int i = 0; i < names.count(); i++) { if (same((int)names.at(i), key, c)) { found = (int)i; } }
            if (found < 0) { b.empty(binding.type, c); }
            else { b.emit(0xfe0c, slots.at((int)found)); b.convert((int)types.at((int)found), binding.type, c); }
            il_finish_binding(child, c, b); child = binding.next;
        }
        return true;
    }
    internal static void il_binding_read(int type, int source, int position, int finished, Compiler c, IlBackend b, bool skip = false)
    {
        if (b.destructure_close >= 0) { b.emit(0x20, 0); b.emit(0xfe0e, b.destructure_close); }
        int missing = b.label(); int done = b.label();
        b.emit(0xfe0c, finished); b.emit(0x3a, missing);
        b.emit(0xfe0c, position); b.op(0x76); b.emit(0xfe0c, source); b.call(RuntimeMember.ForOfLength); b.op(0xfe04); b.emit(0x39, missing);
        b.emit(0xfe0c, source); b.emit(0xfe0c, position);
        if (skip) { b.call(RuntimeMember.ForOfSkip); }
        else if (get_type(type, c).kind == TypeKind.Number) { b.call(RuntimeMember.ForOfNumber); }
        else { b.call(RuntimeMember.ForOfValue); b.from_object(type, c); }
        if (b.destructure_close >= 0) { b.emit(0x20, 1); b.emit(0xfe0e, b.destructure_close); }
        b.emit(0xfe0c, position); b.emit(0xfe0c, source); b.emit(0xfe0c, position); b.call(RuntimeMember.ForOfStep); b.op(0x58); b.emit(0xfe0e, position);
        b.emit(0x38, done); b.mark(missing);
        b.emit(0x20, 1); b.emit(0xfe0e, finished); b.empty(type, c); b.mark(done);
    }
    internal static void il_binding_rest(int type, int source, int position, int finished, Compiler c, IlBackend b)
    {
        bool numeric = il_number_array(type, c); int element = get_type(type, c).element;
        b.emit(0x20, 0); b.emit(0x73, b.RuntimeToken(numeric ? RuntimeMember.NumberArrayConstructor : RuntimeMember.ArrayConstructor));
        int array = b.local(-5); b.emit(0xfe0e, array); int start = b.label(); int done = b.label(); b.mark(start);
        b.emit(0xfe0c, finished); b.emit(0x3a, done);
        b.emit(0xfe0c, position); b.op(0x76); b.emit(0xfe0c, source); b.call(RuntimeMember.ForOfLength); b.op(0xfe04); b.emit(0x39, done);
        b.emit(0xfe0c, array); il_binding_read(element, source, position, finished, c, b);
        if (numeric) { b.call(RuntimeMember.NumberPush); b.op(0x26); }
        else { b.box(element, c); b.call(RuntimeMember.ArrayAppend); }
        b.emit(0x38, start); b.mark(done); b.emit(0x20, 1); b.emit(0xfe0e, finished); b.emit(0xfe0c, array);
    }
    internal static void il_binding_pattern(int entry, int source, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.constant) { il_object_bindings(entry, source, c, b); return; }
        // Even an empty pattern must require an iterable value.
        b.emit(0xfe0c, source); b.call(RuntimeMember.IterationStart); b.emit(0xfe0e, source);
        int previousClose = b.destructure_close, closeable = b.local(-2); b.destructure_close = closeable;
        b.emit(0x20, 1); b.emit(0xfe0e, closeable);
        int start = b.label(), end = b.label(), handler = b.label(), handlerEnd = b.label(), normal = b.label(); b.mark(start);
        int position = b.local(-6); int finished = b.local(-2);
        b.emit(0x20, 0); b.emit(0xfe0e, position); b.emit(0x20, 0); b.emit(0xfe0e, finished);
        int child = n.children;
        while (child != 0)
        {
            Node binding = get_node(child, c);
            if (binding.kind == NodeKind.Undefined)
            {
                il_binding_read(binding.type, source, position, finished, c, b, true); b.op(0x26);
            }
            else
            {
                var assignment = il_prepare_assignment_binding(child,c,b);
                if (binding.constant) { il_binding_rest(binding.type, source, position, finished, c, b); }
                else { il_binding_read(binding.type, source, position, finished, c, b); }
                il_finish_binding(child, c, b, assignment);
            }
            child = binding.next;
        }
        b.emit(0xdd, normal); b.mark(end); b.mark(handler);
        int exception = b.RuntimeToken(RuntimeMember.ExceptionType), error = b.local(exception); b.emit(0xfe0e, error);
        il_close_iteration(source, closeable, true, b, position); b.emit(0xfe0c, error); b.op(0x7a); b.mark(handlerEnd);
        b.plan.catch_region(b.method, start, end, handler, handlerEnd, exception);
        b.mark(normal); il_close_iteration(source, closeable, false, b, position); b.destructure_close = previousClose;
    }
    internal static void il_object_bindings(int entry, int source, Compiler c, IlBackend b)
    {
        if(il_has_object_rest(entry,c)){il_object_rest_bindings(entry,source,c,b);return;}
        Node pattern = get_node(entry, c); int child = pattern.children;
        b.emit(0xfe0c, source); b.call(RuntimeMember.RequireObject);
        while (child != 0)
        {
            Node binding = get_node(child, c); Node key = get_node(binding.third, c); int member = 0;
            if (binding.name != 0 && is_text(binding.name,"$assignment",c)) {
                il_boxed(binding.third,c,b); b.call(RuntimeMember.PropertyKey); int name=b.local(-4); b.emit(0xfe0e,name);
                var assignment=il_prepare_assignment_binding(child,c,b);
                b.emit(0xfe0c,source); b.emit(0xfe0c,name); b.call(RuntimeMember.IndexGet); b.from_object(binding.type,c);
                il_finish_binding(child,c,b,assignment); child=binding.next; continue;
            }
            if (get_type(pattern.type, c).kind == TypeKind.Shape && key.kind == NodeKind.String) { member = field(pattern.type, get_token(key.token, c).text, c); }
            b.emit(0xfe0c, source);
            if (member != 0 && b.getter(member) != 0 && get_type(pattern.type, c).name == 0)
            {
                b.emit(0x74, b.interface_type(pattern.type)); b.emit(0x6f, b.getter(member)); b.convert(get_field(member, c).type, binding.type, c);
            }
            else
            {
                il_boxed(binding.third, c, b); b.call(RuntimeMember.IndexGet); b.from_object(binding.type, c);
            }
            il_finish_binding(child, c, b); child = binding.next;
        }
    }
}

internal sealed partial class IlBackend
{
    internal int destructure_close = -1;
}
