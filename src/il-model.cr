import "assembly-plan.cr";

enum class RuntimeMember {
    ObjectType, DoubleType, BooleanType, StringType, JsType, TsObjectType,
    Constructor, Get, Set, Cast, Add, Equal, Number, Boolean, Text, DynamicGet, DynamicSet,
    Log, Now, Length, StringEqual, Concat, ActionType, ActionConstructor, Run, Truthy, Undefined, TypeOf, IsUndefined, IsNullish, Count
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
    Vector<int> slots;
    Vector<int> breaks;
    Vector<int> continues;
    int method;
    int local_count;
    int label_count;
    static Self create(const Compiler& c) {
        size_t types = c.type_store.count() + 1;
        size_t fields = c.field_store.count() + 1;
        size_t symbols = c.symbol_store.count() + 1;
        return (Self){.plan=AssemblyPlan::create(), .runtime=zero_ids((size_t)RuntimeMember::Count),
            .interfaces=zero_ids(types), .classes=zero_ids(types), .constructors=zero_ids(types),
            .fields=zero_ids(fields), .getters=zero_ids(fields), .setters=zero_ids(fields),
            .casts=zero_ids(types), .methods=zero_ids(symbols), .slots=empty_slots(symbols),
            .breaks=zero_ids(c.node_store.count()+1),.continues=zero_ids(c.node_store.count()+1),
            .method=0, .local_count=0, .label_count=0};
    }
    int ref(RuntimeMember member) const { return this.runtime.at((size_t)member); }
    int field_token(FieldId id) const { return this.fields.at(id); }
    int getter(FieldId id) const { return this.getters.at(id); }
    int setter(FieldId id) const { return this.setters.at(id); }
    int cast(TypeId id) const { return this.casts.at(id); }
    int function(SymbolId id) const { return this.methods.at(id); }
    int constructor(TypeId id) const { return this.constructors.at(id); }
    int break_target(NodeId id) const { return this.breaks.at(id); }
    int continue_target(NodeId id) const { return this.continues.at(id); }
    void ref_set(RuntimeMember member, int id) { this.runtime.at_mut((size_t)member) = id; }
    int type(TypeId type, const Compiler& c) const {
        if (type == 0) { return -1; }
        TypeKind kind = get_type(type, c).kind;
        if (kind == TypeKind::Number) { return -3; }
        if (kind == TypeKind::Boolean) { return -2; }
        if (kind == TypeKind::String) { return -4; }
        if (kind == TypeKind::Void) { return -1; }
        if (kind == TypeKind::Shape) { return this.interfaces.at(type); }
        return -5;
    }
    void emit(int opcode, int operand) { this.plan.instruction(this.method, opcode, operand); }
    void op(int opcode) { this.emit(opcode, 0); }
    void call(RuntimeMember member) { this.emit(0x28, this.ref(member)); }
    int label() { return ++this.label_count; }
    void mark(int label) { this.emit(-1, label); }
    int local(int type) { this.plan.local(this.method, type); return this.local_count++; }
    void begin(int method) { this.method=method; this.local_count=0; this.label_count=0; }
    void load(SymbolId symbol) {
        int slot = this.slots.at(symbol);
        if (slot==-2147483647) { print("tsnetc: captured binding or function value needs lowering");exit(1); }
        if (slot < 0) { this.emit(0xfe09, -slot - 1); } else { this.emit(0xfe0c, slot); }
    }
    void store(SymbolId symbol) {
        int slot = this.slots.at(symbol);
        if (slot==-2147483647) { print("tsnetc: captured binding needs lowering");exit(1); }
        if (slot < 0) { this.emit(0xfe0b, -slot - 1); } else { this.emit(0xfe0e, slot); }
    }
    void box(TypeId type, const Compiler& c) {
        if (get_type(type, c).kind == TypeKind::Number) { this.emit(0x8c, this.ref(RuntimeMember::DoubleType)); }
        else if (get_type(type, c).kind == TypeKind::Boolean) { this.emit(0x8c, this.ref(RuntimeMember::BooleanType)); }
    }
};

