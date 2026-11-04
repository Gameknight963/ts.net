import "il-model.cr";

void il_as(NodeId entry, TypeId target, const Compiler& c, IlBackend& b) {
    TypeId source=get_node(entry,c).type;
    il_expression(entry,c,b);
    if (b.type(target,c) == -5) { b.box(source,c); }
    else if (source != target && (b.type(source,c) == -5 || get_type(target,c).kind == TypeKind::Shape || !fits(source,target,c))) {
        b.box(source,c); b.plan.string(b.method,"checked conversion failed"); b.emit(0x28,(int)b.cast(target));
    }
}
void il_boxed(NodeId entry, const Compiler& c, IlBackend& b) { il_expression(entry,c,b); b.box(get_node(entry,c).type,c); }
void il_number(NodeId entry, const Compiler& c, IlBackend& b) {
    il_expression(entry,c,b);
    if (get_type(get_node(entry,c).type,c).kind == TypeKind::Any) { b.call(RuntimeMember::Number); }
}
void il_condition(NodeId entry, const Compiler& c, IlBackend& b) {
    il_expression(entry,c,b);
    if (get_type(get_node(entry,c).type,c).kind == TypeKind::Any) { b.call(RuntimeMember::Boolean); }
}
void il_invert(IlBackend& b) { b.emit(0x20,0); b.op(0xfe01); }

void il_load_target(NodeId entry, int receiver, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind == NodeKind::Name) { b.load(n.symbol); }
    else {
        b.emit(0xfe0c,receiver);
        TypeId owner=get_node(n.left,c).type;
        if (get_type(owner,c).kind == TypeKind::Any) { il_text(n.name,c,b); b.call(RuntimeMember::DynamicGet); }
        else { b.emit(0x6f,b.getter(field(owner,n.name,c))); }
    }
}
void il_mutation(NodeId entry, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c); Node target=get_node(n.left,c);
    int receiver=-1;
    if (target.kind == NodeKind::Property) {
        il_expression(target.left,c,b); receiver=b.local(b.type(get_node(target.left,c).type,c)); b.emit(0xfe0e,receiver);
    }
    int previous=-1;
    TextId op=get_token(n.token,c).text;
    if (n.kind == NodeKind::Update || !is_text(op,&text_assign,c)) {
        il_load_target(n.left,receiver,c,b);
        if (n.kind == NodeKind::Update) { previous=b.local(b.type(target.type,c)); b.op(0x25); b.emit(0xfe0e,previous); b.plan.number(b.method,1); }
        else { il_expression(n.right,c,b); }
        if (is_text(op,&text_increment,c) || is_text(op,&text_add_assign,c)) { b.op(0x58); }
        else if (is_text(op,&text_decrement,c) || is_text(op,&text_subtract_assign,c)) { b.op(0x59); }
        else if (is_text(op,&text_multiply_assign,c)) { b.op(0x5a); }
        else { b.op(0x5b); }
    } else { il_as(n.right,target.type,c,b); }
    int value=b.local(b.type(target.type,c)); b.emit(0xfe0e,value);
    if (target.kind == NodeKind::Name) { b.emit(0xfe0c,value); b.store(target.symbol); }
    else {
        b.emit(0xfe0c,receiver);
        TypeId owner=get_node(target.left,c).type;
        if (get_type(owner,c).kind == TypeKind::Any) {
            il_text(target.name,c,b); b.emit(0xfe0c,value); b.call(RuntimeMember::DynamicSet); b.op(0x26);
        } else { b.emit(0xfe0c,value); b.emit(0x6f,b.setter(field(owner,target.name,c))); }
    }
    b.emit(0xfe0c,previous >= 0 ? previous : value);
}

