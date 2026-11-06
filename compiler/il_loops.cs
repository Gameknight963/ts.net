namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static int il_loop_binding(int entry, Compiler c)
    {
        Node loop = get_node(entry, c);
        if (loop.left == 0) { return 0; }
        Node binding = get_node(loop.left, c);
        if (binding.kind == NodeKind.Block)
        {
            if (binding.children == 0 || get_node(binding.children, c).next != 0) { return 0; }
            return binding.children;
        }
        return loop.left;
    }
    internal static int il_counted_loop(int entry, Compiler c, IlBackend b)
    {
        Node loop = get_node(entry, c);
        if (loop.kind != NodeKind.For || loop.left == 0 || loop.right == 0 || loop.third == 0) { return 0; }
        int declaration = il_loop_binding(entry, c);
        if (declaration == 0) { return 0; }
        Node binding = get_node(declaration, c);
        if (binding.kind != NodeKind.Variable || binding.left == 0) { return 0; }
        Node initial = get_node(binding.left, c); Node condition = get_node(loop.right, c); Node increment = get_node(loop.third, c);
        if (binding.kind != NodeKind.Variable || initial.kind != NodeKind.Number || !il_int_result(binding.left, c) || number(get_token(initial.token, c).text, c) < 0 ||
            b.raw_slot(binding.symbol) != -3 || condition.kind != NodeKind.Binary || !is_text(get_token(condition.token, c).text, text_less, c) ||
            get_node(condition.left, c).kind != NodeKind.Name || get_node(condition.left, c).symbol != binding.symbol ||
            increment.kind != NodeKind.Update || !is_text(get_token(increment.token, c).text, text_increment, c) ||
            get_node(increment.left, c).kind != NodeKind.Name || get_node(increment.left, c).symbol != binding.symbol) { return 0; }
        Node bound = get_node(condition.right, c);
        if (bound.kind != NodeKind.Number && (bound.kind != NodeKind.Name || bound.symbol == binding.symbol ||
            get_type(bound.type, c).kind != TypeKind.Number || b.written_symbols.at(bound.symbol) != 0)) { return 0; }
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if (n.kind == NodeKind.Variable && n.symbol == binding.symbol && i != declaration) { return 0; }
            if ((n.kind == NodeKind.Assign || n.kind == NodeKind.Update) && get_node(n.left, c).kind == NodeKind.Name &&
                get_node(n.left, c).symbol == binding.symbol && i != loop.third) { return 0; }
            if (n.kind == NodeKind.Name && n.symbol == binding.symbol)
            {
                int parent = i;
                while (parent != 0 && parent != entry) { parent = (int)b.node_parents.at(parent); }
                if (parent == 0 || scalar_owner(i, b.node_parents, c) != scalar_owner(declaration, b.node_parents, c)) { return 0; }
            }
        }
        return binding.symbol;
    }
    internal static void il_loop_code(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        int start = b.label(); int next = b.label(); int done = b.label();
        b.breaks.at_mut(entry) = done; b.continues.at_mut(entry) = n.kind == NodeKind.While ? start : next;
        if (n.kind == NodeKind.For && n.left != 0) { il_statement(n.left, result, c, b); }
        if (n.kind == NodeKind.For) { il_context_clone(entry, c, b); }
        int stop = -1;
        if (counter != 0) { stop = b.local(-6); b.emit(0xfe0c, bound); b.call(RuntimeMember.LoopLimit); b.emit(0xfe0e, stop); }
        b.mark(start);
        if (n.kind != NodeKind.Do)
        {
            int condition = n.kind == NodeKind.While ? n.left : n.right;
            if (counter != 0)
            {
                b.load_storage(counter); b.emit(0xfe0c, stop); b.op(0xfe04); b.emit(0x39, done);
            }
            else if (condition != 0) { il_condition(condition, c, b); b.emit(0x39, done); }
        }
        il_statement(n.kind == NodeKind.For ? n.fourth : n.right, result, c, b); b.mark(next);
        if (n.kind == NodeKind.Do) { il_condition(n.left, c, b); b.emit(0x39, done); }
        if (n.kind == NodeKind.For) { il_context_clone(entry, c, b); }
        if (counter != 0)
        {
            b.load_storage(counter); b.emit(0x20, 1); b.op(0x58); b.store_storage(counter);
        }
        else if (n.kind == NodeKind.For && n.third != 0)
        {
            il_expression(n.third, c, b); if (get_type(get_node(n.third, c).type, c).kind != TypeKind.Void) { b.op(0x26); }
        }
        b.emit(0x38, start); b.mark(done);
    }
    internal static void il_tagged_loop_code(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        int accumulator = il_loop_accumulator(entry, c, b);
        if (accumulator == 0) { il_loop_code(entry, result, counter, bound, c, b); return; }
        int original = b.slots.at(accumulator); int raw = b.local(-3); int fallback = b.label(); int done = b.label();
        b.load(accumulator); b.call(RuntimeMember.NumberMissing); b.emit(0x3a, fallback);
        b.load(accumulator); b.call(RuntimeMember.NumberValue); b.emit(0xfe0e, raw);
        b.slots.at_mut(accumulator) = raw; b.raw_slots.at_mut(accumulator) = -3;
        il_loop_code(entry, result, counter, bound, c, b);
        b.emit(0xfe0c, raw); b.call(RuntimeMember.WrapNumber); b.emit(0xfe0e, original);
        b.slots.at_mut(accumulator) = original; b.raw_slots.at_mut(accumulator) = 0;
        b.emit(0x38, done); b.mark(fallback); il_loop_code(entry, result, counter, bound, c, b); b.mark(done);
    }
    internal static void il_loop_body_inner(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        if (il_integer_call_loop(entry, result, counter, bound, c, b)) { return; }
        if (counter != 0)
        {
            int pushes = il_push_loop(entry, c, b);
            if (pushes != 0) { b.load(pushes); b.emit(0xfe0c, bound); b.call(RuntimeMember.ArrayReservePushes); }
            int fill = il_fill_loop(entry, counter, c, b);
            if (fill != 0)
            {
                int skip = b.label(); b.emit(0xfe0c, bound); b.plan.number(b.method, 0); b.op(0xfe02); b.emit(0x39, skip);
                b.load(fill); b.emit(0xfe0c, bound); b.call(RuntimeMember.ArrayReserveCapacity); b.mark(skip);
            }
        }
        int array = il_packed_loop(entry, counter, c, b);
        if (array == 0)
        {
            array = il_dense_loop(entry, counter, c, b);
            if (array == 0) { il_loop_code(entry, result, counter, bound, c, b); return; }
            int tagged_done = 0;
            if (il_tag_loop(entry, array, c, b) != 0)
            {
                int tags = b.local(-13); int ordinary = b.label(); tagged_done = b.label();
                b.emit(0xfe0c, bound); b.plan.number(b.method, 0); b.op(0xfe02); b.emit(0x39, ordinary);
                b.load(array); il_buffer_bound(entry, bound, c, b); b.call(RuntimeMember.ArrayTryTags); b.emit(0xfe0e, tags);
                b.emit(0xfe0c, tags); b.emit(0x39, ordinary);
                b.tag_arrays.at_mut(array) = tags + 1;
                il_tagged_loop_code(entry, result, counter, bound, c, b); b.emit(0x38, tagged_done);
                b.tag_arrays.at_mut(array) = 0;
                b.mark(ordinary);
            }
            int snapshotLocal = b.local(-7); int fallbackLocal = b.label(); int doneLocal = b.label();
            // Empty loops must not read the array binding before the body would.
            b.emit(0xfe0c, bound); b.plan.number(b.method, 0); b.op(0xfe02); b.emit(0x39, fallbackLocal);
            b.load(array); il_buffer_bound(entry, bound, c, b); b.call(RuntimeMember.ArrayTryDense); b.emit(0xfe0e, snapshotLocal);
            b.emit(0xfe0c, snapshotLocal); b.emit(0x39, fallbackLocal);
            b.dense_arrays.at_mut(array) = snapshotLocal + 1;
            il_loop_code(entry, result, counter, bound, c, b); b.emit(0x38, doneLocal);
            b.dense_arrays.at_mut(array) = 0;
            b.mark(fallbackLocal); il_loop_code(entry, result, counter, bound, c, b); b.mark(doneLocal); if (tagged_done != 0) { b.mark(tagged_done); }
            return;
        }
        int snapshot = b.local(-12); int fallback = b.label(); int done = b.label();
        b.emit(0xfe0c, bound); b.plan.number(b.method, 0); b.op(0xfe02); b.emit(0x39, fallback);
        b.load(array); il_buffer_bound(entry, bound, c, b); b.call(RuntimeMember.ArrayTryPacked); b.emit(0xfe0e, snapshot);
        b.emit(0xfe0c, snapshot); b.emit(0x39, fallback);
        b.packed_arrays.at_mut(array) = snapshot + 1;
        il_loop_code(entry, result, counter, bound, c, b); b.emit(0x38, done);
        b.packed_arrays.at_mut(array) = 0;
        b.mark(fallback); il_loop_code(entry, result, counter, bound, c, b); b.mark(done);
    }
    internal static void il_loop_body(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        int previous = b.push_bound; b.push_bound = counter != 0 ? bound : -1;
        il_loop_body_inner(entry, result, counter, bound, c, b); b.push_bound = previous;
    }
    internal static void il_loop_inner(int entry, int result, Compiler c, IlBackend b)
    {
        int counter = il_counted_loop(entry, c, b);
        if (counter == 0) { il_loop_body(entry, result, 0, -1, c, b); return; }
        Node n = get_node(entry, c); Node condition = get_node(n.right, c);
        Node limit = get_node(condition.right, c);
        double initial = number(get_token(get_node(get_node(il_loop_binding(entry, c), c).left, c).token, c).text, c);
        if (limit.kind == NodeKind.Number)
        {
            if (number(get_token(limit.token, c).text, c) > 2147483647 || number(get_token(limit.token, c).text, c) <= initial) { il_loop_body(entry, result, 0, -1, c, b); return; }
            int boundLocal = b.local(-3); il_number(condition.right, c, b); b.emit(0xfe0e, boundLocal);
            int previousLocal = b.raw_slot(counter); b.raw_slots.at_mut(counter) = -6; b.nonnegative_ints.at_mut(counter) = 1;
            il_loop_body(entry, result, counter, boundLocal, c, b);
            b.raw_slots.at_mut(counter) = previousLocal; b.nonnegative_ints.at_mut(counter) = 0; return;
        }
        // Versioning nested dynamic loops would multiply the emitted body size.
        if (b.loop_versions != 0) { il_loop_body(entry, result, 0, -1, c, b); return; }
        int fallback = b.label(); int done = b.label(); int bound = b.local(-3);
        il_number(condition.right, c, b); b.emit(0xfe0e, bound);
        // An unordered comparison sends NaN to the original loop too.
        b.emit(0xfe0c, bound); b.plan.number(b.method, 2147483647); b.op(0xfe03); b.emit(0x3a, fallback);
        // Buffer guards may read the array before the loop: keep empty nonzero loops on the original path.
        if (initial > 0) { b.emit(0xfe0c, bound); b.plan.number(b.method, initial); b.op(0xfe02); b.emit(0x39, fallback); }
        int previous = b.raw_slot(counter); b.raw_slots.at_mut(counter) = -6; b.nonnegative_ints.at_mut(counter) = 1;
        b.loop_versions++;
        il_loop_body(entry, result, counter, bound, c, b); b.emit(0x38, done);
        b.raw_slots.at_mut(counter) = previous; b.nonnegative_ints.at_mut(counter) = 0;
        b.mark(fallback); il_loop_body(entry, result, 0, -1, c, b); b.mark(done);
        b.loop_versions--;
    }
    internal static void il_loop(int entry, int result, Compiler c, IlBackend b)
    {
        int scope = b.context_scope; int context_slot = b.context_slot; il_context_enter(entry, c, b);
        Vector<int> globals = il_loop_globals(entry, c, b); Vector<int> slots = new Vector<int>();
        for (int i = 0; i < globals.count(); i++)
        {
            int symbol = (int)globals.at(i); int slot = b.local(b.type(get_symbol(symbol, c).type, c));
            b.load_storage(symbol); b.emit(0xfe0e, slot); slots.push(slot); b.global_cache.at_mut(symbol) = slot + 1;
        }
        il_loop_inner(entry, result, c, b);
        for (int i = 0; i < globals.count(); i++)
        {
            int symbol = (int)globals.at(i); int slot = slots.at(i); int field = b.global_fields.at(symbol);
            b.global_cache.at_mut(symbol) = 0; b.emit(0xfe0c, slot); b.emit(0x80, field);
        }
        b.context_scope = scope; b.context_slot = context_slot;
    }
    internal static bool il_integer_remainder(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind != NodeKind.Binary || !is_text(get_token(n.token, c).text, text_modulo, c)) { return false; }
        Node left = get_node(n.left, c); Node right = get_node(n.right, c);
        if (left.kind != NodeKind.Name || b.raw_slot(left.symbol) != -6 || b.nonnegative_ints.at(left.symbol) == 0 || right.kind != NodeKind.Number) { return false; }
        double divisor = number(get_token(right.token, c).text, c);
        return divisor > 0 && divisor <= 2147483647 && divisor == (double)(int)divisor;
    }
    internal static bool il_integer_value(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        return (n.kind == NodeKind.Name && b.raw_slot(n.symbol) == -6) || il_int_result(entry, c) || il_integer_remainder(entry, c, b);
    }
    internal static bool il_nonnegative_integer(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Name) { return b.raw_slot(n.symbol) == -6 && b.nonnegative_ints.at(n.symbol) != 0; }
        if (n.kind == NodeKind.Number) { return il_int_result(entry, c) && number(get_token(n.token, c).text, c) >= 0; }
        if (il_integer_remainder(entry, c, b)) { return true; }
        if (n.kind != NodeKind.Binary) { return false; }
        int op = get_token(n.token, c).text;
        // AND clears the sign bit when either operand has it clear. OR and XOR
        // need both operands to have it clear. Bitwise emission keeps JS coercions.
        if (is_text(op, text_bit_and, c)) { return il_nonnegative_integer(n.left, c, b) || il_nonnegative_integer(n.right, c, b); }
        if (is_text(op, text_bit_or, c) || is_text(op, text_bit_xor, c)) { return il_nonnegative_integer(n.left, c, b) && il_nonnegative_integer(n.right, c, b); }
        return false;
    }
}
