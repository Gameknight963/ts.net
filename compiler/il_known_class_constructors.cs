namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal Vector<int> constructor_definitions, constructor_declarations, constructor_parameters;
    internal bool constructor_bindings_ready;
    internal readonly Dictionary<int, (int Method, int Fields, int Constructor, int Count)> known_class_constructors = [];
    internal readonly Dictionary<int, List<int>> known_class_literal_fields = [];
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
        int[] parameters = Parameters(constructor).ToArray();
        int count = 0;
        for (int argument = construction.children; argument != 0; argument = get_node(argument, c).next) {
            if (get_node(argument, c).kind == NodeKind.Spread || count >= parameters.Length ||
                b.type(get_node(argument, c).type, c) != b.type(get_node(parameters[count], c).type, c)) return false;
            count++;
        }
        if (constructor == 0 ? count != 0 : Count(constructor) != count) return false;
        if (!b.known_class_constructors.TryGetValue(construction.type, out var plan)) {
            int[] signature = new[] {-5}.Concat(parameters.Select(parameter => b.type(get_node(parameter, c).type, c))).ToArray();
            int method = b.plan.method(5, b.classes.at(construction.type), 0x16, -5, signature, "Construct_body");
            plan = (method, fields, constructor, count);
            b.known_class_constructors.Add(construction.type, plan);
            var literals = il_class_literal_fields(fields, construction.type, c, b);
            if (literals != null) b.known_class_literal_fields[construction.type] = literals;
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
            if (node.symbol == 0 || node.left == 0 || node.right != 0 || node.internal_continuation || (node.fourth != 0 && get_node(node.fourth, c).constant) ||
                b.function_contexts.at(node.symbol) != 0) return 0;
            return node.symbol;
        }

        IEnumerable<int> Parameters(int symbol) {
            if (symbol == 0) yield break;
            Node function = get_node(get_symbol(symbol, c).function, c);
            for (int parameter = function.fourth == 0 ? function.children : get_node(function.fourth, c).next;
                parameter != 0; parameter = get_node(parameter, c).next) yield return parameter;
        }

        int Count(int symbol) {
            Node function = get_node(get_symbol(symbol, c).function, c);
            int count = 0;
            if (function.fourth != 0 && get_node(function.fourth, c).name != 0) return -1;
            int parameter = function.fourth == 0 ? function.children : get_node(function.fourth, c).next;
            for (; parameter != 0; parameter = get_node(parameter, c).next) {
                Node value = get_node(parameter, c);
                if (value.kind != NodeKind.Variable || value.symbol == 0 ||
                    value.constant || value.left != 0 || value.right != 0 || value.third != 0 || value.fourth != 0 ||
                    value.children != 0 || get_type(value.type, c).kind == TypeKind.Void) return -1;
                count++;
            }
            return count;
        }
    }

    // No source code can observe these stores before the header is installed.
    // Require every field, in layout order, and only effect-free literal values.
    static List<int> il_class_literal_fields(int symbol, int type, Compiler c, IlBackend b)
    {
        Node function = get_node(get_symbol(symbol, c).function, c);
        if (function.fourth == 0 || function.children != function.fourth || function.left == 0) return null;
        Node receiver = get_node(function.fourth, c), body = get_node(function.left, c);
        if (receiver.next != 0 || body.kind != NodeKind.Block) return null;
        var members = b.numeric_class_members[type];
        var values = new List<int>();
        for (int statement = body.children; statement != 0; statement = get_node(statement, c).next) {
            Node field = get_node(statement, c);
            if (values.Count >= members.Count || field.kind != NodeKind.ClassField || field.name == 0 ||
                field.left == 0 || field.right == 0 || field.third != 0 || field.fourth != 0 ||
                get_node(field.left, c).kind != NodeKind.Name || get_node(field.left, c).symbol != receiver.symbol) return null;
            Field member = get_field(members[values.Count], c);
            TypeKind kind = get_type(member.type, c).kind;
            if (!same(field.name, member.name, c) || kind is not (TypeKind.Number or TypeKind.String or TypeKind.Boolean)) return null;
            Node value = get_node(field.right, c);
            bool empty = value.kind == NodeKind.Undefined && !value.constant && value.left == 0 && value.right == 0 &&
                value.third == 0 && value.fourth == 0 && value.children == 0;
            if (!empty && !scalar_class_literal(field.right, kind, c)) return null;
            values.Add(field.right);
        }
        return values.Count == members.Count ? values : null;
    }

    static void il_known_class_constructor_bodies(Compiler c, IlBackend b)
    {
        foreach (var entry in b.known_class_constructors) {
            int type = entry.Key;
            var plan = entry.Value;
            b.begin(plan.Method);
            int instance = b.local(b.classes.at(type));
            b.emit(0x73, b.constructors.at(type)); b.emit(0xfe0e, instance);
            if (b.known_class_literal_fields.TryGetValue(type, out var literals)) {
                ulong states = 0;
                var members = b.numeric_class_members[type];
                for (int i = 0; i < literals.Count; i++) {
                    int fieldType = get_field(members[i], c).type;
                    b.emit(0xfe0c, instance); il_as(literals[i], fieldType, c, b);
                    if (get_type(fieldType, c).kind == TypeKind.Number) b.call(RuntimeMember.NumberValue);
                    else if (get_type(fieldType, c).kind == TypeKind.Boolean) b.call(RuntimeMember.BooleanValue);
                    else b.box(fieldType, c);
                    b.emit(0x7d, b.class_field_tokens[type][i]);
                    states |= (get_node(literals[i], c).kind == NodeKind.Undefined ? 1ul : 3ul) << (i * 2);
                }
                b.emit(0xfe0c, instance); b.emit(0xfe09, 0); b.emit(0x20, unchecked((int)states));
                if (members.Count > 16) { b.emit(0x20, unchecked((int)(states >> 32))); b.call(RuntimeMember.InitializeDirectClassWide); }
                else b.call(RuntimeMember.InitializeDirectClass);
            }
            else {
                b.emit(0xfe0c, instance); b.emit(0xfe09, 0); b.emit(0x20, 0); b.call(RuntimeMember.InitializeDirectClass);
                Call(plan.Fields, false);
            }
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
                if (arguments) {
                    int parameter = function.fourth == 0 ? function.children : get_node(function.fourth, c).next;
                    for (int i = 0; i < plan.Count; i++, parameter = get_node(parameter, c).next) {
                        b.emit(0xfe09, i + 1);
                        // Reference checks belong after field initialization, as in
                        // the ordinary constructor adapter, and after all arguments run.
                        int parameterType = get_node(parameter, c).type;
                        if (b.type(parameterType, c) == -5) b.from_object(parameterType, c);
                    }
                }
                b.emit(0x28, b.function(symbol));
                if (!arguments && get_type(function.type, c).kind != TypeKind.Void) b.op(0x26);
            }
        }
    }
}
