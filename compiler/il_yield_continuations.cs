namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    internal static void il_prepare_private_continuations(int program, Compiler c, IlBackend b)
    {
        void Cache(int entry)
        {
            Node value = get_node(entry, c);
            if (value.kind != NodeKind.FunctionValue || value.symbol == 0) return;
            Node function = get_node(get_symbol(value.symbol, c).function, c);
            if (!function.internal_continuation || b.yield_continuation_caches.at(value.symbol) != 0) return;
            // These functions are created by the frontend and passed only to the
            // async or generator runtime. Their identity is never exposed to user code.
            // Cache on the captured frame so separate activations and loop scopes
            // retain their own bindings. Frames cloned for a new scope start empty.
            int context = b.function_contexts.at(value.symbol);
            int owner = context == 0 ? program : b.context_types.at(context);
            Vector<byte> name = numbered_name('y', value.symbol);
            b.yield_continuation_caches.at_mut(value.symbol) = b.plan.field(owner,
                context == 0 ? 0x13 : 6, b.RuntimeToken(RuntimeMember.FunctionType), name.view());
        }
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node call = get_node(i, c);
            if (call.kind == NodeKind.AsyncTry)
            {
                Cache(call.left); Cache(call.right); Cache(call.third); Cache(call.fourth);
            }
            else if (call.kind == NodeKind.AsyncIteratorTry)
            {
                Cache(call.left); Cache(call.right); Cache(call.third);
            }
            else if (call.kind == NodeKind.Call && call.children != 0)
            {
                Node target = get_node(call.left, c);
                if (target.kind != NodeKind.BuiltinNamespace || !is_text(get_token(target.token, c).text, "__generatorYield", c)) continue;
                int argument = get_node(call.children, c).next;
                if (argument != 0 && get_node(argument, c).next == 0) Cache(argument);
            }
        }
    }
}
