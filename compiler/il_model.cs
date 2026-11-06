namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal enum RuntimeMember
{
    BeginDestructuring, ReadDestructuring, RestDestructuring, AsyncResume, BeginAsyncIteration, NextAsyncIteration, AsyncIterationValue,
    DynamicObjectType, DynamicObjectConstructor, AsyncFunction, AsyncAwait, AsyncAwaitContinue, MarkFunctionPrototype, In, InNumber, SealModuleNamespace, InitializeEnum, BuiltinNamespace, CopyObjectRest, DeleteOwn, DefineAccessor, ResolveThis, NewTarget, EnableNewTarget, Arguments, EnableArguments, SuperBase, SuperCall, SuperInitializer, ClassInstanceType, ClassInstanceConstructor, CreateClass, DefineClassMethod, InitializeClass, DefineClassField, DefineAutoAccessor, InitializeAutoAccessor, DefinePrivateMember, InitializePrivateMember, PrivateGet, PrivateSet, PrivateHas, GetErrorConstructor, Construct, ConstructArguments, InstanceOf, ExceptionType, MakeThrown, CaughtValue, CallStringSearch, CallStringSearchNumber, CallStringSearchBoolean, StringIndexAt, StringIndexNumber, RelativeAtTarget, CallMethodIndices, CallMethodIndex, CallMethod0, CallMethod, CallMethodArguments, CallMethodNumber0, CallMethodNumber1, CallMethodNumber2, OwnValuesBase, OwnEntriesBase, OwnNumberValuesBase, NumberValuesFromFields, ObjectValues, ObjectNumberValues, ObjectEntries, ObjectValuesFunction, ObjectEntriesFunction, NamePropertyFunction, PropertyKey, EncodeNumberField, DecodeNumberField, IsExactSpread, CopySpreadExtras, ObjectSpread, DefineOwn, MarkPresent, TryCloneSpread, OwnKeysBase, ObjectKeyCount, KeyCountWithFixed, ObjectKeys, ObjectKeysFunction, KeysFromTextFields, KeysFromFixed, GuardIntegerLoop, ObjectConstructor, ObjectType, DoubleType, BooleanType, StringType, JsType, TsObjectType,
    Constructor, Get, Set, HasOwn, HasOwnNumber, HasOwnIndex, HasOwnBase, Cast, Check, Add, NumberAtIsNullish, LooseEqual, LooseEqualNumber, Equal, Number, Boolean, Text, DynamicGet, DynamicSet,
    Log, Now, Length, StringEqual, Concat, ActionType, ActionConstructor, Run, Truthy, Undefined, TypeOf, IsNumber, IsUndefined, IsNullish, EqualNumbers, EqualBooleans,
    WrapNumber, WrapBoolean, EmptyNumber, EmptyBoolean, BoxNumber, BoxBoolean, NumberValue, BooleanValue, NumberMissing, BooleanMissing, NumberTruthy, StringRelational, Relational, Power, Remainder, ToInt32,
    ArrayType, NumberArrayType, ArrayConstructor, NumberArrayConstructor, IndexGet, IndexSet, NumberIndexGet, NumberIndexSet, NumberIndexGetAt, NumberIndexSetAt, ArrayLength, ArraySetLength, ArrayPushOneReserved, ArrayPushOne, ArrayPush, ArrayPop, NumberPush, NumberPop, ArrayTryPacked, ArrayReservePushes, CanonicalNumber,
    ForInStart, ForInNext, ForOfLength, ForOfValue, ForOfNumber, ForOfStep, ArrayAppend, ArrayHole, ArraySpread, RequireObject, LoopLimit, ArrayIndexSetAt, ArrayIndexGetAt, ArrayTryDense, ArrayDecode, ArrayReserveCapacity, LogArguments, PushArguments, CallRest, ArrayBufferConstructor, ArrayHoleValue, Slice, ArraySearch, ArraySearchNumber, ArrayReverse, ArrayReverseBuiltin, IsArrayBuiltin, ReversibleConstructor, ReversibleBufferConstructor, FunctionBodyConstructor, FunctionType, FunctionConstructor, ReceiverFunctionConstructor, NumberFunction0Constructor, NumberFunction1Constructor, NumberFunction2Constructor, RawNumberFunction0Constructor, RawNumberFunction1Constructor, RawNumberFunction2Constructor, FunctionArgument, FunctionRest, CallFunction0, CallFunction, CallFunctionArguments, CallFunctionNumber0, CallFunctionNumber1, CallFunctionNumber2, CheckInitialized, TaggedConstructor, TaggedBufferConstructor, ArrayTryTags, ArrayIsNumberAt, ArraySetTaggedNumberAt, ArraySetTaggedReferenceAt, NumberPredicate, NumberPredicateNumber, NumberPredicateFunction, Count
}

internal struct ScalarField
{
    public int root;
    public int name;
    public int type;
    public int slot;
}

