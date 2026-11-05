import "assembly-plan.cr";

enum class RuntimeMember {
    ObjectType, DoubleType, BooleanType, StringType, JsType, TsObjectType,
    Constructor, Get, Set, Cast, Check, Add, Equal, Number, Boolean, Text, DynamicGet, DynamicSet,
    Log, Now, Length, StringEqual, Concat, ActionType, ActionConstructor, Run, Truthy, Undefined, TypeOf, IsUndefined, IsNullish, EqualNumbers, EqualBooleans,
    WrapNumber, WrapBoolean, EmptyNumber, EmptyBoolean, BoxNumber, BoxBoolean, NumberValue, BooleanValue, NumberMissing, BooleanMissing, NumberTruthy, Remainder, ToInt32,
    ArrayType, NumberArrayType, ArrayConstructor, NumberArrayConstructor, IndexGet, IndexSet, NumberIndexGet, NumberIndexSet, NumberIndexGetAt, NumberIndexSetAt, ArrayLength, ArraySetLength, ArrayPush, ArrayPop, NumberPush, NumberPop, ArrayTryPacked, ArrayReservePushes, CanonicalNumber,
    ForOfLength, ForOfValue, ForOfNumber, ForOfStep, ArrayAppend, ArrayHole, ArraySpread, RequireObject, LoopLimit, ArrayIndexSetAt, LogArguments, PushArguments, CallRest, ArrayBufferConstructor, ArrayHoleValue, Count
};

Vector<int> zero_ids(size_t count) {
    Vector<int> result = Vector<int>::create();
    for (size_t i = 0; i < count; i++) { result.push(0); }
    return move(result);
}
Vector<int> empty_slots(size_t count) {
    Vector<int> result=Vector<int>::create();
    for (size_t i=0;i<count;i++) { result.push(-2147483647); }
    return move(result);
}

