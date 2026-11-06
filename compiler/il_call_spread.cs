namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static bool il_call_has_spread(int entry, Compiler c)
    {
        int argument = get_node(entry, c).children;
        while (argument != 0) { if (get_node(argument, c).kind == NodeKind.Spread) { return true; } argument = get_node(argument, c).next; }
        return false;
    }
    internal static bool il_static_spread_call(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c); int symbol = il_direct_function(call.left, c, b);
        if (symbol == 0 || get_symbol(symbol, c).function == 0) { return false; }
        int parameter = get_node(get_symbol(symbol, c).function, c).children; int p = parameter;
        while (p != 0) { if (get_node(p, c).third != 0) { return false; } p = get_node(p, c).next; }
        Vector<int> arguments = new Vector<int>(); if (!il_flat_arguments(call.children, arguments, c)) { return false; }
        for (int i = 0; i < arguments.count(); i++)
        {
            int argument = (int)arguments.at(i); Node value = get_node(argument, c);
            if (parameter == 0) { il_expression(argument, c, b); if (get_type(value.type, c).kind != TypeKind.Void) { b.op(0x26); } continue; }
            Node target = get_node(parameter, c);
            if (get_type(value.type, c).kind == TypeKind.Void) { il_boxed(argument, c, b); b.from_object(target.type, c); }
            else { il_as(argument, target.type, c, b); }
            parameter = target.next;
        }
        while (parameter != 0) { Node target = get_node(parameter, c); b.empty(target.type, c); parameter = target.next; }
        b.emit(0x28, (int)b.function(symbol)); b.convert(get_symbol(symbol, c).type, call.type, c); return true;
    }
    internal static int il_call_number_type(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c); Node callee = get_node(call.left, c);
        if (il_array_call(entry, c))
        {
            if (is_text(callee.name, text_slice, c) || is_text(callee.name, text_reverse, c) || il_array_search(entry, c)) { return 0; }
            int owner = get_node(callee.left, c).type;
            return il_number_array(owner, c) ? get_type(owner, c).element : 0;
        }
        int symbol = il_direct_function(call.left, c, b);
        if (symbol == 0) { return 0; }
        int parameter = get_node(get_symbol(symbol, c).function, c).children; int number = 0;
        while (parameter != 0)
        {
            Node p = get_node(parameter, c);
            if (p.third != 0)
            {
                int rest = get_node(p.third, c).type;
                if (!il_number_array(rest, c)) { return 0; }
                number = get_type(rest, c).element;
            }
            else
            {
                if (get_type(p.type, c).kind != TypeKind.Number) { return 0; }
                number = p.type;
            }
            parameter = p.next;
        }
        return number;
    }
    internal static void il_append_call_argument(int entry, int array, int number, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Spread && get_node(n.left, c).kind == NodeKind.Array)
        {
            int child = get_node(n.left, c).children;
            while (child != 0) { il_append_call_argument(child, array, number, c, b); child = get_node(child, c).next; }
            return;
        }
        b.emit(0xfe0c, array);
        if (n.kind == NodeKind.Spread) { il_expression(n.left, c, b); b.emit(0x20, 0); b.call(RuntimeMember.ArraySpread); }
        else if (number != 0) { il_as(entry, number, c, b); b.call(RuntimeMember.NumberPush); b.op(0x26); }
        else { il_boxed(entry, c, b); b.call(RuntimeMember.ArrayAppend); }
    }
    internal static void il_spread_call(int entry, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c); Node callee = get_node(call.left, c); int receiver = -1; int reverse_builtin = -1;
        if (callee.symbol != 0 && get_symbol(callee.symbol, c).function != 0 && il_flat_parameter_call(entry, c, b)) { return; }
        if (il_direct_function(call.left, c, b) != 0 && callee.kind == NodeKind.Name && b.global_flags.at(callee.symbol) != 0) { b.check_global(callee.symbol); }
        if (il_static_spread_call(entry, c, b)) { return; }
        bool array_call = il_array_call(entry, c);
        bool value_call = !array_call && il_direct_function(call.left, c, b) == 0 && !builtin(call.left, text_console, text_log, c) && !builtin(call.left, text_performance, text_now, c) && !builtin_has_own(call.left, c);
        int target = -1; if (value_call) { il_boxed(call.left, c, b); target = b.local(-5); b.emit(0xfe0e, target); }
        if (array_call) { il_expression(callee.left, c, b); receiver = b.local(-5); b.emit(0xfe0e, receiver); }
        if (array_call && is_text(callee.name, text_reverse, c))
        {
            b.emit(0xfe0c, receiver); b.call(RuntimeMember.ArrayReverseBuiltin); reverse_builtin = b.local(-2); b.emit(0xfe0e, reverse_builtin);
        }
        int numeric = il_call_number_type(entry, c, b);
        b.emit(0x20, 0); b.emit(0x73, b.RuntimeToken(numeric != 0 ? RuntimeMember.NumberArrayConstructor : RuntimeMember.ArrayConstructor));
        int array = b.local(-5); b.emit(0xfe0e, array); int argument = call.children;
        while (argument != 0) { il_append_call_argument(argument, array, numeric, c, b); argument = get_node(argument, c).next; }
        if (value_call) { b.emit(0xfe0c, target); b.emit(0xfe0c, array); b.call(RuntimeMember.CallFunctionArguments); il_function_result(call.type, c, b); return; }
        if (builtin(call.left, text_console, text_log, c)) { b.emit(0xfe0c, array); b.call(RuntimeMember.LogArguments); return; }
        if (builtin_has_own(call.left, c))
        {
            b.emit(0xfe0c, array); b.emit(0x20, 0); b.call(RuntimeMember.ForOfValue);
            b.emit(0xfe0c, array); b.emit(0x20, 1); b.call(RuntimeMember.ForOfValue);
            b.call(RuntimeMember.HasOwn); b.call(RuntimeMember.WrapBoolean); return;
        }
        if (builtin(call.left, text_performance, text_now, c)) { b.call(RuntimeMember.Now); b.call(RuntimeMember.WrapNumber); return; }
        if (array_call)
        {
            b.emit(0xfe0c, receiver);
            if (is_text(callee.name, text_reverse, c)) { b.emit(0xfe0c, reverse_builtin); b.call(RuntimeMember.ArrayReverse); return; }
            if (il_array_search(entry, c))
            {
                b.emit(0xfe0c, array); b.emit(0x20, 0); b.call(RuntimeMember.ForOfValue);
                b.emit(0xfe0c, array); b.emit(0x20, 1); b.call(RuntimeMember.ForOfValue);
                b.emit(0x20, is_text(callee.name, text_includes, c) ? 1 : 0); b.call(RuntimeMember.ArraySearch);
                il_array_search_result(entry, c, b); return;
            }
            if (is_text(callee.name, text_slice, c))
            {
                b.emit(0xfe0c, array); b.emit(0x20, 0); b.call(RuntimeMember.ForOfValue);
                b.emit(0xfe0c, array); b.emit(0x20, 1); b.call(RuntimeMember.ForOfValue); b.call(RuntimeMember.Slice); return;
            }
            if (is_text(callee.name, text_push, c))
            {
                b.emit(0xfe0c, array); b.call(RuntimeMember.PushArguments); b.call(RuntimeMember.WrapNumber);
                if (get_type(call.type, c).kind != TypeKind.Number) { b.call(RuntimeMember.BoxNumber); }
            }
            else if (il_number_array(get_node(callee.left, c).type, c)) { b.call(RuntimeMember.NumberPop); }
            else { b.call(RuntimeMember.ArrayPop); b.from_object(call.type, c); }
            return;
        }
        int symbol = il_direct_function(call.left, c, b);
        int context = b.function_contexts.at(symbol); if (context != 0) { b.load_context(context); }
        int parameter = get_node(get_symbol(symbol, c).function, c).children; int index = 0;
        while (parameter != 0)
        {
            Node p = get_node(parameter, c); b.emit(0xfe0c, array); b.emit(0x20, index);
            if (p.third != 0) { b.emit(0x20, il_number_array(get_node(p.third, c).type, c) ? 1 : 0); b.call(RuntimeMember.CallRest); }
            else if (get_type(p.type, c).kind == TypeKind.Number) { b.call(RuntimeMember.ForOfNumber); }
            else { b.call(RuntimeMember.ForOfValue); b.from_object(p.type, c); }
            index++; parameter = p.next;
        }
        b.emit(0x28, (int)b.function(symbol)); b.convert(get_symbol(symbol, c).type, call.type, c);
    }
}