internal sealed partial class IlBackend
{
    public AssemblyPlan plan;
    public Vector<int> runtime;
    public Vector<int> interfaces;
    public Vector<int> classes;
    public Vector<int> constructors;
    public Vector<int> fields;
    public Vector<int> getters;
    public Vector<int> setters;
    public Vector<int> casts;
    public Vector<int> methods;
    public Vector<int> binding_methods;
    public Vector<int> global_fields;
    public Vector<int> global_flags;
    public Vector<int> global_vars;
    public Vector<int> global_cache;
    public Vector<int> slots;
    public Vector<int> raw_slots;
    public Vector<int> numeric_slots;
    public Vector<int> numeric_methods;
    public Vector<int> written_symbols;
    public Vector<int> nonnegative_ints;
    public Vector<int> node_parents;
    public Vector<int> packed_arrays;
    public Vector<int> dense_arrays;
    public Vector<int> tag_arrays;
    public Vector<int> reversible_literals;
    public Vector<int> tagged_literals;
    public Vector<int> spread_source_classes;
    public Vector<int> literal_classes;
    public Vector<int> spread_classes;
    public Vector<int> spread_types;
    public Vector<int> spread_constructors;
    public Vector<int> spread_finishes;
    public bool enumerate_values;
    public bool enumerate_entries;
    public int spread_any_constructor;
    public int function_allocator;
    public int function_allocator_cache;
    public Vector<int> layout_needed;
    public Vector<int> literal_constructors;
    public Vector<int> function_adapters;
    public Vector<int> raw_functions;
    public Vector<int> function_caches;
    public Vector<int> function_constructors;
    public Vector<int> capture_fields;
    public Vector<int> capture_flags;
    public Vector<int> capture_scopes;
    public Vector<int> capture_types;
    public Vector<int> capture_vars;
    public Vector<int> function_contexts;
    public Vector<int> context_types;
    public Vector<int> context_constructors;
    public Vector<int> context_parents;
    public Vector<int> context_parent_fields;
    public int context_scope;
    public int context_slot;
    public Vector<int> function_targets;
    public Vector<int> predicate_targets;
    public Vector<int> function_values_needed;
    public Vector<int> iteration_vars;
    public Vector<int> scalar_roots;
    public Vector<ScalarField> scalar_fields;
    public Vector<int> breaks;
    public Vector<int> continues;
    public int method;
    public int local_count;
    public int label_count;
    public int push_bound;
    public int loop_versions;
    public bool numeric_globals;
    public bool raw_function_return;
    public static IlBackend create(Compiler c)
    {
        int types = c.type_store.count() + 1;
        int fields = c.field_store.count() + 1;
        int symbols = c.symbol_store.count() + 1;
        return new IlBackend
        {
            plan = AssemblyPlan.create(),
            runtime = zero_ids((int)RuntimeMember.Count),
            interfaces = zero_ids(types),
            classes = zero_ids(types),
            constructors = zero_ids(types),
            fields = zero_ids(fields),
            getters = zero_ids(fields),
            setters = zero_ids(fields),
            casts = zero_ids(types),
            methods = zero_ids(symbols),
            binding_methods = zero_ids(symbols),
            global_fields = zero_ids(symbols),
            global_flags = zero_ids(symbols),
            global_vars = zero_ids(symbols),
            global_cache = zero_ids(symbols),
            slots = empty_slots(symbols),
            raw_slots = zero_ids(symbols),
            numeric_slots = zero_ids(symbols),
            numeric_methods = zero_ids(symbols),
            written_symbols = zero_ids(symbols),
            nonnegative_ints = zero_ids(symbols),
            node_parents = new Vector<int>(),
            packed_arrays = zero_ids(symbols),
            dense_arrays = zero_ids(symbols),
            tag_arrays = zero_ids(symbols),
            reversible_literals = zero_ids(c.node_store.count() + 1),
            tagged_literals = zero_ids(c.node_store.count() + 1),
            spread_source_classes = zero_ids(symbols),
            literal_classes = zero_ids(c.node_store.count() + 1),
            spread_classes = zero_ids(types),
            spread_types = zero_ids(types),
            spread_constructors = zero_ids(types),
            spread_finishes = zero_ids(types),
            enumerate_values = false,
            enumerate_entries = false,
            spread_any_constructor = 0,
            layout_needed = zero_ids(types),
            literal_constructors = zero_ids(c.node_store.count() + 1),
            function_adapters = zero_ids(symbols),
            raw_functions = zero_ids(symbols),
            function_caches = zero_ids(symbols),
            function_constructors = zero_ids(symbols),
            capture_fields = zero_ids(symbols),
            capture_flags = zero_ids(symbols),
            capture_scopes = zero_ids(symbols),
            capture_types = zero_ids(symbols),
            capture_vars = zero_ids(symbols),
            function_contexts = zero_ids(symbols),
            context_types = zero_ids(c.node_store.count() + 1),
            context_constructors = zero_ids(c.node_store.count() + 1),
            context_parents = zero_ids(c.node_store.count() + 1),
            context_parent_fields = zero_ids(c.node_store.count() + 1),
            context_scope = 0,
            context_slot = -1,
            function_targets = zero_ids(symbols),
            predicate_targets = zero_ids(symbols),
            function_values_needed = zero_ids(symbols),
            iteration_vars = zero_ids(symbols),
            scalar_roots = zero_ids(symbols),
            scalar_fields = new Vector<ScalarField>(),
            breaks = zero_ids(c.node_store.count() + 1),
            continues = zero_ids(c.node_store.count() + 1),
            method = 0,
            local_count = 0,
            label_count = 0,
            push_bound = -1,
            loop_versions = 0,
            numeric_globals = false,
            raw_function_return = false
        };
    }
    public int RuntimeToken(RuntimeMember member) { return this.runtime.at((int)member); }
    public int field_storage(int type, Compiler c) { return get_type(type, c).kind == TypeKind.Number ? -3 : this.type(type, c); }
    public void load_field(int member, Compiler c)
    {
        if (il_dynamic_index_field(member,c)) { il_text(get_field(member,c).name,c,this);this.call(RuntimeMember.Get);this.from_object(get_field(member,c).type,c);return; }
        this.emit(0x7b, this.field_token(member));
        if (get_type(get_field(member, c).type, c).kind == TypeKind.Number) { this.call(RuntimeMember.DecodeNumberField); }
    }
    public void store_field(int member, Compiler c)
    {
        if (il_dynamic_index_field(member,c))
        {
            int type=get_field(member,c).type;int value=this.local(this.type(type,c));this.emit(0xfe0e,value);
            il_text(get_field(member,c).name,c,this);this.emit(0xfe0c,value);this.box(type,c);this.call(RuntimeMember.Set);this.op(0x26);return;
        }
        if (get_type(get_field(member, c).type, c).kind == TypeKind.Number) { this.call(RuntimeMember.EncodeNumberField); }
        this.emit(0x7d, this.field_token(member));
    }
    public int field_token(int id) { return this.fields.at(id); }
    public int getter(int id) { return this.getters.at(id); }
    public int setter(int id) { return this.setters.at(id); }
    public int cast(int id) { return this.casts.at(id); }
    public int function(int id) { return this.methods.at(id); }
    public int constructor(int id) { return this.constructors.at(id); }
    public int interface_type(int id) { return this.interfaces.at(id); }
    public int break_target(int id) { return this.breaks.at(id); }
    public int continue_target(int id) { return this.continues.at(id); }
    public int raw_slot(int id) { return this.capture_fields.at(id) != 0 ? 0 : this.raw_slots.at(id); }
    public void ref_set(RuntimeMember member, int id) { this.runtime.at_mut((int)member) = id; }
    public int type(int type, Compiler c)
    {
        if (type == 0) { return -1; }
        TypeKind kind = get_type(type, c).kind;
        if (kind == TypeKind.Number) { return -10; }
        if (kind == TypeKind.Boolean) { return -11; }
        if (kind == TypeKind.String) { return -5; }
        if (kind == TypeKind.Void) { return -1; }
        if (kind == TypeKind.Shape || kind == TypeKind.Array) { return -5; }
        return -5;
    }
    public void emit(int opcode, int operand) { this.plan.instruction(this.method, opcode, operand); }
    public void op(int opcode) { this.emit(opcode, 0); }
    public void call(RuntimeMember member) { this.emit(0x28, this.RuntimeToken(member)); }
    public int label() { return ++this.label_count; }
    public void mark(int label) { this.emit(-1, label); }
    public int local(int type) { this.plan.local(this.method, type); return this.local_count++; }
    public void begin(int method) { this.exception_frames.Clear(); this.caught_exception=-1; this.method = method; this.local_count = 0; this.label_count = 0; this.loop_versions = 0; this.push_bound = -1; this.context_scope = 0; this.context_slot = -1; }
    public void load_context(int scope)
    {
        if (this.context_scope == 0) { print("tsnetc: closure environment missing"); exit(1); }
        if (this.context_slot < 0) { this.emit(0xfe09, 0); } else { this.emit(0xfe0c, this.context_slot); }
        int current = this.context_scope;
        while (current != scope)
        {
            int field = this.context_parent_fields.at(current);
            if (field == 0) { print("tsnetc: closure environment is outside lexical scope"); exit(1); }
            this.emit(0x7b, field); current = this.context_parents.at(current);
        }
    }
    public void load_storage(int symbol)
    {
        int capture = this.capture_fields.at(symbol);
        if (capture != 0)
        {
            int scope = this.capture_scopes.at(symbol); this.load_context(scope); this.op(0x25);
            int flag = this.capture_flags.at(symbol); this.emit(0x7b, flag); this.call(RuntimeMember.CheckInitialized); this.emit(0x7b, capture); return;
        }
        int cached = this.global_cache.at(symbol);
        if (cached != 0) { this.emit(0xfe0c, cached - 1); return; }
        int global = this.global_fields.at(symbol);
        if (global != 0) { this.check_global(symbol); this.emit(0x7e, global); return; }
        int slot = this.slots.at(symbol);
        if (slot == -2147483647) { print("tsnetc: captured binding or function value needs lowering"); exit(1); }
        if (slot < 0) { this.emit(0xfe09, -slot - 1); } else { this.emit(0xfe0c, slot); }
    }
    public void load(int symbol)
    {
        this.load_storage(symbol);
        if (this.raw_slot(symbol) == -6) { this.op(0x6c); this.call(RuntimeMember.WrapNumber); }
        else if (this.raw_slot(symbol) == -3) { this.call(RuntimeMember.WrapNumber); }
        else if (this.raw_slot(symbol) == -2) { this.call(RuntimeMember.WrapBoolean); }
    }
    public void store(int symbol)
    {
        if (this.raw_slot(symbol) == -6) { this.call(RuntimeMember.NumberValue); this.op(0x69); }
        else if (this.raw_slot(symbol) == -3) { this.call(RuntimeMember.NumberValue); }
        else if (this.raw_slot(symbol) == -2) { this.call(RuntimeMember.BooleanValue); }
        this.store_storage(symbol);
    }
    public void store_storage(int symbol)
    {
        int capture = this.capture_fields.at(symbol);
        if (capture != 0)
        {
            int storage = this.capture_types.at(symbol); int value = this.local(storage); this.emit(0xfe0e, value);
            int scope = this.capture_scopes.at(symbol); this.load_context(scope); this.op(0x25);
            int flag = this.capture_flags.at(symbol); this.emit(0x7b, flag); this.call(RuntimeMember.CheckInitialized);
            this.emit(0xfe0c, value); this.emit(0x7d, capture); return;
        }
        int cached = this.global_cache.at(symbol);
        if (cached != 0) { this.emit(0xfe0e, cached - 1); return; }
        int global = this.global_fields.at(symbol);
        if (global != 0) { this.check_global(symbol); this.emit(0x80, global); return; }
        int slot = this.slots.at(symbol);
        if (slot == -2147483647) { print("tsnetc: captured binding needs lowering"); exit(1); }
        if (slot < 0) { this.emit(0xfe0b, -slot - 1); } else { this.emit(0xfe0e, slot); }
    }
    public void check_global(int symbol)
    {
        if (this.global_vars.at(symbol) == 0)
        {
            int flag = this.global_flags.at(symbol); this.emit(0x7e, flag); this.call(RuntimeMember.CheckInitialized);
        }
    }
    public void initialize(int symbol)
    {
        int capture = this.capture_fields.at(symbol);
        if (capture != 0)
        {
            int storage = this.capture_types.at(symbol); int value = this.local(storage); this.emit(0xfe0e, value);
            int scope = this.capture_scopes.at(symbol); this.load_context(scope); this.op(0x25);
            this.emit(0xfe0c, value); this.emit(0x7d, capture); this.emit(0x20, 1); int flagLocal = this.capture_flags.at(symbol); this.emit(0x7d, flagLocal); return;
        }
        int global = this.global_fields.at(symbol);
        if (global == 0) { this.store(symbol); return; }
        this.emit(0x80, global); int flag = this.global_flags.at(symbol);
        if (flag != 0) { this.emit(0x20, 1); this.emit(0x80, flag); }
    }
    public void box(int type, Compiler c)
    {
        if (get_type(type, c).kind == TypeKind.Number) { this.call(RuntimeMember.BoxNumber); }
        else if (get_type(type, c).kind == TypeKind.Boolean) { this.call(RuntimeMember.BoxBoolean); }
    }
    public void from_object(int target, Compiler c)
    {
        TypeKind kind = get_type(target, c).kind;
        if (this.type(target, c) != -5 || kind == TypeKind.String || kind == TypeKind.Shape || kind == TypeKind.Array)
        {
            this.plan.StringLiteral(this.method, "checked conversion failed"); this.emit(0x28, this.cast(target));
        }
    }
    public void convert(int source, int target, Compiler c)
    {
        TypeKind target_kind = get_type(target, c).kind;
        if (source == target || (this.type(source, c) == this.type(target, c) && target_kind != TypeKind.String && target_kind != TypeKind.Shape && target_kind != TypeKind.Array)) { return; }
        if ((get_type(source, c).kind == TypeKind.String && target_kind == TypeKind.String) ||
            (get_type(source, c).kind == TypeKind.Array && target_kind == TypeKind.Array) || fits(source, target, c)) { return; }
        if (get_type(source, c).kind == TypeKind.Void) { this.call(RuntimeMember.Undefined); }
        else { this.box(source, c); }
        if (get_type(target, c).kind == TypeKind.Void) { this.op(0x26); }
        else { this.from_object(target, c); }
    }
    public void empty(int type, Compiler c)
    {
        if (get_type(type, c).kind == TypeKind.Number) { this.call(RuntimeMember.EmptyNumber); }
        else if (get_type(type, c).kind == TypeKind.Boolean) { this.call(RuntimeMember.EmptyBoolean); }
        else { this.call(RuntimeMember.Undefined); }
    }
    public void missing(int type, Compiler c)
    {
        if (get_type(type, c).kind == TypeKind.Number) { this.call(RuntimeMember.NumberMissing); }
        else if (get_type(type, c).kind == TypeKind.Boolean) { this.call(RuntimeMember.BooleanMissing); }
        else { this.call(RuntimeMember.IsUndefined); }
    }
    public void truth(int type, Compiler c)
    {
        if (get_type(type, c).kind == TypeKind.Number) { this.call(RuntimeMember.NumberTruthy); }
        else if (get_type(type, c).kind == TypeKind.Boolean) { this.call(RuntimeMember.BooleanValue); }
        else { this.call(RuntimeMember.Truthy); }
    }
}

