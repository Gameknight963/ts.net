import "il-scalar.cr";

NodeId il_loop_binding(NodeId entry, const Compiler& c) {
    Node loop=get_node(entry,c);
    if (loop.left==0) { return 0; }
    Node binding=get_node(loop.left,c);
    if (binding.kind==NodeKind::Block) {
        if (binding.children==0 || get_node(binding.children,c).next!=0) { return 0; }
        return binding.children;
    }
    return loop.left;
}

SymbolId il_counted_loop(NodeId entry, const Compiler& c, const IlBackend& b) {
    Node loop=get_node(entry,c);
    if (loop.kind!=NodeKind::For || loop.left==0 || loop.right==0 || loop.third==0) { return 0; }
    NodeId declaration=il_loop_binding(entry,c);
    if (declaration==0) { return 0; }
    Node binding=get_node(declaration,c);
    if (binding.kind!=NodeKind::Variable || binding.left==0) { return 0; }
    Node initial=get_node(binding.left,c);Node condition=get_node(loop.right,c);Node increment=get_node(loop.third,c);
    if (binding.kind!=NodeKind::Variable || initial.kind!=NodeKind::Number || number(get_token(initial.token,c).text,c)!=0 ||
        b.raw_slot(binding.symbol)!=-3 || condition.kind!=NodeKind::Binary || !is_text(get_token(condition.token,c).text,&text_less,c) ||
        get_node(condition.left,c).kind!=NodeKind::Name || get_node(condition.left,c).symbol!=binding.symbol ||
        increment.kind!=NodeKind::Update || !is_text(get_token(increment.token,c).text,&text_increment,c) ||
        get_node(increment.left,c).kind!=NodeKind::Name || get_node(increment.left,c).symbol!=binding.symbol) { return 0; }
    Node bound=get_node(condition.right,c);
    if (bound.kind!=NodeKind::Number && (bound.kind!=NodeKind::Name || bound.symbol==binding.symbol ||
        get_type(bound.type,c).kind!=TypeKind::Number || b.written_symbols.at(bound.symbol)!=0)) { return 0; }
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if ((n.kind==NodeKind::Assign || n.kind==NodeKind::Update) && get_node(n.left,c).kind==NodeKind::Name &&
            get_node(n.left,c).symbol==binding.symbol && i!=loop.third) { return 0; }
        if (n.kind==NodeKind::Name && n.symbol==binding.symbol) {
            NodeId parent=i;
            while (parent!=0 && parent!=entry) { parent=(NodeId)b.node_parents.at(parent); }
            if (parent==0 || scalar_owner(i,&b.node_parents,c)!=scalar_owner(declaration,&b.node_parents,c)) { return 0; }
        }
    }
    return binding.symbol;
}

void il_loop_body(NodeId entry, TypeId result, SymbolId counter, int bound, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);
    int start=b.label();int next=b.label();int done=b.label();
    b.breaks.at_mut(entry)=done;b.continues.at_mut(entry)=n.kind==NodeKind::While ? start : next;
    if (n.kind==NodeKind::For && n.left!=0) { il_statement(n.left,result,c,b); }
    b.mark(start);
    if (n.kind!=NodeKind::Do) {
        NodeId condition=n.kind==NodeKind::While ? n.left : n.right;
        if (counter!=0) {
            b.load_storage(counter);b.op(0x6c);b.emit(0xfe0c,bound);b.op(0xfe04);b.emit(0x39,done);
        } else if (condition!=0) { il_condition(condition,c,b);b.emit(0x39,done); }
    }
    il_statement(n.kind==NodeKind::For ? n.fourth : n.right,result,c,b);b.mark(next);
    if (n.kind==NodeKind::Do) { il_condition(n.left,c,b);b.emit(0x39,done); }
    if (counter!=0) {
        b.load_storage(counter);b.emit(0x20,1);b.op(0x58);b.store_storage(counter);
    } else if (n.kind==NodeKind::For && n.third!=0) {
        il_expression(n.third,c,b);if (get_type(get_node(n.third,c).type,c).kind!=TypeKind::Void) { b.op(0x26); }
    }
    b.emit(0x38,start);b.mark(done);
}

void il_loop(NodeId entry, TypeId result, const Compiler& c, IlBackend& b) {
    SymbolId counter=il_counted_loop(entry,c,b);
    if (counter==0) { il_loop_body(entry,result,0,-1,c,b);return; }
    Node n=get_node(entry,c);Node condition=get_node(n.right,c);
    Node limit=get_node(condition.right,c);
    if (limit.kind==NodeKind::Number) {
        if (number(get_token(limit.token,c).text,c)>2147483647) { il_loop_body(entry,result,0,-1,c,b);return; }
        int bound=b.local(-3);il_number(condition.right,c,b);b.emit(0xfe0e,bound);
        int previous=b.raw_slot(counter);b.raw_slots.at_mut(counter)=-6;b.nonnegative_ints.at_mut(counter)=1;
        il_loop_body(entry,result,counter,bound,c,b);
        b.raw_slots.at_mut(counter)=previous;b.nonnegative_ints.at_mut(counter)=0;return;
    }
    // Versioning nested dynamic loops would multiply the emitted body size.
    if (b.loop_versions!=0) { il_loop_body(entry,result,0,-1,c,b);return; }
    int fallback=b.label();int done=b.label();int bound=b.local(-3);
    il_number(condition.right,c,b);b.emit(0xfe0e,bound);
    // An unordered comparison sends NaN to the original loop too.
    b.emit(0xfe0c,bound);b.plan.number(b.method,2147483647);b.op(0xfe03);b.emit(0x3a,fallback);
    int previous=b.raw_slot(counter);b.raw_slots.at_mut(counter)=-6;b.nonnegative_ints.at_mut(counter)=1;
    b.loop_versions++;
    il_loop_body(entry,result,counter,bound,c,b);b.emit(0x38,done);
    b.raw_slots.at_mut(counter)=previous;b.nonnegative_ints.at_mut(counter)=0;
    b.mark(fallback);il_loop_body(entry,result,0,-1,c,b);b.mark(done);
    b.loop_versions--;
}

bool il_integer_remainder(NodeId entry, const Compiler& c, const IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind!=NodeKind::Binary || !is_text(get_token(n.token,c).text,&text_modulo,c)) { return false; }
    Node left=get_node(n.left,c);Node right=get_node(n.right,c);
    if (left.kind!=NodeKind::Name || b.raw_slot(left.symbol)!=-6 || b.nonnegative_ints.at(left.symbol)==0 || right.kind!=NodeKind::Number) { return false; }
    double divisor=number(get_token(right.token,c).text,c);
    return divisor>0 && divisor<=2147483647 && divisor==(double)(int)divisor;
}
bool il_integer_value(NodeId entry, const Compiler& c, const IlBackend& b) {
    Node n=get_node(entry,c);
    return (n.kind==NodeKind::Name && b.raw_slot(n.symbol)==-6) || il_int_result(entry,c) || il_integer_remainder(entry,c,b);
}
