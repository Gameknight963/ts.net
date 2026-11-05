import "il-scalar.cr";

bool il_inside(NodeId entry, NodeId scope, const IlBackend& b) {
    NodeId parent=entry;
    while (parent!=0 && parent!=scope) { parent=(NodeId)b.node_parents.at(parent); }
    return parent==scope;
}

SymbolId il_packed_loop(NodeId entry, SymbolId counter, const Compiler& c, const IlBackend& b) {
    if (counter==0) { return 0; }
    Node loop=get_node(entry,c);NodeId body=loop.fourth;SymbolId array=0;
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (!il_inside(i,body,b)) { continue; }
        // A call or nested loop can invalidate a buffer or its range proof.
        if (n.kind==NodeKind::Call || n.kind==NodeKind::Function || n.kind==NodeKind::For || n.kind==NodeKind::ForOf || n.kind==NodeKind::While || n.kind==NodeKind::Do) { return 0; }
        if (n.kind==NodeKind::Index) {
            Node receiver=get_node(n.left,c);Node index=get_node(n.right,c);
            if (receiver.kind!=NodeKind::Name || get_type(receiver.type,c).kind!=TypeKind::Array ||
                get_type(get_type(receiver.type,c).element,c).kind!=TypeKind::Number || index.kind!=NodeKind::Name || index.symbol!=counter) { return 0; }
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
    return array;
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