internal static partial class CompilerFunctions
{
    internal static Vector<int> zero_ids(int count)
    {
        Vector<int> result = new Vector<int>();
        for (int i = 0; i < count; i++) { result.push(0); }
        return identity(result);
    }
    internal static Vector<int> empty_slots(int count)
    {
        Vector<int> result = new Vector<int>();
        for (int i = 0; i < count; i++) { result.push(-2147483647); }
        return identity(result);
    }
    internal static // This proof concerns values, not TS annotations. Calls, property reads, and
                    // names are unknown; arithmetic results are defined even when they are NaN.
bool il_logical_assignment(int op, Compiler c)
    {
        return is_text(op, text_and_assign, c) || is_text(op, text_or_assign, c) || is_text(op, text_nullish_assign, c);
    }
    internal static bool il_defined(int entry, Compiler c)
    {
        if (entry == 0) { return false; }
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Number || n.kind == NodeKind.Boolean || n.kind == NodeKind.String || n.kind == NodeKind.Object ||
            n.kind == NodeKind.Unary || n.kind == NodeKind.Update || n.kind == NodeKind.TypeOf) { return true; }
        if (n.kind == NodeKind.Assert) { return il_defined(n.left, c); }
        if (n.kind == NodeKind.Sequence) { return il_defined(n.right, c); }
        if (n.kind == NodeKind.Conditional) { return il_defined(n.right, c) && il_defined(n.third, c); }
        if (n.kind == NodeKind.Nullish) { return il_defined(n.right, c); }
        if (n.kind == NodeKind.Assign)
        {
            if (il_logical_assignment(get_token(n.token, c).text, c)) { return false; }
            return !is_text(get_token(n.token, c).text, text_assign, c) || il_defined(n.right, c);
        }
        if (n.kind == NodeKind.Binary)
        {
            int op = get_token(n.token, c).text;
            if (is_text(op, text_and, c) || is_text(op, text_or, c)) { return il_defined(n.left, c) && il_defined(n.right, c); }
            return true;
        }
        return false;
    }
    internal static bool il_int_result(int entry, Compiler c)
    {
        if (entry == 0) { return false; }
        Node n = get_node(entry, c);
        if (n.kind == NodeKind.Number)
        {
            double value = number(get_token(n.token, c).text, c);
            return value >= -2147483648.0 && value <= 2147483647.0 && value == (double)(int)value;
        }
        if (n.kind == NodeKind.Conditional) { return il_int_result(n.right, c) && il_int_result(n.third, c); }
        if (n.kind == NodeKind.Sequence) { return il_int_result(n.right, c); }
        if (n.kind == NodeKind.Assert) { return il_int_result(n.left, c); }
        if (n.kind != NodeKind.Unary && n.kind != NodeKind.Binary && n.kind != NodeKind.Assign) { return false; }
        int op = get_token(n.token, c).text;
        if (n.kind == NodeKind.Unary) { return is_text(op, text_bit_not, c); }
        if (n.kind == NodeKind.Binary || n.kind == NodeKind.Assign)
        {
            if (n.kind == NodeKind.Assign && is_text(op, text_assign, c)) { return il_int_result(n.right, c); }
            return is_text(op, text_bit_and, c) || is_text(op, text_bit_or, c) || is_text(op, text_bit_xor, c) ||
                is_text(op, text_shift_left, c) || is_text(op, text_shift_right, c) ||
                is_text(op, text_bit_and_assign, c) || is_text(op, text_bit_or_assign, c) || is_text(op, text_bit_xor_assign, c) ||
                is_text(op, text_shift_left_assign, c) || is_text(op, text_shift_right_assign, c);
        }
        return false;
    }
    internal static void il_prove_locals(Compiler c, IlBackend b)
    {
        Vector<int> declarations = zero_ids(c.symbol_store.count() + 1);
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if (n.kind == NodeKind.Variable || (n.kind == NodeKind.Binding && n.symbol != 0)) { declarations.at_mut(n.symbol)++; }
            if (n.kind == NodeKind.Variable && il_defined(n.left, c))
            {
                TypeKind kind = get_type(n.type, c).kind;
                if (kind == TypeKind.Number) { b.raw_slots.at_mut(n.symbol) = il_int_result(n.left, c) ? -6 : -3; }
                else if (kind == TypeKind.Boolean) { b.raw_slots.at_mut(n.symbol) = -2; }
            }
        }
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            // Redeclarations share storage, including declarations of parameters.
            // Their first initializer does not prove later values remain defined.
            if ((n.kind == NodeKind.Variable || n.kind == NodeKind.Binding) && n.symbol != 0 && declarations.at(n.symbol) > 1)
            {
                b.raw_slots.at_mut(n.symbol) = 0; b.written_symbols.at_mut(n.symbol) = 1;
                b.iteration_vars.at_mut(n.symbol) = 1;
            }
            if (n.kind == NodeKind.Binding && n.symbol != 0) { b.raw_slots.at_mut(n.symbol) = 0; b.written_symbols.at_mut(n.symbol) = 1; }
            if (n.kind == NodeKind.ForOf || n.kind == NodeKind.ForIn)
            {
                Node target = get_node(n.left, c);
                if (target.kind == NodeKind.Block) { target = get_node(target.children, c); }
                if (target.kind == NodeKind.Variable || target.kind == NodeKind.Name)
                {
                    b.raw_slots.at_mut(target.symbol) = 0; b.written_symbols.at_mut(target.symbol) = 1;
                }
            }
            if (n.kind == NodeKind.Assign || n.kind == NodeKind.Update)
            {
                Node target = get_node(n.left, c);
                if (target.kind == NodeKind.Name)
                {
                    b.written_symbols.at_mut(target.symbol) = 1;
                    if (!il_defined(i, c)) { b.raw_slots.at_mut(target.symbol) = 0; }
                    else if (b.raw_slot(target.symbol) == -6 && !il_int_result(i, c)) { b.raw_slots.at_mut(target.symbol) = -3; }
                }
            }
        }
        // Unchanged integer literals retain their sign proof at every use.
        for (int i = 1; i <= c.node_store.count(); i++)
        {
            Node n = get_node(i, c);
            if (n.kind == NodeKind.Variable && n.left != 0 && b.raw_slot(n.symbol) == -6 && b.written_symbols.at(n.symbol) == 0)
            {
                Node value = get_node(n.left, c);
                if (value.kind == NodeKind.Number && number(get_token(value.token, c).text, c) >= 0) { b.nonnegative_ints.at_mut(n.symbol) = 1; }
            }
        }
    }
    internal static void il_runtime(IlBackend b)
    {
        b.ref_set(RuntimeMember.ObjectType, b.plan.reference("System.Runtime", "System", "Object"));
        b.ref_set(RuntimeMember.DoubleType, b.plan.reference("System.Runtime", "System", "Double"));
        b.ref_set(RuntimeMember.BooleanType, b.plan.reference("System.Runtime", "System", "Boolean"));
        b.ref_set(RuntimeMember.StringType, b.plan.reference("System.Runtime", "System", "String"));
        b.ref_set(RuntimeMember.JsType, b.plan.reference("Tsnet.Runtime", "", "Js"));
        b.ref_set(RuntimeMember.DynamicObjectType, b.plan.reference("Tsnet.Runtime", "", "JsDynamicObject"));
        b.ref_set(RuntimeMember.DynamicObjectConstructor,b.plan.method0(6,b.RuntimeToken(RuntimeMember.DynamicObjectType),1,-1,".ctor"));
        b.ref_set(RuntimeMember.TsObjectType, b.plan.reference("Tsnet.Runtime", "", "TsObject"));
        int object_type = b.RuntimeToken(RuntimeMember.ObjectType);
        b.ref_set(RuntimeMember.ObjectConstructor, b.plan.method0(6, object_type, 1, -1, ".ctor"));
        int js = b.RuntimeToken(RuntimeMember.JsType); int obj = b.RuntimeToken(RuntimeMember.TsObjectType);
        b.ref_set(RuntimeMember.BuiltinNamespace,b.plan.method1(6,js,0,-5,-4,"BuiltinNamespace"));
        Vector<int> restParameters=new Vector<int>();restParameters.push(-5);restParameters.push(-5);restParameters.push(-7);
        b.ref_set(RuntimeMember.CopyObjectRest,b.plan.method(6,js,0,-1,restParameters.view(),"CopyObjectRest"));
        b.ref_set(RuntimeMember.ResolveThis,b.plan.method1(6,js,0,-5,-5,"ResolveThis"));
        b.ref_set(RuntimeMember.NewTarget,b.plan.method1(6,js,0,-5,-5,"NewTarget"));
        b.ref_set(RuntimeMember.EnableNewTarget,b.plan.method1(6,js,0,-5,-5,"EnableNewTarget"));
        b.ref_set(RuntimeMember.Arguments,b.plan.method1(6,js,0,-5,-5,"Arguments"));
        b.ref_set(RuntimeMember.EnableArguments,b.plan.method1(6,js,0,-5,-5,"EnableArguments"));
        Vector<int> superBase=new Vector<int>();superBase.push(-5);superBase.push(-5);superBase.push(-2);
        b.ref_set(RuntimeMember.SuperBase,b.plan.method(6,js,0,-5,superBase.view(),"SuperBase"));
        b.ref_set(RuntimeMember.SuperCall,b.plan.method2(6,js,0,-5,-5,-5,"SuperCall"));
        b.ref_set(RuntimeMember.SuperInitializer,b.plan.method2(6,js,0,-1,-5,-5,"SuperInitializer"));

        int classInstance=b.plan.reference("Tsnet.Runtime","","JsClassInstance");
        b.ref_set(RuntimeMember.ClassInstanceType,classInstance);
        b.ref_set(RuntimeMember.ClassInstanceConstructor,b.plan.method0(6,classInstance,1,-1,".ctor"));
        Vector<int> classArgs=new Vector<int>();classArgs.push(-4);classArgs.push(-5);classArgs.push(-5);classArgs.push(-5);classArgs.push(-5);classArgs.push(-2);
        b.ref_set(RuntimeMember.CreateClass,b.plan.method(6,js,0,-5,classArgs.view(),"CreateClass"));
        Vector<int> methodArgs=new Vector<int>();methodArgs.push(-5);methodArgs.push(-4);methodArgs.push(-5);methodArgs.push(-2);methodArgs.push(-6);
        b.ref_set(RuntimeMember.DefineClassMethod,b.plan.method(6,js,0,-1,methodArgs.view(),"DefineClassMethod"));
        Vector<int> accessorArgs=new Vector<int>();accessorArgs.push(-5);accessorArgs.push(-4);accessorArgs.push(-5);accessorArgs.push(-6);
        b.ref_set(RuntimeMember.DefineAccessor,b.plan.method(6,js,0,-1,accessorArgs.view(),"DefineAccessor"));
        b.ref_set(RuntimeMember.InitializeClass,b.plan.method2(6,js,0,-1,-5,-5,"InitializeClass"));
        Vector<int> fieldArgs=new Vector<int>();fieldArgs.push(-5);fieldArgs.push(-4);fieldArgs.push(-5);
        Vector<int> privateArgs = new(); privateArgs.push(-5); privateArgs.push(-4); privateArgs.push(-6); privateArgs.push(-5); privateArgs.push(-5);
        b.ref_set(RuntimeMember.DefinePrivateMember, b.plan.method(6, js, 0, -5, privateArgs.view(), "DefinePrivateMember"));
        Vector<int> privateInit = new(); privateInit.push(-5); privateInit.push(-5); privateInit.push(-5); privateInit.push(-6); privateInit.push(-2);
        b.ref_set(RuntimeMember.InitializePrivateMember, b.plan.method(6, js, 0, -1, privateInit.view(), "InitializePrivateMember"));
        b.ref_set(RuntimeMember.PrivateGet, b.plan.method2(6, js, 0, -5, -5, -5, "PrivateGet"));
        Vector<int> privateSet = new(); privateSet.push(-5); privateSet.push(-5); privateSet.push(-5);
        b.ref_set(RuntimeMember.PrivateSet, b.plan.method(6, js, 0, -5, privateSet.view(), "PrivateSet"));
        b.ref_set(RuntimeMember.PrivateHas, b.plan.method2(6, js, 0, -2, -5, -5, "PrivateHas"));
        Vector<int> autoArgs = new(); autoArgs.push(-5); autoArgs.push(-4); autoArgs.push(-2);
        b.ref_set(RuntimeMember.DefineAutoAccessor, b.plan.method(6, js, 0, -5, autoArgs.view(), "DefineAutoAccessor"));
        Vector<int> autoInitArgs = new(); autoInitArgs.push(-5); autoInitArgs.push(-5); autoInitArgs.push(-5); autoInitArgs.push(-2);
        b.ref_set(RuntimeMember.InitializeAutoAccessor, b.plan.method(6, js, 0, -1, autoInitArgs.view(), "InitializeAutoAccessor"));
        b.ref_set(RuntimeMember.DefineClassField,b.plan.method(6,js,0,-1,fieldArgs.view(),"DefineClassField"));

        b.ref_set(RuntimeMember.NamePropertyFunction, b.plan.method2(6, js, 0, -5, -5, -4, "NamePropertyFunction"));
        b.ref_set(RuntimeMember.PropertyKey, b.plan.method1(6, js, 0, -4, -5, "PropertyKey"));
        b.ref_set(RuntimeMember.EncodeNumberField, b.plan.method1(6, js, 0, -3, -10, "EncodeNumberField"));
        b.ref_set(RuntimeMember.DecodeNumberField, b.plan.method1(6, js, 0, -10, -3, "DecodeNumberField"));
        b.ref_set(RuntimeMember.IsExactSpread, b.plan.method1(6, js, 2, -2, -5, "IsExactSpread"));
        b.ref_set(RuntimeMember.CopySpreadExtras, b.plan.method1(6, obj, 1, -1, -5, "CopySpreadExtras"));
        b.ref_set(RuntimeMember.AsyncAwaitContinue, b.plan.method2(6, js, 0, -5, -5, -5, "AsyncAwaitContinue"));
        b.ref_set(RuntimeMember.BeginDestructuring, b.plan.method2(6, js, 0, -5, -5, -2, "BeginDestructuring"));
        b.ref_set(RuntimeMember.ReadDestructuring, b.plan.method2(6, js, 0, -5, -5, -5, "ReadDestructuring"));
        b.ref_set(RuntimeMember.RestDestructuring, b.plan.method2(6, js, 0, -5, -5, -5, "RestDestructuring"));
        b.ref_set(RuntimeMember.AsyncAwait, b.plan.method1(6, js, 0, -5, -5, "AsyncAwait"));
        b.ref_set(RuntimeMember.AsyncResume, b.plan.method1(6, js, 0, -5, -5, "AsyncResume"));
        b.ref_set(RuntimeMember.BeginAsyncIteration, b.plan.method2(6, js, 0, -5, -5, -2, "BeginAsyncIteration"));
        b.ref_set(RuntimeMember.NextAsyncIteration, b.plan.method1(6, js, 0, -2, -5, "NextAsyncIteration"));
        b.ref_set(RuntimeMember.AsyncIterationValue, b.plan.method1(6, js, 0, -5, -5, "AsyncIterationValue"));
        b.ref_set(RuntimeMember.AsyncFunction, b.plan.method1(6, js, 0, -5, -5, "AsyncFunction"));
        b.ref_set(RuntimeMember.MarkFunctionPrototype, b.plan.method2(6, js, 0, -5, -5, -5, "MarkFunctionPrototype"));
        b.ref_set(RuntimeMember.In, b.plan.method2(6, js, 0, -2, -5, -5, "In"));
        b.ref_set(RuntimeMember.InNumber, b.plan.method2(6, js, 0, -2, -10, -5, "InNumber"));
        b.ref_set(RuntimeMember.SealModuleNamespace, b.plan.method1(6, js, 0, -1, -5, "SealModuleNamespace"));
        b.ref_set(RuntimeMember.InitializeEnum, b.plan.method1(6, js, 0, -1, -5, "InitializeEnum"));
        b.ref_set(RuntimeMember.ObjectSpread, b.plan.method2(6, js, 0, -1, -5, -5, "ObjectSpread"));
        b.ref_set(RuntimeMember.DefineOwn, b.plan.method2(6, obj, 1, -1, -4, -5, "DefineOwn"));
        b.ref_set(RuntimeMember.DeleteOwn, b.plan.method1(6, obj, 1, -2, -4, "DeleteOwn"));
        b.ref_set(RuntimeMember.MarkPresent, b.plan.method1(6, obj, 1, -1, -4, "MarkPresent"));
        b.ref_set(RuntimeMember.TryCloneSpread, b.plan.method1(6, js, 2, -5, -5, "TryCloneSpread"));
        b.ref_set(RuntimeMember.OwnKeysBase, b.plan.method0(6, obj, 1, -5, "OwnKeys"));
        b.ref_set(RuntimeMember.ObjectKeyCount, b.plan.method1(6, js, 0, -3, -5, "ObjectKeyCount"));
        b.ref_set(RuntimeMember.KeyCountWithFixed, b.plan.method1(6, obj, 1, -3, -6, "KeyCountWithFixed"));
        b.ref_set(RuntimeMember.OwnValuesBase, b.plan.method0(6, obj, 1, -5, "OwnValues"));
        b.ref_set(RuntimeMember.OwnEntriesBase, b.plan.method0(6, obj, 1, -5, "OwnEntries"));
        b.ref_set(RuntimeMember.OwnNumberValuesBase, b.plan.method0(6, obj, 1, -5, "OwnNumberValues"));
        b.ref_set(RuntimeMember.NumberValuesFromFields, b.plan.method1(6, js, 0, -5, -12, "NumberValuesFromFields"));
        b.ref_set(RuntimeMember.ObjectValues, b.plan.method1(6, js, 0, -5, -5, "ObjectValues"));
        b.ref_set(RuntimeMember.ObjectNumberValues, b.plan.method1(6, js, 0, -5, -5, "ObjectNumberValues"));
        b.ref_set(RuntimeMember.ObjectEntries, b.plan.method1(6, js, 0, -5, -5, "ObjectEntries"));
        b.ref_set(RuntimeMember.ObjectValuesFunction, b.plan.method0(6, js, 0, -5, "GetObjectValuesFunction"));
        b.ref_set(RuntimeMember.ObjectEntriesFunction, b.plan.method0(6, js, 0, -5, "GetObjectEntriesFunction"));
        b.ref_set(RuntimeMember.ObjectKeys, b.plan.method1(6, js, 0, -5, -5, "ObjectKeys"));
        b.ref_set(RuntimeMember.ObjectKeysFunction, b.plan.method0(6, js, 0, -5, "GetObjectKeysFunction"));
        b.ref_set(RuntimeMember.KeysFromFixed, b.plan.method1(6, obj, 1, -5, -7, "KeysFromFixed"));
        b.ref_set(RuntimeMember.KeysFromTextFields, b.plan.method1(6, obj, 1, -5, -7, "KeysFromTextFields"));
        int[] integer_loop_parameters = { -10, -3, -6, -6 };
        b.ref_set(RuntimeMember.GuardIntegerLoop, b.plan.method(6, js, 0, -2, integer_loop_parameters, "GuardIntegerLoop"));
        b.ref_set(RuntimeMember.NumberPredicate, b.plan.method2(6, js, 0, -2, -5, -6, "NumberPredicate"));
        b.ref_set(RuntimeMember.NumberPredicateNumber, b.plan.method2(6, js, 0, -2, -10, -6, "NumberPredicateNumber"));
        b.ref_set(RuntimeMember.NumberPredicateFunction, b.plan.method1(6, js, 0, -5, -6, "NumberPredicateFunction"));

        int tagged = b.plan.reference("Tsnet.Runtime", "", "TaggedArray");
        b.ref_set(RuntimeMember.TaggedConstructor, b.plan.method1(6, tagged, 1, -1, -6, ".ctor"));
        b.ref_set(RuntimeMember.TaggedBufferConstructor, b.plan.method1(6, tagged, 1, -1, -7, ".ctor"));
        b.ref_set(RuntimeMember.ArrayTryTags, b.plan.method2(6, js, 0, -13, -5, -3, "ArrayTryTags"));
        b.ref_set(RuntimeMember.ArrayIsNumberAt, b.plan.method2(6, js, 0, -2, -5, -6, "ArrayIsNumberAt"));
        int[] tagged_parameters = { -5, -6, -10 };
        b.ref_set(RuntimeMember.ArraySetTaggedNumberAt, b.plan.method(6, js, 0, -1, tagged_parameters, "ArraySetTaggedNumberAt"));

        int function_body = b.plan.reference("Tsnet.Runtime", "", "FunctionBody");
        int function_type = b.plan.reference("Tsnet.Runtime", "", "JsFunction");
        b.ref_set(RuntimeMember.FunctionType, function_type);
        int number0 = b.plan.reference("Tsnet.Runtime", "", "NumberFunction0");
        int number1 = b.plan.reference("Tsnet.Runtime", "", "NumberFunction1");
        int number2 = b.plan.reference("Tsnet.Runtime", "", "NumberFunction2");
        int raw0 = b.plan.reference("Tsnet.Runtime", "", "RawNumberFunction0");
        int raw1 = b.plan.reference("Tsnet.Runtime", "", "RawNumberFunction1");
        int raw2 = b.plan.reference("Tsnet.Runtime", "", "RawNumberFunction2");
        b.ref_set(RuntimeMember.FunctionBodyConstructor, b.plan.method2(6, function_body, 1, -1, -5, -9, ".ctor"));
        b.ref_set(RuntimeMember.NumberFunction0Constructor, b.plan.method2(6, number0, 1, -1, -5, -9, ".ctor"));
        b.ref_set(RuntimeMember.NumberFunction1Constructor, b.plan.method2(6, number1, 1, -1, -5, -9, ".ctor"));
        b.ref_set(RuntimeMember.NumberFunction2Constructor, b.plan.method2(6, number2, 1, -1, -5, -9, ".ctor"));
        b.ref_set(RuntimeMember.RawNumberFunction0Constructor, b.plan.method2(6, raw0, 1, -1, -5, -9, ".ctor"));
        b.ref_set(RuntimeMember.RawNumberFunction1Constructor, b.plan.method2(6, raw1, 1, -1, -5, -9, ".ctor"));
        b.ref_set(RuntimeMember.RawNumberFunction2Constructor, b.plan.method2(6, raw2, 1, -1, -5, -9, ".ctor"));
        Vector<int> function_parameters = new Vector<int>(); function_parameters.push(-5); function_parameters.push(-5); function_parameters.push(-4); function_parameters.push(-6);
        b.ref_set(RuntimeMember.FunctionConstructor, b.plan.method(6, function_type, 1, -1, function_parameters.view(), ".ctor"));
        int receiverFunction = b.plan.reference("Tsnet.Runtime", "", "JsReceiverFunction");
        b.ref_set(RuntimeMember.ReceiverFunctionConstructor,b.plan.method(6,receiverFunction,1,-1,function_parameters.view(),".ctor"));
        b.ref_set(RuntimeMember.FunctionArgument, b.plan.method2(6, js, 0, -5, -7, -6, "FunctionArgument"));
        Vector<int> function_rest = new Vector<int>(); function_rest.push(-7); function_rest.push(-6); function_rest.push(-2);
        b.ref_set(RuntimeMember.FunctionRest, b.plan.method(6, js, 0, -5, function_rest.view(), "FunctionRest"));
        b.ref_set(RuntimeMember.GetErrorConstructor,b.plan.method1(6,js,0,-5,-4,"GetErrorConstructor"));
        b.ref_set(RuntimeMember.Construct,b.plan.method2(6,js,0,-5,-5,-7,"Construct"));
        b.ref_set(RuntimeMember.ConstructArguments,b.plan.method2(6,js,0,-5,-5,-5,"ConstructArguments"));
        b.ref_set(RuntimeMember.InstanceOf,b.plan.method2(6,js,0,-2,-5,-5,"InstanceOf"));
        int exception=b.plan.reference("System.Runtime","System","Exception");
        b.ref_set(RuntimeMember.ExceptionType,exception);
        b.ref_set(RuntimeMember.MakeThrown,b.plan.method1(6,js,0,exception,-5,"MakeThrown"));
        b.ref_set(RuntimeMember.CaughtValue,b.plan.method1(6,js,0,-5,exception,"CaughtValue"));
        int[] string_search_parameters={-5,-5,-5,-10,-6};
        b.ref_set(RuntimeMember.CallStringSearch,b.plan.method(6,js,0,-5,string_search_parameters,"CallStringSearch"));
        b.ref_set(RuntimeMember.CallStringSearchNumber,b.plan.method(6,js,0,-10,string_search_parameters,"CallStringSearchNumber"));
        b.ref_set(RuntimeMember.CallStringSearchBoolean,b.plan.method(6,js,0,-11,string_search_parameters,"CallStringSearchBoolean"));
        b.ref_set(RuntimeMember.StringIndexAt,b.plan.method2(6,js,0,-5,-5,-6,"StringIndexAt"));
        b.ref_set(RuntimeMember.StringIndexNumber,b.plan.method2(6,js,0,-5,-5,-10,"StringIndexNumber"));
        b.ref_set(RuntimeMember.RelativeAtTarget,b.plan.method1(6,js,0,-5,-5,"RelativeAtTarget"));
        int[] at_index_parameters={-5,-5,-10};
        b.ref_set(RuntimeMember.CallMethodIndex,b.plan.method(6,js,0,-5,at_index_parameters,"CallMethodIndex"));
        b.ref_set(RuntimeMember.CallMethod0,b.plan.method2(6,js,0,-5,-5,-5,"CallMethod0"));
        int[] method_parameters={-5,-5,-7};
        b.ref_set(RuntimeMember.CallMethod,b.plan.method(6,js,0,-5,method_parameters,"CallMethod"));
        int[] spread_method_parameters={-5,-5,-5};
        b.ref_set(RuntimeMember.CallMethodArguments,b.plan.method(6,js,0,-5,spread_method_parameters,"CallMethodArguments"));
        b.ref_set(RuntimeMember.CallMethodNumber0,b.plan.method2(6,js,0,-10,-5,-5,"CallMethodNumber0"));
        b.ref_set(RuntimeMember.CallMethodNumber1,b.plan.method(6,js,0,-10,at_index_parameters,"CallMethodNumber1"));
        int[] method_number2_parameters={-5,-5,-10,-10};
        b.ref_set(RuntimeMember.CallMethodNumber2,b.plan.method(6,js,0,-10,method_number2_parameters,"CallMethodNumber2"));
        b.ref_set(RuntimeMember.CallMethodIndices,b.plan.method(6,js,0,-5,method_number2_parameters,"CallMethodIndices"));
        b.ref_set(RuntimeMember.CallFunction0, b.plan.method1(6, js, 0, -5, -5, "CallFunction0"));
        b.ref_set(RuntimeMember.CallFunction, b.plan.method2(6, js, 0, -5, -5, -7, "CallFunction"));
        b.ref_set(RuntimeMember.CallFunctionArguments, b.plan.method2(6, js, 0, -5, -5, -5, "CallFunctionArguments"));
        b.ref_set(RuntimeMember.CallFunctionNumber0, b.plan.method1(6, js, 0, -10, -5, "CallFunctionNumber0"));
        b.ref_set(RuntimeMember.CallFunctionNumber1, b.plan.method2(6, js, 0, -10, -5, -10, "CallFunctionNumber1"));
        Vector<int> function_number2 = new Vector<int>(); function_number2.push(-5); function_number2.push(-10); function_number2.push(-10);
        b.ref_set(RuntimeMember.CallFunctionNumber2, b.plan.method(6, js, 0, -10, function_number2.view(), "CallFunctionNumber2"));

        b.ref_set(RuntimeMember.HasOwn, b.plan.method2(6, js, 0, -2, -5, -5, "HasOwn"));
        b.ref_set(RuntimeMember.HasOwnNumber, b.plan.method2(6, js, 0, -2, -5, -10, "HasOwnNumber"));
        b.ref_set(RuntimeMember.HasOwnIndex, b.plan.method2(6, js, 0, -2, -5, -6, "HasOwnIndex"));
        b.ref_set(RuntimeMember.HasOwnBase, b.plan.method1(6, obj, 1, -2, -4, "HasOwn"));
        b.ref_set(RuntimeMember.WrapNumber, b.plan.method1(6, js, 0, -10, -3, "WrapNumber"));
        b.ref_set(RuntimeMember.WrapBoolean, b.plan.method1(6, js, 0, -11, -2, "WrapBoolean"));
        b.ref_set(RuntimeMember.EmptyNumber, b.plan.method0(6, js, 0, -10, "EmptyNumber"));
        b.ref_set(RuntimeMember.EmptyBoolean, b.plan.method0(6, js, 0, -11, "EmptyBoolean"));
        b.ref_set(RuntimeMember.BoxNumber, b.plan.method1(6, js, 0, -5, -10, "BoxNumber"));
        b.ref_set(RuntimeMember.BoxBoolean, b.plan.method1(6, js, 0, -5, -11, "BoxBoolean"));
        b.ref_set(RuntimeMember.NumberValue, b.plan.method1(6, js, 0, -3, -10, "NumberValue"));
        b.ref_set(RuntimeMember.BooleanValue, b.plan.method1(6, js, 0, -2, -11, "BooleanValue"));
        b.ref_set(RuntimeMember.NumberMissing, b.plan.method1(6, js, 0, -2, -10, "NumberMissing"));
        b.ref_set(RuntimeMember.BooleanMissing, b.plan.method1(6, js, 0, -2, -11, "BooleanMissing"));
        b.ref_set(RuntimeMember.NumberTruthy, b.plan.method1(6, js, 0, -2, -10, "NumberTruthy"));
        b.ref_set(RuntimeMember.Constructor, b.plan.method0(6, obj, 1, -1, ".ctor"));
        b.ref_set(RuntimeMember.Get, b.plan.method1(6, obj, 1, -5, -4, "Get"));
        b.ref_set(RuntimeMember.Set, b.plan.method2(6, obj, 1, -5, -4, -5, "Set"));
        b.ref_set(RuntimeMember.Cast, b.plan.method2(6, js, 2, -8, -5, -4, "Cast"));
        b.ref_set(RuntimeMember.Check, b.plan.method2(6, js, 2, -5, -5, -4, "Check"));
        b.ref_set(RuntimeMember.Add, b.plan.method2(6, js, 0, -5, -5, -5, "Add"));
        b.ref_set(RuntimeMember.NumberAtIsNullish, b.plan.method2(6, js, 0, -2, -5, -6, "NumberAtIsNullish"));
        b.ref_set(RuntimeMember.LooseEqual, b.plan.method2(6, js, 0, -2, -5, -5, "LooseEqual"));
        b.ref_set(RuntimeMember.LooseEqualNumber, b.plan.method2(6, js, 0, -2, -10, -5, "LooseEqualNumber"));
        b.ref_set(RuntimeMember.Equal, b.plan.method2(6, js, 0, -2, -5, -5, "Equal"));
        b.ref_set(RuntimeMember.EqualNumbers, b.plan.method2(6, js, 0, -2, -10, -10, "EqualNumbers"));
        b.ref_set(RuntimeMember.Remainder, b.plan.method2(6, js, 0, -3, -3, -3, "Remainder"));
        int[] relational_parameters = { -5, -5, -6 };
        b.ref_set(RuntimeMember.Relational, b.plan.method(6, js, 0, -2, relational_parameters, "Relational"));
        b.ref_set(RuntimeMember.StringRelational, b.plan.method(6, js, 0, -2, relational_parameters, "StringRelational"));
        b.ref_set(RuntimeMember.Power, b.plan.method2(6, js, 0, -3, -3, -3, "Power"));
        b.ref_set(RuntimeMember.ToInt32, b.plan.method1(6, js, 0, -6, -3, "ToInt32"));
        int array = b.plan.reference("Tsnet.Runtime", "", "JsArray"); b.ref_set(RuntimeMember.ArrayType, array);
        int numbers = b.plan.reference("Tsnet.Runtime", "", "NumberArray"); b.ref_set(RuntimeMember.NumberArrayType, numbers);
        b.ref_set(RuntimeMember.ArrayConstructor, b.plan.method1(6, array, 1, -1, -6, ".ctor"));
        b.ref_set(RuntimeMember.ArrayBufferConstructor, b.plan.method1(6, array, 1, -1, -7, ".ctor"));
        int reversible = b.plan.reference("Tsnet.Runtime", "", "ReversibleArray");
        b.ref_set(RuntimeMember.ReversibleConstructor, b.plan.method1(6, reversible, 1, -1, -6, ".ctor"));
        b.ref_set(RuntimeMember.ReversibleBufferConstructor, b.plan.method1(6, reversible, 1, -1, -7, ".ctor"));
        b.ref_set(RuntimeMember.ArrayHoleValue, b.plan.method0(6, array, 0, -5, "HoleValue"));
        b.ref_set(RuntimeMember.NumberArrayConstructor, b.plan.method1(6, numbers, 1, -1, -6, ".ctor"));
        b.ref_set(RuntimeMember.IndexGet, b.plan.method2(6, js, 0, -5, -5, -5, "IndexGet"));
        Vector<int> index_parameters = new Vector<int>(); index_parameters.push(-5); index_parameters.push(-5); index_parameters.push(-5);
        b.ref_set(RuntimeMember.IndexSet, b.plan.method(6, js, 0, -1, index_parameters.view(), "IndexSet"));
        b.ref_set(RuntimeMember.NumberIndexGet, b.plan.method2(6, js, 0, -10, -5, -3, "ArrayGetNumber"));
        Vector<int> number_index_parameters = new Vector<int>(); number_index_parameters.push(-5); number_index_parameters.push(-3); number_index_parameters.push(-10);
        b.ref_set(RuntimeMember.NumberIndexSet, b.plan.method(6, js, 0, -1, number_index_parameters.view(), "ArraySetNumber"));
        b.ref_set(RuntimeMember.NumberIndexGetAt, b.plan.method2(6, js, 0, -10, -5, -6, "ArrayGetNumberAt"));
        Vector<int> integer_index_parameters = new Vector<int>(); integer_index_parameters.push(-5); integer_index_parameters.push(-6); integer_index_parameters.push(-10);
        b.ref_set(RuntimeMember.NumberIndexSetAt, b.plan.method(6, js, 0, -1, integer_index_parameters.view(), "ArraySetNumberAt"));
        b.ref_set(RuntimeMember.ArrayLength, b.plan.method1(6, js, 0, -3, -5, "ArrayLength"));
        b.ref_set(RuntimeMember.ArraySetLength, b.plan.method2(6, js, 0, -1, -5, -3, "ArraySetLength"));
        Vector<int> push_hint_parameters = new Vector<int>(); push_hint_parameters.push(-5); push_hint_parameters.push(-5); push_hint_parameters.push(-3);
        b.ref_set(RuntimeMember.ArrayPushOneReserved, b.plan.method(6, js, 0, -3, push_hint_parameters.view(), "ArrayPushOneReserved"));
        b.ref_set(RuntimeMember.ArrayPushOne, b.plan.method2(6, js, 0, -3, -5, -5, "ArrayPushOne"));
        b.ref_set(RuntimeMember.ArrayPush, b.plan.method2(6, js, 0, -3, -5, -7, "ArrayPush"));
        b.ref_set(RuntimeMember.ArrayAppend, b.plan.method2(6, js, 0, -1, -5, -5, "ArrayAppend"));
        b.ref_set(RuntimeMember.ArrayHole, b.plan.method1(6, js, 0, -1, -5, "ArrayHole"));
        Vector<int> array_set_parameters = new Vector<int>(); array_set_parameters.push(-5); array_set_parameters.push(-6); array_set_parameters.push(-5);
        b.ref_set(RuntimeMember.ArrayReserveCapacity, b.plan.method2(6, js, 0, -1, -5, -3, "ArrayReserveCapacity"));
        b.ref_set(RuntimeMember.ArrayTryDense, b.plan.method2(6, js, 0, -7, -5, -3, "ArrayTryDense"));
        b.ref_set(RuntimeMember.ArrayDecode, b.plan.method1(6, js, 0, -5, -5, "ArrayDecode"));
        b.ref_set(RuntimeMember.ArrayIndexGetAt, b.plan.method2(6, js, 0, -5, -5, -6, "ArrayGetAt"));
        b.ref_set(RuntimeMember.ArrayIndexSetAt, b.plan.method(6, js, 0, -1, array_set_parameters.view(), "ArraySetAt"));
        b.ref_set(RuntimeMember.ArraySetTaggedReferenceAt, b.plan.method(6, js, 0, -1, array_set_parameters.view(), "ArraySetTaggedReferenceAt"));
        Vector<int> spread_parameters = new Vector<int>(); spread_parameters.push(-5); spread_parameters.push(-5); spread_parameters.push(-6);
        b.ref_set(RuntimeMember.ArraySpread, b.plan.method(6, js, 0, -1, spread_parameters.view(), "ArraySpread"));
        b.ref_set(RuntimeMember.LogArguments, b.plan.method1(6, js, 0, -1, -5, "LogArguments"));
        b.ref_set(RuntimeMember.PushArguments, b.plan.method2(6, js, 0, -3, -5, -5, "PushArguments"));
        Vector<int> call_rest_parameters = new Vector<int>(); call_rest_parameters.push(-5); call_rest_parameters.push(-6); call_rest_parameters.push(-2);
        b.ref_set(RuntimeMember.CallRest, b.plan.method(6, js, 0, -5, call_rest_parameters.view(), "CallRest"));
        b.ref_set(RuntimeMember.ArrayPop, b.plan.method1(6, js, 0, -5, -5, "ArrayPop"));
        b.ref_set(RuntimeMember.ArrayReverse, b.plan.method2(6, js, 0, -5, -5, -2, "ArrayReverse"));
        Vector<int> array_builtin_parameters = new Vector<int>(); array_builtin_parameters.push(-5); array_builtin_parameters.push(-5); array_builtin_parameters.push(-4);
        b.ref_set(RuntimeMember.IsArrayBuiltin, b.plan.method(6, js, 0, -2, array_builtin_parameters.view(), "IsArrayBuiltin"));
        b.ref_set(RuntimeMember.ArrayReverseBuiltin, b.plan.method1(6, js, 0, -2, -5, "ArrayReverseBuiltin"));
        Vector<int> slice_parameters = new Vector<int>(); slice_parameters.push(-5); slice_parameters.push(-5); slice_parameters.push(-5);
        b.ref_set(RuntimeMember.Slice, b.plan.method(6, js, 0, -5, slice_parameters.view(), "Slice"));
        Vector<int> search_parameters = new Vector<int>(); search_parameters.push(-5); search_parameters.push(-5); search_parameters.push(-5); search_parameters.push(-2);
        b.ref_set(RuntimeMember.ArraySearch, b.plan.method(6, js, 0, -3, search_parameters.view(), "ArraySearch"));
        search_parameters.at_mut(1) = -10;
        b.ref_set(RuntimeMember.ArraySearchNumber, b.plan.method(6, js, 0, -3, search_parameters.view(), "ArraySearchNumber"));
        b.ref_set(RuntimeMember.NumberPush, b.plan.method2(6, js, 0, -3, -5, -10, "ArrayPushNumber"));
        b.ref_set(RuntimeMember.NumberPop, b.plan.method1(6, js, 0, -10, -5, "ArrayPopNumber"));
        b.ref_set(RuntimeMember.ArrayTryPacked, b.plan.method2(6, js, 0, -12, -5, -3, "ArrayTryPacked"));
        b.ref_set(RuntimeMember.ArrayReservePushes, b.plan.method2(6, js, 0, -1, -5, -3, "ArrayReservePushes"));
        b.ref_set(RuntimeMember.CanonicalNumber, b.plan.method1(6, js, 0, -3, -3, "CanonicalNumber"));
        b.ref_set(RuntimeMember.ForInStart, b.plan.method1(6, js, 0, -5, -5, "ForInStart"));
        b.ref_set(RuntimeMember.ForInNext, b.plan.method1(6, js, 0, -5, -5, "ForInNext"));
        b.ref_set(RuntimeMember.ForOfLength, b.plan.method1(6, js, 0, -3, -5, "ForOfLength"));
        b.ref_set(RuntimeMember.ForOfValue, b.plan.method2(6, js, 0, -5, -5, -6, "ForOfValue"));
        b.ref_set(RuntimeMember.ForOfNumber, b.plan.method2(6, js, 0, -10, -5, -6, "ForOfNumber"));
        b.ref_set(RuntimeMember.ForOfStep, b.plan.method2(6, js, 0, -6, -5, -6, "ForOfStep"));
        b.ref_set(RuntimeMember.EqualBooleans, b.plan.method2(6, js, 0, -2, -11, -11, "EqualBooleans"));
        b.ref_set(RuntimeMember.Number, b.plan.method1(6, js, 0, -3, -5, "Number"));
        b.ref_set(RuntimeMember.Boolean, b.plan.method1(6, js, 0, -2, -5, "Boolean"));
        b.ref_set(RuntimeMember.Truthy, b.plan.method1(6, js, 0, -2, -5, "Truthy"));
        b.ref_set(RuntimeMember.Undefined, b.plan.method0(6, js, 0, -5, "GetUndefined"));
        b.ref_set(RuntimeMember.IsNumber, b.plan.method1(6, js, 0, -2, -5, "IsNumber"));
        b.ref_set(RuntimeMember.IsUndefined, b.plan.method1(6, js, 0, -2, -5, "IsUndefined"));
        b.ref_set(RuntimeMember.IsNullish, b.plan.method1(6, js, 0, -2, -5, "IsNullish"));
        b.ref_set(RuntimeMember.RequireObject, b.plan.method1(6, js, 0, -1, -5, "RequireObject"));
        b.ref_set(RuntimeMember.CheckInitialized, b.plan.method1(6, js, 0, -1, -2, "CheckInitialized"));
        b.ref_set(RuntimeMember.LoopLimit, b.plan.method1(6, js, 0, -6, -3, "LoopLimit"));
        b.ref_set(RuntimeMember.TypeOf, b.plan.method1(6, js, 0, -4, -5, "TypeOf"));
        b.ref_set(RuntimeMember.Text, b.plan.method1(6, js, 0, -4, -5, "Text"));
        b.ref_set(RuntimeMember.DynamicGet, b.plan.method2(6, js, 0, -5, -5, -4, "Get"));
        Vector<int> set_parameters = new Vector<int>(); set_parameters.push(-5); set_parameters.push(-4); set_parameters.push(-5);
        b.ref_set(RuntimeMember.DynamicSet, b.plan.method(6, js, 0, -5, set_parameters.view(), "Set"));
        b.ref_set(RuntimeMember.Log, b.plan.method1(6, js, 0, -1, -7, "ConsoleLog"));
        b.ref_set(RuntimeMember.Now, b.plan.method0(6, js, 0, -3, "PerformanceNow"));
        int string_type = b.RuntimeToken(RuntimeMember.StringType);
        b.ref_set(RuntimeMember.Length, b.plan.method1(6, js, 0, -6, -5, "StringLength"));
        b.ref_set(RuntimeMember.StringEqual, b.plan.method2(6, string_type, 0, -2, -4, -4, "op_Equality"));
        b.ref_set(RuntimeMember.Concat, b.plan.method2(6, string_type, 0, -4, -4, -4, "Concat"));
        int action_type = b.plan.reference("System.Runtime", "System", "Action");
        b.ref_set(RuntimeMember.ActionType, action_type);
        b.ref_set(RuntimeMember.ActionConstructor, b.plan.method2(6, action_type, 1, -1, -5, -9, ".ctor"));
        b.ref_set(RuntimeMember.Run, b.plan.method1(6, js, 0, -6, action_type, "Run"));
    }
    internal static void il_text(int text, Compiler c, IlBackend b)
    {
        Vector<byte> bytes = new Vector<byte>();
        for (int i = 0; i < text_size(text, c); i++) { bytes.push(text_byte(text, i, c)); }
        b.plan.raw_string(b.method, bytes.view());
    }
    internal static void il_optional_members(int type, int klass, Compiler c, IlBackend b)
    {
        Vector<int> emitted = new Vector<int>(); int target = c.types;
        while (target != 0)
        {
            bool assigned = il_assignment_fits(type,target,c,b);
            if (fits(type, target, c) || assigned)
            {
                int member = get_type(target, c).fields;
                while (member != 0)
                {
                    Field record = get_field(member, c); bool seen = false;
                    for (int i = 0; i < emitted.count(); i++)
                    {
                        Field previous = get_field(emitted.at(i), c);
                        if (b.type(previous.type, c) == b.type(record.type, c) && same(previous.name, record.name, c)) { seen = true; }
                    }
                    int actual = field(type, record.name, c);
                    bool missing = actual == 0 && (record.optional || assigned);
                    bool adapted = actual != 0 && b.type(get_field(actual, c).type, c) != b.type(record.type, c);
                    if ((missing || adapted) && !seen)
                    {
                        emitted.push(member); int value_type = b.type(record.type, c);
                        Vector<byte> get = field_name(record.name, "get_p_", c);
                        Vector<byte> set = field_name(record.name, "set_p_", c);
                        b.begin(b.plan.method0(5, klass, 0x1e6, value_type, get.view()));
                        if (adapted)
                        {
                            int original = get_field(actual, c).type;
                            b.emit(0xfe09, 0); b.load_field(actual, c); b.convert(original, record.type, c); b.op(0x2a);
                            b.begin(b.plan.method1(5, klass, 0x1e6, -1, value_type, set.view()));
                            b.emit(0xfe09, 0); b.emit(0xfe09, 1); b.convert(record.type, original, c); b.store_field(actual, c);
                            if (b.spread_types.at(type) != 0 && !il_dynamic_index_field(member,c)) { b.emit(0xfe09, 0); il_text(record.name, c, b); b.emit(0x6f, b.RuntimeToken(RuntimeMember.MarkPresent)); }
                            b.op(0x2a);
                        }
                        else
                        {
                            b.emit(0xfe09, 0); il_text(record.name, c, b); b.call(RuntimeMember.Get); if(!assigned || value_type!=-5)b.from_object(record.type, c); b.op(0x2a);
                            b.begin(b.plan.method1(5, klass, 0x1e6, -1, value_type, set.view()));
                            b.emit(0xfe09, 0); il_text(record.name, c, b); b.emit(0xfe09, 1); b.box(record.type, c);
                            b.call(RuntimeMember.Set); b.op(0x26); b.op(0x2a);
                        }
                    }
                    member = record.next;
                }
            }
            target = get_type(target, c).next;
        }
    }
    internal static void il_shapes(Compiler c, IlBackend b)
    {
        int type = c.types;
        while (type != 0)
        {
            if (get_type(type, c).kind == TypeKind.Shape)
            {
                Vector<byte> name = numbered_name('I', get_type(type, c).id);
                b.interfaces.at_mut(type) = b.plan.type(name.view(), 0, 0xa1);
            }
            type = get_type(type, c).next;
        }
        type = c.types;
        while (type != 0)
        {
            if (get_type(type, c).kind == TypeKind.Shape)
            {
                int member = get_type(type, c).fields;
                while (member != 0)
                {
                    Vector<byte> get = field_name(get_field(member, c).name, "get_p_", c);
                    Vector<byte> set = field_name(get_field(member, c).name, "set_p_", c);
                    int field_type = b.type(get_field(member, c).type, c);
                    int interface_type = b.interfaces.at(type);
                    b.getters.at_mut(member) = b.plan.method0(5, interface_type, 0x5c6, field_type, get.view());
                    b.setters.at_mut(member) = b.plan.method1(5, interface_type, 0x5c6, -1, field_type, set.view());
                    member = get_field(member, c).next;
                }
            }
            if (get_type(type, c).kind != TypeKind.Any && get_type(type, c).kind != TypeKind.Unknown && get_type(type, c).kind != TypeKind.Void)
            {
                int cast = b.RuntimeToken(RuntimeMember.Cast); int signature_type = b.type(type, c);
                TypeKind kind = get_type(type, c).kind;
                if (kind == TypeKind.String || kind == TypeKind.Shape || kind == TypeKind.Array)
                {
                    cast = b.RuntimeToken(RuntimeMember.Check);
                    // A class constructor can return a different JavaScript object.
                    // Its fields must not require the allocated CLR class layout.
                    signature_type = kind == TypeKind.String ? b.RuntimeToken(RuntimeMember.StringType) : kind == TypeKind.Shape ? get_type(type, c).name != 0 ? b.RuntimeToken(RuntimeMember.TsObjectType) : b.interfaces.at(type) : b.RuntimeToken(RuntimeMember.ArrayType);
                }
                b.casts.at_mut(type) = b.plan.specialize(cast, signature_type);
            }
            type = get_type(type, c).next;
        }
        type = c.types;
        while (type != 0)
        {
            if (get_type(type, c).kind == TypeKind.Shape)
            {
                if (get_type(type,c).name != 0) { il_class_shape(type,c,b); type=get_type(type,c).next; continue; }
                Vector<byte> name = numbered_name('O', get_type(type, c).id);
                int base_type = b.RuntimeToken(RuntimeMember.TsObjectType);
                int klass = b.plan.type(name.view(), base_type, b.layout_needed.at(type) != 0 || b.spread_types.at(type) != 0 ? 1 : 0x101);
                b.classes.at_mut(type) = klass;
                int target = c.types;
                while (target != 0)
                {
                    if (fits(type, target, c) || il_assignment_fits(type,target,c,b)) { int interface_type = b.interfaces.at(target); b.plan.implements(klass, interface_type); }
                    target = get_type(target, c).next;
                }
                int constructor = b.plan.method0(5, klass, 0x1886, -1, ".ctor");
                b.constructors.at_mut(type) = constructor;
                b.begin(constructor); b.emit(0xfe09, 0); b.call(RuntimeMember.Constructor);
                for(int numeric=get_type(type,c).fields;numeric!=0;numeric=get_field(numeric,c).next)
                    if(il_has_dynamic_indices(type,c)){b.emit(0xfe09,0);il_text(get_field(numeric,c).name,c,b);b.call(RuntimeMember.Undefined);b.call(RuntimeMember.DefineOwn);}
                b.op(0x2a);
                int member = get_type(type, c).fields;
                while (member != 0)
                {
                    Field record = get_field(member, c); int value_type = b.type(record.type, c);
                    Vector<byte> storage = field_name(record.name, "p_", c);
                    int storage_type = b.field_storage(record.type, c); int value = b.plan.field(klass, 6, storage_type, storage.view()); b.fields.at_mut(member) = value;
                    Vector<byte> get = field_name(record.name, "get_p_", c);
                    Vector<byte> set = field_name(record.name, "set_p_", c);
                    b.begin(b.plan.method0(5, klass, 0x1e6, value_type, get.view()));
                    b.emit(0xfe09, 0); b.load_field(member, c); b.op(0x2a);
                    b.begin(b.plan.method1(5, klass, 0x1e6, -1, value_type, set.view()));
                    b.emit(0xfe09, 0); b.emit(0xfe09, 1);
                    TypeKind member_kind = get_type(record.type, c).kind;
                    if (member_kind == TypeKind.String || member_kind == TypeKind.Shape || member_kind == TypeKind.Array) { b.from_object(record.type, c); }
                    b.store_field(member, c);
                    if (b.spread_types.at(type) != 0 && !il_dynamic_index_field(member,c)) { b.emit(0xfe09, 0); il_text(record.name, c, b); b.emit(0x6f, b.RuntimeToken(RuntimeMember.MarkPresent)); }
                    b.op(0x2a);
                    member = record.next;
                }
                b.begin(b.plan.method0(5, klass, 0xc6, -5, "OwnKeys"));
                b.emit(0xfe09, 0); int key_count = 0; bool text_keys = true; member = get_type(type, c).fields;
                while (member != 0) { if(!il_has_dynamic_indices(type,c)){key_count++; int nameLocal = get_field(member, c).name; if (text_size(nameLocal, c) > 0 && text_byte(nameLocal, 0, c) >= '0' && text_byte(nameLocal, 0, c) <= '9') { text_keys = false; }} member = get_field(member, c).next; }
                b.emit(0x20, key_count); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType)); member = get_type(type, c).fields; int key_index = 0;
                while (member != 0) { if(!il_has_dynamic_indices(type,c)){b.op(0x25); b.emit(0x20, key_index++); il_text(get_field(member, c).name, c, b); b.op(0xa2);} member = get_field(member, c).next; }
                b.call(text_keys ? RuntimeMember.KeysFromTextFields : RuntimeMember.KeysFromFixed); b.op(0x2a);
                b.begin(b.plan.method0(5, klass, 0xc6, -3, "OwnKeyCount"));
                b.emit(0xfe09, 0); b.emit(0x20, key_count); b.call(RuntimeMember.KeyCountWithFixed); b.op(0x2a);
                b.begin(b.plan.method1(5, klass, 0xc6, -2, -4, "HasOwn"));
                member = get_type(type, c).fields;
                while (member != 0)
                {
                    Field record = get_field(member, c); int next = b.label();
                    b.emit(0xfe09, 1); il_text(record.name, c, b); b.call(RuntimeMember.StringEqual); b.emit(0x39, next);
                    if(il_dynamic_index_field(member,c)){b.emit(0xfe09,0);b.emit(0xfe09,1);b.call(RuntimeMember.HasOwnBase);}else b.emit(0x20,1);
                    b.op(0x2a); b.mark(next); member = record.next;
                }
                b.emit(0xfe09, 0); b.emit(0xfe09, 1); b.call(RuntimeMember.HasOwnBase); b.op(0x2a);
                b.begin(b.plan.method1(5, klass, 0xc6, -5, -4, "Get"));
                member = get_type(type, c).fields;
                while (member != 0)
                {
                    Field record = get_field(member, c); int next = b.label();
                    b.emit(0xfe09, 1); il_text(record.name, c, b); b.call(RuntimeMember.StringEqual); b.emit(0x39, next);
                    b.emit(0xfe09, 0); b.load_field(member, c); b.box(record.type, c); b.op(0x2a); b.mark(next);
                    member = record.next;
                }
                b.emit(0xfe09, 0); b.emit(0xfe09, 1); b.call(RuntimeMember.Get); b.op(0x2a);
                b.begin(b.plan.method2(5, klass, 0xc6, -5, -4, -5, "Set"));
                member = get_type(type, c).fields;
                while (member != 0)
                {
                    Field record = get_field(member, c); int next = b.label();
                    b.emit(0xfe09, 1); il_text(record.name, c, b); b.call(RuntimeMember.StringEqual); b.emit(0x39, next);
                    b.emit(0xfe09, 0); b.emit(0xfe09, 2);
                    TypeKind value_kind = get_type(record.type, c).kind;
                    if (b.type(record.type, c) != -5 || value_kind == TypeKind.String || value_kind == TypeKind.Shape || value_kind == TypeKind.Array)
                    {
                        b.plan.StringLiteral(b.method, "property write violates static field type"); b.emit(0x28, (int)b.cast(record.type));
                    }
                    b.store_field(member, c);
                    if (b.spread_types.at(type) != 0 && !il_dynamic_index_field(member,c)) { b.emit(0xfe09, 0); il_text(record.name, c, b); b.emit(0x6f, b.RuntimeToken(RuntimeMember.MarkPresent)); }
                    b.emit(0xfe09, 2); b.op(0x2a); b.mark(next);
                    member = record.next;
                }
                b.emit(0xfe09, 0); b.emit(0xfe09, 1); b.emit(0xfe09, 2); b.call(RuntimeMember.Set); b.op(0x2a);
                il_optional_members(type, klass, c, b); il_enumeration_shape(type, klass, 0, c, b);
            }
            type = get_type(type, c).next;
        }
    }
}
