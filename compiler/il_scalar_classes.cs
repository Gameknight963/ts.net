namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, List<(int Name, int Value)>> scalar_class_initializers = [];
}

internal static partial class CompilerFunctions
{
    internal static int scalar_field_function(int value, Compiler c)
    {
        Node reference = get_node(value, c);
        if (reference.kind != NodeKind.FunctionValue || reference.symbol == 0 ||
            get_symbol(reference.symbol, c).function != reference.left) return 0;
        Node function = get_node(reference.left, c);
        return function.kind == NodeKind.Function && function.right == 0 && !function.internal_continuation ? reference.left : 0;
    }

    internal static List<(int Name, int Value)> scalar_class_fields(int entry, Vector<int> definitions,
        Vector<int> declarations, Vector<int> parameters, Compiler c, IlBackend b)
    {
        Node construction = get_node(entry, c);
        if (construction.kind != NodeKind.Construct || construction.children != 0 || get_type(construction.type, c).kind != TypeKind.Shape) return null;
        Node reference = get_node(construction.left, c);
        if (reference.kind != NodeKind.Name || reference.symbol == 0 || parameters.at(reference.symbol) != 0 ||
            declarations.at(reference.symbol) != 1 || b.written_symbols.at(reference.symbol) != 0) return null;
        Node definition = get_node((int)definitions.at(reference.symbol), c);
        Node call = get_node(definition.left, c);
        if (call.kind != NodeKind.Call || call.children != 0) return null;
        int factory = scalar_field_function(call.left, c);
        if (factory == 0 || get_node(factory, c).children != 0) return null;
        Node scope = get_node(get_node(factory, c).left, c);
        if (scope.kind != NodeKind.Block) return null;
        Node binding = get_node(scope.children, c);
        Node initialize = get_node(binding.next, c);
        Node result = get_node(initialize.next, c);
        if (binding.kind != NodeKind.Variable || binding.symbol == 0 || b.written_symbols.at(binding.symbol) != 0 ||
            initialize.kind != NodeKind.ClassStatic || result.kind != NodeKind.Return || result.next != 0 ||
            get_node(initialize.left, c).kind != NodeKind.Name || get_node(initialize.left, c).symbol != binding.symbol ||
            get_node(result.left, c).kind != NodeKind.Name || get_node(result.left, c).symbol != binding.symbol) return null;
        Node klass = get_node(binding.left, c);
        // Constructors, inheritance, methods, computed keys and decorators keep
        // the ordinary construction path. Static initializers must also be empty.
        if (klass.kind != NodeKind.ClassValue || klass.right != 0 || klass.third != 0 || klass.children != 0 ||
            get_node(klass.fourth, c).type != construction.type) return null;
        int statics = scalar_field_function(initialize.right, c);
        if (statics == 0 || get_node(get_node(statics, c).left, c).kind != NodeKind.Block ||
            get_node(get_node(statics, c).left, c).children != 0) return null;
        int fields = scalar_field_function(klass.left, c);
        if (fields == 0) return null;
        Node function = get_node(fields, c), body = get_node(get_node(fields, c).left, c);
        Node receiver = get_node(function.fourth, c);
        if (body.kind != NodeKind.Block || function.fourth == 0 || function.children != function.fourth || receiver.next != 0) return null;
        int count = 0;
        for (int member = get_type(construction.type, c).fields; member != 0; member = get_field(member, c).next)
        {
            Field field = get_field(member, c);
            if (field.optional || get_type(field.type, c).kind is not (TypeKind.Number or TypeKind.String or TypeKind.Boolean) || ++count > 8) return null;
        }
        if (count == 0) return null;
        var values = new List<(int Name, int Value)>();
        for (int statement = body.children; statement != 0; statement = get_node(statement, c).next)
        {
            Node field = get_node(statement, c);
            if (field.kind != NodeKind.ClassField || field.name == 0 || field.third != 0 || field.fourth != 0 ||
                get_node(field.left, c).kind != NodeKind.Name || get_node(field.left, c).symbol != receiver.symbol ||
                values.Any(previous => same(previous.Name, field.name, c))) return null;
            int member = CompilerFunctions.field(construction.type, field.name, c);
            if (member == 0) return null;
            Node value = get_node(field.right, c);
            if (value.left != 0 || value.right != 0 || value.third != 0 || value.fourth != 0 || value.children != 0) return null;
            TypeKind kind = get_type(get_field(member, c).type, c).kind;
            bool literal = kind == TypeKind.Number && value.kind == NodeKind.Number ||
                kind == TypeKind.String && value.kind == NodeKind.String || kind == TypeKind.Boolean && value.kind == NodeKind.Boolean;
            if (!literal && (value.kind != NodeKind.Undefined || value.constant)) return null;
            values.Add((field.name, field.right));
        }
        return values.Count == count ? values : null;
    }

    internal static bool scalar_initial_value(int entry, Vector<int> definitions, Vector<int> declarations,
        Vector<int> parameters, Compiler c, IlBackend b)
    {
        if (scalar_literal(entry, c)) return true;
        var fields = scalar_class_fields(entry, definitions, declarations, parameters, c, b);
        if (fields == null) return false;
        b.scalar_class_initializers[entry] = fields;
        return true;
    }
}
