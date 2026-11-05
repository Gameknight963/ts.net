import "il-model.cr";

void il_as(NodeId entry, TypeId target, const Compiler& c, IlBackend& b) {
    TypeId source=get_node(entry,c).type;
    il_expression(entry,c,b);
    b.convert(source,target,c);
}
void il_boxed(NodeId entry, const Compiler& c, IlBackend& b) {
    il_expression(entry,c,b);
    if (get_type(get_node(entry,c).type,c).kind==TypeKind::Void) { b.call(RuntimeMember::Undefined); } else { b.box(get_node(entry,c).type,c); }
}
void il_number(NodeId entry, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind==NodeKind::Name && b.raw_slot(n.symbol)==-3) { b.load_storage(n.symbol);return; }
    il_expression(entry,c,b);
    if (get_type(get_node(entry,c).type,c).kind == TypeKind::Number) { b.call(RuntimeMember::NumberValue); }
    else { b.call(RuntimeMember::Number); }
}
void il_condition(NodeId entry, const Compiler& c, IlBackend& b) {
    il_expression(entry,c,b);b.truth(get_node(entry,c).type,c);
}
void il_invert(IlBackend& b) { b.emit(0x20,0); b.op(0xfe01); }

void il_load_target(NodeId entry, int receiver, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind == NodeKind::Name) { il_expression(entry,c,b); }
    else {
        b.emit(0xfe0c,receiver);
        TypeId owner=get_node(n.left,c).type;
        if (get_type(owner,c).kind == TypeKind::Any) {
            il_text(n.name,c,b);b.call(RuntimeMember::DynamicGet);
            b.from_object(n.type,c);
        }
        else {
            FieldId member=field(owner,n.name,c);
            if (member==0) { fail(n.token,"unresolved lowered field",c); }
            b.emit(0x6f,b.getter(member));
            b.convert(get_field(member,c).type,n.type,c);
        }
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
    if (n.kind==NodeKind::Assign && is_text(op,&text_add_assign,c) && get_type(target.type,c).kind==TypeKind::String) {
        il_load_target(n.left,receiver,c,b);b.call(RuntimeMember::Text);
        il_boxed(n.right,c,b);b.call(RuntimeMember::Text);b.call(RuntimeMember::Concat);
    } else if (n.kind == NodeKind::Update || !is_text(op,&text_assign,c)) {
        if (target.kind==NodeKind::Name) { il_number(n.left,c,b); }
        else { il_load_target(n.left,receiver,c,b);b.call(RuntimeMember::NumberValue); }
        if (n.kind == NodeKind::Update) {
            previous=b.local(b.type(target.type,c));
            b.op(0x25);b.call(RuntimeMember::WrapNumber);b.emit(0xfe0e,previous);b.plan.number(b.method,1);
        }
        else { il_number(n.right,c,b); }
        if (is_text(op,&text_increment,c) || is_text(op,&text_add_assign,c)) { b.op(0x58); }
        else if (is_text(op,&text_decrement,c) || is_text(op,&text_subtract_assign,c)) { b.op(0x59); }
        else if (is_text(op,&text_multiply_assign,c)) { b.op(0x5a); }
        else { b.op(0x5b); }
        b.call(RuntimeMember::WrapNumber);
    } else { il_as(n.right,target.type,c,b); }
    int value=b.local(b.type(target.type,c)); b.emit(0xfe0e,value);
    if (target.kind == NodeKind::Name) {
        b.emit(0xfe0c,value);
        b.convert(target.type,get_symbol(target.symbol,c).type,c);
        b.store(target.symbol);
    }
    else {
        b.emit(0xfe0c,receiver);
        TypeId owner=get_node(target.left,c).type;
        if (get_type(owner,c).kind == TypeKind::Any) {
            il_text(target.name,c,b); b.emit(0xfe0c,value);b.box(target.type,c);b.call(RuntimeMember::DynamicSet); b.op(0x26);
        } else {
            FieldId member=field(owner,target.name,c);b.emit(0xfe0c,value);
            b.convert(target.type,get_field(member,c).type,c);b.emit(0x6f,b.setter(member));
        }
    }
    b.emit(0xfe0c,previous >= 0 && !n.constant ? previous : value);
}

