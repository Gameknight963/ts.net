namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, List<(int Name, int Value)>> direct_class_initializers = [];
    internal readonly Dictionary<int, int> direct_class_constructors = [];
    internal readonly Dictionary<(int Type, string Parameters), int> direct_class_constructor_methods = [];
}

internal static partial class CompilerFunctions
{
    static bool il_direct_class_construction(int entry, Compiler c, IlBackend b)
    {
        if (!b.direct_class_initializers.TryGetValue(entry, out var initializers)) return false;
        Node construction = get_node(entry, c);
        var members = b.numeric_class_members[construction.type];
        // The raw layout must preserve the source's property creation order.
        var order = initializers.Select(value => value.Name).ToList();
        if (b.scalar_constructor_assignments.TryGetValue(entry, out var assignments))
            foreach (var assignment in assignments)
                if (!order.Any(name => same(name, assignment.Name, c))) order.Add(assignment.Name);
        if (order.Count != members.Count) return false;
        for (int i = 0; i < order.Count; i++)
            if (!same(order[i], get_field(members[i], c).name, c)) return false;
        var arguments = b.scalar_constructor_arguments.GetValueOrDefault(entry) ?? [];
        if (!b.direct_class_constructors.TryGetValue(entry, out int method)) {
            // The body depends on the class and converted parameter types,
            // while call-site expressions are evaluated separately below.
            var key = (construction.type, string.Join(",", arguments.Select(argument => argument.Type)));
            if (!b.direct_class_constructor_methods.TryGetValue(key, out method)) {
                int[] signature = new[] {-5}.Concat(arguments.Select(argument => b.type(argument.Type, c))).ToArray();
                method = b.plan.method(5, b.classes.at(construction.type), 0x16, b.classes.at(construction.type), signature, "Construct_" + entry);
                b.direct_class_constructor_methods.Add(key, method);
            }
            b.direct_class_constructors.Add(entry, method);
        }
        // The binding read and every supplied argument retain their source order.
        il_boxed(construction.left, c, b);
        var evaluated = new List<(int Slot, int Type)>();
        foreach (var argument in arguments) {
            int type = get_node(argument.Argument, c).type, slot = b.local(b.type(type, c));
            il_expression(argument.Argument, c, b); b.emit(0xfe0e, slot); evaluated.Add((slot, type));
        }
        for (int i = 0; i < arguments.Count; i++) {
            b.emit(0xfe0c, evaluated[i].Slot); b.convert(evaluated[i].Type, arguments[i].Type, c);
        }
        b.emit(0x28, method);
        return true;
    }

    static void il_direct_class_constructor_bodies(Compiler c, IlBackend b)
    {
        var emitted = new HashSet<int>();
        foreach (var entry in b.direct_class_constructors) {
            if (!emitted.Add(entry.Value)) continue;
            int type = get_node(entry.Key, c).type;
            var members = b.numeric_class_members[type];
            var arguments = b.scalar_constructor_arguments.GetValueOrDefault(entry.Key) ?? [];
            var initializers = b.direct_class_initializers[entry.Key];
            var assignments = b.scalar_constructor_assignments.GetValueOrDefault(entry.Key) ?? [];
            b.begin(entry.Value);
            var parameters = new Dictionary<int, (int Slot, int Type)>();
            for (int i = 0; i < arguments.Count; i++) {
                int slot = b.local(b.type(arguments[i].Type, c));
                b.emit(0xfe09, i + 1);
                il_scalar_constructor_default(entry.Key, arguments[i].Symbol, arguments[i].Type, c, b);
                b.emit(0xfe0e, slot);
                parameters[arguments[i].Symbol] = (slot, arguments[i].Type);
            }
            int instance = b.local(b.classes.at(type)), states = b.local(-6);
            b.emit(0x73, b.constructors.at(type)); b.emit(0xfe0e, instance);
            b.emit(0x20, 0); b.emit(0xfe0e, states);
            int highStates = members.Count > 16 ? b.local(-6) : -1;
            if (highStates >= 0) { b.emit(0x20, 0); b.emit(0xfe0e, highStates); }
            for (int i = 0; i < members.Count; i++) {
                int fieldStates = i < 16 ? states : highStates;
                Field field = get_field(members[i], c);
                int value = b.local(b.type(field.type, c));
                var assignment = assignments.LastOrDefault(item => same(item.Name, field.name, c));
                if (assignment.Value != 0) {
                    int assignedType = il_scalar_constructor_expression(assignment.Value, parameters, c, b);
                    b.convert(assignedType, field.type, c);
                }
                else il_as(initializers.Last(item => same(item.Name, field.name, c)).Value, field.type, c, b);
                b.emit(0xfe0e, value);
                b.emit(0xfe0c, instance); b.emit(0xfe0c, value);
                if (get_type(field.type, c).kind == TypeKind.Number) b.call(RuntimeMember.NumberValue);
                else if (get_type(field.type, c).kind == TypeKind.Boolean) b.call(RuntimeMember.BooleanValue);
                else b.box(field.type, c);
                b.emit(0x7d, b.class_field_tokens[type][i]);
                if (assignment.Value == 0) {
                    int initializer = initializers.Last(item => same(item.Name, field.name, c)).Value;
                    int state = get_node(initializer, c).kind == NodeKind.Undefined ? 1 : 3;
                    b.emit(0xfe0c, fieldStates); b.emit(0x20, state << ((i % 16) * 2)); b.op(0x60); b.emit(0xfe0e, fieldStates);
                    continue;
                }
                int missing = b.label(), ready = b.label();
                b.emit(0xfe0c, fieldStates); b.emit(0xfe0c, value); b.missing(field.type, c); b.emit(0x3a, missing);
                b.emit(0x20, 3 << ((i % 16) * 2)); b.emit(0x38, ready);
                b.mark(missing); b.emit(0x20, 1 << ((i % 16) * 2)); b.mark(ready); b.op(0x60); b.emit(0xfe0e, fieldStates);
            }
            b.emit(0xfe0c, instance); b.emit(0xfe09, 0); b.emit(0xfe0c, states);
            if (highStates >= 0) { b.emit(0xfe0c, highStates); b.call(RuntimeMember.InitializeDirectClassWide); }
            else b.call(RuntimeMember.InitializeDirectClass);
            b.emit(0xfe0c, instance); b.op(0x2a);
        }
    }
}