struct ScalarField {
    SymbolId root;
    TextId name;
    TypeId type;
    int slot;
};
struct IlBackend {
    AssemblyPlan plan;
    Vector<int> runtime;
    Vector<int> interfaces;
    Vector<int> classes;
    Vector<int> constructors;
    Vector<int> fields;
    Vector<int> getters;
    Vector<int> setters;
    Vector<int> casts;
    Vector<int> methods;
    Vector<int> binding_methods;
    Vector<int> slots;
    Vector<int> raw_slots;
    Vector<int> numeric_slots;
    Vector<int> numeric_methods;
    Vector<int> written_symbols;
    Vector<int> nonnegative_ints;
    Vector<int> node_parents;
    Vector<int> packed_arrays;
    Vector<int> iteration_vars;
    Vector<int> scalar_roots;
    Vector<ScalarField> scalar_fields;
    Vector<int> breaks;
    Vector<int> continues;
    int method;
    int local_count;
    int label_count;
    int loop_versions;
    static Self create(const Compiler& c) {
        size_t types = c.type_store.count() + 1;
        size_t fields = c.field_store.count() + 1;
        size_t symbols = c.symbol_store.count() + 1;
        return (Self){.plan=AssemblyPlan::create(), .runtime=zero_ids((size_t)RuntimeMember::Count),
            .interfaces=zero_ids(types), .classes=zero_ids(types), .constructors=zero_ids(types),
            .fields=zero_ids(fields), .getters=zero_ids(fields), .setters=zero_ids(fields),
            .casts=zero_ids(types), .methods=zero_ids(symbols), .binding_methods=zero_ids(symbols), .slots=empty_slots(symbols),.raw_slots=zero_ids(symbols),
            .numeric_slots=zero_ids(symbols),.numeric_methods=zero_ids(symbols),.written_symbols=zero_ids(symbols),
            .nonnegative_ints=zero_ids(symbols),.node_parents=Vector<int>::create(),
            .packed_arrays=zero_ids(symbols),
            .iteration_vars=zero_ids(symbols),
            .scalar_roots=zero_ids(symbols),.scalar_fields=Vector<ScalarField>::create(),
            .breaks=zero_ids(c.node_store.count()+1),.continues=zero_ids(c.node_store.count()+1),
            .method=0, .local_count=0, .label_count=0,.loop_versions=0};
    }
    int ref(RuntimeMember member) const { return this.runtime.at((size_t)member); }
    int field_token(FieldId id) const { return this.fields.at(id); }
    int getter(FieldId id) const { return this.getters.at(id); }
    int setter(FieldId id) const { return this.setters.at(id); }
    int cast(TypeId id) const { return this.casts.at(id); }
    int function(SymbolId id) const { return this.methods.at(id); }
    int constructor(TypeId id) const { return this.constructors.at(id); }
    int interface_type(TypeId id) const { return this.interfaces.at(id); }
    int break_target(NodeId id) const { return this.breaks.at(id); }
    int continue_target(NodeId id) const { return this.continues.at(id); }
    int raw_slot(SymbolId id) const { return this.raw_slots.at(id); }
    void ref_set(RuntimeMember member, int id) { this.runtime.at_mut((size_t)member) = id; }
    int type(TypeId type, const Compiler& c) const {
        if (type == 0) { return -1; }
        TypeKind kind = get_type(type, c).kind;
        if (kind == TypeKind::Number) { return -10; }
        if (kind == TypeKind::Boolean) { return -11; }
        if (kind == TypeKind::String) { return -5; }
        if (kind == TypeKind::Void) { return -1; }
        if (kind == TypeKind::Shape || kind==TypeKind::Array) { return -5; }
        return -5;
    }
    void emit(int opcode, int operand) { this.plan.instruction(this.method, opcode, operand); }
    void op(int opcode) { this.emit(opcode, 0); }
    void call(RuntimeMember member) { this.emit(0x28, this.ref(member)); }
    int label() { return ++this.label_count; }
    void mark(int label) { this.emit(-1, label); }
    int local(int type) { this.plan.local(this.method, type); return this.local_count++; }
    void begin(int method) { this.method=method; this.local_count=0; this.label_count=0;this.loop_versions=0; }
    void load_storage(SymbolId symbol) {
        int slot = this.slots.at(symbol);
        if (slot==-2147483647) { print("tsnetc: captured binding or function value needs lowering");exit(1); }
        if (slot < 0) { this.emit(0xfe09, -slot - 1); } else { this.emit(0xfe0c, slot); }
    }
    void load(SymbolId symbol) {
        this.load_storage(symbol);
        if (this.raw_slot(symbol)==-6) { this.op(0x6c);this.call(RuntimeMember::WrapNumber); }
        else if (this.raw_slot(symbol)==-3) { this.call(RuntimeMember::WrapNumber); }
        else if (this.raw_slot(symbol)==-2) { this.call(RuntimeMember::WrapBoolean); }
    }
    void store(SymbolId symbol) {
        if (this.raw_slot(symbol)==-6) { this.call(RuntimeMember::NumberValue);this.op(0x69); }
        else if (this.raw_slot(symbol)==-3) { this.call(RuntimeMember::NumberValue); }
        else if (this.raw_slot(symbol)==-2) { this.call(RuntimeMember::BooleanValue); }
        this.store_storage(symbol);
    }
    void store_storage(SymbolId symbol) {
        int slot = this.slots.at(symbol);
        if (slot==-2147483647) { print("tsnetc: captured binding needs lowering");exit(1); }
        if (slot < 0) { this.emit(0xfe0b, -slot - 1); } else { this.emit(0xfe0e, slot); }
    }
    void box(TypeId type, const Compiler& c) {
        if (get_type(type, c).kind == TypeKind::Number) { this.call(RuntimeMember::BoxNumber); }
        else if (get_type(type, c).kind == TypeKind::Boolean) { this.call(RuntimeMember::BoxBoolean); }
    }
    void from_object(TypeId target, const Compiler& c) {
        TypeKind kind=get_type(target,c).kind;
        if (this.type(target,c)!=-5 || kind==TypeKind::String || kind==TypeKind::Shape || kind==TypeKind::Array) {
            this.plan.string(this.method,"checked conversion failed");this.emit(0x28,this.cast(target));
        }
    }
    void convert(TypeId source, TypeId target, const Compiler& c) {
        TypeKind target_kind=get_type(target,c).kind;
        if (source==target || (this.type(source,c)==this.type(target,c) && target_kind!=TypeKind::String && target_kind!=TypeKind::Shape && target_kind!=TypeKind::Array)) { return; }
        if ((get_type(source,c).kind==TypeKind::String && target_kind==TypeKind::String) ||
            (get_type(source,c).kind==TypeKind::Array && target_kind==TypeKind::Array) || fits(source,target,c)) { return; }
        if (get_type(source,c).kind==TypeKind::Void) { this.call(RuntimeMember::Undefined); }
        else { this.box(source,c); }
        if (get_type(target,c).kind==TypeKind::Void) { this.op(0x26); }
        else { this.from_object(target,c); }
    }
    void empty(TypeId type, const Compiler& c) {
        if (get_type(type,c).kind==TypeKind::Number) { this.call(RuntimeMember::EmptyNumber); }
        else if (get_type(type,c).kind==TypeKind::Boolean) { this.call(RuntimeMember::EmptyBoolean); }
        else { this.call(RuntimeMember::Undefined); }
    }
    void missing(TypeId type, const Compiler& c) {
        if (get_type(type,c).kind==TypeKind::Number) { this.call(RuntimeMember::NumberMissing); }
        else if (get_type(type,c).kind==TypeKind::Boolean) { this.call(RuntimeMember::BooleanMissing); }
        else { this.call(RuntimeMember::IsUndefined); }
    }
    void truth(TypeId type, const Compiler& c) {
        if (get_type(type,c).kind==TypeKind::Number) { this.call(RuntimeMember::NumberTruthy); }
        else if (get_type(type,c).kind==TypeKind::Boolean) { this.call(RuntimeMember::BooleanValue); }
        else { this.call(RuntimeMember::Truthy); }
    }
};

