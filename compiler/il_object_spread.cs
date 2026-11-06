namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static bool il_object_needs_entries(int entry, Compiler c)
    {
        Node n = get_node(entry, c); if (n.kind != NodeKind.Object) { return false; }
        int property = n.children;
        while (property != 0) { Node p = get_node(property, c); if (p.kind == NodeKind.Spread || (p.kind == NodeKind.Property && (p.right != 0 || p.third != 0))) { return true; } property = p.next; }
        return false;
    }
    internal static void il_prepare_spreads(Compiler c, IlBackend b)
    {
        for(int i=1;i<=c.node_store.count();i++){
            Node node=get_node(i,c);
            if(node.kind==NodeKind.Destructure && node.constant && il_has_object_rest(i,c))b.spread_any_constructor=-1;
        }
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            if (!il_object_needs_entries(i, c)) { continue; }
            int type = get_node(i, c).type;
            if (get_type(type, c).kind == TypeKind.Shape) { b.spread_types.at_mut(type) = 1; }
            else { b.spread_any_constructor = -1; }
        }
    }
    internal static bool il_same_spread_fields(int source, int target, Compiler c)
    {
        if (get_type(source, c).kind != TypeKind.Shape || get_type(target, c).kind != TypeKind.Shape) { return false; }
        // Class instances store properties dynamically, not in fixed CLR fields.
        if (get_type(source,c).name!=0 || get_type(target,c).name!=0) { return false; }
        int count = 0; int member = get_type(source, c).fields;
        while (member != 0)
        {
            Field a = get_field(member, c); int found = field(target, a.name, c);
            if (found == 0) { return false; }
            Field other = get_field(found, c);
            if (a.type != other.type || a.optional != other.optional) { return false; }
            count++; member = a.next;
        }
        member = get_type(target, c).fields;
        while (member != 0) { count--; member = get_field(member, c).next; }
        return count == 0;
    }
    internal static void il_prepare_spread_hints(Compiler c, IlBackend b)
    {
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c); if (n.kind != NodeKind.Variable || n.left == 0) { continue; }
            Node source = get_node(n.left, c);
            if (source.kind != NodeKind.Object || il_object_needs_entries(n.left, c) || !il_same_spread_fields(source.type, n.type, c)) { continue; }
            int klass = b.literal_classes.at(n.left); if (klass == 0) { klass = b.classes.at(source.type); }
            b.spread_source_classes.at_mut(n.symbol) = klass;
        }
    }
    internal static void il_spread_count(int klass, IlBackend b)
    {
        b.begin(b.plan.method0(5, klass, 0xc6, -3, "OwnKeyCount"));
        b.emit(0xfe09, 0); b.emit(0x20, 0); b.call(RuntimeMember.KeyCountWithFixed); b.op(0x2a);
    }
    internal static // Spread objects retain typed fields, with the base dictionary recording which
                    // keys actually exist and their insertion order. Missing fields stay undefined.
