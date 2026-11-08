namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    internal static bool il_typed_buffer_read(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind != NodeKind.Index) return false;
        Node receiver = get_node(n.left, c);
        if (receiver.kind != NodeKind.Name || b.typed_buffers.at(receiver.symbol) == 0) return false;
        b.emit(0xfe0c, b.typed_buffers.at(receiver.symbol) - 1);
        il_integer(n.right, c, b); b.call(RuntimeMember.ReadFloat64Buffer); return true;
    }
    internal static bool il_typed_length_loop(int entry, int result, Compiler c, IlBackend b)
    {
        if (b.loop_versions != 0) return false;
        Node loop = get_node(entry, c);
        if (loop.kind != NodeKind.For || loop.right == 0) return false;
        Node condition = get_node(loop.right, c);
        if (condition.kind != NodeKind.Binary || condition.right == 0) return false;
        Node length = get_node(condition.right, c);
        if (length.kind != NodeKind.Property || length.fourth != 0 || !is_text(length.name, text_length, c)) return false;
        int counter = il_counted_loop(entry, c, b, true);
        if (counter == 0) return false;
        Node receiver = get_node(length.left, c);
        if (receiver.kind != NodeKind.Name || receiver.symbol == counter) return false;
        int array = receiver.symbol, reads = 0;
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            if (!il_inside(i, loop.fourth, b)) continue;
            Node n = get_node(i, c);
            if (il_key_may_invoke(n, c) || il_coercion_may_invoke(n, c)) return false;
            if (n.kind is not (NodeKind.Block or NodeKind.Expression or NodeKind.If or NodeKind.Variable or NodeKind.Name or NodeKind.Number or NodeKind.Boolean or
                NodeKind.Index or NodeKind.Binary or NodeKind.Unary or NodeKind.Conditional or NodeKind.Sequence or NodeKind.Assign or NodeKind.Update)) return false;
            if (n.kind == NodeKind.Index)
            {
                Node source = get_node(n.left, c), index = get_node(n.right, c);
                if (source.kind != NodeKind.Name || source.symbol != array || index.kind != NodeKind.Name || index.symbol != counter || get_type(n.type, c).kind != TypeKind.Number) return false;
                reads++;
            }
            if (n.kind == NodeKind.Variable && (n.symbol == array || n.symbol == counter || get_type(n.type, c).kind is not (TypeKind.Number or TypeKind.Boolean))) return false;
            if (n.kind is NodeKind.Assign or NodeKind.Update)
            {
                Node target = get_node(n.left, c);
                if (target.kind != NodeKind.Name || target.symbol == array || target.symbol == counter) return false;
            }
            if (n.kind == NodeKind.Name && n.symbol == array)
            {
                Node parent = get_node(b.node_parents.at(i), c);
                if (parent.kind != NodeKind.Index || parent.left != i) return false;
            }
        }
        if (reads == 0) return false;
        int bytes = b.local(-13), bound = b.local(-3), fallback = b.label(), done = b.label();
        b.load(array); b.call(RuntimeMember.TryFloat64Buffer); b.emit(0xfe0e, bytes);
        b.emit(0xfe0c, bytes); b.emit(0x39, fallback);
        b.emit(0xfe0c, bytes); b.call(RuntimeMember.Float64BufferLength); b.op(0x6c); b.emit(0xfe0e, bound);
        int previous = b.raw_slot(counter), nonnegative = b.nonnegative_ints.at(counter);
        b.raw_slots.at_mut(counter) = -6; b.nonnegative_ints.at_mut(counter) = 1;
        b.typed_buffers.at_mut(array) = bytes + 1;
        il_loop_body(entry, result, counter, bound, c, b);
        b.typed_buffers.at_mut(array) = 0;
        b.raw_slots.at_mut(counter) = previous; b.nonnegative_ints.at_mut(counter) = nonnegative;
        b.emit(0x38, done);
        b.mark(fallback); il_loop_body(entry, result, 0, -1, c, b); b.mark(done);
        return true;
    }
}
