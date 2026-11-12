namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    internal static int il_numeric_receiver_function(int symbol, Compiler c)
    {
        Node f = get_node(get_symbol(symbol, c).function, c);
        if (f.fourth == 0 || f.right != 0 || f.internal_continuation || get_type(f.type, c).kind != TypeKind.Number) return -1;
        Node receiver = get_node(f.fourth, c);
        if (receiver.name != 0 || receiver.constant) return -1;
        int count = 0;
        for (int parameter = get_node(f.children, c).next; parameter != 0; parameter = get_node(parameter, c).next)
        {
            Node p = get_node(parameter, c);
            if (p.third != 0 || get_type(p.type, c).kind != TypeKind.Number || ++count > 2) return -1;
        }
        return count;
    }

    internal static void il_numeric_receiver_methods(int symbol, int type, int context, int environment, int count, Compiler c, IlBackend b)
    {
        for (int bound = 0; bound < 2; bound++)
        {
            for (int arity = 0; arity <= 2; arity++)
            {
                Vector<int> parameters = new();
                if (bound != 0) parameters.push(-5);
                for (int i = 0; i < arity; i++) parameters.push(-10);
                string name = bound == 0 ? $"InvokeNumber{arity}" : $"InvokeMethodNumber{arity}";
                b.begin(b.plan.method(5, type, 0xc6, -10, parameters.view(), name));
                if (context != 0) { b.emit(0xfe09, 0); b.emit(0x7b, environment); }
                b.emit(0xfe09, 0);
                if (bound != 0) b.emit(0xfe09, 1); else b.call(RuntimeMember.Undefined);
                b.call(RuntimeMember.PrepareReceiver);
                b.from_object(get_node(get_node(get_symbol(symbol, c).function, c).children, c).type, c);
                for (int i = 0; i < count; i++)
                {
                    if (i < arity) b.emit(0xfe09, i + bound + 1);
                    else b.call(RuntimeMember.EmptyNumber);
                }
                b.emit(0x28, b.function(symbol)); b.op(0x2a);
            }
        }
    }
}
