import "il-arrays.cr";

void il_prepare_iteration_vars(NodeId owner, const Compiler& c, IlBackend& b) {
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (scalar_owner(i,&b.node_parents,c)!=owner) { continue; }
        Node target=n;
        if (n.kind==NodeKind::ForOf && !n.constant) {
            target=get_node(n.left,c);
            if (target.kind==NodeKind::Block) { target=get_node(target.children,c); }
        } else if (n.kind!=NodeKind::Variable || b.iteration_vars.at(n.symbol)==0) { continue; }
        if (target.kind!=NodeKind::Variable) { continue; }
        b.iteration_vars.at_mut(target.symbol)=1;
        if (b.slots.at(target.symbol)==-2147483647) {
            TypeId type=get_symbol(target.symbol,c).type;
            int slot=b.local(b.type(type,c));b.slots.at_mut(target.symbol)=slot;
            b.empty(type,c);b.store(target.symbol);
        }
    }
}

void il_iteration_store(NodeId entry, TypeId type, int value, const Compiler& c, IlBackend& b) {
    Node target=get_node(entry,c);
    if (target.kind==NodeKind::Variable || target.kind==NodeKind::Name) {
        b.emit(0xfe0c,value);b.convert(type,get_symbol(target.symbol,c).type,c);b.store(target.symbol);return;
    }
    ScalarField scalar=il_scalar_field(entry,c,b);
    if (scalar.slot>=0) { b.emit(0xfe0c,value);b.convert(type,scalar.type,c);b.emit(0xfe0e,scalar.slot);return; }
    if (target.kind==NodeKind::Index) {
        il_expression(target.left,c,b);il_boxed(target.right,c,b);b.emit(0xfe0c,value);b.box(type,c);b.call(RuntimeMember::IndexSet);return;
    }
    if (target.kind!=NodeKind::Property) { fail(target.token,"unsupported iteration binding",c); }
    il_expression(target.left,c,b);TypeId owner=get_node(target.left,c).type;
    if (get_type(owner,c).kind==TypeKind::Any) {
        il_text(target.name,c,b);b.emit(0xfe0c,value);b.box(type,c);b.call(RuntimeMember::DynamicSet);b.op(0x26);
    } else if (get_type(owner,c).kind==TypeKind::Array && is_text(target.name,&text_length,c)) {
        b.emit(0xfe0c,value);b.call(RuntimeMember::NumberValue);b.call(RuntimeMember::ArraySetLength);
    } else {
        FieldId member=field(owner,target.name,c);
        b.emit(0x74,b.interface_type(owner));b.emit(0xfe0c,value);b.convert(type,get_field(member,c).type,c);b.emit(0x6f,b.setter(member));
    }
}

void il_for_of(NodeId entry, TypeId result, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);NodeId binding=n.left;Node target=get_node(binding,c);
    if (target.kind==NodeKind::Block) {
        if (target.children==0 || get_node(target.children,c).next!=0) { fail(n.token,"unsupported iteration declaration",c); }
        binding=target.children;target=get_node(binding,c);
    }
    TypeId type=(target.kind==NodeKind::Name || target.kind==NodeKind::Variable) ? get_symbol(target.symbol,c).type : target.type;
    if (target.kind==NodeKind::Variable && b.slots.at(target.symbol)==-2147483647) {
        int slot=b.local(b.type(type,c));b.slots.at_mut(target.symbol)=slot;b.empty(type,c);b.store(target.symbol);
    }
    il_expression(n.right,c,b);int iterable=b.local(-5);b.emit(0xfe0e,iterable);
    int position=b.local(-6);b.emit(0x20,0);b.emit(0xfe0e,position);
    int value=b.local(b.type(type,c));int start=b.label();int done=b.label();
    b.breaks.at_mut(entry)=done;b.continues.at_mut(entry)=start;
    b.mark(start);
    // The unsigned position covers every legal array index, including sparse
    // indices above int.MaxValue. Array iteration observes the live length.
    b.emit(0xfe0c,position);b.op(0x76);b.emit(0xfe0c,iterable);b.call(RuntimeMember::ForOfLength);b.op(0xfe04);b.emit(0x39,done);
    b.emit(0xfe0c,iterable);b.emit(0xfe0c,position);
    if (get_type(type,c).kind==TypeKind::Number) { b.call(RuntimeMember::ForOfNumber); }
    else { b.call(RuntimeMember::ForOfValue);b.from_object(type,c); }
    b.emit(0xfe0e,value);
    b.emit(0xfe0c,position);
    if (get_type(get_node(n.right,c).type,c).kind==TypeKind::Array) { b.emit(0x20,1); }
    else { b.emit(0xfe0c,iterable);b.emit(0xfe0c,position);b.call(RuntimeMember::ForOfStep); }
    b.op(0x58);b.emit(0xfe0e,position);
    il_iteration_store(binding,type,value,c,b);il_statement(n.fourth,result,c,b);
    b.emit(0x38,start);b.mark(done);
}
