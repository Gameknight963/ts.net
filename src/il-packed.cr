import "il-array-inference.cr";

bool il_inside(NodeId entry, NodeId scope, const IlBackend& b) {
    NodeId parent=entry;
    while (parent!=0 && parent!=scope) { parent=(NodeId)b.node_parents.at(parent); }
    return parent==scope;
}

int il_index_extent(NodeId entry, const Compiler& c, const IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind==NodeKind::Number && il_nonnegative_integer(entry,c,b)) {
        double value=number(get_token(n.token,c).text,c);
        return value<2147483647 ? (int)value+1 : 0;
    }
    if (il_integer_remainder(entry,c,b)) { return (int)number(get_token(get_node(n.right,c).token,c).text,c); }
    if (n.kind==NodeKind::Binary && is_text(get_token(n.token,c).text,&text_bit_and,c)) {
        int left=il_index_extent(n.left,c,b);int right=il_index_extent(n.right,c,b);
        return left==0 ? right : right==0 || left<right ? left : right;
    }
    return 0;
}

int il_buffer_extent(NodeId entry, const Compiler& c, const IlBackend& b) {
    NodeId body=get_node(entry,c).fourth;int extent=0;
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind!=NodeKind::Index || !il_inside(i,body,b)) { continue; }
        int current=il_index_extent(n.right,c,b);
        if (current==0) { return 0; }
        if (current>extent) { extent=current; }
    }
    return extent;
}

void il_buffer_bound(NodeId entry, int bound, const Compiler& c, IlBackend& b) {
    int extent=il_buffer_extent(entry,c,b);
    if (extent!=0) { b.plan.number(b.method,(double)extent); }
    else { b.emit(0xfe0c,bound); }
}

SymbolId il_buffer_loop(NodeId entry, SymbolId counter, bool numeric, const Compiler& c, const IlBackend& b) {
    if (counter==0) { return 0; }
    Node loop=get_node(entry,c);NodeId body=loop.fourth;SymbolId array=0;int mode=-1;
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (!il_inside(i,body,b)) { continue; }
        // A call or nested loop can invalidate a buffer or its range proof.
        if (n.kind==NodeKind::Call || n.kind==NodeKind::Function || n.kind==NodeKind::For || n.kind==NodeKind::ForOf || n.kind==NodeKind::While || n.kind==NodeKind::Do) { return 0; }
        if (n.kind==NodeKind::Index) {
            Node receiver=get_node(n.left,c);Node index=get_node(n.right,c);
            if (receiver.kind!=NodeKind::Name || get_type(receiver.type,c).kind!=TypeKind::Array ||
                (get_type(get_type(receiver.type,c).element,c).kind==TypeKind::Number)!=numeric) { return 0; }
            int current=il_index_extent(n.right,c,b)>0 ? 1 : 0;
            if (current==0 && (index.kind!=NodeKind::Name || index.symbol!=counter)) { return 0; }
            // A constant extent and the loop limit need separate guards. Keep
            // mixed accesses on the ordinary path until both can be checked.
            if (mode!=-1 && current!=mode) { return 0; }
            mode=current;
            if (array!=0 && array!=receiver.symbol) { return 0; }
            array=receiver.symbol;
        }
    }
    if (array==0) { return 0; }
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (!il_inside(i,body,b)) { continue; }
        if ((n.kind==NodeKind::Variable || n.kind==NodeKind::Binding) && n.symbol==array) { return 0; }
        if (n.kind==NodeKind::Assign || n.kind==NodeKind::Update) {
            Node target=get_node(n.left,c);
            if (!numeric && target.kind==NodeKind::Index) { return 0; }
            if (target.kind==NodeKind::Property || (target.kind==NodeKind::Name && target.symbol==array)) { return 0; }
            if (target.kind==NodeKind::Index && n.kind==NodeKind::Assign && (is_text(get_token(n.token,c).text,&text_assign,c) || il_logical_assignment(get_token(n.token,c).text,c))) {
                Node value=get_node(n.right,c);
                if (!il_defined(n.right,c) && !(value.kind==NodeKind::Name && b.raw_slot(value.symbol)!=0)) { return 0; }
            }
        }
        if (n.kind==NodeKind::Name && n.symbol==array) {
            Node parent=get_node((NodeId)b.node_parents.at(i),c);
            if (parent.kind!=NodeKind::Index || parent.left!=i) { return 0; }
        }
    }
    if (!numeric && b.global_fields.at(array)!=0) { return 0; }
    return array;
}

