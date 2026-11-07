namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static bool il_uses_arguments(int symbol, Compiler c)
    {
        int function = get_symbol(symbol, c).function;
        int receiver = function == 0 ? 0 : get_node(function, c).fourth;
        return receiver != 0 && get_node(receiver, c).name != 0;
    }
    internal static bool il_suspending_function(int symbol, Compiler c)
    {
        int function = get_symbol(symbol, c).function;
        return function != 0 && get_node(function, c).right != 0;
    }
    internal static int il_direct_function(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.symbol != 0 && il_suspending_function(n.symbol, c)) return 0;
        if (n.kind == NodeKind.FunctionValue) { return il_uses_arguments(n.symbol, c) ? 0 : n.symbol; }
        if (n.kind != NodeKind.Name || n.symbol == 0) { return 0; }
        if (get_symbol(n.symbol, c).function != 0)
        {
            if (il_uses_arguments(n.symbol, c)) return 0;
            Node function = get_node(get_symbol(n.symbol, c).function, c);
            return b.function_contexts.at(n.symbol) == 0 || function.constant ? n.symbol : 0;
        }
        int target = (int)b.function_targets.at(n.symbol);
        return target != 0 && (b.function_contexts.at(target) != 0 || il_uses_arguments(target, c) || il_suspending_function(target, c)) ? 0 : target;
    }
    internal static int il_predicate_reference(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        while (n.kind == NodeKind.Assert) { n = get_node(n.left, c); }
        if (n.kind >= NodeKind.NumberIsNaNFunction && n.kind <= NodeKind.NumberIsSafeIntegerFunction)
        {
            return (int)n.kind - (int)NodeKind.NumberIsNaNFunction + 1;
        }
        return n.kind == NodeKind.Name ? b.predicate_targets.at(n.symbol) : 0;
    }
    internal static void il_prepare_function_values(int program, Compiler c, IlBackend b)
    {
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if (n.kind == NodeKind.Variable && n.left != 0 && n.fourth == 0 && b.written_symbols.at(n.symbol) == 0 && get_node((int)b.node_parents.at(i), c).kind != NodeKind.Function)
            {
                Node value = get_node(n.left, c); int target = 0;
                if (value.kind == NodeKind.FunctionValue && b.function_contexts.at(value.symbol) == 0) { target = value.symbol; }
                if (value.kind == NodeKind.Name && get_symbol(value.symbol, c).function != 0 && b.function_contexts.at(value.symbol) == 0) { target = value.symbol; }
                b.function_targets.at_mut(n.symbol) = (int)target;
            }
        }
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 1; i <= c.node_store.count(); i++)
            {
                Node n = get_node(i, c);
                // Defaults can be replaced by callers; var bindings are hoisted.
                if (n.kind != NodeKind.Variable || n.left == 0 || n.fourth != 0 || b.written_symbols.at(n.symbol) != 0 ||
                    get_node((int)b.node_parents.at(i), c).kind == NodeKind.Function) { continue; }
                if (b.predicate_targets.at(n.symbol) == 0)
                {
                    int target = il_predicate_reference(n.left, c, b);
                    if (target != 0) { b.predicate_targets.at_mut(n.symbol) = target; changed = true; }
                }
                Node value = get_node(n.left, c);
                if (b.function_targets.at(n.symbol) == 0 && value.kind == NodeKind.Name)
                {
                    int target = il_direct_function(n.left, c, b);
                    if (target != 0) { b.function_targets.at_mut(n.symbol) = (int)target; changed = true; }
                }
            }
        }
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if (n.kind != NodeKind.Name || n.symbol == 0) { continue; }
            Node parent = get_node((int)b.node_parents.at(i), c);
            bool direct = parent.kind == NodeKind.Call && parent.left == i && il_direct_function(i, c, b) != 0;
            if (!direct) { b.function_values_needed.at_mut(n.symbol) = 1; }
            if (get_symbol(n.symbol, c).function != 0)
            {
                if (b.written_symbols.at(n.symbol) != 0) { fail(n.token, "reassigning function declarations needs lowering", c); }
                if (!direct && get_node(get_symbol(n.symbol, c).function, c).constant) { fail(n.token, "named function expression self values need lowering", c); }
                if (!direct && b.function_caches.at(n.symbol) == 0)
                {
                    Vector<byte> name = numbered_name('c', get_symbol(n.symbol, c).id);
                    b.function_caches.at_mut(n.symbol) = b.plan.field(program, 0x13, -5, name.view());
                }
            }
        }
    }
    internal static int il_numeric_function(int symbol, Compiler c)
    {
        Node f = get_node(get_symbol(symbol, c).function, c);
        if (get_type(f.type, c).kind != TypeKind.Number) { return -1; }
        int count = 0; int parameter = f.children;
        while (parameter != 0)
        {
            Node p = get_node(parameter, c);
            if (p.third != 0 || get_type(p.type, c).kind != TypeKind.Number || ++count > 2) { return -1; }
            parameter = p.next;
        }
        return count;
    }
    internal static bool il_function_returns(int entry, Compiler c)
    {
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Return) { return true; }
        if (n.kind == NodeKind.Block)
        {
            int child = n.children; if (child == 0) { return false; }
            while (get_node(child, c).next != 0) { child = get_node(child, c).next; }
            return il_function_returns(child, c);
        }
        if (n.kind == NodeKind.If && n.third != 0) { return il_function_returns(n.right, c) && il_function_returns(n.third, c); }
        return false;
    }
    internal static bool il_raw_numeric_function(int entry, Compiler c, IlBackend b)
    {
        Node f = get_node(entry, c);
        if (b.function_contexts.at(f.symbol) != 0 || il_owns_context(entry, c, b) || il_numeric_function(f.symbol, c) < 0 || !il_function_returns(f.left, c)) { return false; }
        int parameter = f.children;
        while (parameter != 0) { Node p = get_node(parameter, c); if (p.left != 0 || b.written_symbols.at(p.symbol) != 0) { return false; } parameter = p.next; }
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if (scalar_owner(i, b.node_parents, c) != entry) { continue; }
            if (n.kind == NodeKind.Return && (n.left == 0 || !il_defined(n.left, c) || get_type(get_node(n.left, c).type, c).kind != TypeKind.Number)) { return false; }
            if (n.kind != NodeKind.Name) { continue; }
            parameter = f.children; bool argument = false;
            while (parameter != 0) { Node p = get_node(parameter, c); if (p.symbol == n.symbol) { argument = true; break; } parameter = p.next; }
            if (!argument) { continue; }
            Node parent = get_node((int)b.node_parents.at(i), c);
            // Arithmetic converts undefined to NaN. Returning, testing, storing,
            // or forwarding the original parameter would distinguish the two.
            if (parent.type == 0 || get_type(parent.type, c).kind != TypeKind.Number) { return false; }
            if (parent.kind == NodeKind.Binary || parent.kind == NodeKind.Unary) { continue; }
            if (parent.kind == NodeKind.Assign && !is_text(get_token(parent.token, c).text, text_assign, c) && !il_logical_assignment(get_token(parent.token, c).text, c)) { continue; }
            return false;
        }
        return true;
    }
    internal static void il_raw_function_body(int entry, Compiler c, IlBackend b)
    {
        Node f = get_node(entry, c); int method = b.raw_functions.at(f.symbol); if (method == 0) { return; }
        b.begin(method); b.raw_function_return = true; int parameter = f.children; int index = 0;
        while (parameter != 0) { Node p = get_node(parameter, c); b.slots.at_mut(p.symbol) = -(++index); b.raw_slots.at_mut(p.symbol) = -3; parameter = p.next; }
        il_prepare_iteration_vars(entry, c, b); il_statement(f.left, f.type, c, b);
        b.plan.number(b.method, 0); b.op(0x2a); b.raw_function_return = false;
        parameter = f.children; while (parameter != 0) { Node p = get_node(parameter, c); b.raw_slots.at_mut(p.symbol) = 0; parameter = p.next; }
    }
    internal static // Give the CLR a virtual call it can devirtualize to the function body.
                    // Other arities keep the base implementation, including missing arguments.
