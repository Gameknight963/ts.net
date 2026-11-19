namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    static bool il_dynamic_counter_loop(int entry, int result, Compiler c, IlBackend b)
    {
        if (b.loop_versions != 0) return false;
        int counter = il_counted_loop(entry, c, b, allowLength: true);
        if (counter == 0 || b.global_fields.at(counter) != 0 || b.iteration_vars.at(counter) != 0) return false;
        Node loop = get_node(entry, c), condition = get_node(loop.right, c), limit = get_node(condition.right, c);
        if (limit.kind != NodeKind.Property || limit.fourth != 0 || !is_text(limit.name, text_length, c)) return false;
        // String index methods still consume number arguments. Changing their
        // counters adds conversions without providing an integer read path.
        if (get_type(get_node(limit.left, c).type, c).kind == TypeKind.String) return false;

        int saved = b.raw_slot(counter), nonnegative = b.nonnegative_ints.at(counter);
        int overflow = b.label(), done = b.label();
        b.raw_slots.at_mut(counter) = -6; b.nonnegative_ints.at_mut(counter) = 1; b.loop_versions++;
        // Keep evaluating the original condition: callbacks and getters may
        // change the length or the receiver between iterations.
        il_loop_code(entry, result, 0, -1, c, b, dynamicCounter: counter, overflow: overflow);
        int integerSlot = b.slots.at(counter);
        b.raw_slots.at_mut(counter) = saved; b.nonnegative_ints.at_mut(counter) = nonnegative;
        b.emit(0x38, done);

        // Stop before integer overflow and continue at exactly that index,
        // retaining the effects and accumulator values of earlier iterations.
        b.mark(overflow);
        int ordinarySlot = b.local(saved);
        b.emit(0xfe0c, integerSlot); b.op(0x6c); b.emit(0xfe0e, ordinarySlot);
        b.slots.at_mut(counter) = ordinarySlot;
        il_loop_code(entry, result, 0, -1, c, b, initialize: false);
        b.mark(done); b.loop_versions--;
        return true;
    }
}
