namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal sealed class ScalarArrayValues(int declaration, int type, List<int> elements)
{
    internal readonly int Declaration = declaration, Type = type;
    internal readonly List<int> Elements = elements;
    internal int[] Slots = [];
    internal int Method;
}
internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, ScalarArrayValues> scalar_arrays = [];
    internal readonly Dictionary<int, (int Symbol, int Index)> scalar_array_reads = [];
}
internal static partial class CompilerFunctions
{
    internal static void il_prove_scalar_arrays(Compiler c, IlBackend b)
    {
        // Only complete numeric literals used by fixed-index arithmetic qualify.
        // Later statements in the same block preserve initialization ordering.
        var declarations = new Dictionary<int, int>();
        var statements = new Dictionary<int, (int Block, int Position)>();
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node node = get_node(i, c);
            if (node.symbol != 0 && node.kind is NodeKind.Variable or NodeKind.Binding)
                declarations[node.symbol] = declarations.GetValueOrDefault(node.symbol) + 1;
            if (node.kind == NodeKind.Block) {
                int position = 0;
                for (int child = node.children; child != 0; child = get_node(child, c).next)
                    statements[child] = (i, position++);
            }
        }
        var candidates = new Dictionary<int, ScalarArrayValues>();
        var blocks = new Dictionary<int, (int Block, int Position)>();
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node node = get_node(i, c);
            if (node.kind != NodeKind.Variable || node.symbol == 0 || node.left == 0 || node.type == 0 || declarations.GetValueOrDefault(node.symbol) != 1 ||
                b.capture_fields.at(node.symbol) != 0 || b.global_fields.at(node.symbol) != 0 ||
                get_type(node.type, c).kind != TypeKind.Array || get_type(get_type(node.type, c).element, c).kind != TypeKind.Number) continue;
            int owner = scalar_owner(i, b.node_parents, c);
            if (owner == 0 || get_node(owner, c).right != 0 || get_node(owner, c).internal_continuation) continue;
            int block = b.node_parents.at(i);
            if (block == 0 || get_node(block, c).kind != NodeKind.Block) continue;
            Node literal = get_node(node.left, c);
            if (literal.kind != NodeKind.Array) continue;
            var elements = new List<int>(); bool valid = true;
            for (int child = literal.children; child != 0; child = get_node(child, c).next) {
                Node value = get_node(child, c);
                if (value.kind == NodeKind.Spread || value.kind == NodeKind.Undefined && value.constant ||
                    value.type == 0 || get_type(value.type, c).kind != TypeKind.Number) { valid = false; break; }
                elements.Add(child);
                if (elements.Count > 8) { valid = false; break; }
            }
            if (!valid || elements.Count == 0) continue;
            if (!statements.TryGetValue(i, out var location) || location.Block != block) continue;
            candidates[node.symbol] = new(i, get_type(node.type, c).element, elements);
            blocks[node.symbol] = location;
        }
        var invalid = new HashSet<int>();
        var reads = new Dictionary<int, List<(int Entry, int Index)>>();
        for (int i = 1; i <= c.node_store.count(); i++) {
            Node name = get_node(i, c);
            if (name.kind != NodeKind.Name || !candidates.TryGetValue(name.symbol, out var array)) continue;
            int parent = b.node_parents.at(i);
            if (parent == 0 || scalar_owner(i, b.node_parents, c) != scalar_owner(array.Declaration, b.node_parents, c)) { invalid.Add(name.symbol); continue; }
            Node indexed = get_node(parent, c);
            if (indexed.kind != NodeKind.Index || indexed.left != i || indexed.right == 0 || indexed.type == 0 || get_type(indexed.type, c).kind != TypeKind.Number) { invalid.Add(name.symbol); continue; }
            Node key = get_node(indexed.right, c);
            int use = b.node_parents.at(parent);
            if (key.kind != NodeKind.Number || key.left != 0 || key.right != 0 || key.third != 0 || key.fourth != 0 || key.children != 0 ||
                use == 0 || get_node(use, c).kind != NodeKind.Binary) { invalid.Add(name.symbol); continue; }
            double index = number(get_token(key.token, c).text, c);
            if (!double.IsFinite(index) || index < 0 || index >= array.Elements.Count || index != Math.Truncate(index)) { invalid.Add(name.symbol); continue; }
            var scope = blocks[name.symbol]; int current = i;
            while (current != 0 && b.node_parents.at(current) != scope.Block) current = b.node_parents.at(current);
            if (current == 0 || !statements.TryGetValue(current, out var location) ||
                location.Block != scope.Block || location.Position <= scope.Position) { invalid.Add(name.symbol); continue; }
            if (!reads.TryGetValue(name.symbol, out var items)) reads[name.symbol] = items = [];
            items.Add((parent, (int)index));
        }
        foreach (var pair in candidates) {
            if (invalid.Contains(pair.Key) || !reads.TryGetValue(pair.Key, out var items)) continue;
            b.scalar_arrays[pair.Key] = pair.Value;
            foreach (var read in items) b.scalar_array_reads[read.Entry] = (pair.Key, read.Index);
        }
    }
    internal static bool il_scalar_array_initialize(int entry, Compiler c, IlBackend b)
    {
        Node node = get_node(entry, c);
        if (!b.scalar_arrays.TryGetValue(node.symbol, out var array) || array.Declaration != entry) return false;
        array.Method = b.method; array.Slots = new int[array.Elements.Count];
        for (int i = 0; i < array.Elements.Count; i++) {
            array.Slots[i] = b.local(b.type(array.Type, c));
            il_as(array.Elements[i], array.Type, c, b); b.emit(0xfe0e, array.Slots[i]);
        }
        return true;
    }
    internal static bool il_scalar_array_read(int entry, Compiler c, IlBackend b)
    {
        if (!b.scalar_array_reads.TryGetValue(entry, out var read)) return false;
        ScalarArrayValues array = b.scalar_arrays[read.Symbol];
        if (array.Method != b.method || array.Slots.Length != array.Elements.Count) throw new CompilationException("Missing local array storage");
        b.emit(0xfe0c, array.Slots[read.Index]); b.convert(array.Type, get_node(entry, c).type, c); return true;
    }
}