void il_function_class(int symbol, Compiler c, IlBackend b)
    {
        int count = il_numeric_function(symbol, c); if (count < 0) { return; }
        Vector<byte> name = numbered_name('F', get_symbol(symbol, c).id);
        int parent = b.RuntimeToken(RuntimeMember.FunctionType); int type = b.plan.type(name.view(), parent, 0x101);
        Vector<int> parameters = new Vector<int>(); parameters.push(-5); parameters.push(-5); parameters.push(-4); parameters.push(-6);
        int context = b.function_contexts.at(symbol); int environment_field = 0;
        if (context != 0) { int environment_type = b.context_types.at((int)context); parameters.push(environment_type); environment_field = b.plan.field(type, 0x26, environment_type, "environment"); }
        int constructor = b.plan.method(5, type, 0x1886, -1, parameters.view(), ".ctor");
        b.function_constructors.at_mut(symbol) = constructor; b.begin(constructor);
        for (int i = 0; i < 5; i++) { b.emit(0xfe09, i); }
        b.call(RuntimeMember.FunctionConstructor);
        if (context != 0) { b.emit(0xfe09, 0); b.emit(0xfe09, 5); b.emit(0x7d, environment_field); }
        b.op(0x2a);
        int method = 0;
        if (count == 0) { method = b.plan.method0(5, type, 0xc6, -10, "InvokeNumber0"); }
        else if (count == 1) { method = b.plan.method1(5, type, 0xc6, -10, -10, "InvokeNumber1"); }
        else { method = b.plan.method2(5, type, 0xc6, -10, -10, -10, "InvokeNumber2"); }
        b.begin(method);
        int raw = b.raw_functions.at(symbol);
        if (context != 0) { b.emit(0xfe09, 0); b.emit(0x7b, environment_field); }
        for (int i = 0; i < count; i++) { b.emit(0xfe09, i + 1); if (raw != 0) { b.call(RuntimeMember.NumberValue); } }
        b.emit(0x28, raw != 0 ? raw : b.function(symbol));
        if (raw != 0) { b.call(RuntimeMember.WrapNumber); }
        b.op(0x2a);
    }
    internal static void il_function_object(int symbol, Compiler c, IlBackend b)
    {
        int context = b.function_contexts.at(symbol); int adapter = b.function_adapters.at(symbol);
        if (context != 0) { b.load_context(context); } else { b.op(0x14); }
        b.emit(0xfe06, adapter); b.emit(0x73, b.RuntimeToken(RuntimeMember.FunctionBodyConstructor));
        int numeric = il_numeric_function(symbol, c);
        if (numeric < 0) { b.op(0x14); }
        else
        {
            int raw = b.raw_functions.at(symbol); if (context != 0) { b.load_context(context); } else { b.op(0x14); }
            b.emit(0xfe06, raw != 0 ? raw : b.function(symbol));
            RuntimeMember constructorLocal = numeric == 0 ? RuntimeMember.NumberFunction0Constructor : numeric == 1 ? RuntimeMember.NumberFunction1Constructor : RuntimeMember.NumberFunction2Constructor;
            if (raw != 0) { constructorLocal = numeric == 0 ? RuntimeMember.RawNumberFunction0Constructor : numeric == 1 ? RuntimeMember.RawNumberFunction1Constructor : RuntimeMember.RawNumberFunction2Constructor; }
            b.emit(0x73, b.RuntimeToken(constructorLocal));
        }
        il_text(get_symbol(symbol, c).name, c, b);
        int length = 0; int parameter = get_node(get_symbol(symbol, c).function, c).children;
        if (get_node(get_symbol(symbol,c).function,c).fourth != 0) { parameter = get_node(parameter,c).next; }
        while (parameter != 0) { Node p = get_node(parameter, c); if (p.left != 0 || p.third != 0) { break; } length++; parameter = p.next; }
        b.emit(0x20, length); int constructor = b.function_constructors.at(symbol);
        if (context != 0 && constructor != 0) { b.load_context(context); }
        b.emit(0x73, constructor != 0 ? constructor : b.RuntimeToken(get_node(get_symbol(symbol,c).function,c).fourth != 0 ? RuntimeMember.ReceiverFunctionConstructor : RuntimeMember.FunctionConstructor));
        int receiver = get_node(get_symbol(symbol, c).function, c).fourth;
        if (receiver != 0 && get_node(receiver, c).constant) b.call(RuntimeMember.EnableNewTarget);
        if (receiver != 0 && get_node(receiver, c).name != 0) b.call(RuntimeMember.EnableArguments);
        if (il_suspending_function(symbol, c))
        {
            int marker = get_node(get_symbol(symbol, c).function, c).right;
            b.call(is_text(get_node(marker, c).name, "generator", c) ? RuntimeMember.GeneratorFunction : RuntimeMember.AsyncFunction);
        }
        if (get_node(get_symbol(symbol,c).function,c).third != 0)
        {
            if(b.function_allocator==0)b.op(0x14);
            else {
                int ready=b.label();b.emit(0x7e,b.function_allocator_cache);b.op(0x25);b.emit(0x3a,ready);b.op(0x26);
                b.op(0x14);b.emit(0xfe06,b.function_allocator);b.emit(0x73,b.RuntimeToken(RuntimeMember.FunctionBodyConstructor));
                b.op(0x25);b.emit(0x80,b.function_allocator_cache);b.mark(ready);
            }
            b.call(RuntimeMember.MarkFunctionPrototype);
        }
    }
    internal static void il_function_reference(int symbol, Compiler c, IlBackend b)
    {
        int field = b.function_caches.at(symbol); int done = b.label();
        if (field == 0) { fail(get_node(get_symbol(symbol, c).function, c).token, "function value cache missing", c); }
        b.emit(0x7e, field); b.op(0x25); b.emit(0x3a, done); b.op(0x26);
        il_function_object(symbol, c, b); b.op(0x25); b.emit(0x80, field); b.mark(done);
    }
    internal static void il_function_adapter(int entry, Compiler c, IlBackend b)
    {
        Node f = get_node(entry, c); int adapter = b.function_adapters.at(f.symbol); b.begin(adapter);
        int context = b.function_contexts.at(f.symbol);
        if (context != 0) { b.emit(0xfe09, 0); }
        int parameter = f.children; int index = 0;
        while (parameter != 0)
        {
            Node p = get_node(parameter, c); b.emit(0xfe09, context == 0 ? 0 : 1); b.emit(0x20, index++);
            if (p.third != 0) { b.emit(0x20, il_number_array(p.type, c) ? 1 : 0); b.call(RuntimeMember.FunctionRest); }
            else { b.call(RuntimeMember.FunctionArgument); }
            b.from_object(p.type, c); parameter = p.next;
        }
        b.emit(0x28, b.function(f.symbol));
        if (get_type(f.type, c).kind == TypeKind.Void) { b.call(RuntimeMember.Undefined); }
        else { b.box(f.type, c); }
        b.op(0x2a);
    }
    internal static void il_function_result(int type, Compiler c, IlBackend b)
    {
        if (get_type(type, c).kind == TypeKind.Void) { b.op(0x26); }
        else { b.from_object(type, c); }
    }
    internal static void il_value_call(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c); int count = 0; bool numeric = get_type(n.type, c).kind == TypeKind.Number; int argument = n.children;
        while (argument != 0) { Node value = get_node(argument, c); count++; if (get_type(value.type, c).kind != TypeKind.Number && value.kind != NodeKind.Undefined) { numeric = false; } argument = value.next; }
        int receiver=il_method_target(n.left,c,b);
        if (receiver>=0) {b.emit(0xfe0c,receiver);}
        if (numeric && count <= 2)
        {
            int number = n.type;
            argument = n.children;
            while (argument != 0) { il_as(argument, number, c, b); argument = get_node(argument, c).next; }
            b.call(receiver>=0 ? (count==0 ? RuntimeMember.CallMethodNumber0 : count==1 ? RuntimeMember.CallMethodNumber1 : RuntimeMember.CallMethodNumber2) :
                (count == 0 ? RuntimeMember.CallFunctionNumber0 : count == 1 ? RuntimeMember.CallFunctionNumber1 : RuntimeMember.CallFunctionNumber2)); return;
        }
        if (count == 0) { b.call(receiver>=0 ? RuntimeMember.CallMethod0 : RuntimeMember.CallFunction0); il_function_result(n.type, c, b); return; }
        b.emit(0x20, count); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType)); argument = n.children; int index = 0;
        while (argument != 0) { b.op(0x25); b.emit(0x20, index++); il_boxed(argument, c, b); b.op(0xa2); argument = get_node(argument, c).next; }
        b.call(receiver>=0 ? RuntimeMember.CallMethod : RuntimeMember.CallFunction); il_function_result(n.type, c, b);
    }
}
