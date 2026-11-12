namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    internal readonly Dictionary<int, List<(int Name, int Value)>> scalar_class_initializers = [];
    internal readonly Dictionary<int, List<(int Argument, int Type, int Symbol)>> scalar_constructor_arguments = [];
    internal readonly Dictionary<int, List<(int Name, int Symbol)>> scalar_constructor_assignments = [];
}

internal static partial class CompilerFunctions
{
    internal static bool scalar_class_literal(int entry, TypeKind kind, Compiler c)
    {
        Node value = get_node(entry, c);
        if (kind == TypeKind.Number && value.kind == NodeKind.Unary && value.left != 0 &&
            value.right == 0 && value.third == 0 && value.fourth == 0 && value.children == 0)
        {
            int operation = get_token(value.token, c).text;
            if (!is_text(operation, text_plus, c) && !is_text(operation, text_minus, c)) return false;
            value = get_node(value.left, c);
        }
        if (value.left != 0 || value.right != 0 || value.third != 0 || value.fourth != 0 || value.children != 0) return false;
        return kind == TypeKind.Number && value.kind == NodeKind.Number ||
            kind == TypeKind.String && value.kind == NodeKind.String || kind == TypeKind.Boolean && value.kind == NodeKind.Boolean;
    }

    internal static int scalar_field_function(int value, Compiler c)
    {
        if (value == 0) return 0;
        Node reference = get_node(value, c);
        if (reference.kind != NodeKind.FunctionValue || reference.symbol == 0 || reference.left == 0 ||
            get_symbol(reference.symbol, c).function != reference.left) return 0;
        Node function = get_node(reference.left, c);
        return function.kind == NodeKind.Function && function.right == 0 && !function.internal_continuation ? reference.left : 0;
    }