void il_runtime(IlBackend& b) {
    b.ref_set(RuntimeMember::ObjectType, b.plan.reference("System.Runtime", "System", "Object"));
    b.ref_set(RuntimeMember::DoubleType, b.plan.reference("System.Runtime", "System", "Double"));
    b.ref_set(RuntimeMember::BooleanType, b.plan.reference("System.Runtime", "System", "Boolean"));
    b.ref_set(RuntimeMember::StringType, b.plan.reference("System.Runtime", "System", "String"));
    b.ref_set(RuntimeMember::JsType, b.plan.reference("Tsnet.Runtime", "", "Js"));
    b.ref_set(RuntimeMember::TsObjectType, b.plan.reference("Tsnet.Runtime", "", "TsObject"));
    int js = b.ref(RuntimeMember::JsType); int obj = b.ref(RuntimeMember::TsObjectType);
    b.ref_set(RuntimeMember::Constructor, b.plan.method0(6, obj, 1, -1, ".ctor"));
    b.ref_set(RuntimeMember::Get, b.plan.method1(6, obj, 1, -5, -4, "Get"));
    b.ref_set(RuntimeMember::Set, b.plan.method2(6, obj, 1, -5, -4, -5, "Set"));
    b.ref_set(RuntimeMember::Cast, b.plan.method2(6, js, 2, -8, -5, -4, "Cast"));
    b.ref_set(RuntimeMember::Add, b.plan.method2(6, js, 0, -5, -5, -5, "Add"));
    b.ref_set(RuntimeMember::Equal, b.plan.method2(6, js, 0, -2, -5, -5, "Equal"));
    b.ref_set(RuntimeMember::Number, b.plan.method1(6, js, 0, -3, -5, "Number"));
    b.ref_set(RuntimeMember::Boolean, b.plan.method1(6, js, 0, -2, -5, "Boolean"));
    b.ref_set(RuntimeMember::Truthy, b.plan.method1(6, js, 0, -2, -5, "Truthy"));
    b.ref_set(RuntimeMember::Undefined, b.plan.method0(6, js, 0, -5, "GetUndefined"));
    b.ref_set(RuntimeMember::IsUndefined,b.plan.method1(6,js,0,-2,-5,"IsUndefined"));
    b.ref_set(RuntimeMember::IsNullish,b.plan.method1(6,js,0,-2,-5,"IsNullish"));
    b.ref_set(RuntimeMember::TypeOf, b.plan.method1(6, js, 0, -4, -5, "TypeOf"));
    b.ref_set(RuntimeMember::Text, b.plan.method1(6, js, 0, -4, -5, "Text"));
    b.ref_set(RuntimeMember::DynamicGet, b.plan.method2(6, js, 0, -5, -5, -4, "Get"));
    Vector<int> set_parameters = Vector<int>::create(); set_parameters.push(-5); set_parameters.push(-4); set_parameters.push(-5);
    b.ref_set(RuntimeMember::DynamicSet, b.plan.method(6, js, 0, -5, set_parameters.view(), "Set"));
    b.ref_set(RuntimeMember::Log, b.plan.method1(6, js, 0, -1, -7, "Log"));
    b.ref_set(RuntimeMember::Now, b.plan.method0(6, js, 0, -3, "Now"));
    int string_type=b.ref(RuntimeMember::StringType);
    b.ref_set(RuntimeMember::Length, b.plan.method0(6, string_type, 1, -6, "get_Length"));
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
                if (b.type(record.type,c) != -5) {
                    b.plan.string(b.method,"property write violates static field type"); b.emit(0x28,(int)b.cast(record.type));
                }
                b.emit(0x7d,(int)b.field_token(member)); b.emit(0xfe09,2); b.op(0x2a); b.mark(next);
                member=record.next;
            }
            b.emit(0xfe09,0); b.emit(0xfe09,1); b.emit(0xfe09,2); b.call(RuntimeMember::Set); b.op(0x2a);
        }
        type=get_type(type,c).next;
    }
}
