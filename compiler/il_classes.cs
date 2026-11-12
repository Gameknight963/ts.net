namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend { internal readonly Dictionary<int, int> class_factories = []; }

internal static partial class CompilerFunctions
{
    // Class instances use dynamic own properties and a shared prototype. Typed
    // interfaces still expose the checked field types at their CLR boundary.
    internal static void il_class_shape(int type, Compiler c, IlBackend b)
    {
        int parent = b.RuntimeToken(RuntimeMember.ClassInstanceType);
        Vector<byte> name = numbered_name('C', get_type(type, c).id);
        int klass = b.plan.type(name.view(), parent, 0x101); b.classes.at_mut(type) = klass;
        int constructor = b.plan.method0(5, klass, 0x1886, -1, ".ctor"); b.constructors.at_mut(type) = constructor;
        b.begin(constructor); b.emit(0xfe09, 0); b.call(RuntimeMember.ClassInstanceConstructor); b.op(0x2a);
        int factory = b.plan.method1(5, klass, 0x16, -5, -7, "CreateInstance"); b.class_factories[type] = factory;
        if(c.class_sources.TryGetValue(type,out int source)) il_function_source(factory,source,c,b);
        b.plan.instruction(factory, 0x73, constructor); b.plan.instruction(factory, 0x2a, 0);

        // This sealed generated class uses the runtime's own-property store.
        b.begin(b.plan.method2(5, klass, 0xc6, -5, -4, -5, "GetWithReceiver"));
        b.emit(0xfe09, 0); b.emit(0xfe09, 1); b.emit(0xfe09, 2);
        b.call(RuntimeMember.GetStoredWithReceiver); b.op(0x2a);

        il_stored_numeric_properties(klass, b);

        var family = new List<int>();
        for (int variant = c.types; variant != 0; variant = get_type(variant, c).next)
            if (get_type(variant, c).name == get_type(type, c).name) family.Add(variant);
        var emitted = new List<(int Name, int Type)>();
        for (int target = c.types; target != 0; target = get_type(target, c).next)
        {
            bool compatible = get_type(target, c).name == get_type(type, c).name || family.Any(variant => fits(variant, target, c));
            if (!compatible) continue;
            b.plan.implements(klass, b.interface_type(target));
            for (int member = get_type(target, c).fields; member != 0; member = get_field(member, c).next)
            {
                Field field = get_field(member, c); int storage = b.type(field.type, c);
                if (emitted.Any(previous => previous.Type == storage && same(previous.Name, field.name, c))) continue;
                emitted.Add((field.name, storage));
                Vector<byte> get = field_name(field.name, "get_p_", c), set = field_name(field.name, "set_p_", c);
                b.begin(b.plan.method0(5, klass, 0x1e6, storage, get.view()));
                b.emit(0xfe09, 0); il_text(field.name, c, b); b.emit(0xfe09, 0);
                if (get_type(field.type, c).kind == TypeKind.Number) b.call(RuntimeMember.GetStoredNumberWithReceiver);
                else { b.call(RuntimeMember.GetStoredWithReceiver); b.from_object(field.type, c); }
                b.op(0x2a);
                b.begin(b.plan.method1(5, klass, 0x1e6, -1, storage, set.view()));
                b.emit(0xfe09, 0); il_text(field.name, c, b); b.emit(0xfe09, 1); b.box(field.type, c); b.call(RuntimeMember.DynamicSet); b.op(0x26); b.op(0x2a);
            }
        }
    }
    internal static void il_class_value(int entry, Compiler c, IlBackend b)
    {
        Node node = get_node(entry, c); int type = get_node(node.fourth, c).type;
        // A factory avoids reflection when allocating the checked CLR class.
        int factory = b.class_factories[type];
        int inferredName = get_node(node.fourth, c).left;
        if (inferredName != 0) { il_boxed(inferredName, c, b); b.call(RuntimeMember.PropertyKey); } else { il_text(node.name, c, b); }
        b.op(0x14); b.emit(0xfe06, factory); b.emit(0x73, b.RuntimeToken(RuntimeMember.FunctionBodyConstructor));
        il_boxed(node.left, c, b); if (node.right != 0) il_boxed(node.right, c, b); else b.call(RuntimeMember.Undefined);
        if (node.third != 0) il_boxed(node.third, c, b); else b.call(RuntimeMember.Undefined);
        b.emit(0x20, node.third != 0 ? 1 : 0);
        b.call(RuntimeMember.CreateClass); int slot = b.local(-5); b.emit(0xfe0e, slot);
        for (int member = node.children; member != 0; member = get_node(member, c).next)
        {
            Node method = get_node(member, c);
            if (method.kind == NodeKind.Expression) { il_statement(member, node.type, c, b); continue; }
            if (method.kind == NodeKind.Variable)
            {
                il_boxed(method.left, c, b); b.call(RuntimeMember.PropertyKey); b.initialize(method.symbol); continue;
            }
            if (method.fourth != 0)
            {
                b.emit(0xfe0c, slot); il_text(method.name, c, b);
                b.emit(0x20, checked((int)number(get_token(get_node(method.fourth, c).token, c).text, c)));
                if (method.left != 0) il_boxed(method.left, c, b); else b.call(RuntimeMember.Undefined);
                il_boxed(method.right, c, b); b.call(RuntimeMember.DefinePrivateMember); b.initialize(method.symbol); continue;
            }
            b.emit(0xfe0c, slot);
            if (method.right != 0) { il_boxed(method.right, c, b); b.call(RuntimeMember.PropertyKey); } else il_text(method.name, c, b);
            if (method.symbol != 0 && method.left == 0)
            {
                b.emit(0x20, method.constant ? 1 : 0); b.call(RuntimeMember.DefineAutoAccessor); b.initialize(method.symbol); continue;
            }
            il_boxed(method.left, c, b); b.emit(0x20, method.constant ? 1 : 0);b.emit(0x20,method.third==0?0:get_node(method.third,c).constant?1:2); b.call(RuntimeMember.DefineClassMethod);
        }
        b.emit(0xfe0c, slot);
    }
}
