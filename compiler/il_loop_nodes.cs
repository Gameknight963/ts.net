namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    Dictionary<int, List<int>> loop_nodes;
    int loop_node_count;

    internal IEnumerable<int> LoopNodes(int scope, Compiler c)
    {
        if (loop_nodes == null || loop_node_count != c.node_store.count())
        {
            loop_nodes = [];
            loop_node_count = c.node_store.count();
            HashSet<int> scopes = [];
            for (int i = 1; i <= loop_node_count; i++)
            {
                Node node = get_node(i, c);
                // Buffer analysis currently queries only counted for loops.
                if (node.kind == NodeKind.For && node.fourth != 0) scopes.Add(node.fourth);
            }
            // Follow the same lexical parents as il_inside, including nested
            // functions. Preserve ascending IDs and the original effect checks.
            for (int i = 1; i <= loop_node_count; i++)
                for (int parent = i; parent != 0; parent = node_parents.at(parent))
                    if (scopes.Contains(parent))
                    {
                        if (!loop_nodes.TryGetValue(parent, out var nodes)) loop_nodes[parent] = nodes = [];
                        nodes.Add(i);
                    }
        }
        return loop_nodes.TryGetValue(scope, out var result) ? result : Enumerable.Range(1, c.node_store.count());
    }
}