void il_spread_shapes(Compiler c, IlBackend b)
    {
        for (int type = 1; type < b.spread_types.count(); type++)
        {
            if (b.spread_types.at(type) == 0) { continue; }
            Vector<byte> name = numbered_name('D', (int)type); int parentLocal = b.classes.at(type);
            int klassLocal = b.plan.type(name.view(), parentLocal, 0x101); b.spread_classes.at_mut(type) = klassLocal;
            int constructorLocal = b.plan.method0(5, klassLocal, 0x1886, -1, ".ctor"); b.spread_constructors.at_mut(type) = constructorLocal;
            b.begin(constructorLocal); b.emit(0xfe09, 0); b.emit(0x28, b.constructor(type));
            int member = get_type(type, c).fields;
            while (member != 0)
            {
                Field record = get_field(member, c); b.emit(0xfe09, 0); b.empty(record.type, c); b.store_field(member, c); member = record.next;
            }
            b.op(0x2a);
            b.begin(b.plan.method1(5, klassLocal, 0xc6, -2, -4, "HasOwn"));
            b.emit(0xfe09, 0); b.emit(0xfe09, 1); b.call(RuntimeMember.HasOwnBase); b.op(0x2a);
            b.begin(b.plan.method0(5, klassLocal, 0xc6, -5, "OwnKeys"));
            b.emit(0xfe09, 0); b.call(RuntimeMember.OwnKeysBase); b.op(0x2a);
            il_spread_count(klassLocal, b);
            b.begin(b.plan.method1(5, klassLocal, 0xc6, -1, -4, "MarkPresent"));
            b.emit(0xfe09, 0); b.emit(0xfe09, 1); b.call(RuntimeMember.Undefined); b.call(RuntimeMember.DefineOwn); b.op(0x2a);
            // Convert only after all overwrites, so an intermediate incompatible
            // value does not fail before a later property replaces it.
            int finish = b.plan.method0(5, klassLocal, 6, -1, "FinishSpread"); b.spread_finishes.at_mut(type) = finish; b.begin(finish);
            member = get_type(type, c).fields;
            while (member != 0)
            {
                Field record = get_field(member, c); int absent = b.label();
                b.emit(0xfe09, 0); il_text(record.name, c, b); b.call(RuntimeMember.HasOwnBase); b.emit(0x39, absent);
                b.emit(0xfe09, 0); b.emit(0xfe09, 0); il_text(record.name, c, b); b.call(RuntimeMember.Get); b.from_object(record.type, c); b.store_field(member, c);
                b.emit(0xfe09, 0); il_text(record.name, c, b); b.call(RuntimeMember.Undefined); b.call(RuntimeMember.DefineOwn);
                b.mark(absent); member = record.next;
            }
            b.op(0x2a);
        }
        if (b.spread_any_constructor != -1) { return; }
        int parent = b.RuntimeToken(RuntimeMember.TsObjectType); int klass = b.plan.type("DAny", parent, 0x101);
        int constructor = b.plan.method0(5, klass, 0x1886, -1, ".ctor"); b.spread_any_constructor = constructor;
        b.begin(constructor); b.emit(0xfe09, 0); b.call(RuntimeMember.Constructor); b.op(0x2a); il_spread_count(klass, b);
        Vector<int> emitted = new Vector<int>(); int target = c.types;
        while (target != 0)
        {
            if (get_type(target, c).kind == TypeKind.Shape)
            {
                int interface_type = b.interface_type(target); b.plan.implements(klass, interface_type); int member = get_type(target, c).fields;
                while (member != 0)
                {
                    Field record = get_field(member, c); bool seen = false;
                    for (int i = 0; i < emitted.count(); i++)
                    {
                        Field previous = get_field(emitted.at(i), c);
                        if (same(previous.name, record.name, c) && b.type(previous.type, c) == b.type(record.type, c)) { seen = true; }
                    }
                    if (!seen)
                    {
                        emitted.push(member); int storage = b.type(record.type, c);
                        Vector<byte> get = field_name(record.name, "get_p_", c); Vector<byte> set = field_name(record.name, "set_p_", c);
                        b.begin(b.plan.method0(5, klass, 0x1e6, storage, get.view()));
                        b.emit(0xfe09, 0); il_text(record.name, c, b); b.call(RuntimeMember.Get);
                        if (storage != -5) { b.from_object(record.type, c); }
                        b.op(0x2a);
                        b.begin(b.plan.method1(5, klass, 0x1e6, -1, storage, set.view()));
                        b.emit(0xfe09, 0); il_text(record.name, c, b); b.emit(0xfe09, 1); b.box(record.type, c); b.call(RuntimeMember.Set); b.op(0x26); b.op(0x2a);
                    }
                    member = record.next;
                }
            }
            target = get_type(target, c).next;
        }
    }
    internal static void il_object_spread_expression(int entry, Compiler c, IlBackend b)
    {
        Node n = get_node(entry, c); bool typed = get_type(n.type, c).kind == TypeKind.Shape;
        int constructor = typed ? b.spread_constructors.at(n.type) : b.spread_any_constructor;
        Node first = get_node(n.children, c); int cached = -1; int done = b.label();
        // The exact runtime class check keeps wider objects on the copy path.
        if (first.kind == NodeKind.Spread && first.next == 0 && il_same_spread_fields(get_node(first.left, c).type, n.type, c))
        {
            Node source = get_node(first.left, c); il_boxed(first.left, c, b); cached = b.local(-5); b.emit(0xfe0e, cached);
            int klass = 0;
            if (source.kind == NodeKind.Name) { klass = b.spread_source_classes.at(source.symbol); }
            if (source.kind == NodeKind.Object && !il_object_needs_entries(first.left, c)) { klass = b.literal_classes.at(first.left); if (klass == 0) { klass = b.classes.at(source.type); } }
            int root = b.classes.at(source.type);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                int candidate = attempt == 0 ? klass : root; if (candidate == 0 || (attempt == 1 && candidate == klass)) { continue; }
                int generic = b.RuntimeToken(RuntimeMember.TryCloneSpread); int method = b.plan.specialize(generic, candidate);
                b.emit(0xfe0c, cached); b.emit(0x28, method); b.op(0x25); b.emit(0x3a, done); b.op(0x26);
            }
        }
        b.emit(0x73, constructor); int destination = b.local(-5); b.emit(0xfe0e, destination);
        if (n.constant) { b.emit(0xfe0c, destination); b.call(RuntimeMember.InitializeEnum); }
        int property = n.children;
        while (property != 0)
        {
            Node p = get_node(property, c); b.emit(0xfe0c, destination);
            if (p.kind == NodeKind.Spread) { if (cached >= 0) { b.emit(0xfe0c, cached); } else { il_boxed(p.left, c, b); } b.call(RuntimeMember.ObjectSpread); }
            else
            {
                if (p.right != 0) { il_boxed(p.right, c, b); b.call(RuntimeMember.PropertyKey); } else { il_text(p.name, c, b); }
                int key = -1; if (p.constant) { key = b.local(-4); b.op(0x25); b.emit(0xfe0e, key); }
                il_boxed(p.left, c, b);
                if (p.constant) { b.emit(0xfe0c, key); b.call(RuntimeMember.NamePropertyFunction); }
                if(p.third!=0){b.emit(0x20,get_node(p.third,c).constant?1:2);b.call(RuntimeMember.DefineAccessor);}else b.call(RuntimeMember.DefineOwn);
            }
            property = p.next;
        }
        if (typed)
        {
            int klass = b.spread_classes.at(n.type); int finish = b.spread_finishes.at(n.type);
            b.emit(0xfe0c, destination); b.emit(0x74, klass); b.emit(0x28, finish);
        }
        b.emit(0xfe0c, destination); b.mark(done);
    }
    internal static // A generated field copy gives the JIT ordinary allocation and stores instead
                    // of a runtime-sized MemberwiseClone operation. The caller checks exact type.