// This proof concerns values, not TS annotations. Calls, property reads, and
// names are unknown; arithmetic results are defined even when they are NaN.
bool il_defined(NodeId entry, const Compiler& c) {
    if (entry==0) { return false; }
    Node n=get_node(entry,c);
    if (n.kind==NodeKind::Number || n.kind==NodeKind::Boolean || n.kind==NodeKind::String || n.kind==NodeKind::Object ||
        n.kind==NodeKind::Unary || n.kind==NodeKind::Update || n.kind==NodeKind::TypeOf) { return true; }
    if (n.kind==NodeKind::Assert) { return il_defined(n.left,c); }
    if (n.kind==NodeKind::Sequence) { return il_defined(n.right,c); }
    if (n.kind==NodeKind::Conditional) { return il_defined(n.right,c) && il_defined(n.third,c); }
    if (n.kind==NodeKind::Nullish) { return il_defined(n.right,c); }
    if (n.kind==NodeKind::Assign) {
        return !is_text(get_token(n.token,c).text,&text_assign,c) || il_defined(n.right,c);
    }
    if (n.kind==NodeKind::Binary) {
        TextId op=get_token(n.token,c).text;
        if (is_text(op,&text_and,c) || is_text(op,&text_or,c)) { return il_defined(n.left,c) && il_defined(n.right,c); }
        return true;
    }
    return false;
}
bool il_int_result(NodeId entry, const Compiler& c) {
    if (entry==0) { return false; }
    Node n=get_node(entry,c);
    if (n.kind==NodeKind::Number) {
        double value=number(get_token(n.token,c).text,c);
        return value>=-2147483648.0 && value<=2147483647.0 && value==(double)(int)value;
    }
    if (n.kind==NodeKind::Conditional) { return il_int_result(n.right,c) && il_int_result(n.third,c); }
    if (n.kind==NodeKind::Sequence) { return il_int_result(n.right,c); }
    if (n.kind==NodeKind::Assert) { return il_int_result(n.left,c); }
    if (n.kind!=NodeKind::Unary && n.kind!=NodeKind::Binary && n.kind!=NodeKind::Assign) { return false; }
    TextId op=get_token(n.token,c).text;
    if (n.kind==NodeKind::Unary) { return is_text(op,&text_bit_not,c); }
    if (n.kind==NodeKind::Binary || n.kind==NodeKind::Assign) {
        if (n.kind==NodeKind::Assign && is_text(op,&text_assign,c)) { return il_int_result(n.right,c); }
        return is_text(op,&text_bit_and,c) || is_text(op,&text_bit_or,c) || is_text(op,&text_bit_xor,c) ||
            is_text(op,&text_shift_left,c) || is_text(op,&text_shift_right,c) ||
            is_text(op,&text_bit_and_assign,c) || is_text(op,&text_bit_or_assign,c) || is_text(op,&text_bit_xor_assign,c) ||
            is_text(op,&text_shift_left_assign,c) || is_text(op,&text_shift_right_assign,c);
    }
    return false;
}
void il_prove_locals(const Compiler& c, IlBackend& b) {
    Vector<int> declarations=zero_ids(c.symbol_store.count()+1);
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind==NodeKind::Variable || (n.kind==NodeKind::Binding && n.symbol!=0)) { declarations.at_mut(n.symbol)++; }
        if (n.kind==NodeKind::Variable && il_defined(n.left,c)) {
            TypeKind kind=get_type(n.type,c).kind;
            if (kind==TypeKind::Number) { b.raw_slots.at_mut(n.symbol)=il_int_result(n.left,c) ? -6 : -3; }
            else if (kind==TypeKind::Boolean) { b.raw_slots.at_mut(n.symbol)=-2; }
        }
    }
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        // Redeclarations share storage, including declarations of parameters.
        // Their first initializer does not prove later values remain defined.
        if ((n.kind==NodeKind::Variable || n.kind==NodeKind::Binding) && n.symbol!=0 && declarations.at(n.symbol)>1) {
            b.raw_slots.at_mut(n.symbol)=0;b.written_symbols.at_mut(n.symbol)=1;
            b.iteration_vars.at_mut(n.symbol)=1;
        }
        if (n.kind==NodeKind::Binding && n.symbol!=0) { b.raw_slots.at_mut(n.symbol)=0;b.written_symbols.at_mut(n.symbol)=1; }
        if (n.kind==NodeKind::ForOf) {
            Node target=get_node(n.left,c);
            if (target.kind==NodeKind::Block) { target=get_node(target.children,c); }
            if (target.kind==NodeKind::Variable || target.kind==NodeKind::Name) {
                b.raw_slots.at_mut(target.symbol)=0;b.written_symbols.at_mut(target.symbol)=1;
            }
        }
        if (n.kind==NodeKind::Assign || n.kind==NodeKind::Update) {
            Node target=get_node(n.left,c);
            if (target.kind==NodeKind::Name) {
                b.written_symbols.at_mut(target.symbol)=1;
                if (!il_defined(i,c)) { b.raw_slots.at_mut(target.symbol)=0; }
                else if (b.raw_slot(target.symbol)==-6 && !il_int_result(i,c)) { b.raw_slots.at_mut(target.symbol)=-3; }
            }
        }
    }
}

