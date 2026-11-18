namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal Vector<int> constructor_definitions, constructor_declarations, constructor_parameters;
    internal bool constructor_bindings_ready;
    internal readonly Dictionary<int, (int Method, int Fields, int Constructor, int Count)> known_class_constructors = [];
}

internal static partial class CompilerFunctions
{
    // Stable base-class bindings can call their generated bodies without a
    // function-object invocation when no frame or captured environment is needed.
    static bool il_known_class_construction(int entry, Compiler c, IlBackend b)
    {
        Node construction = get_node(entry, c);
        if (!il_numeric_class_eligible(construction.type, c, b) || b.classes.at(construction.type) == 0) return false;
        if (!b.constructor_bindings_ready) {
            b.constructor_bindings_ready = true;
            b.constructor_definitions = zero_ids(c.symbol_store.count() + 1);
            b.constructor_declarations = zero_ids(c.symbol_store.count() + 1);
            b.constructor_parameters = zero_ids(c.symbol_store.count() + 1);
            for (int i = 1; i <= c.node_store.count(); i++) {
                Node node = get_node(i, c);
                if ((node.kind is NodeKind.Variable or NodeKind.Binding) && node.symbol != 0) {
                    b.constructor_definitions.at_mut(node.symbol) = i;
                    b.constructor_declarations.at_mut(node.symbol)++;
                }
                if (node.kind == NodeKind.Function)
                    for (int parameter = node.children; parameter != 0; parameter = get_node(parameter, c).next)
                        if (get_node(parameter, c).symbol != 0) b.constructor_parameters.at_mut(get_node(parameter, c).symbol) = 1;
            }
        }
        int definition = known_class_definition(entry, b.constructor_definitions, b.constructor_declarations,
            b.constructor_parameters, c, b, allowMembers: true);
        if (definition == 0) return false;
        Node klass = get_node(definition, c);
        int fields = Function(klass.left), constructor = klass.right == 0 ? 0 : Function(klass.right);
        if (fields == 0 || (klass.right != 0 && constructor == 0) || Count(fields) != 0) return false;
        int count = 0;
        for (int argument = construction.children; argument != 0; argument = get_node(argument, c).next) {
            if (++count > 2 || get_node(argument, c).kind == NodeKind.Spread ||
                get_type(get_node(argument, c).type, c).kind != TypeKind.Number) return false;
        }
        if (constructor == 0 ? count != 0 : Count(constructor) != count) return false;
        if (!b.known_class_constructors.TryGetValue(construction.type, out var plan)) {
            int[] signature = new[] {-5}.Concat(Enumerable.Repeat(-10, count)).ToArray();
            int method = b.plan.method(5, b.classes.at(construction.type), 0x16, -5, signature, "Construct_body");
            plan = (method, fields, constructor, count);
            b.known_class_constructors.Add(construction.type, plan);
        }
        // Read the class binding before evaluating arguments. The source bodies
        // still initialize fields and execute the constructor in their original order.
        il_boxed(construction.left, c, b);
        for (int argument = construction.children; argument != 0; argument = get_node(argument, c).next) il_expression(argument, c, b);
        b.emit(0x28, plan.Method); b.from_object(construction.type, c);
        return true;

        int Function(int value) {
            int function = scalar_field_function(value, c);
            if (function == 0) return 0;
            Node node = get_node(function, c);
            if (node.symbol == 0 || node.left == 0 || (node.fourth != 0 && get_node(node.fourth, c).constant) ||
                b.function_contexts.at(node.symbol) != 0) return 0;
            return node.symbol;
        }

        int Count(int symbol) {
            Node function = get_node(get_symbol(symbol, c).function, c);
            int count = 0;
            if (function.fourth != 0 && (count = il_class_receiver_arguments(symbol, c, b)) < 0) return -1;
            int parameter = function.fourth == 0 ? function.children : get_node(function.fourth, c).next;
            for (; parameter != 0; parameter = get_node(parameter, c).next) {
                Node value = get_node(parameter, c);
                if ((function.fourth == 0 && ++count > 2) || value.kind != NodeKind.Variable || value.symbol == 0 ||
                    value.constant || value.left != 0 || value.right != 0 || value.third != 0 || value.fourth != 0 ||
                    value.children != 0 || get_type(value.type, c).kind != TypeKind.Number) return -1;
            }
            return count;
        }
    }

    static void il_known_class_constructor_bodies(Compiler c, IlBackend b)
    {
        foreach (var entry in b.known_class_constructors) {
            int type = entry.Key;
            var plan = entry.Value;
            b.begin(plan.Method);
            int instance = b.local(b.classes.at(type));
            b.emit(0x73, b.constructors.at(type)); b.emit(0xfe0e, instance);
            b.emit(0xfe0c, instance); b.emit(0xfe09, 0); b.emit(0x20, 0); b.call(RuntimeMember.InitializeDirectClass);
            Call(plan.Fields, false);
            if (plan.Constructor != 0) {
                Node function = get_node(get_symbol(plan.Constructor, c).function, c);
                Call(plan.Constructor, true);
                if (get_type(function.type, c).kind != TypeKind.Void) {
                    b.box(function.type, c);
                    int returned = b.local(-5), ordinary = b.label();
                    b.emit(0xfe0e, returned); b.emit(0xfe0c, returned); b.emit(0x75, b.RuntimeToken(RuntimeMember.TsObjectType));
                    b.emit(0x39, ordinary); b.emit(0xfe0c, returned); b.op(0x2a); b.mark(ordinary);
                }
            }
            b.emit(0xfe0c, instance); b.op(0x2a);

            void Call(int symbol, bool arguments) {
                Node function = get_node(get_symbol(symbol, c).function, c);
                if (function.fourth != 0) b.emit(0xfe0c, instance);
                if (arguments) for (int i = 0; i < plan.Count; i++) b.emit(0xfe09, i + 1);
                b.emit(0x28, b.function(symbol));
                if (!arguments && get_type(function.type, c).kind != TypeKind.Void) b.op(0x26);
            }
        }
    }
}
