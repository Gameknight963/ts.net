namespace Tsnet.Compiler;

internal sealed class LazyObjectMethod(int owner, int member, int symbol)
{
    internal readonly int Owner = owner, Member = member, Symbol = symbol;
    internal int Marker, Loader;
}

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, LazyObjectMethod> lazy_object_methods = [];
    internal readonly Dictionary<int, LazyObjectMethod> lazy_method_values = [];
}

internal static partial class CompilerFunctions
{
    static void il_prepare_lazy_object_methods(Compiler c, IlBackend b)
    {
        var locals = new Dictionary<int, HashSet<int>>();
        var references = new Dictionary<int, HashSet<int>>();
        var nested = new HashSet<int>();
        var literals = new Dictionary<int, int>();
        var ambiguous = new HashSet<int>();
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node node = get_node(i, c);
            int owner = scalar_owner(i, b.node_parents, c);
            if (node.kind == NodeKind.Object && !literals.TryAdd(node.type, i)) ambiguous.Add(node.type);
            if (owner == 0) continue;
            if (node.kind == NodeKind.FunctionValue) nested.Add(owner);
            var map = node.kind == NodeKind.Variable ? locals : node.kind == NodeKind.Name ? references : null;
            if (map == null || node.symbol == 0) continue;
            if (!map.TryGetValue(owner, out var symbols)) map[owner] = symbols = [];
            symbols.Add(node.symbol);
        }
        foreach (var literal in literals) {
            Node value = get_node(literal.Value, c);
            if (ambiguous.Contains(literal.Key) || il_object_needs_entries(literal.Value, c) ||
                get_type(value.type, c).kind != TypeKind.Shape || get_type(value.type, c).name != 0 ||
                il_has_dynamic_indices(value.type, c)) continue;
            for (int item = value.children; item != 0; item = get_node(item, c).next) {
                Node property = get_node(item, c), functionValue = get_node(property.left, c);
                int member = field(value.type, property.name, c);
                if (member == 0 || functionValue.kind != NodeKind.FunctionValue || functionValue.symbol == 0 ||
                    il_class_receiver_arguments(functionValue.symbol, c, b) < 0 || b.object_home_functions.Contains(functionValue.symbol)) continue;
                int function = get_symbol(functionValue.symbol, c).function;
                Node definition = get_node(function, c);
                if (nested.Contains(function) || get_node(definition.fourth, c).constant ||
                    references.TryGetValue(function, out var names) &&
                    (!locals.TryGetValue(function, out var bindings) || names.Any(name => !bindings.Contains(name)))) continue;
                var method = new LazyObjectMethod(value.type, member, functionValue.symbol);
                b.lazy_object_methods[member] = method;
                b.lazy_method_values[property.left] = method;
            }
        }
    }

    static void il_declare_lazy_object_method(int member, int klass, IlBackend b)
    {
        if (!b.lazy_object_methods.TryGetValue(member, out var method)) return;
        method.Marker = b.plan.field(klass, 0x33, -5, "unread_method_" + member);
        method.Loader = b.plan.method1(5, klass, 0x16, -5, klass, "Read_method_" + member);
        // A private marker is never returned by a property read. Each instance
        // gets its own function when its method value is first observed.
        // The initializer is emitted once per class below.
    }

    static void il_lazy_object_method_bodies(Compiler c, IlBackend b)
    {
        foreach (var group in b.lazy_object_methods.Values.Where(method => method.Loader != 0).GroupBy(method => method.Owner)) {
            int klass = b.classes.at(group.Key);
            b.begin(b.plan.method0(5, klass, 0x1891, -1, ".cctor"));
            int constructor = b.plan.method0(6, b.RuntimeToken(RuntimeMember.ObjectType), 1, -1, ".ctor");
            foreach (var method in group) { b.emit(0x73, constructor); b.emit(0x80, method.Marker); }
            b.op(0x2a);
            foreach (var method in group) {
                b.begin(method.Loader);
                int ready = b.label(), value = b.local(-5);
                b.emit(0xfe09, 0); b.emit(0x7b, b.field_token(method.Member)); b.emit(0xfe0e, value);
                b.emit(0xfe0c, value); b.emit(0x7e, method.Marker); b.op(0xfe01); b.emit(0x39, ready);
                il_create_function_object(method.Symbol, c, b); b.emit(0xfe0e, value);
                b.emit(0xfe09, 0); b.emit(0xfe0c, value); b.emit(0x7d, b.field_token(method.Member));
                b.mark(ready); b.emit(0xfe0c, value); b.op(0x2a);
            }
        }
    }

    static bool il_lazy_object_method_call(int entry, Compiler c, IlBackend b)
    {
        if (b.lazy_object_methods.Count == 0) return false;
        Node call = get_node(entry, c), property = get_node(call.left, c);
        if (property.kind != NodeKind.Property || property.fourth != 0 || property.left == 0) return false;
        int owner = get_node(property.left, c).type;
        if (get_type(owner, c).kind != TypeKind.Shape || get_type(owner, c).name != 0) return false;
        LazyObjectMethod selected = null;
        bool ambiguous = false;
        foreach (var method in b.lazy_object_methods.Values) {
            if (method.Loader == 0 || !same(get_field(method.Member, c).name, property.name, c) || !fits(method.Owner, owner, c)) continue;
            if (method.Owner == owner) { selected = method; ambiguous = false; break; }
            if (selected != null) ambiguous = true;
            else selected = method;
        }
        if (selected == null || ambiguous) return false;
        Node function = get_node(get_symbol(selected.Symbol, c).function, c);
        var arguments = new List<(int Entry, int Parameter)>();
        int argument = call.children, parameter = get_node(function.fourth, c).next;
        while (argument != 0 || parameter != 0) {
            if (argument == 0 || parameter == 0 || get_node(argument, c).kind == NodeKind.Spread) return false;
            arguments.Add((argument, parameter));
            argument = get_node(argument, c).next; parameter = get_node(parameter, c).next;
        }
        int receiver = b.local(-5), typed = b.local(b.classes.at(selected.Owner));
        int fallback = b.label(), done = b.label();
        il_boxed(property.left, c, b); b.emit(0xfe0e, receiver);
        b.emit(0xfe0c, receiver); b.emit(0x75, b.classes.at(selected.Owner)); b.emit(0xfe0e, typed);
        b.emit(0xfe0c, receiver);
        b.emit(0x28, b.plan.specialize(b.RuntimeToken(RuntimeMember.IsPlainDataRecord), b.classes.at(selected.Owner)));
        b.emit(0x39, fallback);
        b.emit(0xfe0c, typed); b.emit(0x7b, b.field_token(selected.Member));
        b.emit(0x7e, selected.Marker); b.op(0xfe01); b.emit(0x39, fallback);
        // Capture the default implementation before evaluating arguments. They
        // may read, replace, or delete the property without changing this call.
        b.emit(0xfe0c, receiver); b.from_object(get_node(function.fourth, c).type, c);
        foreach (var pair in arguments) il_as(pair.Entry, get_node(pair.Parameter, c).type, c, b);
        b.emit(0x28, b.function(selected.Symbol)); b.convert(function.type, call.type, c); b.emit(0x38, done);
        b.mark(fallback);
        b.emit(0xfe0c, receiver);
        int originalMember = field(owner, property.name, c);
        if (originalMember != 0 && b.shape_readers.ContainsKey(originalMember)) il_shape_read(owner, originalMember, b);
        else { il_text(property.name, c, b); b.call(RuntimeMember.DynamicGet); }
        b.emit(0xfe0c, receiver);
        bool numeric = get_type(call.type, c).kind == TypeKind.Number && arguments.All(pair =>
            get_type(get_node(pair.Entry, c).type, c).kind == TypeKind.Number || get_node(pair.Entry, c).kind == NodeKind.Undefined);
        if (numeric) {
            foreach (var pair in arguments) il_as(pair.Entry, call.type, c, b);
            b.call(arguments.Count == 0 ? RuntimeMember.CallMethodNumber0 : arguments.Count == 1 ? RuntimeMember.CallMethodNumber1 : RuntimeMember.CallMethodNumber2);
        } else {
            if (arguments.Count == 0) b.call(RuntimeMember.CallMethod0);
            else if (arguments.Count == 1) {
                Node value = get_node(arguments[0].Entry, c);
                if (get_type(value.type, c).kind == TypeKind.Number) { il_expression(arguments[0].Entry, c, b); b.call(RuntimeMember.CallMethodNumberArgument); }
                else { il_boxed(arguments[0].Entry, c, b); b.call(RuntimeMember.CallMethodArgument); }
            } else {
                b.emit(0x20, arguments.Count); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType));
                for (int i = 0; i < arguments.Count; i++) {
                    b.op(0x25); b.emit(0x20, i); il_boxed(arguments[i].Entry, c, b); b.op(0xa2);
                }
                b.call(RuntimeMember.CallMethod);
            }
            il_function_result(call.type, c, b);
        }
        b.mark(done); return true;
    }
}