void il_runtime(IlBackend& b) {
    b.ref_set(RuntimeMember::ObjectType, b.plan.reference("System.Runtime", "System", "Object"));
    b.ref_set(RuntimeMember::DoubleType, b.plan.reference("System.Runtime", "System", "Double"));
    b.ref_set(RuntimeMember::BooleanType, b.plan.reference("System.Runtime", "System", "Boolean"));
    b.ref_set(RuntimeMember::StringType, b.plan.reference("System.Runtime", "System", "String"));
    b.ref_set(RuntimeMember::JsType, b.plan.reference("Tsnet.Runtime", "", "Js"));
    b.ref_set(RuntimeMember::TsObjectType, b.plan.reference("Tsnet.Runtime", "", "TsObject"));
    int js = b.ref(RuntimeMember::JsType); int obj = b.ref(RuntimeMember::TsObjectType);
    b.ref_set(RuntimeMember::WrapNumber,b.plan.method1(6,js,0,-10,-3,"WrapNumber"));
    b.ref_set(RuntimeMember::WrapBoolean,b.plan.method1(6,js,0,-11,-2,"WrapBoolean"));
    b.ref_set(RuntimeMember::EmptyNumber,b.plan.method0(6,js,0,-10,"EmptyNumber"));
    b.ref_set(RuntimeMember::EmptyBoolean,b.plan.method0(6,js,0,-11,"EmptyBoolean"));
    b.ref_set(RuntimeMember::BoxNumber,b.plan.method1(6,js,0,-5,-10,"BoxNumber"));
    b.ref_set(RuntimeMember::BoxBoolean,b.plan.method1(6,js,0,-5,-11,"BoxBoolean"));
    b.ref_set(RuntimeMember::NumberValue,b.plan.method1(6,js,0,-3,-10,"NumberValue"));
    b.ref_set(RuntimeMember::BooleanValue,b.plan.method1(6,js,0,-2,-11,"BooleanValue"));
    b.ref_set(RuntimeMember::NumberMissing,b.plan.method1(6,js,0,-2,-10,"NumberMissing"));
    b.ref_set(RuntimeMember::BooleanMissing,b.plan.method1(6,js,0,-2,-11,"BooleanMissing"));
    b.ref_set(RuntimeMember::NumberTruthy,b.plan.method1(6,js,0,-2,-10,"NumberTruthy"));
    b.ref_set(RuntimeMember::Constructor, b.plan.method0(6, obj, 1, -1, ".ctor"));
    b.ref_set(RuntimeMember::Get, b.plan.method1(6, obj, 1, -5, -4, "Get"));
    b.ref_set(RuntimeMember::Set, b.plan.method2(6, obj, 1, -5, -4, -5, "Set"));
    b.ref_set(RuntimeMember::Cast, b.plan.method2(6, js, 2, -8, -5, -4, "Cast"));
    b.ref_set(RuntimeMember::Check, b.plan.method2(6, js, 2, -5, -5, -4, "Check"));
    b.ref_set(RuntimeMember::Add, b.plan.method2(6, js, 0, -5, -5, -5, "Add"));
    b.ref_set(RuntimeMember::Equal, b.plan.method2(6, js, 0, -2, -5, -5, "Equal"));
    b.ref_set(RuntimeMember::EqualNumbers, b.plan.method2(6, js, 0, -2, -10, -10, "EqualNumbers"));
    b.ref_set(RuntimeMember::Remainder, b.plan.method2(6, js, 0, -3, -3, -3, "Remainder"));
    b.ref_set(RuntimeMember::ToInt32, b.plan.method1(6, js, 0, -6, -3, "ToInt32"));
    int array=b.plan.reference("Tsnet.Runtime","","JsArray");b.ref_set(RuntimeMember::ArrayType,array);
    int numbers=b.plan.reference("Tsnet.Runtime","","NumberArray");b.ref_set(RuntimeMember::NumberArrayType,numbers);
    b.ref_set(RuntimeMember::ArrayConstructor,b.plan.method1(6,array,1,-1,-6,".ctor"));
    b.ref_set(RuntimeMember::ArrayBufferConstructor,b.plan.method1(6,array,1,-1,-7,".ctor"));
    b.ref_set(RuntimeMember::ArrayHoleValue,b.plan.method0(6,array,0,-5,"HoleValue"));
    b.ref_set(RuntimeMember::NumberArrayConstructor,b.plan.method1(6,numbers,1,-1,-6,".ctor"));
    b.ref_set(RuntimeMember::IndexGet,b.plan.method2(6,js,0,-5,-5,-5,"IndexGet"));
    Vector<int> index_parameters=Vector<int>::create();index_parameters.push(-5);index_parameters.push(-5);index_parameters.push(-5);
    b.ref_set(RuntimeMember::IndexSet,b.plan.method(6,js,0,-1,index_parameters.view(),"IndexSet"));
    b.ref_set(RuntimeMember::NumberIndexGet,b.plan.method2(6,js,0,-10,-5,-3,"ArrayGetNumber"));
    Vector<int> number_index_parameters=Vector<int>::create();number_index_parameters.push(-5);number_index_parameters.push(-3);number_index_parameters.push(-10);
    b.ref_set(RuntimeMember::NumberIndexSet,b.plan.method(6,js,0,-1,number_index_parameters.view(),"ArraySetNumber"));
    b.ref_set(RuntimeMember::NumberIndexGetAt,b.plan.method2(6,js,0,-10,-5,-6,"ArrayGetNumberAt"));
    Vector<int> integer_index_parameters=Vector<int>::create();integer_index_parameters.push(-5);integer_index_parameters.push(-6);integer_index_parameters.push(-10);
    b.ref_set(RuntimeMember::NumberIndexSetAt,b.plan.method(6,js,0,-1,integer_index_parameters.view(),"ArraySetNumberAt"));
    b.ref_set(RuntimeMember::ArrayLength,b.plan.method1(6,js,0,-3,-5,"ArrayLength"));
    b.ref_set(RuntimeMember::ArraySetLength,b.plan.method2(6,js,0,-1,-5,-3,"ArraySetLength"));
    b.ref_set(RuntimeMember::ArrayPush,b.plan.method2(6,js,0,-3,-5,-7,"ArrayPush"));
    b.ref_set(RuntimeMember::ArrayAppend,b.plan.method2(6,js,0,-1,-5,-5,"ArrayAppend"));
    b.ref_set(RuntimeMember::ArrayHole,b.plan.method1(6,js,0,-1,-5,"ArrayHole"));
    Vector<int> array_set_parameters=Vector<int>::create();array_set_parameters.push(-5);array_set_parameters.push(-6);array_set_parameters.push(-5);
    b.ref_set(RuntimeMember::ArrayIndexSetAt,b.plan.method(6,js,0,-1,array_set_parameters.view(),"ArraySetAt"));
    Vector<int> spread_parameters=Vector<int>::create();spread_parameters.push(-5);spread_parameters.push(-5);spread_parameters.push(-6);
    b.ref_set(RuntimeMember::ArraySpread,b.plan.method(6,js,0,-1,spread_parameters.view(),"ArraySpread"));
    b.ref_set(RuntimeMember::LogArguments,b.plan.method1(6,js,0,-1,-5,"LogArguments"));
    b.ref_set(RuntimeMember::PushArguments,b.plan.method2(6,js,0,-3,-5,-5,"PushArguments"));
    Vector<int> call_rest_parameters=Vector<int>::create();call_rest_parameters.push(-5);call_rest_parameters.push(-6);call_rest_parameters.push(-2);
    b.ref_set(RuntimeMember::CallRest,b.plan.method(6,js,0,-5,call_rest_parameters.view(),"CallRest"));
    b.ref_set(RuntimeMember::ArrayPop,b.plan.method1(6,js,0,-5,-5,"ArrayPop"));
    b.ref_set(RuntimeMember::NumberPush,b.plan.method2(6,js,0,-3,-5,-10,"ArrayPushNumber"));
    b.ref_set(RuntimeMember::NumberPop,b.plan.method1(6,js,0,-10,-5,"ArrayPopNumber"));
    b.ref_set(RuntimeMember::ArrayTryPacked,b.plan.method2(6,js,0,-12,-5,-3,"ArrayTryPacked"));
    b.ref_set(RuntimeMember::ArrayReservePushes,b.plan.method2(6,js,0,-1,-5,-3,"ArrayReservePushes"));
    b.ref_set(RuntimeMember::CanonicalNumber,b.plan.method1(6,js,0,-3,-3,"CanonicalNumber"));
    b.ref_set(RuntimeMember::ForOfLength,b.plan.method1(6,js,0,-3,-5,"ForOfLength"));
    b.ref_set(RuntimeMember::ForOfValue,b.plan.method2(6,js,0,-5,-5,-6,"ForOfValue"));
    b.ref_set(RuntimeMember::ForOfNumber,b.plan.method2(6,js,0,-10,-5,-6,"ForOfNumber"));
    b.ref_set(RuntimeMember::ForOfStep,b.plan.method2(6,js,0,-6,-5,-6,"ForOfStep"));
    b.ref_set(RuntimeMember::EqualBooleans, b.plan.method2(6, js, 0, -2, -11, -11, "EqualBooleans"));
    b.ref_set(RuntimeMember::Number, b.plan.method1(6, js, 0, -3, -5, "Number"));
    b.ref_set(RuntimeMember::Boolean, b.plan.method1(6, js, 0, -2, -5, "Boolean"));
    b.ref_set(RuntimeMember::Truthy, b.plan.method1(6, js, 0, -2, -5, "Truthy"));
    b.ref_set(RuntimeMember::Undefined, b.plan.method0(6, js, 0, -5, "GetUndefined"));
    b.ref_set(RuntimeMember::IsUndefined,b.plan.method1(6,js,0,-2,-5,"IsUndefined"));
    b.ref_set(RuntimeMember::IsNullish,b.plan.method1(6,js,0,-2,-5,"IsNullish"));
    b.ref_set(RuntimeMember::RequireObject,b.plan.method1(6,js,0,-1,-5,"RequireObject"));
    b.ref_set(RuntimeMember::LoopLimit,b.plan.method1(6,js,0,-6,-3,"LoopLimit"));
    b.ref_set(RuntimeMember::TypeOf, b.plan.method1(6, js, 0, -4, -5, "TypeOf"));
    b.ref_set(RuntimeMember::Text, b.plan.method1(6, js, 0, -4, -5, "Text"));
    b.ref_set(RuntimeMember::DynamicGet, b.plan.method2(6, js, 0, -5, -5, -4, "Get"));
    Vector<int> set_parameters = Vector<int>::create(); set_parameters.push(-5); set_parameters.push(-4); set_parameters.push(-5);
    b.ref_set(RuntimeMember::DynamicSet, b.plan.method(6, js, 0, -5, set_parameters.view(), "Set"));
    b.ref_set(RuntimeMember::Log, b.plan.method1(6, js, 0, -1, -7, "Log"));
    b.ref_set(RuntimeMember::Now, b.plan.method0(6, js, 0, -3, "Now"));
    int string_type=b.ref(RuntimeMember::StringType);
    b.ref_set(RuntimeMember::Length, b.plan.method1(6, js, 0, -6, -5, "StringLength"));
    b.ref_set(RuntimeMember::StringEqual, b.plan.method2(6, string_type, 0, -2, -4, -4, "op_Equality"));
    b.ref_set(RuntimeMember::Concat, b.plan.method2(6, string_type, 0, -4, -4, -4, "Concat"));
    int action_type=b.plan.reference("System.Runtime","System","Action");
    b.ref_set(RuntimeMember::ActionType,action_type);
    b.ref_set(RuntimeMember::ActionConstructor,b.plan.method2(6,action_type,1,-1,-5,-9,".ctor"));
    b.ref_set(RuntimeMember::Run,b.plan.method1(6,js,0,-6,action_type,"Run"));
}