void il_expression(NodeId entry, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind == NodeKind::Number) { b.plan.number(b.method,number(get_token(n.token,c).text,c)); }
    else if (n.kind == NodeKind::Boolean) { b.emit(0x20,is_text(get_token(n.token,c).text,&text_true,c) ? 1 : 0); }
    else if (n.kind == NodeKind::String) {
        TextId text=get_token(n.token,c).text; Vector<char> value=Vector<char>::create();
        for (size_t i=1; i+1<text_size(text,c); i++) {
            char ch=text_byte(text,i,c);
            if (ch == '\\') {
                ch=text_byte(text,++i,c);
                if (ch == 'n') { ch='\n'; } else if (ch == 'r') { ch='\r'; } else if (ch == 't') { ch='\t'; }
            }
            value.push(ch);
        }
        b.plan.raw_string(b.method,value.view());
    } else if (n.kind == NodeKind::Name) { b.load(n.symbol); }
    else if (n.kind == NodeKind::Object) {
        b.emit(0x73,b.constructor(n.type));
        NodeId property=n.children;
        while (property != 0) {
            Node p=get_node(property,c); FieldId member=field(n.type,p.name,c);
            b.op(0x25); il_as(p.left,get_field(member,c).type,c,b); b.emit(0x7d,(int)b.field_token(member)); property=p.next;
        }
    } else if (n.kind == NodeKind::Property) {
        il_expression(n.left,c,b); TypeId owner=get_node(n.left,c).type;
        if (get_type(owner,c).kind == TypeKind::Any) { il_text(n.name,c,b); b.call(RuntimeMember::DynamicGet); }
        else if (get_type(owner,c).kind == TypeKind::String) { b.emit(0x6f,b.ref(RuntimeMember::Length)); b.op(0x6c); }
        else { b.emit(0x6f,b.getter(field(owner,n.name,c))); }
    } else if (n.kind == NodeKind::Assert) { il_as(n.left,n.type,c,b); }
    else if (n.kind == NodeKind::Assign || n.kind == NodeKind::Update) { il_mutation(entry,c,b); }
    else if (n.kind == NodeKind::Unary) {
        TextId op=get_token(n.token,c).text;
        if (is_text(op,&text_not,c)) { il_condition(n.left,c,b); il_invert(b); }
        else { il_number(n.left,c,b); if (is_text(op,&text_minus,c)) { b.op(0x65); } }
    } else if (n.kind == NodeKind::Call) {
        if (builtin(n.left,&text_performance,&text_now,c)) { b.call(RuntimeMember::Now); }
        else if (builtin(n.left,&text_console,&text_log,c)) {
            int count=0; NodeId argument=n.children;
            while (argument != 0) { count++; argument=get_node(argument,c).next; }
            b.emit(0x20,count); b.emit(0x8d,b.ref(RuntimeMember::ObjectType));
            int index=0; argument=n.children;
            while (argument != 0) {
                b.op(0x25); b.emit(0x20,index++); il_boxed(argument,c,b); b.op(0xa2); argument=get_node(argument,c).next;
            }
            b.call(RuntimeMember::Log);
        } else {
            SymbolId symbol=get_node(n.left,c).symbol;
            NodeId parameter=get_node(get_symbol(symbol,c).function,c).children;
            NodeId argument=n.children;
            while (argument != 0) {
                il_as(argument,get_node(parameter,c).type,c,b);
                argument=get_node(argument,c).next; parameter=get_node(parameter,c).next;
            }
            b.emit(0x28,(int)b.function(symbol));
        }
    } else if (n.kind == NodeKind::Binary) {
        TextId op=get_token(n.token,c).text;
        if (is_text(op,&text_and,c) || is_text(op,&text_or,c)) {
            int done=b.label(); il_condition(n.left,c,b); b.op(0x25);
            b.emit(is_text(op,&text_and,c) ? 0x39 : 0x3a,done); b.op(0x26); il_condition(n.right,c,b); b.mark(done);
        } else if (is_text(op,&text_strict_equal,c) || is_text(op,&text_strict_not_equal,c)) {
            il_boxed(n.left,c,b); il_boxed(n.right,c,b); b.call(RuntimeMember::Equal);
            if (is_text(op,&text_strict_not_equal,c)) { il_invert(b); }
        } else if (is_text(op,&text_plus,c) && get_type(n.type,c).kind == TypeKind::Any) {
            il_boxed(n.left,c,b); il_boxed(n.right,c,b); b.call(RuntimeMember::Add);
        } else if (is_text(op,&text_plus,c) && get_type(n.type,c).kind == TypeKind::String) {
            il_boxed(n.left,c,b); b.call(RuntimeMember::Text); il_boxed(n.right,c,b); b.call(RuntimeMember::Text); b.call(RuntimeMember::Concat);
        } else {
            il_number(n.left,c,b); il_number(n.right,c,b);
            if (is_text(op,&text_plus,c)) { b.op(0x58); }
            else if (is_text(op,&text_minus,c)) { b.op(0x59); }
            else if (is_text(op,&text_star,c)) { b.op(0x5a); }
            else if (is_text(op,&text_slash,c)) { b.op(0x5b); }
            else if (is_text(op,&text_modulo,c)) { b.op(0x5d); }
            else if (is_text(op,&text_less,c)) { b.op(0xfe04); }
            else if (is_text(op,&text_greater,c)) { b.op(0xfe02); }
            else if (is_text(op,&text_less_equal,c)) { b.op(0xfe03); il_invert(b); }
            else if (is_text(op,&text_greater_equal,c)) { b.op(0xfe05); il_invert(b); }
        }
    }
}

