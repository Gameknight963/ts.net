namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    Dictionary<int, List<int>> function_nodes;
    int function_node_count;

    internal List<int> FunctionNodes(int owner, Compiler c)
    {
        if (function_nodes == null || function_node_count != c.node_store.count())
        {
            function_nodes = [];
            function_node_count = c.node_store.count();
            for (int i = 1; i <= function_node_count; i++)
            {
                int function = scalar_owner(i, node_parents, c);
                if (!function_nodes.TryGetValue(function, out var nodes)) function_nodes[function] = nodes = [];
                nodes.Add(i);
            }
        }
        // Preserve node order: forward reads affect hoisted-variable storage.
        return function_nodes.TryGetValue(owner, out var result) ? result : [];
    }
}
