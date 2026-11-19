namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    static readonly (TextBytes Operator, TextBytes Assignment, RuntimeMember Method, TextBytes Name)[] bigint_operators =
    {
        ("+", "+=", RuntimeMember.BigIntAdd, "op_Addition"),
        ("-", "-=", RuntimeMember.BigIntSubtract, "op_Subtraction"),
        ("*", "*=", RuntimeMember.BigIntMultiply, "op_Multiply"),
        ("/", "/=", RuntimeMember.BigIntDivide, "BigIntDivide"),
        ("%", "%=", RuntimeMember.BigIntRemainder, "BigIntRemainder"),
        ("&", "&=", RuntimeMember.BigIntAnd, "op_BitwiseAnd"),
        ("|", "|=", RuntimeMember.BigIntOr, "op_BitwiseOr"),
        ("^", "^=", RuntimeMember.BigIntXor, "op_ExclusiveOr")
    };
    static RuntimeMember? il_bigint_operator(int text, bool assignment, Compiler c)
    {
        foreach (var operation in bigint_operators)
            if (is_text(text, assignment ? operation.Assignment : operation.Operator, c)) return operation.Method;
        return null;
    }
    internal static void il_bigint_runtime(IlBackend b)
    {
        int integer = b.plan.reference("System.Runtime.Numerics", "System.Numerics", "BigInteger", valueType: true);
        b.ref_set(RuntimeMember.BigIntegerType, integer);
        foreach (var operation in bigint_operators)
        {
            int owner = operation.Method is RuntimeMember.BigIntDivide or RuntimeMember.BigIntRemainder
                ? b.RuntimeToken(RuntimeMember.JsType) : integer;
            b.ref_set(operation.Method, b.plan.method2(6, owner, 0, integer, integer, integer, operation.Name));
        }
        b.ref_set(RuntimeMember.BigIntNegate, b.plan.method1(6, integer, 0, integer, integer, "op_UnaryNegation"));
        b.ref_set(RuntimeMember.BigIntNot, b.plan.method1(6, integer, 0, integer, integer, "op_OnesComplement"));
    }
    static bool il_known_bigint(int entry, Compiler c, Func<int, bool> name)
    {
        Stack<int> pending = null;
        while (true)
        {
            if (entry == 0) return false;
            Node n = get_node(entry, c);
            if (il_bigint_literal_text(entry, c) != 0 || n.kind == NodeKind.Name && name(n.symbol))
            {
                if (pending == null || pending.Count == 0) return true;
                entry = pending.Pop();
                continue;
            }
            if (n.kind is not (NodeKind.Binary or NodeKind.Unary)) return false;
            int op = get_token(n.token, c).text;
            if (n.kind == NodeKind.Binary && il_bigint_operator(op, false, c) != null)
                (pending ??= new()).Push(n.right);
            else if (n.kind != NodeKind.Unary || !(is_text(op, "-", c) || is_text(op, "~", c)))
                return false;
            // Match the recursive walk's left-first short circuit and name
            // callback order without consuming stack for long expressions.
            entry = n.left;
        }
    }
    internal static void il_prove_bigint_locals(Compiler c, IlBackend b)
    {
        Dictionary<int, int> declarations = [], owners = [], counts = [];
        HashSet<int> candidates = [], invalid = [];
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if (n.symbol == 0 || n.kind is not (NodeKind.Variable or NodeKind.Binding)) continue;
            counts[n.symbol] = counts.GetValueOrDefault(n.symbol) + 1;
            if (n.kind != NodeKind.Variable || il_bigint_literal_text(n.left, c) == 0 ||
                get_type(n.type, c).kind is not (TypeKind.Any or TypeKind.Unknown) || b.iteration_vars.at(n.symbol) != 0) continue;
            // The initializer must dominate every use. Conditional declarations,
            // parameters, captures, hoisted forward reads, and suspended locals
            // retain ordinary storage.
            int parent = b.node_parents.at(i);
            if (parent == 0 || get_node(parent, c).kind != NodeKind.Block) continue;
            while (parent != 0 && get_node(parent, c).kind == NodeKind.Block) parent = b.node_parents.at(parent);
            if (parent == 0 || get_node(parent, c).kind != NodeKind.Function || get_node(parent, c).right != 0) continue;
            declarations[n.symbol] = i; owners[n.symbol] = parent; candidates.Add(n.symbol);
        }
        if (candidates.Count == 0) return;
        Dictionary<int, List<int>> dependants = [];
        foreach (int symbol in candidates) if (counts[symbol] != 1) invalid.Add(symbol);
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if (n.kind == NodeKind.Name && candidates.Contains(n.symbol) &&
                (i < declarations[n.symbol] || scalar_owner(i, b.node_parents, c) != owners[n.symbol])) invalid.Add(n.symbol);
            if (n.kind == NodeKind.Binding)
            {
                if (candidates.Contains(n.symbol)) invalid.Add(n.symbol);
                if (n.name != 0 && is_text(n.name, "$assignment", c) && n.fourth != 0)
                {
                    Node target = get_node(get_node(n.fourth, c).left, c);
                    if (target.kind == NodeKind.Name && candidates.Contains(target.symbol)) invalid.Add(target.symbol);
                }
            }
            if (n.kind is not (NodeKind.Assign or NodeKind.Update)) continue;
            Node left = get_node(n.left, c);
            if (left.kind != NodeKind.Name || !candidates.Contains(left.symbol)) continue;
            int op = get_token(n.token, c).text;
            if (n.kind == NodeKind.Update || !is_text(op, "=", c) && il_bigint_operator(op, true, c) == null ||
                !il_known_bigint(n.right, c, source => {
                    if (!candidates.Contains(source)) return false;
                    if (!dependants.TryGetValue(source, out var users)) dependants[source] = users = [];
                    users.Add(left.symbol); return true;
                })) invalid.Add(left.symbol);
        }
        Queue<int> pending = new(invalid);
        while (pending.TryDequeue(out int symbol))
        {
            if (!candidates.Remove(symbol)) continue;
            if (dependants.TryGetValue(symbol, out var users)) foreach (int user in users) pending.Enqueue(user);
        }
        foreach (int symbol in candidates) b.raw_slots.at_mut(symbol) = b.RuntimeToken(RuntimeMember.BigIntegerType);
    }
    static bool il_known_bigint(int entry, Compiler c, IlBackend b) => il_known_bigint(entry, c,
        symbol => b.raw_slot(symbol) == b.RuntimeToken(RuntimeMember.BigIntegerType));
    internal static void il_bigint_value(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (il_bigint_literal(entry, c, b)) { b.emit(0xa5, b.RuntimeToken(RuntimeMember.BigIntegerType)); return; }
        if (n.kind == NodeKind.Name) { b.load_storage(n.symbol); return; }
        int op = get_token(n.token, c).text;
        il_bigint_value(n.left, c, b);
        if (n.kind == NodeKind.Unary) b.call(is_text(op, "-", c) ? RuntimeMember.BigIntNegate : RuntimeMember.BigIntNot);
        else { il_bigint_value(n.right, c, b); b.call(il_bigint_operator(op, false, c).Value); }
    }
    internal static bool il_bigint_expression(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c);
        if (n.kind is not (NodeKind.Binary or NodeKind.Unary) || !il_known_bigint(entry, c, b)) return false;
        il_bigint_value(entry, c, b); b.emit(0x8c, b.RuntimeToken(RuntimeMember.BigIntegerType)); return true;
    }
    internal static bool il_bigint_mutation(int entry, bool discardBox, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c), target = get_node(n.left, c);
        if (n.kind != NodeKind.Assign || target.kind != NodeKind.Name ||
            b.raw_slot(target.symbol) != b.RuntimeToken(RuntimeMember.BigIntegerType)) return false;
        int op = get_token(n.token, c).text;
        if (!is_text(op, "=", c)) b.load_storage(target.symbol);
        il_bigint_value(n.right, c, b);
        if (!is_text(op, "=", c)) b.call(il_bigint_operator(op, true, c).Value);
        b.op(0x25); b.store_storage(target.symbol);
        if (!discardBox) b.emit(0x8c, b.RuntimeToken(RuntimeMember.BigIntegerType));
        return true;
    }
}
