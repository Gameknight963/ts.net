namespace Tsnet.Compiler;

internal sealed partial class IlBackend
{
    internal Dictionary<(int Type, int Name), int> class_method_slots;
}

internal static partial class CompilerFunctions
{
    internal static int il_class_method_slot(int type, int name, Compiler c, IlBackend b)
    {
        if (b.class_method_slots == null)
        {
            b.class_method_slots = new();
            var ambiguous = new HashSet<int>();
            var types = new HashSet<int>();
            for (int i = 1; i <= c.node_store.count(); i++)
            {
                Node node = get_node(i, c);
                if (node.kind != NodeKind.ClassValue || node.fourth == 0) continue;
                int owner = get_node(node.fourth, c).type;
                if (!types.Add(owner)) ambiguous.Add(owner);
                // The prototype's constructor occupies its first data slot.
                var names = new List<int>();
                for (int member = node.children; member != 0; member = get_node(member, c).next)
                {
                    Node method = get_node(member, c);
                    // Computed names and static blocks can change the layout.
                    if (method.kind is NodeKind.Expression or NodeKind.Variable || method.right != 0) break;
                    if (method.fourth != 0) continue;
                    if (method.constant) continue;
                    if (method.name == 0) break;
                    int position = names.FindIndex(previous => same(previous, method.name, c));
                    if (position < 0) { position = names.Count; names.Add(method.name); }
                    int slot = position + 1;
                    if (method.left != 0 && method.third == 0 && slot < 4)
                        b.class_method_slots[(owner, method.name)] = slot;
                }
            }
            foreach (var key in b.class_method_slots.Keys.Where(key => ambiguous.Contains(key.Type)).ToArray())
                b.class_method_slots.Remove(key);
        }
        return b.class_method_slots.TryGetValue((type, name), out int found) ? found : -1;
    }
}