void il_expression(NodeId entry, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind == NodeKind::Undefined) {
        if (n.left!=0) { il_expression(n.left,c,b);if (get_type(get_node(n.left,c).type,c).kind!=TypeKind::Void) { b.op(0x26); } }
        b.call(RuntimeMember::Undefined);
    }
    else if (n.kind == NodeKind::Null) { b.op(0x14); }
    else if (n.kind == NodeKind::TypeOf) { il_boxed(n.left,c,b);b.call(RuntimeMember::TypeOf); }
    else if (n.kind==NodeKind::Sequence) {
        il_expression(n.left,c,b);if (get_type(get_node(n.left,c).type,c).kind!=TypeKind::Void) { b.op(0x26); }
        il_as(n.right,n.type,c,b);
    }
    else if (n.kind == NodeKind::Nullish) {
        int alternate=b.label();int done=b.label();
        il_boxed(n.left,c,b);b.op(0x25);b.call(RuntimeMember::IsNullish);b.emit(0x3a,alternate);
        b.from_object(n.type,c);
        b.emit(0x38,done);b.mark(alternate);b.op(0x26);il_as(n.right,n.type,c,b);b.mark(done);
    }
    else if (n.kind == NodeKind::Conditional) {
        int alternate=b.label();int done=b.label();il_condition(n.left,c,b);b.emit(0x39,alternate);
        il_as(n.right,n.type,c,b);b.emit(0x38,done);b.mark(alternate);il_as(n.third,n.type,c,b);b.mark(done);
    }
    else if (n.kind == NodeKind::Number) { b.plan.number(b.method,number(get_token(n.token,c).text,c));b.call(RuntimeMember::WrapNumber); }
    else if (n.kind == NodeKind::Boolean) { b.emit(0x20,is_text(get_token(n.token,c).text,&text_true,c) ? 1 : 0);b.call(RuntimeMember::WrapBoolean); }
    else if (n.kind == NodeKind::String) {
        il_text(get_token(n.token,c).text,c,b);
    } else if (n.kind == NodeKind::Name) {
        b.load(n.symbol);
        TypeId stored=get_symbol(n.symbol,c).type;
        b.convert(stored,n.type,c);
    }
    else if (n.kind == NodeKind::Object) {
        b.emit(0x73,b.constructor(n.type));
        NodeId property=n.children;
        while (property != 0) {
            Node p=get_node(property,c); FieldId member=field(n.type,p.name,c);
            b.op(0x25); il_as(p.left,get_field(member,c).type,c,b); b.emit(0x7d,(int)b.field_token(member)); property=p.next;
        }
    } else if (n.kind == NodeKind::Property) {
        il_expression(n.left,c,b); TypeId owner=get_node(n.left,c).type;
        if (get_type(owner,c).kind == TypeKind::Any) {
            il_text(n.name,c,b);b.call(RuntimeMember::DynamicGet);
            b.from_object(n.type,c);
        }
        else if (get_type(owner,c).kind == TypeKind::String) { b.emit(0x6f,b.ref(RuntimeMember::Length)); b.op(0x6c);b.call(RuntimeMember::WrapNumber); }
        else {
            FieldId member=field(owner,n.name,c);
            if (member==0 || b.getter(member)==0) { fail(n.token,"unresolved lowered getter",c); }
            b.emit(0x6f,b.getter(member));
            b.convert(get_field(member,c).type,n.type,c);
        }
    } else if (n.kind == NodeKind::Assert) { il_as(n.left,n.type,c,b); }
    else if (n.kind == NodeKind::Assign || n.kind == NodeKind::Update) { il_mutation(entry,c,b); }
    else if (n.kind == NodeKind::Unary) {
        TextId op=get_token(n.token,c).text;
        if (is_text(op,&text_not,c)) { il_condition(n.left,c,b); il_invert(b);b.call(RuntimeMember::WrapBoolean); }
        else { il_number(n.left,c,b); if (is_text(op,&text_minus,c)) { b.op(0x65); }b.call(RuntimeMember::WrapNumber); }
    } else if (n.kind == NodeKind::Call) {
        if (builtin(n.left,&text_performance,&text_now,c)) { b.call(RuntimeMember::Now);b.call(RuntimeMember::WrapNumber); }
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
            if (symbol==0 || get_symbol(symbol,c).function==0) { fail(n.token,"function value calls need lowering",c); }
            NodeId parameter=get_node(get_symbol(symbol,c).function,c).children;
            NodeId argument=n.children;
            while (argument != 0) {
                if (parameter==0) {
                    il_expression(argument,c,b);
                    if (get_type(get_node(argument,c).type,c).kind!=TypeKind::Void) { b.op(0x26); }
                    argument=get_node(argument,c).next;continue;
                }
                il_as(argument,get_node(parameter,c).type,c,b);
                argument=get_node(argument,c).next; parameter=get_node(parameter,c).next;
            }
            while (parameter!=0) {
                if (!get_node(parameter,c).constant) { fail(n.token,"omitted required argument",c); }
                b.empty(get_node(parameter,c).type,c);parameter=get_node(parameter,c).next;
            }
            b.emit(0x28,(int)b.function(symbol));
            TypeId declared=get_symbol(symbol,c).type;
            b.convert(declared,n.type,c);
        }
    } else if (n.kind == NodeKind::Binary) {
        TextId op=get_token(n.token,c).text;
        if (is_text(op,&text_and,c) || is_text(op,&text_or,c)) {
            int done=b.label();il_as(n.left,n.type,c,b);b.op(0x25);b.truth(n.type,c);
            b.emit(is_text(op,&text_and,c) ? 0x39 : 0x3a,done);b.op(0x26);il_as(n.right,n.type,c,b);b.mark(done);
        } else if (is_text(op,&text_strict_equal,c) || is_text(op,&text_strict_not_equal,c)) {
            TypeKind left_kind=get_type(get_node(n.left,c).type,c).kind;
            TypeKind right_kind=get_type(get_node(n.right,c).type,c).kind;
            Node left=get_node(n.left,c);Node right=get_node(n.right,c);
            bool left_defined=il_defined(n.left,c) || (left.kind==NodeKind::Name && b.raw_slot(left.symbol)==-3);
            bool right_defined=il_defined(n.right,c) || (right.kind==NodeKind::Name && b.raw_slot(right.symbol)==-3);
            if (left_kind==TypeKind::Number && right_kind==TypeKind::Number && left_defined && right_defined) {
                il_number(n.left,c,b);il_number(n.right,c,b);b.op(0xfe01);
            } else if (left_kind==right_kind && (left_kind==TypeKind::Number || left_kind==TypeKind::Boolean)) {
                il_expression(n.left,c,b);il_expression(n.right,c,b);
                b.call(left_kind==TypeKind::Number ? RuntimeMember::EqualNumbers : RuntimeMember::EqualBooleans);
            } else {
                il_boxed(n.left,c,b);il_boxed(n.right,c,b);b.call(RuntimeMember::Equal);
            }
            if (is_text(op,&text_strict_not_equal,c)) { il_invert(b); }
            b.call(RuntimeMember::WrapBoolean);
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
            b.call(get_type(n.type,c).kind==TypeKind::Boolean ? RuntimeMember::WrapBoolean : RuntimeMember::WrapNumber);
        }
    }
}