void il_text(TextId text, const Compiler& c, IlBackend& b) {
    Vector<char> bytes = Vector<char>::create();
    for (size_t i=0; i<text_size(text,c); i++) { bytes.push(text_byte(text,i,c)); }
    b.plan.raw_string(b.method, bytes.view());
}

void il_optional_members(TypeId type, int klass, const Compiler& c, IlBackend& b) {
    Vector<FieldId> emitted=Vector<FieldId>::create();TypeId target=c.types;
    while (target!=0) {
        if (fits(type,target,c)) {
            FieldId member=get_type(target,c).fields;
            while (member!=0) {
                Field record=get_field(member,c);bool seen=false;
                for (size_t i=0;i<emitted.count();i++) {
                    Field previous=get_field(emitted.at(i),c);
                    if (b.type(previous.type,c)==b.type(record.type,c) && same(previous.name,record.name,c)) { seen=true; }
                }
                if (field(type,record.name,c)==0 && record.optional && !seen) {
                    emitted.push(member);int value_type=b.type(record.type,c);
                    Vector<char> get=field_name(record.name,"get_p_",c);
                    Vector<char> set=field_name(record.name,"set_p_",c);
                    b.begin(b.plan.method0(5,klass,0x1e6,value_type,get.view()));
                    b.emit(0xfe09,0);il_text(record.name,c,b);b.call(RuntimeMember::Get);b.from_object(record.type,c);b.op(0x2a);
                    b.begin(b.plan.method1(5,klass,0x1e6,-1,value_type,set.view()));
                    b.emit(0xfe09,0);il_text(record.name,c,b);b.emit(0xfe09,1);b.box(record.type,c);
                    b.call(RuntimeMember::Set);b.op(0x26);b.op(0x2a);
                }
                member=record.next;
            }
        }
        target=get_type(target,c).next;
    }
}

