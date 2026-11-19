namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend { internal readonly Dictionary<int, (int Load, RuntimeMember Reader)> buffer_readers = []; }

internal static partial class CompilerFunctions
{
    internal static bool il_typed_buffer_read(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Assert && get_type(n.type, c).kind == TypeKind.Number) n = get_node(n.left, c);
        if (n.kind != NodeKind.Index) return false;
        Node receiver = get_node(n.left, c);
        if (receiver.kind != NodeKind.Name || b.typed_buffers.at(receiver.symbol) == 0) return false;
        b.emit(0xfe0c, b.typed_buffers.at(receiver.symbol) - 1);
        il_integer(n.right, c, b);
        var reader = b.buffer_readers[receiver.symbol];
        if (reader.Load != 0) { b.op(reader.Load); b.op(0x6c); }
        else b.call(reader.Reader);
        return true;
    }
    internal static bool il_typed_length_loop(int entry, int result, Compiler c, IlBackend b)
    {
        if (b.loop_versions != 0) return false;
        Node loop = get_node(entry, c);
        if (loop.kind != NodeKind.For || loop.right == 0) return false;
        Node condition = get_node(loop.right, c);
        if (condition.kind != NodeKind.Binary || condition.right == 0) return false;
        Node length = get_node(condition.right, c);
        bool lengthBound = length.kind == NodeKind.Property && length.fourth == 0 && is_text(length.name, text_length, c);
        if (!lengthBound && (length.kind != NodeKind.Number || !il_int_result(condition.right, c))) return false;
        int counter = il_counted_loop(entry, c, b, true);
        if (counter == 0) return false;
        int array = 0, reads = 0;
        double fixedBound = -1;
        if (lengthBound) {
            Node receiver = get_node(length.left, c);
            if (receiver.kind != NodeKind.Name || receiver.symbol == counter) return false;
            array = receiver.symbol;
        } else {
            fixedBound = number(get_token(length.token, c).text, c);
            Node binding = get_node(il_loop_binding(entry, c), c);
            double initial = number(get_token(get_node(binding.left, c).token, c).text, c);
            // An empty fixed-bound loop must not read an otherwise unused receiver.
            if (fixedBound <= initial) return false;
            foreach (int i in b.LoopNodes(loop.fourth, c)) {
                Node index = get_node(i, c);
                if (index.kind == NodeKind.Index && get_node(index.left, c).kind == NodeKind.Name) {
                    array = get_node(index.left, c).symbol; break;
                }
            }
            if (array == 0 || array == counter) return false;
        }
        bool assertedReads = false;
        foreach (int i in b.LoopNodes(loop.fourth, c))
        {
            Node n = get_node(i, c);
            if (il_key_may_invoke(n, c) || il_coercion_may_invoke(n, c)) return false;
            if (n.kind is not (NodeKind.Block or NodeKind.Expression or NodeKind.If or NodeKind.Variable or NodeKind.Name or NodeKind.Number or NodeKind.Boolean or
                NodeKind.Index or NodeKind.Binary or NodeKind.Unary or NodeKind.Conditional or NodeKind.Sequence or NodeKind.Assign or NodeKind.Update or NodeKind.Assert)) return false;
            if (n.kind == NodeKind.Assert && (get_type(n.type, c).kind != TypeKind.Number || get_node(n.left, c).kind != NodeKind.Index)) return false;
            if (n.kind == NodeKind.Index)
            {
                Node source = get_node(n.left, c), index = get_node(n.right, c);
                if (source.kind != NodeKind.Name || source.symbol != array || index.kind != NodeKind.Name || index.symbol != counter) return false;
                if (get_type(n.type, c).kind != TypeKind.Number) {
                    Node parent = get_node(b.node_parents.at(i), c);
                    if (parent.kind != NodeKind.Assert || parent.left != i || get_type(parent.type, c).kind != TypeKind.Number) return false;
                    assertedReads = true;
                }
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
        // The checks above exclude array writes and user callbacks, so a packed
        // numeric buffer stays valid until this traversal finishes.
        int slowPath = b.label();
        if (!lengthBound) {
            // A guarded read must not move a temporal-dead-zone error ahead
            // of an earlier statement, or introduce it in an untaken branch.
            if (b.capture_fields.at(array) != 0) {
                b.load_context(b.capture_scopes.at(array)); b.emit(0x7b, b.capture_flags.at(array)); b.emit(0x39, slowPath);
            } else if (b.global_fields.at(array) != 0 && b.global_vars.at(array) == 0) {
                b.emit(0x7e, b.global_flags.at(array)); b.emit(0x39, slowPath);
            }
        }
        int packed = b.local(-12), packedBound = b.local(-3), ordinary = b.label(), packedDone = b.label();
        if (!assertedReads) {
            b.load(array); b.call(RuntimeMember.ArrayTryPackedRead); b.emit(0xfe0e, packed);
            b.emit(0xfe0c, packed); b.emit(0x39, ordinary);
            b.load(array); b.call(RuntimeMember.ArrayLength); b.emit(0xfe0e, packedBound);
            SetBound(packedBound, ordinary);
            int savedCounter = b.raw_slot(counter), savedNonnegative = b.nonnegative_ints.at(counter), savedPacked = b.packed_arrays.at(array);
            b.raw_slots.at_mut(counter) = -6; b.nonnegative_ints.at_mut(counter) = 1; b.packed_arrays.at_mut(array) = packed + 1;
            il_loop_code(entry, result, counter, packedBound, c, b);
            b.raw_slots.at_mut(counter) = savedCounter; b.nonnegative_ints.at_mut(counter) = savedNonnegative; b.packed_arrays.at_mut(array) = savedPacked;
            b.emit(0x38, packedDone);
        }
        b.mark(ordinary);

        EmitBuffer("Float64", 8, 0, RuntimeMember.ReadFloat64Buffer);
        EmitBuffer("Uint8", 1, 0x91, default);
        EmitBuffer("Int8", 1, 0x90, default);
        EmitBuffer("Float32", 4, 0, RuntimeMember.ReadFloat32Buffer);
        EmitBuffer("Uint32", 4, 0, RuntimeMember.ReadUint32Buffer);
        EmitBuffer("Int32", 4, 0, RuntimeMember.ReadInt32Buffer);
        EmitBuffer("Uint16", 2, 0, RuntimeMember.ReadUint16Buffer);
        EmitBuffer("Int16", 2, 0, RuntimeMember.ReadInt16Buffer);
        EmitBuffer("Float16", 2, 0, RuntimeMember.ReadFloat16Buffer);
        b.mark(slowPath);
        il_loop_body(entry, result, 0, -1, c, b);
        b.mark(packedDone);
        return true;

        void SetBound(int bound, int fallback) {
            if (lengthBound) return;
            b.emit(0xfe0c, bound); b.plan.number(b.method, fixedBound); b.op(0xfe04); b.emit(0x3a, fallback);
            b.plan.number(b.method, fixedBound); b.emit(0xfe0e, bound);
        }
        void EmitBuffer(string kind, int width, int load, RuntimeMember reader) {
            int bytes = b.local(-13), bound = b.local(-3), fallback = b.label();
            b.load(array); b.plan.raw_string(b.method, System.Text.Encoding.UTF8.GetBytes(kind)); b.call(RuntimeMember.TryTypedNumberBuffer); b.emit(0xfe0e, bytes);
            b.emit(0xfe0c, bytes); b.emit(0x39, fallback);
            b.emit(0xfe0c, bytes);
            b.op(0x8e); b.op(0x69);
            if (width != 1) { b.emit(0x20, width); b.op(0x5b); }
            b.op(0x6c); b.emit(0xfe0e, bound);
            SetBound(bound, fallback);
            int previous = b.raw_slot(counter), nonnegative = b.nonnegative_ints.at(counter);
            b.raw_slots.at_mut(counter) = -6; b.nonnegative_ints.at_mut(counter) = 1;
            b.typed_buffers.at_mut(array) = bytes + 1;
            b.buffer_readers[array] = (load, reader);
            il_loop_body(entry, result, counter, bound, c, b);
            b.typed_buffers.at_mut(array) = 0;
            b.buffer_readers.Remove(array);
            b.raw_slots.at_mut(counter) = previous; b.nonnegative_ints.at_mut(counter) = nonnegative;
            b.emit(0x38, packedDone);
            b.mark(fallback);
        }
    }
}