void il_statement(NodeId entry, TypeId return_type, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind == NodeKind::Break || n.kind == NodeKind::Continue) {
        int target=n.kind==NodeKind::Break ? b.break_target(n.left) : b.continue_target(n.left);
        if (target==0) { fail(n.token,"missing control-flow target",c); }
        b.emit(0x38,target);
    } else if (n.kind == NodeKind::Label) {
        int done=b.label();b.breaks.at_mut(entry)=done;il_statement(n.left,return_type,c,b);b.mark(done);
    } else if (n.kind == NodeKind::Switch) {
        int done=b.label();b.breaks.at_mut(entry)=done;
        il_boxed(n.left,c,b);int value=b.local(-5);b.emit(0xfe0e,value);
        int fallback=done;NodeId clause=n.children;
        while (clause!=0) {
            int label=b.label();b.breaks.at_mut(clause)=label;Node item=get_node(clause,c);
            if (item.left==0) { fallback=label; }
            else { b.emit(0xfe0c,value);il_boxed(item.left,c,b);b.call(RuntimeMember::Equal);b.emit(0x3a,label); }
            clause=item.next;
        }
        b.emit(0x38,fallback);clause=n.children;
        while (clause!=0) {
            b.mark(b.break_target(clause));NodeId child=get_node(clause,c).children;
            while (child!=0) { il_statement(child,return_type,c,b);child=get_node(child,c).next; }
            clause=get_node(clause,c).next;
        }
        b.mark(done);
    } else if (n.kind == NodeKind::Block) {
        NodeId child=n.children;
        while (child != 0) { il_statement(child,return_type,c,b); child=get_node(child,c).next; }
    } else if (n.kind == NodeKind::Variable) {
        int raw=b.raw_slot(n.symbol);int slot=b.local(raw!=0 ? raw : b.type(n.type,c));
        b.slots.at_mut(n.symbol)=slot;il_as(n.left,n.type,c,b);b.store(n.symbol);
    } else if (n.kind == NodeKind::Return) {
        if (n.left != 0) { il_as(n.left,return_type,c,b); }
        else if (b.type(return_type,c)!=-1) { b.empty(return_type,c); }
        b.op(0x2a);
    } else if (n.kind == NodeKind::Expression) {
        il_expression(n.left,c,b); if (get_type(get_node(n.left,c).type,c).kind != TypeKind::Void) { b.op(0x26); }
    } else if (n.kind == NodeKind::If) {
        int alternate=b.label(); int done=b.label();
        il_condition(n.left,c,b); b.emit(0x39,alternate); il_statement(n.right,return_type,c,b); b.emit(0x38,done);
        b.mark(alternate); if (n.third != 0) { il_statement(n.third,return_type,c,b); } b.mark(done);
    } else if (n.kind == NodeKind::While || n.kind == NodeKind::For || n.kind==NodeKind::Do) {
        int start=b.label();int next=b.label();int done=b.label();
        b.breaks.at_mut(entry)=done;b.continues.at_mut(entry)=n.kind==NodeKind::While ? start : next;
        if (n.kind == NodeKind::For && n.left!=0) { il_statement(n.left,return_type,c,b); }
        b.mark(start);
        if (n.kind!=NodeKind::Do) {
            NodeId condition=n.kind==NodeKind::While ? n.left : n.right;
            if (condition!=0) { il_condition(condition,c,b);b.emit(0x39,done); }
        }
        il_statement(n.kind == NodeKind::For ? n.fourth : n.right,return_type,c,b);b.mark(next);
        if (n.kind==NodeKind::Do) { il_condition(n.left,c,b);b.emit(0x39,done); }
        if (n.kind == NodeKind::For && n.third!=0) { il_expression(n.third,c,b); if (get_type(get_node(n.third,c).type,c).kind != TypeKind::Void) { b.op(0x26); } }
        b.emit(0x38,start); b.mark(done);
    }
}