SymbolId il_packed_loop(NodeId entry, SymbolId counter, const Compiler& c, const IlBackend& b) { return il_buffer_loop(entry,counter,true,c,b); }
SymbolId il_dense_loop(NodeId entry, SymbolId counter, const Compiler& c, const IlBackend& b) { return il_buffer_loop(entry,counter,false,c,b); }

int il_dense_slot(NodeId entry, const Compiler& c, const IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind!=NodeKind::Index || get_node(n.left,c).kind!=NodeKind::Name) { return -1; }
    return b.dense_arrays.at(get_node(n.left,c).symbol)-1;
}

int il_packed_slot(NodeId entry, const Compiler& c, const IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind!=NodeKind::Index || get_node(n.left,c).kind!=NodeKind::Name) { return -1; }
    return b.packed_arrays.at(get_node(n.left,c).symbol)-1;
}

SymbolId il_push_loop(NodeId entry, const Compiler& c, const IlBackend& b) {
    Node loop=get_node(entry,c);NodeId body=loop.fourth;
    Node statement=get_node(body,c);
    if (statement.kind==NodeKind::Block) {
        if (statement.children==0 || get_node(statement.children,c).next!=0) { return 0; }
        body=statement.children;statement=get_node(body,c);
    }
    if (statement.kind!=NodeKind::Expression) { return 0; }
    Node call=get_node(statement.left,c);
    if (call.kind!=NodeKind::Call) { return 0; }
    Node callee=get_node(call.left,c);
    if (callee.kind!=NodeKind::Property || !is_text(callee.name,&text_push,c)) { return 0; }
    Node receiver=get_node(callee.left,c);
    if (receiver.kind!=NodeKind::Name || get_type(receiver.type,c).kind!=TypeKind::Array ||
        get_type(get_type(receiver.type,c).element,c).kind!=TypeKind::Number || call.children==0 || get_node(call.children,c).next!=0) { return 0; }
    for (size_t i=1;i<=c.node_store.count();i++) {
        if (!il_inside(i,call.children,b)) { continue; }
        Node n=get_node(i,c);
        if (n.kind==NodeKind::Call || n.kind==NodeKind::Assign || n.kind==NodeKind::Update) { return 0; }
    }
    return receiver.symbol;
}

SymbolId il_fill_loop(NodeId entry, SymbolId counter, const Compiler& c, const IlBackend& b) {
    NodeId body=get_node(entry,c).fourth;Node statement=get_node(body,c);
    if (statement.kind==NodeKind::Block) {
        if (statement.children==0 || get_node(statement.children,c).next!=0) { return 0; }
        body=statement.children;statement=get_node(body,c);
    }
    if (statement.kind!=NodeKind::Expression) { return 0; }
    Node assignment=get_node(statement.left,c);
    if (assignment.kind!=NodeKind::Assign || !is_text(get_token(assignment.token,c).text,&text_assign,c)) { return 0; }
    Node target=get_node(assignment.left,c);
    if (target.kind!=NodeKind::Index || get_node(target.left,c).kind!=NodeKind::Name || get_node(target.right,c).kind!=NodeKind::Name || get_node(target.right,c).symbol!=counter) { return 0; }
    Node array=get_node(target.left,c);
    if (get_type(array.type,c).kind!=TypeKind::Array || get_type(get_type(array.type,c).element,c).kind==TypeKind::Number || b.global_fields.at(array.symbol)!=0) { return 0; }
    for (size_t i=1;i<=c.node_store.count();i++) {
        if (!il_inside(i,assignment.right,b)) { continue; }
        Node n=get_node(i,c);
        if (n.kind==NodeKind::Call || n.kind==NodeKind::Assign || n.kind==NodeKind::Update) { return 0; }
    }
    return array.symbol;
}
