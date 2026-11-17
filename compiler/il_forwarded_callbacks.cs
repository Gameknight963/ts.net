namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    // A forwarding helper does not inspect its callback. Once both bindings are
    // fixed, its erased call can use the callback's existing static entry point.
    internal static bool il_forwarded_callback(int entry, int symbol, Compiler c, IlBackend b)
    {
        Node call = get_node(entry, c), helper = get_node(get_symbol(symbol, c).function, c);
        if (helper.fourth != 0 || helper.right != 0 || b.function_contexts.at(symbol) != 0 ||
            b.written_symbols.at(symbol) != 0 || get_type(helper.type, c).kind != TypeKind.Number) return false;
        Node statement = get_node(helper.left, c);
        if (statement.kind == NodeKind.Block)
        {
            if (statement.children == 0) return false;
            statement = get_node(statement.children, c);
            if (statement.next != 0) return false;
        }
        if (statement.kind != NodeKind.Return || statement.left == 0) return false;
        Node forwarded = get_node(statement.left, c);
        if (forwarded.kind != NodeKind.Call || get_type(forwarded.type, c).kind != TypeKind.Number) return false;
        Node target = get_node(forwarded.left, c);
        if (target.kind != NodeKind.Name) return false;
        List<int> parameters = [], arguments = [];
        for (int parameter = helper.children, argument = call.children; parameter != 0 || argument != 0;)
        {
            if (parameter == 0 || argument == 0) return false;
            Node p = get_node(parameter, c), a = get_node(argument, c);
            if (!ordinary(p) || a.kind == NodeKind.Spread) return false;
            parameters.Add(parameter); arguments.Add(argument);
            parameter = p.next; argument = a.next;
        }
        int callbackIndex = parameters.FindIndex(p => get_node(p, c).symbol == target.symbol);
        if (callbackIndex < 0) return false;
        Node reference = get_node(arguments[callbackIndex], c);
        if (reference.symbol != 0 && b.written_symbols.at(reference.symbol) != 0) return false;
        int callback = il_direct_function(arguments[callbackIndex], c, b);
        if (callback == 0 || b.written_symbols.at(callback) != 0 || b.function_contexts.at(callback) != 0) return false;
        Node function = get_node(get_symbol(callback, c).function, c);
        if (function.fourth != 0 || function.right != 0 || get_type(function.type, c).kind != TypeKind.Number) return false;
        List<(int Argument, int Parameter)> forwardedParameters = [];
        for (int argument = forwarded.children, parameter = function.children; argument != 0 || parameter != 0;)
        {
            if (argument == 0 || parameter == 0 || forwardedParameters.Count == 2) return false;
            Node a = get_node(argument, c), p = get_node(parameter, c);
            if (a.kind != NodeKind.Name || !ordinary(p)) return false;
            int index = parameters.FindIndex(helperParameter => get_node(helperParameter, c).symbol == a.symbol);
            if (index < 0) return false;
            forwardedParameters.Add((index, parameter));
            argument = a.next; parameter = p.next;
        }
        // Evaluate and convert every outer argument before callback argument
        // checks, including unused arguments and the callback reference itself.
        List<int> slots = [];
        for (int i = 0; i < arguments.Count; i++)
        {
            int type = get_node(parameters[i], c).type;
            il_as(arguments[i], type, c, b);
            int slot = b.local(b.type(type, c)); b.emit(0xfe0e, slot); slots.Add(slot);
        }
        foreach (var parameter in forwardedParameters)
        {
            int source = get_node(parameters[parameter.Argument], c).type;
            int destination = get_node(parameter.Parameter, c).type;
            b.emit(0xfe0c, slots[parameter.Argument]);
            // Numeric callback entries already accept nullable numbers. Other
            // arguments retain the checks performed by the erased adapter.
            if (get_type(source, c).kind != TypeKind.Number || get_type(destination, c).kind != TypeKind.Number)
            { b.box(source, c); b.from_object(destination, c); }
        }
        b.emit(0x28, b.function(callback));
        b.convert(function.type, helper.type, c); b.convert(helper.type, call.type, c);
        return true;
        static bool ordinary(Node p) => p.symbol != 0 && p.left == 0 && p.right == 0 &&
            p.third == 0 && p.fourth == 0 && p.children == 0;
    }
}
