import "il-packed.cr";

Vector<int> il_loop_globals(NodeId entry, const Compiler& c, const IlBackend& b) {
    if (!b.numeric_globals) { return Vector<int>::create(); }
    Vector<int> globals=Vector<int>::create();
    // Without calls, checked conversions, or escaping exits, no source code can
    // observe the deferred stores. Keep nullable values intact for empty loops.
    for (size_t i=1;i<=c.node_store.count();i++) {
        if (!il_inside(i,entry,b)) { continue; }
        Node n=get_node(i,c);
        if (n.kind==NodeKind::Name) {
            if (n.symbol==0) { return Vector<int>::create(); }
            TypeKind kind=get_type(get_symbol(n.symbol,c).type,c).kind;
            if (kind!=TypeKind::Number && kind!=TypeKind::Boolean) { return Vector<int>::create(); }
            if (b.global_fields.at(n.symbol)!=0 && b.global_vars.at(n.symbol)==0) { return Vector<int>::create(); }
        } else if (n.kind==NodeKind::Variable) {
            TypeKind kind=get_type(n.type,c).kind;
            if (b.global_fields.at(n.symbol)!=0 || (kind!=TypeKind::Number && kind!=TypeKind::Boolean)) { return Vector<int>::create(); }
            Node value=get_node(n.left,c);
            if (value.kind!=NodeKind::Undefined && get_type(value.type,c).kind!=kind) { return Vector<int>::create(); }
        } else if (n.kind==NodeKind::String) {
            Node parent=get_node((NodeId)b.node_parents.at(i),c);
            if (parent.kind!=NodeKind::Variable || parent.fourth!=i) { return Vector<int>::create(); }
        } else if (n.kind==NodeKind::For || n.kind==NodeKind::While || n.kind==NodeKind::Do) {
            if (i!=entry) { return Vector<int>::create(); }
        } else if (n.kind==NodeKind::Break || n.kind==NodeKind::Continue) {
            if (n.left!=entry) { return Vector<int>::create(); }
        } else if (n.kind!=NodeKind::Block && n.kind!=NodeKind::Expression && n.kind!=NodeKind::If &&
            n.kind!=NodeKind::Assign && n.kind!=NodeKind::Update && n.kind!=NodeKind::Binary && n.kind!=NodeKind::Unary &&
            n.kind!=NodeKind::Number && n.kind!=NodeKind::Boolean && n.kind!=NodeKind::String && n.kind!=NodeKind::Undefined &&
            n.kind!=NodeKind::Null && n.kind!=NodeKind::Conditional && n.kind!=NodeKind::TypeOf && n.kind!=NodeKind::Nullish &&
            n.kind!=NodeKind::Sequence && n.kind!=NodeKind::Switch && n.kind!=NodeKind::Case && n.kind!=NodeKind::Label) {
            return Vector<int>::create();
        }
        if (n.kind==NodeKind::Assign || n.kind==NodeKind::Update) {
            Node target=get_node(n.left,c);
            if (target.kind!=NodeKind::Name) { return Vector<int>::create(); }
            SymbolId symbol=target.symbol;
            TypeKind kind=get_type(get_symbol(symbol,c).type,c).kind;
            if (n.kind==NodeKind::Update && kind!=TypeKind::Number) { return Vector<int>::create(); }
            if (n.kind==NodeKind::Assign) {
                Node value=get_node(n.right,c);
                if (value.kind!=NodeKind::Undefined && get_type(value.type,c).kind!=kind) { return Vector<int>::create(); }
            }
            if (b.global_fields.at(symbol)==0 || b.global_vars.at(symbol)==0 || get_type(get_symbol(symbol,c).type,c).kind!=TypeKind::Number) { continue; }
            bool seen=false;
            for (size_t j=0;j<globals.count();j++) { if (globals.at(j)==(int)symbol) { seen=true; } }
            if (!seen) { globals.push((int)symbol); }
        }
    }
    return move(globals);
}