void il_statement(NodeId entry, TypeId return_type, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind == NodeKind::Block) {
        NodeId child=n.children;
        while (child != 0) { il_statement(child,return_type,c,b); child=get_node(child,c).next; }
    } else if (n.kind == NodeKind::Variable) {
        int slot=b.local(b.type(n.type,c)); b.slots.at_mut(n.symbol)=slot; il_as(n.left,n.type,c,b); b.store(n.symbol);
    } else if (n.kind == NodeKind::Return) {
        if (n.left != 0) { il_as(n.left,return_type,c,b); } b.op(0x2a);
    } else if (n.kind == NodeKind::Expression) {
        il_expression(n.left,c,b); if (get_type(get_node(n.left,c).type,c).kind != TypeKind::Void) { b.op(0x26); }
    } else if (n.kind == NodeKind::If) {
        int alternate=b.label(); int done=b.label();
        il_condition(n.left,c,b); b.emit(0x39,alternate); il_statement(n.right,return_type,c,b); b.emit(0x38,done);
        b.mark(alternate); if (n.third != 0) { il_statement(n.third,return_type,c,b); } b.mark(done);
    } else if (n.kind == NodeKind::While || n.kind == NodeKind::For) {
        int start=b.label(); int done=b.label();
        if (n.kind == NodeKind::For) { il_statement(n.left,return_type,c,b); }
        b.mark(start); il_condition(n.kind == NodeKind::While ? n.left : n.right,c,b); b.emit(0x39,done);
        il_statement(n.kind == NodeKind::While ? n.right : n.fourth,return_type,c,b);
        if (n.kind == NodeKind::For) { il_expression(n.third,c,b); if (get_type(get_node(n.third,c).type,c).kind != TypeKind::Void) { b.op(0x26); } }
        b.emit(0x38,start); b.mark(done);
    }
}

void emit_assembly(const char[]& path, const Compiler& c) {
    IlBackend b=IlBackend::create(c); il_runtime(&b); il_shapes(c,&b);
    int object_type=b.ref(RuntimeMember::ObjectType);
    int program=b.plan.type("Program",object_type,0x181);
    NodeId function=c.functions;
    while (function != 0) {
        Node n=get_node(function,c); Vector<int> parameters=Vector<int>::create(); NodeId parameter=n.children;
        while (parameter != 0) { parameters.push(b.type(get_node(parameter,c).type,c)); parameter=get_node(parameter,c).next; }
        Vector<char> name=numbered_name('f',get_symbol(n.symbol,c).id);
        int result_type=b.type(n.type,c);
        b.methods.at_mut(n.symbol)=b.plan.method(5,program,0x16,result_type,parameters.view(),name.view());
        function=n.next;
    }
    int main=b.plan.method0(5,program,0x16,-1,"Body");
    int entry=b.plan.method0(5,program,0x16,-6,"Main");
    function=c.functions;
    while (function != 0) {
        Node n=get_node(function,c); b.begin((int)b.function(n.symbol)); NodeId parameter=n.children; int index=0;
        while (parameter != 0) { b.slots.at_mut(get_node(parameter,c).symbol)=-(++index); parameter=get_node(parameter,c).next; }
        il_statement(n.left,n.type,c,&b);
        if (get_type(n.type,c).kind == TypeKind::Void) { b.op(0x2a); } else { b.op(0x14); b.op(0x7a); }
        function=n.next;
    }
    b.begin(main); NodeId statement=c.statements;
    while (statement != 0) { il_statement(statement,0,c,&b); statement=get_node(statement,c).next; }
    b.op(0x2a);
    b.begin(entry); b.op(0x14); b.emit(0xfe06,main); b.emit(0x73,b.ref(RuntimeMember::ActionConstructor)); b.call(RuntimeMember::Run); b.op(0x2a);
    b.plan.finish(entry,path);
}