void emit_assembly(const char[]& path, const Compiler& c) {
    IlBackend b=IlBackend::create(c);il_prove_locals(c,&b);il_runtime(&b);il_shapes(c,&b);
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
        while (parameter != 0) {
            SymbolId symbol=get_node(parameter,c).symbol;b.slots.at_mut(symbol)=-(++index);b.raw_slots.at_mut(symbol)=0;
            parameter=get_node(parameter,c).next;
        }
        parameter=n.children;
        while (parameter!=0) {
            Node p=get_node(parameter,c);
            if (p.left!=0) {
                int done=b.label();b.load(p.symbol);b.missing(p.type,c);b.emit(0x39,done);
                il_as(p.left,p.type,c,&b);b.store(p.symbol);b.mark(done);
            }
            parameter=p.next;
        }
        il_statement(n.left,n.type,c,&b);
        if (get_type(n.type,c).kind != TypeKind::Void) { b.empty(n.type,c); }
        b.op(0x2a);
        function=n.next;
    }
    b.begin(main); NodeId statement=c.statements;
    while (statement != 0) { il_statement(statement,0,c,&b); statement=get_node(statement,c).next; }
    b.op(0x2a);
    b.begin(entry); b.op(0x14); b.emit(0xfe06,main); b.emit(0x73,b.ref(RuntimeMember::ActionConstructor)); b.call(RuntimeMember::Run); b.op(0x2a);
    b.plan.finish(entry,path);
}