    internal static List<(int Name, int Value)> scalar_class_fields(int entry, Vector<int> definitions,
        Vector<int> declarations, Vector<int> parameters, Compiler c, IlBackend b)
    {
        Node construction = get_node(entry, c);
        if (construction.kind != NodeKind.Construct || construction.left == 0 || construction.type == 0 ||
            get_type(construction.type, c).kind != TypeKind.Shape) return null;
        Node reference = get_node(construction.left, c);
        if (reference.kind != NodeKind.Name || reference.symbol == 0 || parameters.at(reference.symbol) != 0 ||
            declarations.at(reference.symbol) != 1 || b.written_symbols.at(reference.symbol) != 0) return null;
        int declaration = (int)definitions.at(reference.symbol);
        if (declaration == 0) return null;
        Node definition = get_node(declaration, c);
        if (definition.kind != NodeKind.Variable || definition.left == 0) return null;
        Node call = get_node(definition.left, c);
        if (call.kind != NodeKind.Call || call.children != 0 || call.left == 0) return null;
        int factory = scalar_field_function(call.left, c);
        if (factory == 0 || get_node(factory, c).children != 0 || get_node(factory, c).left == 0) return null;
        Node scope = get_node(get_node(factory, c).left, c);
        if (scope.kind != NodeKind.Block || scope.children == 0) return null;
        Node binding = get_node(scope.children, c);
        if (binding.kind != NodeKind.Variable || binding.symbol == 0 || binding.left == 0 || binding.next == 0) return null;
        Node initialize = get_node(binding.next, c);
        if (initialize.kind != NodeKind.ClassStatic || initialize.left == 0 || initialize.right == 0 || initialize.next == 0) return null;
        Node result = get_node(initialize.next, c);
        if (binding.kind != NodeKind.Variable || binding.symbol == 0 || b.written_symbols.at(binding.symbol) != 0 ||
            result.kind != NodeKind.Return || result.left == 0 || result.next != 0 ||
            get_node(initialize.left, c).kind != NodeKind.Name || get_node(initialize.left, c).symbol != binding.symbol ||
            get_node(result.left, c).kind != NodeKind.Name || get_node(result.left, c).symbol != binding.symbol) return null;
        Node klass = get_node(binding.left, c);
        // Inheritance, methods, computed keys and decorators keep the ordinary
        // construction path. Static initializers must also be empty.
        if (klass.kind != NodeKind.ClassValue || klass.left == 0 || klass.fourth == 0 || klass.third != 0 || klass.children != 0 ||
            get_node(klass.fourth, c).type != construction.type) return null;
        int statics = scalar_field_function(initialize.right, c);
        if (statics == 0 || get_node(statics, c).left == 0 || get_node(get_node(statics, c).left, c).kind != NodeKind.Block ||
            get_node(get_node(statics, c).left, c).children != 0) return null;
        int fields = scalar_field_function(klass.left, c);
        if (fields == 0) return null;
        Node function = get_node(fields, c);
        if (function.left == 0 || function.fourth == 0) return null;
        Node body = get_node(function.left, c);
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
            if (field.kind != NodeKind.ClassField || field.name == 0 || field.left == 0 || field.right == 0 || field.third != 0 || field.fourth != 0 ||
                get_node(field.left, c).kind != NodeKind.Name || get_node(field.left, c).symbol != receiver.symbol ||
                values.Any(previous => same(previous.Name, field.name, c))) return null;
            int member = CompilerFunctions.field(construction.type, field.name, c);
            if (member == 0) return null;
            Node value = get_node(field.right, c);
            TypeKind kind = get_type(get_field(member, c).type, c).kind;
            bool literal = scalar_class_literal(field.right, kind, c);
            if (!literal && (value.kind != NodeKind.Undefined || value.constant)) return null;
            values.Add((field.name, field.right));
        }
        if (!scalar_constructor(entry, klass.right, values.Select(value => value.Name).ToList(), count, c, b)) return null;
        return values;
    }

    internal static bool scalar_constructor(int entry, int constructor, List<int> owned, int count, Compiler c, IlBackend b)
    {
        Node construction = get_node(entry, c);
        if (constructor == 0) return construction.children == 0 && owned.Count == count;
        int target = scalar_field_function(constructor, c);
        if (target == 0) return false;
        Node function = get_node(target, c);
        if (function.left == 0 || function.fourth == 0) return false;
        Node body = get_node(function.left, c);
        Node receiver = get_node(function.fourth, c);
        if (body.kind != NodeKind.Block || function.fourth == 0 || function.children != function.fourth ||
            receiver.kind != NodeKind.Variable || receiver.symbol == 0 || receiver.name != 0 || receiver.constant) return false;
        var arguments = new List<(int Argument, int Type, int Symbol)>();
        int parameter = receiver.next, argument = construction.children;
        while (parameter != 0)
        {
            Node p = get_node(parameter, c);
            bool supplied = argument != 0;
            int source = supplied ? argument : p.left;
            if (source == 0 || p.type == 0) return false;
            Node value = get_node(source, c);
            if (value.type == 0) return false;
            int storageType = supplied ? p.type : value.type;
            TypeKind kind = get_type(storageType, c).kind;
            if (p.kind != NodeKind.Variable || p.symbol == 0 || (supplied && p.constant) || p.right != 0 ||
                p.third != 0 || p.fourth != 0 || p.children != 0 ||
                kind is not (TypeKind.Number or TypeKind.String or TypeKind.Boolean) ||
                value.kind == NodeKind.Spread || get_type(value.type, c).kind != kind) return false;
            // A supplied nullable value can still trigger its default. Keep that
            // case on the ordinary path; omitted literal defaults have no effects.
            if (supplied && p.left != 0) return false;
            if (!supplied && !scalar_class_literal(source, kind, c)) return false;
            if (arguments.Count == 8) return false;
            arguments.Add((source, storageType, p.symbol));
            parameter = p.next;
            if (supplied) argument = value.next;
        }
        if (argument != 0) return false;
        var assignments = new List<(int Name, int Symbol)>();
        for (int statement = body.children; statement != 0; statement = get_node(statement, c).next)
        {
            Node expression = get_node(statement, c);
            int name;
            Node value;
            if (expression.kind == NodeKind.ClassField)
            {
                if (expression.name == 0 || expression.left == 0 || expression.right == 0 ||
                    expression.third != 0 || expression.fourth != 0) return false;
                Node owner = get_node(expression.left, c);
                if (owner.kind != NodeKind.Name || owner.symbol != receiver.symbol) return false;
                name = expression.name;
                value = get_node(expression.right, c);
                if (!owned.Any(previous => same(previous, name, c))) owned.Add(name);
            }
            else
            {
                if (expression.kind != NodeKind.Expression || expression.left == 0) return false;
                Node assign = get_node(expression.left, c);
                if (assign.kind != NodeKind.Assign || assign.left == 0 || assign.right == 0 ||
                    !is_text(get_token(assign.token, c).text, text_assign, c)) return false;
                Node property = get_node(assign.left, c);
                if (property.kind != NodeKind.Property || property.left == 0 || property.fourth != 0) return false;
                Node owner = get_node(property.left, c);
                if (owner.kind != NodeKind.ResolveThis || owner.left == 0 || get_node(owner.left, c).kind != NodeKind.Name ||
                    get_node(owner.left, c).symbol != receiver.symbol) return false;
                name = property.name;
                value = get_node(assign.right, c);
                // Assignments must not skip a possible inherited setter.
                if (!owned.Any(previous => same(previous, name, c))) return false;
            }
            if (value.kind != NodeKind.Name) return false;
            int member = field(construction.type, name, c);
            if (member == 0 || !arguments.Any(p => p.Symbol == value.symbol &&
                get_type(p.Type, c).kind == get_type(get_field(member, c).type, c).kind)) return false;
            if (assignments.Count == 32) return false;
            assignments.Add((name, value.symbol));
        }
        if (owned.Count != count) return false;
        b.scalar_constructor_arguments[entry] = arguments;
        b.scalar_constructor_assignments[entry] = assignments;
        return true;
    }

    internal static void il_scalar_class_initialize(int entry, List<ScalarField> fields, Compiler c, IlBackend b)
    {
        Node value = get_node(entry, c);
        // Keep the constructor binding read, including TDZ checks, before arguments.
        il_boxed(value.left, c, b); b.op(0x26);
        var parameters = new Dictionary<int, (int Slot, int Type)>();
        if (b.scalar_constructor_arguments.TryGetValue(entry, out var arguments))
        {
            var evaluated = new List<(int Slot, int Type)>();
            // Evaluate all arguments before checking any constructor parameters.
            foreach (var argument in arguments)
            {
                int type = get_node(argument.Argument, c).type, slot = b.local(b.type(type, c));
                il_expression(argument.Argument, c, b); b.emit(0xfe0e, slot); evaluated.Add((slot, type));
            }
            for (int i = 0; i < arguments.Count; i++)
            {
                var parameter = arguments[i]; int slot = b.local(b.type(parameter.Type, c));
                b.emit(0xfe0c, evaluated[i].Slot); b.convert(evaluated[i].Type, parameter.Type, c); b.emit(0xfe0e, slot);
                parameters[parameter.Symbol] = (slot, parameter.Type);
            }
        }
        foreach (var initializer in b.scalar_class_initializers[entry])
        {
            ScalarField storage = fields.First(field => same(field.name, initializer.Name, c));
            il_as(initializer.Value, storage.type, c, b); b.emit(0xfe0e, storage.slot);
        }
        if (b.scalar_constructor_assignments.TryGetValue(entry, out var assignments))
        {
            foreach (var assignment in assignments)
            {
                var parameter = parameters[assignment.Symbol];
                ScalarField storage = fields.First(field => same(field.name, assignment.Name, c));
                b.emit(0xfe0c, parameter.Slot); b.convert(parameter.Type, storage.type, c); b.emit(0xfe0e, storage.slot);
            }
        }
    }

    internal static bool il_scalar_class_property(int entry, Compiler c, IlBackend b)
    {
        Node property = get_node(entry, c);
        if (property.fourth != 0 || !b.scalar_class_initializers.ContainsKey(property.left)) return false;
        int member = field(get_node(property.left, c).type, property.name, c);
        if (member == 0) return false;
        var fields = new List<ScalarField>();
        for (int item = get_type(get_node(property.left, c).type, c).fields; item != 0; item = get_field(item, c).next)
        {
            Field field = get_field(item, c);
            fields.Add(new ScalarField {name = field.name, type = field.type, slot = b.local(b.type(field.type, c))});
        }
        il_scalar_class_initialize(property.left, fields, c, b);
        ScalarField result = fields.First(field => same(field.name, property.name, c));
        b.emit(0xfe0c, result.slot); b.convert(result.type, property.type, c);
        return true;
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