void il_spread_clone(int type, int klass, int constructor, Compiler c, IlBackend b)
    {
        b.begin(b.plan.method0(5, klass, 0xc6, -5, "CloneForSpread"));
        b.emit(0x73, constructor); int result = b.local(klass); b.emit(0xfe0e, result);
        int member = get_type(type, c).fields;
        while (member != 0)
        {
            int token = b.field_token(member); b.emit(0xfe0c, result); b.emit(0xfe09, 0); b.emit(0x7b, token); b.emit(0x7d, token);
            member = get_field(member, c).next;
        }
        b.emit(0xfe09, 0); b.emit(0xfe0c, result); b.call(RuntimeMember.CopySpreadExtras);
        b.emit(0xfe0c, result); b.op(0x2a);
    }
    internal static void il_spread_clones(Compiler c, IlBackend b)
    {
        Vector<int> candidates = new Vector<int>();
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            if (!il_object_needs_entries(i, c)) { continue; }
            Node literal = get_node(i, c); Node spread = get_node(literal.children, c); Node source = get_node(spread.left, c);
            if (spread.kind != NodeKind.Spread || spread.next != 0 || !il_same_spread_fields(source.type, literal.type, c)) { continue; }
            candidates.push(b.classes.at(source.type));
            if (source.kind == NodeKind.Name) { candidates.push(b.spread_source_classes.at(source.symbol)); }
            if (source.kind == NodeKind.Object && !il_object_needs_entries(spread.left, c))
            {
                int klass = b.literal_classes.at(spread.left); if (klass == 0) { klass = b.classes.at(source.type); }
                candidates.push(klass);
            }
        }
        if (candidates.count() == 0) { return; }
        for (int type = 1; type < b.classes.count(); type++)
        {
            int klass = b.classes.at(type); if (klass == 0) { continue; }
            bool needed = false;
            for (int j = 0; j < candidates.count(); j++) { if (candidates.at(j) == klass) { needed = true; } }
            if (!needed) { continue; }
            int constructor = b.constructor(type); il_spread_clone(type, klass, constructor, c, b);
        }
        Vector<int> seen = new Vector<int>();
        for (int i = 1; i < b.literal_classes.count(); i++)
        {
            int klass = b.literal_classes.at(i); if (klass == 0) { continue; }
            bool needed = false;
            for (int j = 0; j < candidates.count(); j++) { if (candidates.at(j) == klass) { needed = true; } }
            if (!needed) { continue; }
            bool duplicate = false;
            for (int j = 0; j < seen.count(); j++) { if (seen.at(j) == klass) { duplicate = true; } }
            if (duplicate) { continue; }
            seen.push(klass);
            int constructor = b.literal_constructors.at(i); il_spread_clone(get_node(i, c).type, klass, constructor, c, b);
        }
    }
    internal static void il_scalar_spread_fields(int symbol, int type, int source, Compiler c, IlBackend b)
    {
        int member = get_type(type, c).fields;
        while (member != 0)
        {
            Field f = get_field(member, c);
            b.emit(0xfe0c, source); b.emit(0x74, b.interface_type(type)); b.emit(0x6f, b.getter(member));
            for (int i = 0; i < b.scalar_fields.count(); i++)
            {
                ScalarField storage = b.scalar_fields.at(i);
                if (storage.root == symbol && same(storage.name, f.name, c)) { b.emit(0xfe0e, storage.slot); break; }
            }
            member = f.next;
        }
    }
    internal static void il_scalar_spread_initialize(int entry, Compiler c, IlBackend b)
    {
        Node binding = get_node(entry, c); Node literal = get_node(binding.left, c); Node spread = get_node(literal.children, c); Node source = get_node(spread.left, c);
        il_boxed(spread.left, c, b); int cached = b.local(-5); b.emit(0xfe0e, cached); int done = b.label();
        int hint = 0; if (source.kind == NodeKind.Name) { hint = b.spread_source_classes.at(source.symbol); }
        if (source.kind == NodeKind.Object && !il_object_needs_entries(spread.left, c)) { hint = b.literal_classes.at(spread.left); if (hint == 0) { hint = b.classes.at(source.type); } }
        int root = b.classes.at(source.type);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            int candidate = attempt == 0 ? hint : root; if (candidate == 0 || (attempt == 1 && candidate == hint)) { continue; }
            int next = b.label(); int generic = b.RuntimeToken(RuntimeMember.IsExactSpread); int method = b.plan.specialize(generic, candidate);
            b.emit(0xfe0c, cached); b.emit(0x28, method); b.emit(0x39, next);
            il_scalar_spread_fields(binding.symbol, literal.type, cached, c, b); b.emit(0x38, done); b.mark(next);
        }
        // Unknown layouts still enumerate every key and evaluate any accessors.
        int constructor = b.spread_constructors.at(literal.type); int klass = b.spread_classes.at(literal.type); int finish = b.spread_finishes.at(literal.type);
        b.emit(0x73, constructor); int result = b.local(klass); b.emit(0xfe0e, result);
        b.emit(0xfe0c, result); b.emit(0xfe0c, cached); b.call(RuntimeMember.ObjectSpread);
        b.emit(0xfe0c, result); b.emit(0x28, finish);
        il_scalar_spread_fields(binding.symbol, literal.type, result, c, b); b.mark(done);
    }
}
