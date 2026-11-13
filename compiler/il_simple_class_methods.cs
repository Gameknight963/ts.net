namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal readonly record struct SimpleClassMethod(int Name, int Function, int Parameter, int Receiver, int Expression);
internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, int> function_class_types = [];
    internal readonly Dictionary<int, List<SimpleClassMethod>> simple_class_methods = [];
    internal readonly Dictionary<(int Type, int Name), int> simple_method_callers = [];
}
internal static partial class CompilerFunctions
{
    // Only small numeric methods with one ordinary number parameter qualify.
    internal static void il_prepare_simple_class_methods(Compiler c, IlBackend b)
    {
        var sources = new Dictionary<int, int>(); var ambiguous = new HashSet<int>();
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node node = get_node(i, c);
            if (node.kind != NodeKind.ClassValue || node.fourth == 0) continue;
            int type = get_node(node.fourth, c).type;
            if (!sources.TryAdd(type, i)) ambiguous.Add(type);
        }
        // A known replacement would only pay for a failing function-type guard.
        // Dynamic replacements still use the runtime fallback.
        var writes = new List<(int ClassName, int Name)>();
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node node = get_node(i, c);
            if (node.kind != NodeKind.Assign || node.left == 0) continue;
            Node target = get_node(node.left, c);
            if (target.kind != NodeKind.Property || target.left == 0) continue;
            Type owner = get_type(get_node(target.left, c).type, c);
            if (owner.kind == TypeKind.Shape && owner.name != 0) writes.Add((owner.name, target.name));
        }
        foreach (var pair in sources) {
            Node klass = get_node(pair.Value, c);
            if (ambiguous.Contains(pair.Key) || klass.third != 0) continue;
            var data = new List<int>(); int fields = scalar_field_function(klass.left, c);
            if (fields != 0 && get_node(fields, c).left != 0) {
                Node body = get_node(get_node(fields, c).left, c);
                for (int item = body.children; item != 0; item = get_node(item, c).next) {
                    Node field = get_node(item, c);
                    if (field.kind == NodeKind.ClassField && field.name != 0 && field.third == 0 && field.fourth == 0) data.Add(field.name);
                }
            }
            var methods = new List<SimpleClassMethod>();
            for (int item = klass.children; item != 0; item = get_node(item, c).next) {
                Node method = get_node(item, c);
                if (method.constant || method.name == 0 || method.right != 0 || method.third != 0 || method.fourth != 0) continue;
                if (writes.Any(write => write.ClassName == get_type(pair.Key, c).name && same(write.Name, method.name, c))) continue;
                int entry = scalar_field_function(method.left, c); if (entry == 0) continue;
                Node function = get_node(entry, c);
                if (get_type(function.type, c).kind != TypeKind.Number || function.left == 0) continue;
                int receiver = 0, parameter = function.children;
                if (function.fourth != 0) {
                    Node self = get_node(function.fourth, c);
                    TypeKind selfKind = get_type(self.type, c).kind;
                    if (selfKind is not (TypeKind.Shape or TypeKind.Any)) continue;
                    if (parameter != function.fourth || self.name != 0 || self.constant ||
                        (selfKind != TypeKind.Any && !fits(pair.Key, self.type, c))) continue;
                    receiver = self.symbol; parameter = self.next;
                }
                if (parameter == 0) continue;
                Node argument = get_node(parameter, c);
                if (argument.next != 0 || argument.symbol == 0 || argument.constant || get_type(argument.type, c).kind != TypeKind.Number ||
                    argument.left != 0 || argument.right != 0 || argument.third != 0 || argument.fourth != 0 || argument.children != 0) continue;
                Node body = get_node(function.left, c); int statement = body.kind == NodeKind.Block ? body.children : function.left;
                if (statement == 0) continue;
                Node returned = get_node(statement, c);
                if (returned.kind != NodeKind.Return || returned.next != 0 || returned.left == 0) continue;
                int remaining = 15;
                bool pure = il_simple_getter_expression(returned.left, receiver, data, ref remaining, c, argument.symbol);
                if (!pure) continue;
                int count = il_numeric_receiver_function(function.symbol, c);
                if (count < 0 && il_numeric_function(function.symbol, c) < 0) continue;
                if (!b.function_class_types.ContainsKey(function.symbol)) {
                    var name = numbered_name('F', get_symbol(function.symbol, c).id);
                    int parent = b.RuntimeToken(count >= 0 ? RuntimeMember.ReceiverFunctionType : RuntimeMember.FunctionType);
                    b.function_class_types[function.symbol] = b.plan.type(name.view(), parent, 0x101);
                }
                methods.Add(new(method.name, function.symbol, argument.symbol, receiver, returned.left));
            }
            if (methods.Count != 0) b.simple_class_methods[pair.Key] = methods;
        }
    }

    internal static void il_simple_class_method_callers(int type, Compiler c, IlBackend b)
    {
        if (!b.simple_class_methods.TryGetValue(type, out var methods)) return;
        foreach (var method in methods) {
            Vector<int> args = new(); args.push(-5); args.push(-5); args.push(-10);
            int caller = b.plan.method(5, b.classes.at(type), 0x16, -10, args.view(), field_name(method.Name, "Call_number_", c).view());
            b.simple_method_callers[(type, method.Name)] = caller; b.begin(caller);
            int fallback = b.label(), receiver = -1;
            b.emit(0xfe09, 0); b.emit(0x75, b.function_class_types[method.Function]); b.emit(0x39, fallback);
            if (method.Receiver != 0) {
                receiver = b.local(b.classes.at(type));
                b.emit(0xfe09, 1); b.emit(0x75, b.classes.at(type)); b.emit(0xfe0e, receiver);
                b.emit(0xfe0c, receiver); b.emit(0x39, fallback);
            }
            Node value = get_node(method.Expression, c);
            if (value.kind == NodeKind.Name && value.symbol == method.Parameter) b.emit(0xfe09, 2);
            else if (value.kind == NodeKind.Property) {
                b.emit(0xfe0c, receiver); il_text(value.name, c, b); b.emit(0xfe0c, receiver);
                b.call(RuntimeMember.GetStoredNumberWithReceiver);
            }
            else { il_simple_getter_number(method.Expression, c, b, receiver, method.Parameter); b.call(RuntimeMember.WrapNumber); }
            b.op(0x2a);
            b.mark(fallback); b.emit(0xfe09, 0); b.emit(0xfe09, 1); b.emit(0xfe09, 2);
            b.call(RuntimeMember.CallMethodNumber1); b.op(0x2a);
        }
    }

    internal static int il_simple_class_method_call(int entry, Compiler c, IlBackend b)
    {
        Node property = get_node(entry, c);
        if (property.kind != NodeKind.Property || property.fourth != 0 || property.left == 0) return 0;
        int owner = get_node(property.left, c).type;
        if (b.simple_method_callers.TryGetValue((owner, property.name), out int direct)) return direct;
        if (get_type(owner, c).kind != TypeKind.Shape || get_type(owner, c).name != 0) return 0;
        // Structural callers may hold other implementations. The captured
        // function type and receiver checks decide whether this body applies.
        foreach (var pair in b.simple_method_callers)
            if (same(pair.Key.Name, property.name, c)) return pair.Value;
        return 0;
    }
}
