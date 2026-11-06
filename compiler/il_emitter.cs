namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static void il_as(int entry, int target, Compiler c, IlBackend b)
    {
        if (get_node(entry, c).kind == NodeKind.Array && (get_type(target, c).kind == TypeKind.Array || get_type(target, c).kind == TypeKind.Any || get_type(target, c).kind == TypeKind.Unknown))
        {
            il_array_literal(entry, target, c, b); return;
        }
        int source = get_node(entry, c).type;
        il_expression(entry, c, b);
        if (get_type(source, c).kind == TypeKind.Void && get_type(target, c).kind != TypeKind.Void) { b.empty(target, c); return; }
        // The freshly allocated class implements every compatible shape interface.
        if (get_node(entry, c).kind == NodeKind.Object && fits(source, target, c)) { return; }
        b.convert(source, target, c);
    }
    internal static void il_boxed(int entry, Compiler c, IlBackend b)
    {
        il_expression(entry, c, b);
        if (get_type(get_node(entry, c).type, c).kind == TypeKind.Void) { b.call(RuntimeMember.Undefined); } else { b.box(get_node(entry, c).type, c); }
    }
    internal static void il_number(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Name && b.numeric_methods.at(n.symbol) == b.method)
        {
            int slot = b.numeric_slots.at(n.symbol); b.emit(0xfe0c, slot); return;
        }
        if (n.kind == NodeKind.Name && b.raw_slot(n.symbol) == -3) { b.load_storage(n.symbol); return; }
        if (n.kind == NodeKind.Name && b.raw_slot(n.symbol) == -6) { b.load_storage(n.symbol); b.op(0x6c); return; }
        il_expression(entry, c, b);
        if (get_type(get_node(entry, c).type, c).kind == TypeKind.Number) { b.call(RuntimeMember.NumberValue); }
        else { b.call(RuntimeMember.Number); }
    }
    internal static void il_condition(int entry, Compiler c, IlBackend b)
    {
        if (il_number_predicate(entry, false, c, b) || il_number_type_test(entry, false, c, b)) { return; }
        il_expression(entry, c, b); b.truth(get_node(entry, c).type, c);
    }
    internal static void il_invert(IlBackend b) { b.emit(0x20, 0); b.op(0xfe01); }
    internal static int il_bitwise_code(int op, Compiler c)
    {
        if (is_text(op, text_bit_and, c) || is_text(op, text_bit_and_assign, c)) { return 0x5f; }
        if (is_text(op, text_bit_or, c) || is_text(op, text_bit_or_assign, c)) { return 0x60; }
        if (is_text(op, text_bit_xor, c) || is_text(op, text_bit_xor_assign, c)) { return 0x61; }
        if (is_text(op, text_shift_left, c) || is_text(op, text_shift_left_assign, c)) { return 0x62; }
        if (is_text(op, text_shift_right, c) || is_text(op, text_shift_right_assign, c)) { return 0x63; }
        if (is_text(op, text_shift_unsigned, c) || is_text(op, text_shift_unsigned_assign, c)) { return 0x64; }
        return 0;
    }
    internal static void il_bitwise_result(int opcode, IlBackend b)
    {
        if (opcode >= 0x62) { b.emit(0x20, 31); b.op(0x5f); }
        b.op(opcode);
        b.op(opcode == 0x64 ? 0x76 : 0x6c);
        b.call(RuntimeMember.WrapNumber);
    }
    internal static bool il_numeric_coercion_calls(int entry, Compiler c)
    {
        TypeKind kind = get_type(get_node(entry, c).type, c).kind;
        return kind == TypeKind.Any || kind == TypeKind.Unknown || kind == TypeKind.Shape || kind == TypeKind.Array;
    }
    internal static void il_numeric_operands(Node n, bool integer, Compiler c, IlBackend b)
    {
        if (il_numeric_coercion_calls(n.left, c))
        {
            // Both expressions finish before valueOf/toString may call user code.
            il_boxed(n.left, c, b); int left = b.local(-5); b.emit(0xfe0e, left);
            il_boxed(n.right, c, b); int right = b.local(-5); b.emit(0xfe0e, right);
            b.emit(0xfe0c, left); b.call(RuntimeMember.Number); if (integer) { b.call(RuntimeMember.ToInt32); }
            b.emit(0xfe0c, right); b.call(RuntimeMember.Number); if (integer) { b.call(RuntimeMember.ToInt32); }
        }
        else if (integer) { il_integer(n.left, c, b); il_integer(n.right, c, b); }
        else { il_number(n.left, c, b); il_number(n.right, c, b); }
    }
    internal static int il_relational_mode(int op, Compiler c)
    {
        if (is_text(op, text_less, c)) { return 0; }
        if (is_text(op, text_greater, c)) { return 1; }
        if (is_text(op, text_less_equal, c)) { return 2; }
        if (is_text(op, text_greater_equal, c)) { return 3; }
        return -1;
    }
    internal static void il_integer(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Name && b.raw_slot(n.symbol) == -6) { b.load_storage(n.symbol); return; }
        if (il_integer_remainder(entry, c, b))
        {
            il_integer(n.left, c, b); b.emit(0x20, (int)number(get_token(get_node(n.right, c).token, c).text, c)); b.op(0x5e); return;
        }
        if (n.kind == NodeKind.Number && il_int_result(entry, c))
        {
            b.emit(0x20, (int)number(get_token(n.token, c).text, c)); return;
        }
        if (n.kind == NodeKind.Unary && is_text(get_token(n.token, c).text, text_bit_not, c))
        {
            il_integer(n.left, c, b); b.op(0x66); return;
        }
        int opcode = n.kind == NodeKind.Binary ? il_bitwise_code(get_token(n.token, c).text, c) : 0;
        if (n.kind == NodeKind.Binary && opcode != 0)
        {
            il_numeric_operands(n, true, c, b);
            if (opcode >= 0x62) { b.emit(0x20, 31); b.op(0x5f); }
            b.op(opcode); return;
        }
        il_number(entry, c, b); b.call(RuntimeMember.ToInt32);
    }
    internal static void il_load_target(int entry, int receiver, int index, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Property && n.fourth != 0) { b.emit(0xfe0c, receiver); il_boxed(n.fourth, c, b); b.call(RuntimeMember.PrivateGet); b.from_object(n.type, c); return; }
        ScalarField scalar = il_scalar_field(entry, c, b);
        if (scalar.slot >= 0) { b.emit(0xfe0c, scalar.slot); b.convert(scalar.type, n.type, c); return; }
        if (n.kind == NodeKind.Index)
        {
            int packed = il_packed_slot(entry, c, b);
            if (packed >= 0)
            {
                b.emit(0xfe0c, packed); b.emit(0xfe0c, index); b.op(0x99); b.call(RuntimeMember.WrapNumber); return;
            }
            b.emit(0xfe0c, receiver); b.emit(0xfe0c, index);
            if (il_direct_array_index(entry, c, b)) { b.call(RuntimeMember.NumberIndexGetAt); }
            else if (il_number_index(entry, c)) { b.call(RuntimeMember.NumberIndexGet); }
            else { b.call(il_direct_index(entry, c, b) ? RuntimeMember.ArrayIndexGetAt : RuntimeMember.IndexGet); b.from_object(n.type, c); }
            return;
        }
        if (n.kind == NodeKind.Name) { il_expression(entry, c, b); }
        else
        {
            b.emit(0xfe0c, receiver);
            int owner = get_node(n.left, c).type;
            if (get_type(owner, c).kind == TypeKind.Array && is_text(n.name, text_length, c))
            {
                b.call(RuntimeMember.ArrayLength); b.call(RuntimeMember.WrapNumber);
            }
            else if (get_type(owner, c).kind == TypeKind.Any || get_type(owner, c).name != 0)
            {
                il_text(n.name, c, b); b.call(RuntimeMember.DynamicGet);
                b.from_object(n.type, c);
            }
            else
            {
                int member = field(owner, n.name, c);
                if (member == 0 && get_type(owner, c).kind is TypeKind.Shape or TypeKind.Array) { il_text(n.name, c, b); b.call(RuntimeMember.DynamicGet); b.from_object(n.type, c); return; }
                if (member == 0) { fail(n.token, "unresolved lowered field", c); }
                b.emit(0x74, b.interface_type(owner));
                b.emit(0x6f, b.getter(member));
                b.convert(get_field(member, c).type, n.type, c);
            }
        }
    }
    internal static void il_mutation(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c); Node target = get_node(n.left, c);
        ScalarField scalar = il_scalar_field(n.left, c, b);
        int receiver = -1;
        int index = -1;
        if ((target.kind == NodeKind.Property || target.kind == NodeKind.Index) && scalar.slot < 0)
        {
            if (target.kind == NodeKind.Property && target.fourth != 0) { il_boxed(target.left, c, b); receiver = b.local(-5); }
            else { il_expression(target.left, c, b); receiver = b.local(b.type(get_node(target.left, c).type, c)); }
            b.emit(0xfe0e, receiver);
        }
        if (target.kind == NodeKind.Index)
        {
            bool numeric = il_number_index(n.left, c);
            bool direct = il_direct_index(n.left, c, b);
            if (direct) { il_integer(target.right, c, b); }
            else if (numeric) { il_number(target.right, c, b); } else { il_boxed(target.right, c, b); }
            index = b.local(direct ? -6 : numeric ? -3 : -5); b.emit(0xfe0e, index);
        }
        int previous = -1;
        int op = get_token(n.token, c).text;
        int bitwise = il_bitwise_code(op, c);
        bool logical = n.kind == NodeKind.Assign && il_logical_assignment(op, c); int logical_done = -1; int value_type = target.type;
        {
            // Writes use the binding's declared storage, even when the read is
            // narrowed to a number or string by TypeScript's control flow.
            if (target.kind == NodeKind.Property && target.fourth != 0) { value_type = get_node(target.third, c).type; }
            else if (target.kind == NodeKind.Name) { value_type = get_symbol(target.symbol, c).type; }
            else if (scalar.slot >= 0) { value_type = scalar.type; }
            else
            {
                int owner = get_node(target.left, c).type; TypeKind kind = get_type(owner, c).kind;
                if (kind == TypeKind.Any) { value_type = owner; }
                else if (target.kind == NodeKind.Index && kind == TypeKind.Array) { value_type = get_type(owner, c).element; }
                else if (kind == TypeKind.Shape)
                {
                    int member = target.kind == NodeKind.Property ? field(owner, target.name, c) : 0;
                    if (member != 0) { value_type = get_field(member, c).type; }
                    else if (get_type(owner, c).element != 0) { value_type = get_type(owner, c).element; }
                }
            }
        }
        if (logical)
        {
            logical_done = b.label(); il_load_target(n.left, receiver, index, c, b); b.convert(target.type, value_type, c); b.op(0x25);
            if (is_text(op, text_nullish_assign, c))
            {
                TypeKind kind = get_type(value_type, c).kind;
                if (kind == TypeKind.Number || kind == TypeKind.Boolean) { b.missing(value_type, c); }
                else { b.call(RuntimeMember.IsNullish); }
                b.emit(0x39, logical_done);
            }
            else { b.truth(value_type, c); b.emit(is_text(op, text_and_assign, c) ? 0x39 : 0x3a, logical_done); }
            b.op(0x26); il_as(n.right, value_type, c, b);
        }
        if (target.kind == NodeKind.Name && b.raw_slot(target.symbol) == -6 && bitwise != 0 && bitwise != 0x64)
        {
            b.load_storage(target.symbol); il_integer(n.right, c, b);
            if (bitwise >= 0x62) { b.emit(0x20, 31); b.op(0x5f); }
            b.op(bitwise); b.op(0x25); b.store_storage(target.symbol);
            b.op(0x6c); b.call(RuntimeMember.WrapNumber); return;
        }
        if (logical)
        {
            // The assignment value is already on the stack; the skip path bypasses
            // both the RHS and the store and keeps the original operand value.
        }
        else if (n.kind == NodeKind.Assign && is_text(op, text_add_assign, c) && get_type(target.type, c).kind == TypeKind.String)
        {
            il_load_target(n.left, receiver, index, c, b);
            if (il_numeric_coercion_calls(n.right, c)) { il_boxed(n.right, c, b); b.call(RuntimeMember.Add); b.from_object(value_type, c); }
            else { b.call(RuntimeMember.Text); il_boxed(n.right, c, b); b.call(RuntimeMember.Text); b.call(RuntimeMember.Concat); }
        }
        else if (n.kind == NodeKind.Assign && is_text(op, text_add_assign, c) && (get_type(target.type, c).kind == TypeKind.Any || get_type(get_node(n.right, c).type, c).kind == TypeKind.Any))
        {
            il_load_target(n.left, receiver, index, c, b); b.box(target.type, c);
            il_boxed(n.right, c, b); b.call(RuntimeMember.Add); b.from_object(value_type, c);
        }
        else if (n.kind == NodeKind.Update || !is_text(op, text_assign, c))
        {
            bool staged = n.kind == NodeKind.Assign && il_numeric_coercion_calls(n.left, c);
            if (staged)
            {
                il_load_target(n.left, receiver, index, c, b); b.box(target.type, c); int left = b.local(-5); b.emit(0xfe0e, left);
                il_boxed(n.right, c, b); int right = b.local(-5); b.emit(0xfe0e, right);
                b.emit(0xfe0c, left); b.call(RuntimeMember.Number); if (bitwise != 0) { b.call(RuntimeMember.ToInt32); }
                b.emit(0xfe0c, right); b.call(RuntimeMember.Number); if (bitwise != 0) { b.call(RuntimeMember.ToInt32); }
            }
            else
            {
                if (target.kind == NodeKind.Name) { il_number(n.left, c, b); }
                else { il_load_target(n.left, receiver, index, c, b); b.call(get_type(target.type, c).kind == TypeKind.Number ? RuntimeMember.NumberValue : RuntimeMember.Number); }
                if (bitwise != 0) { b.call(RuntimeMember.ToInt32); }
            }
            if (n.kind == NodeKind.Update)
            {
                previous = b.local(-10);
                b.op(0x25); b.call(RuntimeMember.WrapNumber); b.emit(0xfe0e, previous); b.plan.number(b.method, 1);
            }
            else if (!staged) { il_number(n.right, c, b); if (bitwise != 0) { b.call(RuntimeMember.ToInt32); } }
            if (bitwise != 0) { il_bitwise_result(bitwise, b); }
            else
            {
                if (is_text(op, text_increment, c) || is_text(op, text_add_assign, c)) { b.op(0x58); }
                else if (is_text(op, text_decrement, c) || is_text(op, text_subtract_assign, c)) { b.op(0x59); }
                else if (is_text(op, text_power_assign, c)) { b.call(RuntimeMember.Power); }
                else if (is_text(op, text_multiply_assign, c)) { b.op(0x5a); }
                else if (is_text(op, text_modulo_assign, c)) { b.call(RuntimeMember.Remainder); }
                else { b.op(0x5b); }
                b.call(RuntimeMember.WrapNumber);
            }
            if (b.type(value_type, c) == -5) { b.call(RuntimeMember.BoxNumber); }
        }
        else { il_as(n.right, value_type, c, b); }
        int value = b.local(b.type(value_type, c)); b.emit(0xfe0e, value);
        if (target.kind == NodeKind.Name)
        {
            b.emit(0xfe0c, value);
            b.convert(value_type, get_symbol(target.symbol, c).type, c);
            b.store(target.symbol);
        }
        else if (target.kind == NodeKind.Index)
        {
            int packed = il_packed_slot(n.left, c, b);
            if (packed >= 0)
            {
                b.emit(0xfe0c, packed); b.emit(0xfe0c, index); b.emit(0xfe0c, value); b.call(RuntimeMember.NumberValue);
                if (!is_text(op, text_assign, c) || !il_integer_value(n.right, c, b)) { b.call(RuntimeMember.CanonicalNumber); }
                b.op(0xa1);
            }
            else
            {
                b.emit(0xfe0c, receiver); b.emit(0xfe0c, index); b.emit(0xfe0c, value);
                if (il_direct_array_index(n.left, c, b)) { b.call(RuntimeMember.NumberIndexSetAt); }
                else if (il_number_index(n.left, c)) { b.call(RuntimeMember.NumberIndexSet); }
                else { b.box(value_type, c); b.call(il_direct_index(n.left, c, b) ? RuntimeMember.ArrayIndexSetAt : RuntimeMember.IndexSet); }
            }
        }
        else if (scalar.slot >= 0)
        {
            b.emit(0xfe0c, value); b.convert(value_type, scalar.type, c); b.emit(0xfe0e, scalar.slot);
        }
        else
        {
            b.emit(0xfe0c, receiver);
            int owner = get_node(target.left, c).type;
            if (target.fourth != 0)
            {
                il_boxed(target.fourth, c, b); b.emit(0xfe0c, value); b.box(value_type, c); b.call(RuntimeMember.PrivateSet); b.op(0x26);
            }
            else if (get_type(owner, c).kind == TypeKind.Array && is_text(target.name, text_length, c))
            {
                b.emit(0xfe0c, value); b.call(RuntimeMember.NumberValue); b.call(RuntimeMember.ArraySetLength);
            }
            else if (get_type(owner, c).kind is TypeKind.Any or TypeKind.Array || get_type(owner, c).name != 0 || (get_type(owner, c).kind == TypeKind.Shape && field(owner, target.name, c) == 0 && get_type(owner, c).element != 0))
            {
                il_text(target.name, c, b); b.emit(0xfe0c, value); b.box(value_type, c); b.call(RuntimeMember.DynamicSet); b.op(0x26);
            }
            else
            {
                int member = field(owner, target.name, c); b.emit(0x74, b.interface_type(owner)); b.emit(0xfe0c, value);
                b.convert(value_type, get_field(member, c).type, c); b.emit(0x6f, b.setter(member));
            }
        }
        if (previous >= 0 && !n.constant) { b.emit(0xfe0c, previous); }
        else { b.emit(0xfe0c, value); if (!logical) { b.convert(value_type, n.type, c); } }
        if (logical) { b.mark(logical_done); b.convert(value_type, n.type, c); }
    }
    internal static bool il_number_type_test(int entry, bool wrap, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind != NodeKind.Binary) { return false; }
        int op = get_token(n.token, c).text;
        if (!is_text(op, text_strict_equal, c) && !is_text(op, text_strict_not_equal, c)) { return false; }
        Node left = get_node(n.left, c); Node right = get_node(n.right, c); int value = 0;
        if (left.kind == NodeKind.TypeOf && right.kind == NodeKind.String && is_text(get_token(right.token, c).text, text_number, c)) { value = left.left; }
        if (right.kind == NodeKind.TypeOf && left.kind == NodeKind.String && is_text(get_token(left.token, c).text, text_number, c)) { value = right.left; }
        if (value == 0) { return false; }
        if (get_type(get_node(value, c).type, c).kind == TypeKind.Number) { il_expression(value, c, b); b.call(RuntimeMember.NumberMissing); il_invert(b); }
        else if (il_direct_index(value, c, b))
        {
            Node index = get_node(value, c); int tags = get_node(index.left, c).kind == NodeKind.Name ? b.tag_arrays.at(get_node(index.left, c).symbol) - 1 : -1;
            if (tags >= 0) { b.emit(0xfe0c, tags); il_integer(index.right, c, b); b.op(0x91); b.emit(0x20, 1); b.op(0xfe01); }
            else if (il_dense_slot(value, c, b) >= 0) { b.emit(0xfe0c, il_dense_slot(value, c, b)); il_integer(index.right, c, b); b.op(0x9a); b.call(RuntimeMember.IsNumber); }
            else { il_expression(index.left, c, b); il_integer(index.right, c, b); b.call(RuntimeMember.ArrayIsNumberAt); }
        }
        else { il_boxed(value, c, b); b.call(RuntimeMember.IsNumber); }
        if (is_text(op, text_strict_not_equal, c) || is_text(op, text_loose_not_equal, c)) { il_invert(b); }
        if (wrap) { b.call(RuntimeMember.WrapBoolean); }
        return true;
    }
    internal static void il_predicate_result(bool wrap, int type, Compiler c, IlBackend b)
    {
        if (!wrap) { return; }
        b.call(RuntimeMember.WrapBoolean);
        if (get_type(type, c).kind != TypeKind.Boolean) { b.call(RuntimeMember.BoxBoolean); }
    }
    internal static bool il_number_predicate(int entry, bool wrap, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c); int mode = (int)n.kind - (int)NodeKind.NumberIsNaN;
        if (n.kind == NodeKind.Call)
        {
            Node callee = get_node(n.left, c); TypeKind result = get_type(n.type, c).kind;
            if (callee.kind != NodeKind.Name || (result != TypeKind.Boolean && result != TypeKind.Any && result != TypeKind.Unknown)) { return false; }
            mode = b.predicate_targets.at(callee.symbol) - 1;
            if (mode < 0) { return false; }
            // Keep initialization checks and the original callee-before-arguments order.
            il_expression(n.left, c, b); b.op(0x26);
        }
        if (mode < 0 || mode > 3) { return false; }
        if (n.children == 0)
        {
            b.emit(0x20, 0); il_predicate_result(wrap, n.type, c, b); return true;
        }
        Node argument = get_node(n.children, c);
        bool numeric = false;
        if (n.children != 0 && argument.next == 0 && argument.kind != NodeKind.Spread)
        {
            if (il_integer_value(n.children, c, b))
            {
                // A proven int32 is finite, integral, safe, and never NaN. Retain
                // argument evaluation even when the predicate result is constant.
                il_expression(n.children, c, b); b.op(0x26); b.emit(0x20, mode == 0 ? 0 : 1);
                il_predicate_result(wrap, n.type, c, b); return true;
            }
            numeric = get_type(argument.type, c).kind == TypeKind.Number;
            if (numeric) { il_expression(n.children, c, b); } else { il_boxed(n.children, c, b); }
        }
        else
        {
            b.emit(0x20, 0); b.emit(0x73, b.RuntimeToken(RuntimeMember.ArrayConstructor)); int array = b.local(-5); b.emit(0xfe0e, array);
            int item = n.children;
            while (item != 0) { il_append_call_argument(item, array, 0, c, b); item = get_node(item, c).next; }
            b.emit(0xfe0c, array); b.emit(0x20, 0); b.call(RuntimeMember.ForOfValue);
        }
        b.emit(0x20, mode); b.call(numeric ? RuntimeMember.NumberPredicateNumber : RuntimeMember.NumberPredicate);
        il_predicate_result(wrap, n.type, c, b); return true;
    }
    internal static int il_numeric_property_key(int entry, Compiler c)
    {
        if (entry == 0) { return 0; }
        Node n = get_node(entry, c);
        if (get_type(n.type, c).kind == TypeKind.Number) { return entry; }
        if (n.kind != NodeKind.Binary || !is_text(get_token(n.token, c).text, text_plus, c)) { return 0; }
        Node left = get_node(n.left, c); Node right = get_node(n.right, c);
        if (left.kind == NodeKind.String && text_size(get_token(left.token, c).text, c) == 0 && get_type(right.type, c).kind == TypeKind.Number) { return n.right; }
        if (right.kind == NodeKind.String && text_size(get_token(right.token, c).text, c) == 0 && get_type(left.type, c).kind == TypeKind.Number) { return n.left; }
        return 0;
    }
    internal static void il_expression(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Undefined)
        {
            if (n.left != 0) { il_expression(n.left, c, b); if (get_type(get_node(n.left, c).type, c).kind != TypeKind.Void) { b.op(0x26); } }
            b.call(RuntimeMember.Undefined);
        }
        else if (n.kind == NodeKind.Null) { b.op(0x14); }
        else if (n.kind == NodeKind.TypeOf) { il_boxed(n.left, c, b); b.call(RuntimeMember.TypeOf); }
        else if (n.kind == NodeKind.Sequence)
        {
            il_expression(n.left, c, b); if (get_type(get_node(n.left, c).type, c).kind != TypeKind.Void) { b.op(0x26); }
            il_as(n.right, n.type, c, b);
        }
        else if (n.kind == NodeKind.Nullish)
        {
            int alternate = b.label(); int done = b.label();
            TypeKind leftKind = get_type(get_node(n.left, c).type, c).kind;
            if (leftKind is TypeKind.Number or TypeKind.Boolean)
            {
                il_expression(n.left, c, b); b.op(0x25);
                b.call(leftKind == TypeKind.Number ? RuntimeMember.NumberMissing : RuntimeMember.BooleanMissing);
                b.emit(0x3a, alternate); b.convert(get_node(n.left, c).type, n.type, c);
            }
            else
            {
                il_boxed(n.left, c, b); b.op(0x25); b.call(RuntimeMember.IsNullish); b.emit(0x3a, alternate);
                b.from_object(n.type, c);
            }
            b.emit(0x38, done); b.mark(alternate); b.op(0x26); il_as(n.right, n.type, c, b); b.mark(done);
        }
        else if (n.kind == NodeKind.Conditional)
        {
            int alternate = b.label(); int done = b.label(); il_condition(n.left, c, b); b.emit(0x39, alternate);
            il_as(n.right, n.type, c, b); b.emit(0x38, done); b.mark(alternate); il_as(n.third, n.type, c, b); b.mark(done);
        }
        else if (n.kind == NodeKind.Number) { b.plan.number(b.method, number(get_token(n.token, c).text, c)); b.call(RuntimeMember.WrapNumber); }
        else if (n.kind == NodeKind.Boolean) { b.emit(0x20, is_text(get_token(n.token, c).text, text_true, c) ? 1 : 0); b.call(RuntimeMember.WrapBoolean); }
        else if (n.kind == NodeKind.HasProperty)
        {
            if (n.constant) { il_boxed(n.left, c, b); il_boxed(n.right, c, b); b.call(RuntimeMember.PrivateHas); b.call(RuntimeMember.WrapBoolean); return; }
            bool numeric=get_type(get_node(n.left,c).type,c).kind==TypeKind.Number;
            if(numeric)il_expression(n.left,c,b);else il_boxed(n.left,c,b);
            il_boxed(n.right,c,b);b.call(numeric?RuntimeMember.InNumber:RuntimeMember.In);b.call(RuntimeMember.WrapBoolean);
        }
        else if(n.kind==NodeKind.OptionalChain){il_optional_chain(entry,false,c,b);}
        else if(n.kind==NodeKind.BuiltinNamespace){il_text(get_token(n.token,c).text,c,b);b.call(RuntimeMember.BuiltinNamespace);}
        else if(n.kind==NodeKind.ResolveThis){il_boxed(n.left,c,b);b.call(n.constant ? RuntimeMember.NewTarget : RuntimeMember.ResolveThis);}
        else if(n.kind==NodeKind.AsyncAwait){il_boxed(n.left,c,b);if(n.right!=0){il_boxed(n.right,c,b);b.call(RuntimeMember.AsyncAwaitContinue);}else b.call(RuntimeMember.AsyncAwait);}
        else if(n.kind==NodeKind.SavedCall){il_saved_call(entry,c,b);}
        else if(n.kind==NodeKind.AsyncPropertyKey){il_boxed(n.left,c,b);b.call(RuntimeMember.PropertyKey);}
        else if (n.kind == NodeKind.AsyncResume) { il_boxed(n.left, c, b); b.call(RuntimeMember.AsyncResume); }
        else if (n.kind == NodeKind.AsyncIterationStart)
        {
            il_boxed(n.left, c, b); b.emit(0x20, n.constant ? 1 : 0); b.call(RuntimeMember.BeginAsyncIteration);
        }
        else if (n.kind == NodeKind.AsyncIterationNext)
        {
            il_boxed(n.left, c, b); b.call(RuntimeMember.NextAsyncIteration); b.call(RuntimeMember.WrapBoolean);
        }
        else if (n.kind == NodeKind.AsyncIterationValue) { il_boxed(n.left, c, b); b.call(RuntimeMember.AsyncIterationValue); }
        else if (n.kind == NodeKind.DestructuringState)
        {
            il_boxed(n.left, c, b); b.emit(0x20, n.constant ? 1 : 0); b.call(RuntimeMember.BeginDestructuring);
        }
        else if (n.kind == NodeKind.DestructuringRead)
        {
            il_boxed(n.left, c, b); il_boxed(n.right, c, b); b.call(RuntimeMember.ReadDestructuring);
        }
        else if (n.kind == NodeKind.DestructuringRest)
        {
            il_boxed(n.left, c, b);
            if (n.constant) b.emit(0x73, b.spread_any_constructor);
            else
            {
                b.emit(0x20, 0);
                b.emit(0x73, b.RuntimeToken(il_number_array(n.type, c) ? RuntimeMember.NumberArrayConstructor : RuntimeMember.ArrayConstructor));
            }
            b.call(RuntimeMember.RestDestructuring); b.from_object(n.type, c);
        }
        else if(n.kind==NodeKind.Arguments){il_boxed(n.left,c,b);b.call(RuntimeMember.Arguments);}
        else if(n.kind==NodeKind.SuperBase){il_boxed(n.left,c,b);il_boxed(n.right,c,b);b.emit(0x20,n.constant?1:0);b.call(RuntimeMember.SuperBase);}
        else if(n.kind==NodeKind.SuperCall){
            il_boxed(n.left,c,b);b.emit(0x20,0);b.emit(0x73,b.RuntimeToken(RuntimeMember.ArrayConstructor));int args=b.local(-5);b.emit(0xfe0e,args);
            int child=n.children;while(child!=0){il_append_call_argument(child,args,0,c,b);child=get_node(child,c).next;}
            b.emit(0xfe0c,args);b.call(RuntimeMember.SuperCall);
        }
        else if(n.kind==NodeKind.ClassValue){il_class_value(entry,c,b);}
        else if(n.kind==NodeKind.BuiltinError){il_text(get_token(n.token,c).text,c,b);b.call(RuntimeMember.GetErrorConstructor);}
        else if(n.kind==NodeKind.Construct){
            il_boxed(n.left,c,b);
            if(il_call_has_spread(entry,c)){
                b.emit(0x20,0);b.emit(0x73,b.RuntimeToken(RuntimeMember.ArrayConstructor));int args=b.local(-5);b.emit(0xfe0e,args);
                int child=n.children;while(child!=0){il_append_call_argument(child,args,0,c,b);child=get_node(child,c).next;}
                b.emit(0xfe0c,args);b.call(RuntimeMember.ConstructArguments);b.from_object(n.type,c);return;
            }
            int argument=n.children,count=0;while(argument!=0){count++;argument=get_node(argument,c).next;}
            b.emit(0x20,count);b.emit(0x8d,b.RuntimeToken(RuntimeMember.ObjectType));argument=n.children;int index=0;
            while(argument!=0){b.op(0x25);b.emit(0x20,index++);il_boxed(argument,c,b);b.op(0xa2);argument=get_node(argument,c).next;}
            b.call(RuntimeMember.Construct);b.from_object(n.type,c);
        }
        else if(n.kind==NodeKind.CatchValue){
            b.emit(0xfe0c,b.caught_exception);b.call(RuntimeMember.CaughtValue);
        }
        else if (n.kind == NodeKind.String)
        {
            il_text(get_token(n.token, c).text, c, b);
        }
        else if (n.kind >= NodeKind.NumberIsNaN && n.kind <= NodeKind.NumberIsSafeInteger) { il_number_predicate(entry, true, c, b); }
        else if (n.kind >= NodeKind.NumberIsNaNFunction && n.kind <= NodeKind.NumberIsSafeIntegerFunction) { b.emit(0x20, (int)n.kind - (int)NodeKind.NumberIsNaNFunction); b.call(RuntimeMember.NumberPredicateFunction); }
        else if (n.kind == NodeKind.ObjectKeysFunction) { b.call(RuntimeMember.ObjectKeysFunction); }
        else if (n.kind == NodeKind.ObjectValuesFunction) { b.call(RuntimeMember.ObjectValuesFunction); }
        else if (n.kind == NodeKind.ObjectEntriesFunction) { b.call(RuntimeMember.ObjectEntriesFunction); }
        else if (n.kind == NodeKind.FunctionValue) { il_function_object(n.symbol, c, b); }
        else if (n.kind == NodeKind.Name)
        {
            if (get_symbol(n.symbol, c).function != 0) { il_function_reference(n.symbol, c, b); return; }
            b.load(n.symbol);
            int stored = get_symbol(n.symbol, c).type;
            b.convert(stored, n.type, c);
        }
        else if (n.kind == NodeKind.Array) { il_array_literal(entry, n.type, c, b); }
        else if (n.kind == NodeKind.Index)
        {
            int dense = il_dense_slot(entry, c, b);
            if (dense >= 0) { b.emit(0xfe0c, dense); il_integer(n.right, c, b); b.op(0x9a); b.call(RuntimeMember.ArrayDecode); b.from_object(n.type, c); return; }
            int packed = il_packed_slot(entry, c, b);
            if (packed >= 0) { b.emit(0xfe0c, packed); il_integer(n.right, c, b); b.op(0x99); b.call(RuntimeMember.WrapNumber); return; }
            il_expression(n.left, c, b);
            if (get_type(get_node(n.left,c).type,c).kind==TypeKind.String && get_type(get_node(n.right,c).type,c).kind==TypeKind.Number) {
                if (il_integer_value(n.right,c,b)) {il_integer(n.right,c,b);b.call(RuntimeMember.StringIndexAt);}
                else {il_expression(n.right,c,b);b.call(RuntimeMember.StringIndexNumber);}
                b.from_object(n.type,c);return;
            }
            if (il_direct_array_index(entry, c, b)) { il_integer(n.right, c, b); b.call(RuntimeMember.NumberIndexGetAt); }
            else if (il_number_index(entry, c)) { il_number(n.right, c, b); b.call(RuntimeMember.NumberIndexGet); }
            else { if (il_direct_index(entry, c, b)) { il_integer(n.right, c, b); b.call(RuntimeMember.ArrayIndexGetAt); } else { il_boxed(n.right, c, b); b.call(RuntimeMember.IndexGet); } b.from_object(n.type, c); }
        }
        else if (n.kind == NodeKind.Object)
        {
            if (il_object_needs_entries(entry, c)) { il_object_spread_expression(entry, c, b); return; }
            int constructor = b.literal_constructors.at(entry); b.emit(0x73, constructor > 0 ? constructor : b.constructor(n.type));
            int property = n.children;
            while (property != 0)
            {
                Node p = get_node(property, c); int member = field(n.type, p.name, c);
                b.op(0x25); il_as(p.left, get_field(member, c).type, c, b); b.store_field(member, c); property = p.next;
            }
        }
        else if (n.kind == NodeKind.Property)
        {
            if (n.fourth != 0) { il_boxed(n.left, c, b); il_boxed(n.fourth, c, b); b.call(RuntimeMember.PrivateGet); b.from_object(n.type, c); return; }
            TypeKind atOwner=get_type(get_node(n.left,c).type,c).kind;
            if ((atOwner==TypeKind.Array || atOwner==TypeKind.String) && is_text(n.name,text_at_method,c)) {
                il_expression(n.left,c,b);b.call(RuntimeMember.RelativeAtTarget);return;
            }

            if (atOwner==TypeKind.String && (il_index_method_name(n.name,c) || il_string_search_name(n.name,c))) {
                il_expression(n.left,c,b);il_text(n.name,c,b);b.call(RuntimeMember.DynamicGet);return;
            }

            Node receiver = get_node(n.left, c);
            if (is_text(n.name, text_length, c) && receiver.kind == NodeKind.Call &&
                get_node(receiver.left, c).kind == NodeKind.ObjectKeysFunction && !il_call_has_spread(n.left, c))
            {
                il_object_keys_call(n.left, true, c, b); return;
            }
            ScalarField scalar = il_scalar_field(entry, c, b);
            if (scalar.slot >= 0) { b.emit(0xfe0c, scalar.slot); b.convert(scalar.type, n.type, c); return; }
            il_expression(n.left, c, b); int owner = get_node(n.left, c).type;
            if (get_type(owner, c).kind == TypeKind.Array && is_text(n.name, text_length, c))
            {
                b.call(RuntimeMember.ArrayLength); b.call(RuntimeMember.WrapNumber);
            }
            else if (get_type(owner, c).kind == TypeKind.Any || get_type(owner, c).name != 0)
            {
                il_text(n.name, c, b); b.call(RuntimeMember.DynamicGet);
                b.from_object(n.type, c);
            }
            else if (get_type(owner, c).kind == TypeKind.String) { if (is_text(n.name, text_length, c)) { b.call(RuntimeMember.Length); b.op(0x6c); b.call(RuntimeMember.WrapNumber); } else { il_text(n.name, c, b); b.call(RuntimeMember.DynamicGet); b.from_object(n.type, c); } }
            else
            {
                int member = field(owner, n.name, c);
                if (member == 0 && get_type(owner, c).kind is TypeKind.Shape or TypeKind.Array) { il_text(n.name, c, b); b.call(RuntimeMember.DynamicGet); b.from_object(n.type, c); return; }
                if (member == 0 || b.getter(member) == 0) { fail(n.token, "unresolved lowered getter", c); }
                b.emit(0x74, b.interface_type(owner));
                b.emit(0x6f, b.getter(member));
                b.convert(get_field(member, c).type, n.type, c);
            }
        }
        else if (n.kind == NodeKind.Assert) { il_as(n.left, n.type, c, b); }
        else if (n.kind == NodeKind.Assign || n.kind == NodeKind.Update) { il_mutation(entry, c, b); }
        else if (n.kind == NodeKind.Unary)
        {
            int op = get_token(n.token, c).text;
            if (is_text(op, text_not, c)) { il_condition(n.left, c, b); il_invert(b); b.call(RuntimeMember.WrapBoolean); }
            else if (is_text(op, text_bit_not, c))
            {
                il_integer(n.left, c, b); b.op(0x66); b.op(0x6c); b.call(RuntimeMember.WrapNumber);
            }
            else { il_number(n.left, c, b); if (is_text(op, text_minus, c)) { b.op(0x65); } b.call(RuntimeMember.WrapNumber); }
        }
        else if (n.kind == NodeKind.Call)
        {
            if (il_number_predicate(entry, true, c, b)) { return; }
            if (il_string_search_call(entry,c)) {il_emit_string_search(entry,c,b);return;}
            if (il_index_method_call(entry,c)) {il_emit_index_method(entry,c,b);return;}
            if (il_call_has_spread(entry, c)) { il_spread_call(entry, c, b); return; }
            if (get_node(n.left, c).kind == NodeKind.ObjectKeysFunction) { il_object_keys_call(entry, false, c, b); return; }
            if (get_node(n.left, c).kind == NodeKind.ObjectValuesFunction || get_node(n.left, c).kind == NodeKind.ObjectEntriesFunction) { il_object_enumeration_call(entry, c, b); return; }
            if (il_array_call(entry, c)) { il_emit_array_call(entry, c, b); }
            else if (builtin_has_own(n.left, c))
            {
                if (il_scalar_own(entry, c, b)) { return; }
                int argument = n.children; int key = argument != 0 ? get_node(argument, c).next : 0;
                int numeric_key = il_numeric_property_key(key, c); bool numeric = numeric_key != 0;
                bool integer = numeric && il_integer_value(numeric_key, c, b);
                for (int i = 0; i < 2; i++)
                {
                    if (argument == 0) { b.call(RuntimeMember.Undefined); }
                    else { if (i == 1 && integer) { il_integer(numeric_key, c, b); } else if (i == 1 && numeric) { il_expression(numeric_key, c, b); } else { il_boxed(argument, c, b); } argument = get_node(argument, c).next; }
                }
                while (argument != 0) { il_expression(argument, c, b); if (get_type(get_node(argument, c).type, c).kind != TypeKind.Void) { b.op(0x26); } argument = get_node(argument, c).next; }
                b.call(integer ? RuntimeMember.HasOwnIndex : numeric ? RuntimeMember.HasOwnNumber : RuntimeMember.HasOwn); b.call(RuntimeMember.WrapBoolean);
            }
            else if (builtin(n.left, text_performance, text_now, c)) { b.call(RuntimeMember.Now); b.call(RuntimeMember.WrapNumber); }
            else if (builtin(n.left, text_console, text_log, c))
            {
                int count = 0; int argument = n.children;
                while (argument != 0) { count++; argument = get_node(argument, c).next; }
                b.emit(0x20, count); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType));
                int index = 0; argument = n.children;
                while (argument != 0)
                {
                    b.op(0x25); b.emit(0x20, index++); il_boxed(argument, c, b); b.op(0xa2); argument = get_node(argument, c).next;
                }
                b.call(RuntimeMember.Log);
            }
            else
            {
                int symbol = il_direct_function(n.left, c, b);
                if (symbol == 0) { il_value_call(entry, c, b); return; }
                if (get_node(n.left, c).kind == NodeKind.Name && b.global_flags.at(get_node(n.left, c).symbol) != 0) { b.check_global(get_node(n.left, c).symbol); }
                if (get_node(n.left, c).symbol == symbol && il_flat_parameter_call(entry, c, b)) { return; }
                int context = b.function_contexts.at(symbol); if (context != 0) { b.load_context(context); }
                int parameter = get_node(get_symbol(symbol, c).function, c).children;
                if (get_node(get_symbol(symbol,c).function,c).fourth != 0) { b.call(RuntimeMember.Undefined); parameter = get_node(parameter,c).next; }
                int argument = n.children;
                while (argument != 0)
                {
                    if (parameter == 0)
                    {
                        il_expression(argument, c, b);
                        if (get_type(get_node(argument, c).type, c).kind != TypeKind.Void) { b.op(0x26); }
                        argument = get_node(argument, c).next; continue;
                    }
                    Node p = get_node(parameter, c);
                    if (p.third != 0)
                    {
                        il_rest_arguments(argument, get_node(p.third, c).type, c, b); argument = 0; parameter = p.next; continue;
                    }
                    il_as(argument, get_node(parameter, c).type, c, b);
                    argument = get_node(argument, c).next; parameter = get_node(parameter, c).next;
                }
                // The frontend checks typed arity. Calls through any can omit
                // required parameters and must pass undefined, as spread calls do.
                while (parameter != 0)
                {
                    Node p = get_node(parameter, c);
                    if (p.third != 0) { il_rest_arguments(0, get_node(p.third, c).type, c, b); }
                    else { b.empty(p.type, c); }
                    parameter = p.next;
                }
                b.emit(0x28, (int)b.function(symbol));
                int declared = get_symbol(symbol, c).type;
                b.convert(declared, n.type, c);
            }
        }
        else if (n.kind == NodeKind.Binary)
        {
            if (il_number_type_test(entry, true, c, b) || il_loose_equality(n, c, b)) { return; }
            int op = get_token(n.token, c).text;
            if(is_text(op,"instanceof",c)){il_boxed(n.left,c,b);il_boxed(n.right,c,b);b.call(RuntimeMember.InstanceOf);b.call(RuntimeMember.WrapBoolean);return;}

            if (is_text(op, text_and, c) || is_text(op, text_or, c))
            {
                int done = b.label(); il_as(n.left, n.type, c, b); b.op(0x25); b.truth(n.type, c);
                b.emit(is_text(op, text_and, c) ? 0x39 : 0x3a, done); b.op(0x26); il_as(n.right, n.type, c, b); b.mark(done);
            }
            else if (is_text(op, text_strict_equal, c) || is_text(op, text_strict_not_equal, c) || il_is_loose_equality(op, c))
            {
                TypeKind left_kind = get_type(get_node(n.left, c).type, c).kind;
                TypeKind right_kind = get_type(get_node(n.right, c).type, c).kind;
                Node left = get_node(n.left, c); Node right = get_node(n.right, c);
                bool left_defined = il_defined(n.left, c) || (left.kind == NodeKind.Name && b.raw_slot(left.symbol) == -3);
                bool right_defined = il_defined(n.right, c) || (right.kind == NodeKind.Name && b.raw_slot(right.symbol) == -3);
                if (left_kind == TypeKind.Number && right_kind == TypeKind.Number && il_integer_value(n.left, c, b) && il_integer_value(n.right, c, b))
                {
                    il_integer(n.left, c, b); il_integer(n.right, c, b); b.op(0xfe01);
                }
                else if (left_kind == TypeKind.Number && right_kind == TypeKind.Number && left_defined && right_defined)
                {
                    il_number(n.left, c, b); il_number(n.right, c, b); b.op(0xfe01);
                }
                else if (left_kind == right_kind && (left_kind == TypeKind.Number || left_kind == TypeKind.Boolean))
                {
                    il_expression(n.left, c, b); il_expression(n.right, c, b);
                    b.call(left_kind == TypeKind.Number ? RuntimeMember.EqualNumbers : RuntimeMember.EqualBooleans);
                }
                else
                {
                    il_boxed(n.left, c, b); il_boxed(n.right, c, b); b.call(RuntimeMember.Equal);
                }
                if (is_text(op, text_strict_not_equal, c) || is_text(op, text_loose_not_equal, c)) { il_invert(b); }
                b.call(RuntimeMember.WrapBoolean);
            }
            else if (il_relational_mode(op, c) >= 0 && (get_type(get_node(n.left, c).type, c).kind != TypeKind.Number || get_type(get_node(n.right, c).type, c).kind != TypeKind.Number))
            {
                il_boxed(n.left, c, b); il_boxed(n.right, c, b); b.emit(0x20, il_relational_mode(op, c));
                b.call(get_type(get_node(n.left, c).type, c).kind == TypeKind.String && get_type(get_node(n.right, c).type, c).kind == TypeKind.String ? RuntimeMember.StringRelational : RuntimeMember.Relational); b.call(RuntimeMember.WrapBoolean);
            }
            else if (il_integer_remainder(entry, c, b))
            {
                il_integer(entry, c, b); b.op(0x6c); b.call(RuntimeMember.WrapNumber);
            }
            else if (il_bitwise_code(op, c) != 0)
            {
                il_numeric_operands(n, true, c, b);
                il_bitwise_result(il_bitwise_code(op, c), b);
            }
            else if (is_text(op, text_power, c) && get_node(n.right, c).kind == NodeKind.Number && number(get_token(get_node(n.right, c).token, c).text, c) == 2)
            {
                il_number(n.left, c, b); b.op(0x25); b.op(0x5a); b.call(RuntimeMember.WrapNumber);
            }
            else if (is_text(op, text_plus, c) && get_type(n.type, c).kind == TypeKind.Any)
            {
                il_boxed(n.left, c, b); il_boxed(n.right, c, b); b.call(RuntimeMember.Add);
            }
            else if (is_text(op, text_plus, c) && get_type(n.type, c).kind == TypeKind.String)
            {
                if (il_numeric_coercion_calls(n.left, c) || il_numeric_coercion_calls(n.right, c))
                {
                    il_boxed(n.left, c, b); il_boxed(n.right, c, b); b.call(RuntimeMember.Add); b.from_object(n.type, c);
                }
                else { il_boxed(n.left, c, b); b.call(RuntimeMember.Text); il_boxed(n.right, c, b); b.call(RuntimeMember.Text); b.call(RuntimeMember.Concat); }
            }
            else
            {
                il_numeric_operands(n, false, c, b);
                if (is_text(op, text_plus, c)) { b.op(0x58); }
                else if (is_text(op, text_minus, c)) { b.op(0x59); }
                else if (is_text(op, text_power, c)) { b.call(RuntimeMember.Power); }
                else if (is_text(op, text_star, c)) { b.op(0x5a); }
                else if (is_text(op, text_slash, c)) { b.op(0x5b); }
                else if (is_text(op, text_modulo, c)) { b.call(RuntimeMember.Remainder); }
                else if (is_text(op, text_less, c)) { b.op(0xfe04); }
                else if (is_text(op, text_greater, c)) { b.op(0xfe02); }
                else if (is_text(op, text_less_equal, c)) { b.op(0xfe03); il_invert(b); }
                else if (is_text(op, text_greater_equal, c)) { b.op(0xfe05); il_invert(b); }
                b.call(get_type(n.type, c).kind == TypeKind.Boolean ? RuntimeMember.WrapBoolean : RuntimeMember.WrapNumber);
            }
        }
        else { fail(n.token, "unsupported lowered expression", c); }
    }
    internal static void il_tagged_store_value(int entry, int receiver, int index, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Conditional)
        {
            int alternate = b.label(); int done = b.label(); il_condition(n.left, c, b); b.emit(0x39, alternate);
            il_tagged_store_value(n.right, receiver, index, c, b); b.emit(0x38, done);
            b.mark(alternate); il_tagged_store_value(n.third, receiver, index, c, b); b.mark(done); return;
        }
        b.emit(0xfe0c, receiver); b.emit(0xfe0c, index);
        if (get_type(n.type, c).kind == TypeKind.Number) { il_expression(entry, c, b); b.call(RuntimeMember.ArraySetTaggedNumberAt); }
        else { il_boxed(entry, c, b); b.call(get_type(n.type, c).kind == TypeKind.String || get_type(n.type, c).kind == TypeKind.Boolean || get_type(n.type, c).kind == TypeKind.Shape || get_type(n.type, c).kind == TypeKind.Array ? RuntimeMember.ArraySetTaggedReferenceAt : RuntimeMember.ArrayIndexSetAt); }
    }
    internal static bool il_tagged_store(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind != NodeKind.Assign || !is_text(get_token(n.token, c).text, text_assign, c) || !il_direct_index(n.left, c, b)) { return false; }
        Node target = get_node(n.left, c);
        if (get_type(target.type, c).kind != TypeKind.Any) { return false; }
        il_expression(target.left, c, b); int receiver = b.local(-5); b.emit(0xfe0e, receiver);
        il_integer(target.right, c, b); int index = b.local(-6); b.emit(0xfe0e, index);
        il_tagged_store_value(n.right, receiver, index, c, b); return true;
    }
    internal static void il_statement(int entry, int return_type, Compiler c, IlBackend b)
    {
        if (get_node(entry, c).kind == NodeKind.AsyncBindingScope) { return; }
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Break || n.kind == NodeKind.Continue)
        {
            int target = n.kind == NodeKind.Break ? b.break_target(n.left) : b.continue_target(n.left);
            if (target == 0) { fail(n.token, "missing control-flow target", c); }
            il_exception_jump(n.left,target,c,b);
        }
        else if(n.kind==NodeKind.Throw){il_boxed(n.left,c,b);b.call(RuntimeMember.MakeThrown);b.op(0x7a);}
        else if(n.kind==NodeKind.Try){il_try(entry,return_type,c,b);}
        else if (n.kind == NodeKind.Label)
        {
            int done = b.label(); b.breaks.at_mut(entry) = done; il_statement(n.left, return_type, c, b); b.mark(done);
        }
        else if (n.kind == NodeKind.Switch)
        {
            int scope = b.context_scope; int context_slot = b.context_slot; il_context_enter(entry, c, b);
            int done = b.label(); b.breaks.at_mut(entry) = done;
            il_boxed(n.left, c, b); int value = b.local(-5); b.emit(0xfe0e, value);
            int fallback = done; int clause = n.children;
            while (clause != 0)
            {
                int label = b.label(); b.breaks.at_mut(clause) = label; Node item = get_node(clause, c);
                if (item.left == 0) { fallback = label; }
                else { b.emit(0xfe0c, value); il_boxed(item.left, c, b); b.call(RuntimeMember.Equal); b.emit(0x3a, label); }
                clause = item.next;
            }
            b.emit(0x38, fallback); clause = n.children;
            while (clause != 0)
            {
                b.mark(b.break_target(clause)); int child = get_node(clause, c).children;
                while (child != 0) { il_statement(child, return_type, c, b); child = get_node(child, c).next; }
                clause = get_node(clause, c).next;
            }
            b.mark(done); b.context_scope = scope; b.context_slot = context_slot;
        }
        else if (n.kind == NodeKind.Block)
        {
            int scope = b.context_scope; int slot = b.context_slot; il_context_enter(entry, c, b);
            int child = n.children;
            while (child != 0) { il_statement(child, return_type, c, b); child = get_node(child, c).next; }
            b.context_scope = scope; b.context_slot = slot;
        }
        else if (n.kind == NodeKind.Destructure)
        {
            if (il_literal_bindings(entry, c, b)) { return; }
            il_boxed(n.left, c, b); int source = b.local(-5); b.emit(0xfe0e, source); il_binding_pattern(entry, source, c, b);
        }
        else if (n.kind == NodeKind.Variable)
        {
            if (b.capture_fields.at(n.symbol) != 0)
            {
                Node initial = get_node(n.left, c);
                if (n.fourth != 0 && initial.kind == NodeKind.Undefined && initial.constant) { return; }
                il_as(n.left, n.type, c, b); b.initialize(n.symbol); return;
            }
            if (b.function_targets.at(n.symbol) != 0 && b.function_values_needed.at(n.symbol) == 0 && b.global_fields.at(n.symbol) == 0)
            {
                Node value = get_node(n.left, c);
                // Reading an alias can still throw before its binding initializes.
                if (value.kind == NodeKind.Name && get_symbol(value.symbol, c).function == 0) { il_boxed(n.left, c, b); b.op(0x26); }
                return;
            }
            if (b.global_fields.at(n.symbol) != 0)
            {
                Node initial = get_node(n.left, c);
                if (b.global_vars.at(n.symbol) != 0 && initial.kind == NodeKind.Undefined && initial.constant) { return; }
                il_as(n.left, n.type, c, b); b.initialize(n.symbol); return;
            }
            if (b.iteration_vars.at(n.symbol) != 0)
            {
                Node initial = get_node(n.left, c);
                if (!(initial.kind == NodeKind.Undefined && initial.constant)) { il_as(n.left, n.type, c, b); b.store(n.symbol); }
                return;
            }
            int root = (int)b.scalar_roots.at(n.symbol);
            if (root != 0)
            {
                if (root == n.symbol) { il_scalar_initialize(entry, c, b); }
                return;
            }
            int raw = b.raw_slot(n.symbol); int slot = b.local(raw != 0 ? raw : b.type(n.type, c));
            b.slots.at_mut(n.symbol) = slot;
            if (raw == -6) { il_integer(n.left, c, b); b.store_storage(n.symbol); }
            else { il_as(n.left, n.type, c, b); b.store(n.symbol); }
        }
        else if(n.kind==NodeKind.SuperInitializer){il_boxed(n.left,c,b);il_boxed(n.right,c,b);b.call(RuntimeMember.SuperInitializer);}
        else if (n.kind == NodeKind.ClassStatic) { il_boxed(n.left,c,b);il_boxed(n.right,c,b);b.call(RuntimeMember.InitializeClass); }
        else if (n.kind == NodeKind.ClassField)
        {
            if (n.symbol != 0)
            {
                il_boxed(n.left, c, b); il_boxed(n.fourth, c, b); il_boxed(n.right, c, b);
                b.emit(0x20, checked((int)number(get_token(get_node(n.third, c).token, c).text, c))); b.emit(0x20, n.constant ? 1 : 0);
                b.call(RuntimeMember.InitializePrivateMember); return;
            }
            if (n.fourth != 0)
            {
                il_boxed(n.left, c, b); il_boxed(n.fourth, c, b); il_boxed(n.right, c, b); b.emit(0x20, n.constant ? 1 : 0); b.call(RuntimeMember.InitializeAutoAccessor); return;
            }
            il_boxed(n.left, c, b);
            if (n.third != 0) { il_boxed(n.third, c, b); b.call(RuntimeMember.PropertyKey); } else { il_text(n.name, c, b); }
            int key = -1;
            if (n.constant) { key = b.local(-4); b.op(0x25); b.emit(0xfe0e, key); }
            il_boxed(n.right, c, b);
            if (n.constant) { b.emit(0xfe0c, key); b.call(RuntimeMember.NamePropertyFunction); }
            b.call(RuntimeMember.DefineClassField);
        }
        else if (n.kind == NodeKind.Return)
        {
            if (n.left != 0) { if (b.raw_function_return) { il_number(n.left, c, b); } else { il_as(n.left, return_type, c, b); } }
            else if (b.type(return_type, c) != -1) { b.empty(return_type, c); }
            il_exception_return(return_type,c,b);
        }
        else if (n.kind == NodeKind.Expression)
        {
            if (il_tagged_store(n.left, c, b)) { return; }
            il_expression(n.left, c, b); if (get_type(get_node(n.left, c).type, c).kind != TypeKind.Void) { b.op(0x26); }
        }
        else if (n.kind == NodeKind.If)
        {
            int alternate = b.label(); int done = b.label();
            il_condition(n.left, c, b); b.emit(0x39, alternate); il_statement(n.right, return_type, c, b); b.emit(0x38, done);
            b.mark(alternate); if (n.third != 0) { il_statement(n.third, return_type, c, b); }
            b.mark(done);
        }
        else if (n.kind == NodeKind.ForOf || n.kind == NodeKind.ForIn)
        {
            il_for_of(entry, return_type, c, b);
        }
        else if (n.kind == NodeKind.While || n.kind == NodeKind.For || n.kind == NodeKind.Do)
        {
            il_loop(entry, return_type, c, b);
        }
        else { fail(n.token, "unsupported lowered statement", c); }
    }
    internal static void emit_assembly(TextBytes path, Compiler c)
    {
        int numeric_array = il_numeric_array_type(c);
        IlBackend b = IlBackend.create(c); il_prove_locals(c, b); il_prove_scalars(c, b); il_prove_reversible_arrays(c, b); il_choose_tagged_arrays(c, b); il_runtime(b); il_prepare_enumeration(c, b); il_prepare_spreads(c, b); il_prepare_key_layouts(c, b); il_shapes(c, b); il_key_layouts(c, b); il_spread_shapes(c, b); il_prepare_spread_hints(c, b); il_spread_clones(c, b);
        int object_type = b.RuntimeToken(RuntimeMember.ObjectType);
        int program = b.plan.type("Program", object_type, 0x181);
        // Keep the constructor allocator with this assembly so its instances
        // implement this program's structural interfaces.
        if (b.spread_any_constructor > 0)
        {
            b.function_allocator=b.plan.method1(5,program,0x16,-5,-7,"AllocateFunctionInstance");
            b.function_allocator_cache=b.plan.field(program,0x13,-5,"functionInstanceFactory");
            b.begin(b.function_allocator);b.emit(0x73,b.spread_any_constructor);b.op(0x2a);
        }
        il_prepare_closures(c, b); il_prepare_function_values(program, c, b);
        il_globals(program, c, b); il_infer_scalar_values(c, b); il_infer_array_values(numeric_array, c, b);
        int function = c.functions;
        while (function != 0)
        {
            Node n = get_node(function, c); Vector<int> parameters = new Vector<int>(); int parameter = n.children;
            while (parameter != 0) { parameters.push(b.type(get_node(parameter, c).type, c)); parameter = get_node(parameter, c).next; }
            Vector<byte> name = numbered_name('f', get_symbol(n.symbol, c).id);
            int result_type = b.type(n.type, c);
            int context = b.function_contexts.at(n.symbol); int owner = context == 0 ? program : b.context_types.at((int)context); int attributes = context == 0 ? 0x16 : 6;
            b.methods.at_mut(n.symbol) = b.plan.method(5, owner, attributes, result_type, parameters.view(), name.view());
            Vector<byte> adapter = numbered_name('a', get_symbol(n.symbol, c).id);
            b.function_adapters.at_mut(n.symbol) = b.plan.method1(5, owner, attributes, -5, -7, adapter.view());
            if (il_raw_numeric_function(function, c, b))
            {
                Vector<int> raw_parameters = new Vector<int>(); int p = n.children;
                while (p != 0) { raw_parameters.push(-3); p = get_node(p, c).next; }
                Vector<byte> raw_name = numbered_name('r', get_symbol(n.symbol, c).id);
                b.raw_functions.at_mut(n.symbol) = b.plan.method(5, program, 0x16, -3, raw_parameters.view(), raw_name.view());
            }
            int pattern = il_flat_parameter(function, c);
            if (pattern != 0 && context == 0 && !il_owns_context(function, c, b) && il_has_flat_calls(n.symbol, c))
            {
                Vector<int> fields = new Vector<int>(); int child = get_node(pattern, c).children;
                while (child != 0) { fields.push(b.type(get_node(child, c).type, c)); child = get_node(child, c).next; }
                Vector<byte> flat_name = numbered_name('g', get_symbol(n.symbol, c).id);
                b.binding_methods.at_mut(n.symbol) = b.plan.method(5, program, 0x16, result_type, fields.view(), flat_name.view());
            }
            function = n.next;
        }
        function = c.functions;
        while (function != 0) { Node n = get_node(function, c); il_function_class(n.symbol, c, b); function = n.next; }
        int main = b.plan.method0(5, program, 0x16, -1, "Body");
        int entry = b.plan.method0(5, program, 0x16, -6, "Main");
        function = c.functions;
        while (function != 0)
        {
            Node n = get_node(function, c); b.begin((int)b.function(n.symbol));
            int context = b.function_contexts.at(n.symbol); b.context_scope = context; b.context_slot = -1;
            int parameter = n.children; int index = context == 0 ? 0 : 1;
            while (parameter != 0)
            {
                int symbol = get_node(parameter, c).symbol; b.slots.at_mut(symbol) = -(++index); b.raw_slots.at_mut(symbol) = 0;
                parameter = get_node(parameter, c).next;
            }
            il_context_enter(function, c, b); il_prepare_iteration_vars(function, c, b);
            parameter = n.children;
            while (parameter != 0)
            {
                Node p = get_node(parameter, c);
                if (b.capture_fields.at(p.symbol) != 0)
                {
                    int argument = -b.slots.at(p.symbol) - 1; int value = b.local(b.type(p.type, c)); b.emit(0xfe09, argument); b.emit(0xfe0e, value);
                    if (p.left != 0)
                    {
                        int defined = b.label(); b.emit(0xfe0c, value); b.missing(p.type, c); b.emit(0x39, defined);
                        il_as(p.left, p.type, c, b); b.emit(0xfe0e, value); b.mark(defined);
                    }
                    b.emit(0xfe0c, value); b.initialize(p.symbol);
                }
                else if (p.left != 0)
                {
                    int done = b.label(); b.load(p.symbol); b.missing(p.type, c); b.emit(0x39, done);
                    il_as(p.left, p.type, c, b); b.store(p.symbol); b.mark(done);
                }
                if (p.right != 0)
                {
                    b.load(p.symbol); b.box(p.type, c); int source = b.local(-5); b.emit(0xfe0e, source);
                    il_binding_pattern(p.right, source, c, b);
                }
                parameter = p.next;
            }
            parameter = n.children;
            while (parameter != 0)
            {
                Node p = get_node(parameter, c);
                if (get_type(p.type, c).kind == TypeKind.Number && b.written_symbols.at(p.symbol) == 0)
                {
                    int slot = b.local(-3); b.load(p.symbol); b.call(RuntimeMember.NumberValue); b.emit(0xfe0e, slot);
                    b.numeric_slots.at_mut(p.symbol) = slot; b.numeric_methods.at_mut(p.symbol) = b.method;
                }
                parameter = p.next;
            }
            il_statement(n.left, n.type, c, b);
            if (get_type(n.type, c).kind != TypeKind.Void) { b.empty(n.type, c); }
            b.op(0x2a);
            il_flat_parameter_body(function, c, b);
            function = n.next;
        }
        function = c.functions;
        while (function != 0) { il_raw_function_body(function, c, b); il_function_adapter(function, c, b); function = get_node(function, c).next; }
        b.begin(main); il_prepare_iteration_vars(0, c, b); int statement = c.statements;
        while (statement != 0) { il_statement(statement, 0, c, b); statement = get_node(statement, c).next; }
        b.op(0x2a);
        b.begin(entry); b.op(0x14); b.emit(0xfe06, main); b.emit(0x73, b.RuntimeToken(RuntimeMember.ActionConstructor)); b.call(RuntimeMember.Run); b.op(0x2a);
        b.plan.finish(entry, path);
    }
}