void il_shapes(const Compiler& c, IlBackend& b) {
    TypeId type = c.types;
    while (type != 0) {
        if (get_type(type,c).kind == TypeKind::Shape) {
            Vector<char> name = numbered_name('I',get_type(type,c).id);
            b.interfaces.at_mut(type) = b.plan.type(name.view(),0,0xa1);
        }
        type=get_type(type,c).next;
    }
    type=c.types;
    while (type != 0) {
        if (get_type(type,c).kind == TypeKind::Shape) {
            FieldId member=get_type(type,c).fields;
            while (member != 0) {
                Vector<char> get=field_name(get_field(member,c).name,"get_p_",c);
                Vector<char> set=field_name(get_field(member,c).name,"set_p_",c);
                int field_type=b.type(get_field(member,c).type,c);
                int interface_type=b.interfaces.at(type);
                b.getters.at_mut(member)=b.plan.method0(5,interface_type,0x5c6,field_type,get.view());
                b.setters.at_mut(member)=b.plan.method1(5,interface_type,0x5c6,-1,field_type,set.view());
                member=get_field(member,c).next;
            }
        }
        if (get_type(type,c).kind != TypeKind::Any && get_type(type,c).kind != TypeKind::Unknown && get_type(type,c).kind != TypeKind::Void) {
            int cast=b.ref(RuntimeMember::Cast); int signature_type=b.type(type,c);
            TypeKind kind=get_type(type,c).kind;
            if (kind==TypeKind::String || kind==TypeKind::Shape || kind==TypeKind::Array) {
                cast=b.ref(RuntimeMember::Check);
                signature_type=kind==TypeKind::String ? b.ref(RuntimeMember::StringType) : kind==TypeKind::Shape ? b.interfaces.at(type) : b.ref(RuntimeMember::ArrayType);
            }
            b.casts.at_mut(type)=b.plan.specialize(cast,signature_type);
        }
        type=get_type(type,c).next;
    }
    type=c.types;
    while (type != 0) {
        if (get_type(type,c).kind == TypeKind::Shape) {
            Vector<char> name=numbered_name('O',get_type(type,c).id);
            int base_type=b.ref(RuntimeMember::TsObjectType);
            int klass=b.plan.type(name.view(),base_type,0x101);
            b.classes.at_mut(type)=klass;
            TypeId target=c.types;
            while (target != 0) {
                if (fits(type,target,c)) { int interface_type=b.interfaces.at(target); b.plan.implements(klass,interface_type); }
                target=get_type(target,c).next;
            }
            int constructor=b.plan.method0(5,klass,0x1886,-1,".ctor");
            b.constructors.at_mut(type)=constructor;
            b.begin(constructor); b.emit(0xfe09,0); b.call(RuntimeMember::Constructor); b.op(0x2a);
            FieldId member=get_type(type,c).fields;
            while (member != 0) {
                Field record=get_field(member,c); int value_type=b.type(record.type,c);
                Vector<char> storage=field_name(record.name,"p_",c);
                int value=b.plan.field(klass,6,value_type,storage.view()); b.fields.at_mut(member)=value;
                Vector<char> get=field_name(record.name,"get_p_",c);
                Vector<char> set=field_name(record.name,"set_p_",c);
                b.begin(b.plan.method0(5,klass,0x1e6,value_type,get.view()));
                b.emit(0xfe09,0); b.emit(0x7b,value); b.op(0x2a);
                b.begin(b.plan.method1(5,klass,0x1e6,-1,value_type,set.view()));
                b.emit(0xfe09,0); b.emit(0xfe09,1); b.emit(0x7d,value); b.op(0x2a);
                member=record.next;
            }
            b.begin(b.plan.method1(5,klass,0xc6,-5,-4,"Get"));
            member=get_type(type,c).fields;
            while (member != 0) {
                Field record=get_field(member,c); int next=b.label();
                b.emit(0xfe09,1); il_text(record.name,c,b); b.call(RuntimeMember::StringEqual); b.emit(0x39,next);
                b.emit(0xfe09,0); b.emit(0x7b,(int)b.field_token(member)); b.box(record.type,c); b.op(0x2a); b.mark(next);
                member=record.next;
            }
            b.emit(0xfe09,0); b.emit(0xfe09,1); b.call(RuntimeMember::Get); b.op(0x2a);
            b.begin(b.plan.method2(5,klass,0xc6,-5,-4,-5,"Set"));
            member=get_type(type,c).fields;
            while (member != 0) {
                Field record=get_field(member,c); int next=b.label();
                b.emit(0xfe09,1); il_text(record.name,c,b); b.call(RuntimeMember::StringEqual); b.emit(0x39,next);
                b.emit(0xfe09,0); b.emit(0xfe09,2);
                TypeKind value_kind=get_type(record.type,c).kind;
                if (b.type(record.type,c)!=-5 || value_kind==TypeKind::String || value_kind==TypeKind::Shape || value_kind==TypeKind::Array) {
                    b.plan.string(b.method,"property write violates static field type"); b.emit(0x28,(int)b.cast(record.type));
                }
                b.emit(0x7d,(int)b.field_token(member)); b.emit(0xfe09,2); b.op(0x2a); b.mark(next);
                member=record.next;
            }
            b.emit(0xfe09,0); b.emit(0xfe09,1); b.emit(0xfe09,2); b.call(RuntimeMember::Set); b.op(0x2a);
            il_optional_members(type,klass,c,b);
        }
        type=get_type(type,c).next;
    }
}
