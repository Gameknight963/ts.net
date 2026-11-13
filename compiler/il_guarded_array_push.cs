namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    internal int guarded_push_call;
}

internal static partial class CompilerFunctions
{
    internal static bool il_guarded_push_loop(int entry, int result, int counter, int bound, Compiler c, IlBackend b)
    {
        if (counter == 0 || b.guarded_push_call != 0) return false;
        Node loop = get_node(entry, c);
        Node initial = get_node(get_node(il_loop_binding(entry, c), c).left, c);
        if (number(get_token(initial.token, c).text, c) != 0) return false;
        int parent = b.node_parents.at(entry);
        if (parent == 0 || get_node(parent, c).kind != NodeKind.Block || loop.fourth == 0) return false;
        Node body = get_node(loop.fourth, c);
        if (body.kind != NodeKind.Block || body.children == 0) return false;
        // Reject ordinary loops before scanning declarations for the whole program.
        int last = body.children, count = 1;
        while (get_node(last, c).next != 0)
        {
            if (++count > 32) return false;
            last = get_node(last, c).next;
        }
        Node tail = get_node(last, c);
        if (tail.kind != NodeKind.Expression || tail.left == 0) return false;
        Node candidate = get_node(tail.left, c);
        if (candidate.kind != NodeKind.Call || candidate.left == 0 || candidate.children == 0) return false;
        Node method = get_node(candidate.left, c);
        if (method.kind != NodeKind.Property || method.left == 0 || !is_text(method.name, text_push, c)) return false;
        Node target = get_node(method.left, c);
        if (target.kind != NodeKind.Name || get_type(target.type, c).kind != TypeKind.Array ||
            get_type(get_type(target.type, c).element, c).kind != TypeKind.Shape) return false;
        var declarations = new Dictionary<int, int>();
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if ((n.kind is NodeKind.Variable or NodeKind.Binding) && n.symbol != 0)
                declarations[n.symbol] = declarations.GetValueOrDefault(n.symbol) + 1;
        }
        bool Stable(int symbol) => symbol != 0 && declarations.GetValueOrDefault(symbol) == 1 &&
            b.written_symbols.at(symbol) == 0 && b.global_fields.at(symbol) == 0 && b.capture_fields.at(symbol) == 0;
        // Numeric and Boolean CLR storage cannot hide an object with coercion hooks.
        bool Plain(int value)
        {
            Node n = get_node(value, c);
            if (n.kind != NodeKind.Object || n.type == 0 || n.constant || n.fourth != 0 || get_type(n.type, c).kind != TypeKind.Shape ||
                get_type(n.type, c).name != 0 || il_object_needs_entries(value, c)) return false;
            int count = 0, properties = 0;
            for (int member = get_type(n.type, c).fields; member != 0; member = get_field(member, c).next)
            {
                Field f = get_field(member, c);
                if (f.optional || get_type(f.type, c).kind is not (TypeKind.Number or TypeKind.Boolean) || ++count > 8) return false;
            }
            var names = new HashSet<int>();
            for (int item = n.children; item != 0; item = get_node(item, c).next)
            {
                Node p = get_node(item, c);
                if (p.kind != NodeKind.Property || p.name == 0 || field(n.type, p.name, c) == 0 || !names.Add(p.name)) return false;
                properties++;
            }
            return count != 0 && count == properties;
        }
        var arrays = new HashSet<int>();
        var records = new Dictionary<int, int>();
        var prefixes = new HashSet<int>();
        // Same-block declarations before this loop are already initialized.
        bool reached = false;
        for (int statement = get_node(parent, c).children; statement != 0; statement = get_node(statement, c).next)
        {
            if (statement == entry) {reached = true;break;}
            Node n = get_node(statement, c);
            if (n.kind != NodeKind.Variable || n.left == 0 || !Stable(n.symbol)) continue;
            Node value = get_node(n.left, c);
            if (value.kind == NodeKind.Array && value.children == 0 && get_type(n.type, c).kind == TypeKind.Array &&
                get_type(get_type(n.type, c).element, c).kind == TypeKind.Shape) arrays.Add(n.symbol);
            if (Plain(n.left) && b.scalar_roots.at(n.symbol) == 0) {records[n.symbol] = n.left;prefixes.Add(n.symbol);}
        }
        if (!reached || arrays.Count == 0) return false;
        var guards = new HashSet<int>();
        bool Record(int symbol)
        {
            if (!records.ContainsKey(symbol)) return false;
            if (prefixes.Contains(symbol)) guards.Add(symbol);
            return true;
        }
        bool Member(Node property)
        {
            if (property.kind != NodeKind.Property || property.left == 0 || property.right != 0 || property.fourth != 0) return false;
            Node owner = get_node(property.left, c);
            return owner.kind == NodeKind.Name && Record(owner.symbol) && field(get_node(records[owner.symbol], c).type, property.name, c) != 0;
        }
        bool Pure(int value)
        {
            if (value == 0) return false;
            Node n = get_node(value, c);
            if (il_key_may_invoke(n, c) || il_coercion_may_invoke(n, c)) return false;
            if (n.kind is NodeKind.Number or NodeKind.String or NodeKind.Boolean or NodeKind.Undefined) return true;
            if (n.kind == NodeKind.Name) return n.symbol != 0 && declarations.ContainsKey(n.symbol) &&
                b.global_fields.at(n.symbol) == 0 && b.capture_fields.at(n.symbol) == 0 && n.type != 0 &&
                (get_type(n.type, c).kind is TypeKind.Number or TypeKind.Boolean) &&
                (get_type(get_symbol(n.symbol, c).type, c).kind is TypeKind.Number or TypeKind.Boolean);
            if (n.kind == NodeKind.Property) return Member(n);
            if (n.kind == NodeKind.Binary) return Pure(n.left) && Pure(n.right);
            if (n.kind == NodeKind.Unary) return (is_text(get_token(n.token, c).text, text_plus, c) ||
                is_text(get_token(n.token, c).text, text_minus, c) || is_text(get_token(n.token, c).text, text_not, c)) && Pure(n.left);
            return false;
        }
        int push = 0, array = 0, statements = 0;
        for (int statement = body.children; statement != 0; statement = get_node(statement, c).next)
        {
            if (++statements > 32) return false;
            Node n = get_node(statement, c);
            if (n.kind == NodeKind.Variable && n.left != 0 && Stable(n.symbol))
            {
                Node value = get_node(n.left, c);
                if (Plain(n.left))
                {
                    for (int item = value.children; item != 0; item = get_node(item, c).next)
                        if (!Pure(get_node(item, c).left)) return false;
                    records[n.symbol] = n.left;
                    continue;
                }
                if (value.kind == NodeKind.Object && value.children != 0)
                {
                    Node spread = get_node(value.children, c);
                    if (spread.kind != NodeKind.Spread || spread.next != 0 || spread.left == 0) return false;
                    Node source = get_node(spread.left, c);
                    if (source.kind != NodeKind.Name || !Record(source.symbol) ||
                        !il_same_spread_fields(get_node(records[source.symbol], c).type, value.type, c)) return false;
                    records[n.symbol] = records[source.symbol];
                    continue;
                }
                if (!Pure(n.left)) return false;
                continue;
            }
            if (n.kind != NodeKind.Expression || n.left == 0) return false;
            Node expression = get_node(n.left, c);
            if (expression.kind == NodeKind.Assign && expression.left != 0 && expression.right != 0 &&
                is_text(get_token(expression.token, c).text, text_assign, c) && Member(get_node(expression.left, c)) && Pure(expression.right)) continue;
            if (expression.kind != NodeKind.Call || expression.left == 0 || expression.children == 0 || n.next != 0) return false;
            Node callee = get_node(expression.left, c), argument = get_node(expression.children, c);
            if (callee.kind != NodeKind.Property || callee.left == 0 || callee.fourth != 0 || !is_text(callee.name, text_push, c) ||
                argument.kind != NodeKind.Name || argument.next != 0 || !Record(argument.symbol)) return false;
            Node owner = get_node(callee.left, c);
            if (owner.kind != NodeKind.Name || !arrays.Contains(owner.symbol)) return false;
            push = n.left;array = owner.symbol;
        }
        if (push == 0 || guards.Count > 8) return false;
        int fallback = b.label(), done = b.label();
        // Empty loops must not inspect bindings earlier than their bodies would.
        b.emit(0xfe0c, bound); b.plan.number(b.method, 0); b.op(0xfe02); b.emit(0x39, fallback);
        foreach (int symbol in guards)
        {
            Node value = get_node(records[symbol], c);
            int klass = b.literal_classes.at(records[symbol]);
            if (klass == 0) klass = b.classes.at(value.type);
            b.load(symbol); b.emit(0x28, b.plan.specialize(b.RuntimeToken(RuntimeMember.IsPlainDataRecord), klass)); b.emit(0x39, fallback);
        }
        b.load(array); b.emit(0xfe0c, bound); b.call(RuntimeMember.GuardPrivateArrayPush); b.emit(0x39, fallback);
        // The accepted body cannot invoke user code or change the array's prototype or descriptors.
        b.guarded_push_call = push;
        il_loop_code(entry, result, counter, bound, c, b);
        b.guarded_push_call = 0;
        b.emit(0x38, done); b.mark(fallback);
        il_loop_code(entry, result, counter, bound, c, b); b.mark(done);
        return true;
    }
}
