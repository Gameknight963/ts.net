import "il-loops.cr";

bool il_number_array(TypeId type, const Compiler& c) {
    return get_type(type,c).kind==TypeKind::Array && get_type(get_type(type,c).element,c).kind==TypeKind::Number;
}
bool il_number_index(NodeId entry, const Compiler& c) {
    Node n=get_node(entry,c);
    return n.kind==NodeKind::Index && il_number_array(get_node(n.left,c).type,c) && get_type(get_node(n.right,c).type,c).kind==TypeKind::Number;
}
bool il_direct_index(NodeId entry, const Compiler& c, const IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind!=NodeKind::Index || get_type(get_node(n.left,c).type,c).kind!=TypeKind::Array || get_type(get_node(n.right,c).type,c).kind!=TypeKind::Number) { return false; }
    return il_nonnegative_integer(n.right,c,b);
}
bool il_direct_array_index(NodeId entry, const Compiler& c, const IlBackend& b) {
    return il_number_index(entry,c) && il_direct_index(entry,c,b);
}
void il_array_literal(NodeId entry, TypeId type, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);int count=0;NodeId value=n.children;
    bool spread=false;
    while (value!=0) { count++;if (get_node(value,c).kind==NodeKind::Spread) { spread=true; }value=get_node(value,c).next; }
    bool numeric=il_number_array(type,c);
    bool tagged=b.tagged_literals.at(entry)!=0 && get_type(type,c).kind==TypeKind::Array && get_type(get_type(type,c).element,c).kind==TypeKind::Any;
    if (tagged && !spread && b.reversible_literals.at(entry)==0) {
        b.emit(0x20,count);b.emit(0x73,b.ref(RuntimeMember::TaggedConstructor));
        if (count!=0) { b.op(0x25);b.plan.number(b.method,(double)count);b.call(RuntimeMember::ArrayReserveCapacity); }
        value=n.children;int index=0;
        while (value!=0) {
            Node item=get_node(value,c);
            if (!(item.kind==NodeKind::Undefined && item.constant)) {
                b.op(0x25);b.emit(0x20,index);
                if (get_type(item.type,c).kind==TypeKind::Number) { il_expression(value,c,b);b.call(RuntimeMember::ArraySetTaggedNumberAt); }
                else { il_boxed(value,c,b);b.call(RuntimeMember::ArrayIndexSetAt); }
            }
            index++;value=item.next;
        }
        return;
    }
    if (!numeric && !spread) {
        b.emit(0x20,count);b.emit(0x8d,b.ref(RuntimeMember::ObjectType));value=n.children;int index=0;
        while (value!=0) {
            Node item=get_node(value,c);b.op(0x25);b.emit(0x20,index++);
            if (item.kind==NodeKind::Undefined && item.constant) { b.call(RuntimeMember::ArrayHoleValue); }
            else { il_boxed(value,c,b); }
            b.op(0xa2);value=item.next;
        }
        b.emit(0x73,b.ref(b.reversible_literals.at(entry)!=0 ? RuntimeMember::ReversibleBufferConstructor : tagged ? RuntimeMember::TaggedBufferConstructor : RuntimeMember::ArrayBufferConstructor));return;
    }
    b.emit(0x20,spread ? 0 : count);b.emit(0x73,b.ref(numeric ? RuntimeMember::NumberArrayConstructor : b.reversible_literals.at(entry)!=0 ? RuntimeMember::ReversibleConstructor : tagged ? RuntimeMember::TaggedConstructor : RuntimeMember::ArrayConstructor));
    value=n.children;int index=0;
    while (value!=0) {
        Node item=get_node(value,c);
        if (spread) {
            b.op(0x25);
            if (item.kind==NodeKind::Spread) {
                il_expression(item.left,c,b);int extra=0;NodeId tail=item.next;
                while (tail!=0) { if (get_node(tail,c).kind!=NodeKind::Spread) { extra++; }tail=get_node(tail,c).next; }
                b.emit(0x20,extra);b.call(RuntimeMember::ArraySpread);
            } else if (item.kind==NodeKind::Undefined && item.constant) { b.call(RuntimeMember::ArrayHole); }
            else if (numeric) { il_as(value,get_type(type,c).element,c,b);b.call(RuntimeMember::NumberPush);b.op(0x26); }
            else { il_boxed(value,c,b);b.call(RuntimeMember::ArrayAppend); }
            value=item.next;continue;
        }
        if (!(item.kind==NodeKind::Undefined && item.constant)) {
            b.op(0x25);b.emit(0x20,index);il_as(value,get_type(type,c).element,c,b);b.call(RuntimeMember::NumberIndexSetAt);
        }
        index++;value=item.next;
    }
}
bool il_array_search(NodeId entry, const Compiler& c) {
    Node callee=get_node(get_node(entry,c).left,c);
    return callee.kind==NodeKind::Property && (is_text(callee.name,&text_includes,c) || is_text(callee.name,&text_index_of,c));
}
void il_array_search_result(NodeId entry, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);Node callee=get_node(n.left,c);
    bool includes=is_text(callee.name,&text_includes,c);
    if (includes) { b.plan.number(b.method,-1);b.op(0xfe02);b.call(RuntimeMember::WrapBoolean); }
    else { b.call(RuntimeMember::WrapNumber); }
    if (get_type(n.type,c).kind==TypeKind::Any) { b.call(includes ? RuntimeMember::BoxBoolean : RuntimeMember::BoxNumber); }
}
bool il_array_call(NodeId entry, const Compiler& c) {
    Node n=get_node(entry,c);Node callee=get_node(n.left,c);
    if (callee.kind!=NodeKind::Property || (!is_text(callee.name,&text_push,c) && !is_text(callee.name,&text_pop,c) && !is_text(callee.name,&text_slice,c) && !il_array_search(entry,c) && !is_text(callee.name,&text_reverse,c))) { return false; }
    TypeKind owner=get_type(get_node(callee.left,c).type,c).kind;
    return owner==TypeKind::Array || owner==TypeKind::Any || (owner==TypeKind::String && is_text(callee.name,&text_slice,c));
}
void il_emit_array_call(NodeId entry, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);Node callee=get_node(n.left,c);TypeId owner=get_node(callee.left,c).type;
    il_expression(callee.left,c,b);
    if (is_text(callee.name,&text_reverse,c)) {
        b.op(0x25);b.call(RuntimeMember::ArrayReverseBuiltin);int builtin=b.local(-2);b.emit(0xfe0e,builtin);
        NodeId argument=n.children;
        while (argument!=0) { il_expression(argument,c,b);if (get_type(get_node(argument,c).type,c).kind!=TypeKind::Void) { b.op(0x26); }argument=get_node(argument,c).next; }
        b.emit(0xfe0c,builtin);b.call(RuntimeMember::ArrayReverse);return;
    }
    if (il_array_search(entry,c)) {
        NodeId argument=n.children;bool numeric=argument!=0 && get_type(get_node(argument,c).type,c).kind==TypeKind::Number;
        if (argument!=0) { if (numeric) { il_expression(argument,c,b); } else { il_boxed(argument,c,b); }argument=get_node(argument,c).next; }
        else { b.call(RuntimeMember::Undefined); }
        if (argument!=0) { il_boxed(argument,c,b);argument=get_node(argument,c).next; }
        else { b.call(RuntimeMember::Undefined); }
        while (argument!=0) { il_expression(argument,c,b);if (get_type(get_node(argument,c).type,c).kind!=TypeKind::Void) { b.op(0x26); }argument=get_node(argument,c).next; }
        b.emit(0x20,is_text(callee.name,&text_includes,c) ? 1 : 0);
        b.call(numeric ? RuntimeMember::ArraySearchNumber : RuntimeMember::ArraySearch);il_array_search_result(entry,c,b);return;
    }
    if (is_text(callee.name,&text_slice,c)) {
        NodeId argument=n.children;
        for (int i=0;i<2;i++) {
            if (argument!=0) { il_boxed(argument,c,b);argument=get_node(argument,c).next; }
            else { b.call(RuntimeMember::Undefined); }
        }
        while (argument!=0) { il_expression(argument,c,b);if (get_type(get_node(argument,c).type,c).kind!=TypeKind::Void) { b.op(0x26); }argument=get_node(argument,c).next; }
        b.call(RuntimeMember::Slice);return;
    }
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
