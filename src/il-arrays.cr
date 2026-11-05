import "il-loops.cr";

bool il_number_array(TypeId type, const Compiler& c) {
    return get_type(type,c).kind==TypeKind::Array && get_type(get_type(type,c).element,c).kind==TypeKind::Number;
}
bool il_number_index(NodeId entry, const Compiler& c) {
    Node n=get_node(entry,c);
    return n.kind==NodeKind::Index && il_number_array(get_node(n.left,c).type,c) && get_type(get_node(n.right,c).type,c).kind==TypeKind::Number;
}
bool il_direct_array_index(NodeId entry, const Compiler& c, const IlBackend& b) {
    if (!il_number_index(entry,c)) { return false; }
    Node index=get_node(get_node(entry,c).right,c);
    if (index.kind==NodeKind::Name) { return b.raw_slot(index.symbol)==-6 && b.nonnegative_ints.at(index.symbol)!=0; }
    return index.kind==NodeKind::Number && il_int_result(get_node(entry,c).right,c) && number(get_token(index.token,c).text,c)>=0;
}
void il_array_literal(NodeId entry, TypeId type, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);int count=0;NodeId value=n.children;
    while (value!=0) { count++;value=get_node(value,c).next; }
    bool numeric=il_number_array(type,c);
    b.emit(0x20,count);b.emit(0x73,b.ref(numeric ? RuntimeMember::NumberArrayConstructor : RuntimeMember::ArrayConstructor));
    value=n.children;int index=0;
    while (value!=0) {
        Node item=get_node(value,c);
        if (!(item.kind==NodeKind::Undefined && item.constant)) {
            b.op(0x25);
            if (numeric) { b.emit(0x20,index);il_as(value,get_type(type,c).element,c,b);b.call(RuntimeMember::NumberIndexSetAt); }
            else {
                b.plan.number(b.method,(double)index);
                b.call(RuntimeMember::WrapNumber);b.call(RuntimeMember::BoxNumber);il_boxed(value,c,b);b.call(RuntimeMember::IndexSet);
            }
        }
        index++;value=item.next;
    }
}
bool il_array_call(NodeId entry, const Compiler& c) {
    Node n=get_node(entry,c);Node callee=get_node(n.left,c);
    if (callee.kind!=NodeKind::Property || (!is_text(callee.name,&text_push,c) && !is_text(callee.name,&text_pop,c))) { return false; }
    TypeKind owner=get_type(get_node(callee.left,c).type,c).kind;
    return owner==TypeKind::Array || owner==TypeKind::Any;
}
void il_emit_array_call(NodeId entry, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);Node callee=get_node(n.left,c);TypeId owner=get_node(callee.left,c).type;
    il_expression(callee.left,c,b);
    if (is_text(callee.name,&text_pop,c)) {
        if (il_number_array(owner,c)) { b.call(RuntimeMember::NumberPop); }
        else { b.call(RuntimeMember::ArrayPop);b.from_object(n.type,c); }
        return;
    }
    if (il_number_array(owner,c) && n.children!=0 && get_node(n.children,c).next==0) {
        il_as(n.children,get_type(owner,c).element,c,b);b.call(RuntimeMember::NumberPush);
    } else {
        int count=0;NodeId argument=n.children;
        while (argument!=0) { count++;argument=get_node(argument,c).next; }
        b.emit(0x20,count);b.emit(0x8d,b.ref(RuntimeMember::ObjectType));argument=n.children;int index=0;
        while (argument!=0) {
            b.op(0x25);b.emit(0x20,index++);il_boxed(argument,c,b);b.op(0xa2);argument=get_node(argument,c).next;
        }
        b.call(RuntimeMember::ArrayPush);
    }
    b.call(RuntimeMember::WrapNumber);
    if (get_type(n.type,c).kind!=TypeKind::Number) { b.call(RuntimeMember::BoxNumber); }
}
