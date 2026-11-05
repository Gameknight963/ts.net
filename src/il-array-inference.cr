import "il-scalar.cr";

TypeId il_numeric_array_type(Compiler& c) {
    for (size_t i=1;i<=c.type_store.count();i++) {
        Type t=get_type(i,c);
        if (t.kind==TypeKind::Array && get_type(t.element,c).kind==TypeKind::Number) { return i; }
    }
    TypeId element=primitive(TypeKind::Number,c);TypeId result=new_type(TypeKind::Array,0,c);
    c.type_store.at_mut(result).element=element;set_type_defined(result,true,c);return result;
}

bool il_local_array_number(NodeId entry, SymbolId root, const Compiler& c) {
    Node n=get_node(entry,c);
    if (get_type(n.type,c).kind==TypeKind::Number || n.kind==NodeKind::Undefined) { return true; }
    if (n.kind==NodeKind::Index && get_node(n.left,c).kind==NodeKind::Name && get_node(n.left,c).symbol==root) { return true; }
    if (n.kind==NodeKind::Binary && is_text(get_token(n.token,c).text,&text_plus,c)) {
        return il_local_array_number(n.left,root,c) && il_local_array_number(n.right,root,c);
    }
    if (n.kind==NodeKind::Conditional) { return il_local_array_number(n.right,root,c) && il_local_array_number(n.third,root,c); }
    if (n.kind==NodeKind::Sequence) { return il_local_array_number(n.right,root,c); }
    return false;
}

bool il_local_array_write(NodeId target, SymbolId root, const Compiler& c, const IlBackend& b) {
    NodeId parent=(NodeId)b.node_parents.at(target);Node n=get_node(parent,c);
    if (n.kind==NodeKind::ForOf || n.kind==NodeKind::Binding || n.kind==NodeKind::Destructure) { return false; }
    if (n.kind!=NodeKind::Assign || n.left!=target) { return true; }
    TextId op=get_token(n.token,c).text;
    return (!is_text(op,&text_assign,c) && !is_text(op,&text_add_assign,c) && !il_logical_assignment(op,c)) || il_local_array_number(n.right,root,c);
}

void il_infer_numeric_expressions(Compiler& c) {
    for (size_t i=c.node_store.count();i>0;i--) {
        Node n=get_node(i,c);
        if (n.type==0 || get_type(n.type,c).kind!=TypeKind::Any) { continue; }
        if (n.kind==NodeKind::Binary && is_text(get_token(n.token,c).text,&text_plus,c) &&
            get_type(get_node(n.left,c).type,c).kind==TypeKind::Number && get_type(get_node(n.right,c).type,c).kind==TypeKind::Number) {
            set_node_type(i,get_node(n.left,c).type,c);
        }
        if ((n.kind==NodeKind::Assign || n.kind==NodeKind::Update) && get_type(get_node(n.left,c).type,c).kind==TypeKind::Number &&
            (n.kind==NodeKind::Update || get_type(get_node(n.right,c).type,c).kind==TypeKind::Number || get_node(n.right,c).kind==NodeKind::Undefined)) {
            set_node_type(i,get_node(n.left,c).type,c);
        }
        if (n.kind==NodeKind::Conditional && get_type(get_node(n.right,c).type,c).kind==TypeKind::Number && get_type(get_node(n.third,c).type,c).kind==TypeKind::Number) {
            set_node_type(i,get_node(n.right,c).type,c);
        }
        if (n.kind==NodeKind::Sequence && get_type(get_node(n.right,c).type,c).kind==TypeKind::Number) { set_node_type(i,get_node(n.right,c).type,c); }
    }
}

void il_infer_array_values(TypeId numeric, Compiler& c, const IlBackend& b) {
    Vector<int> definitions=zero_ids(c.symbol_store.count()+1);Vector<int> counts=zero_ids(c.symbol_store.count()+1);
    Vector<int> invalid=zero_ids(c.symbol_store.count()+1);
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind==NodeKind::Variable || (n.kind==NodeKind::Binding && n.symbol!=0)) { counts.at_mut(n.symbol)++; }
    }
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind!=NodeKind::Variable || n.left==0 || n.fourth!=0 || counts.at(n.symbol)!=1 || b.written_symbols.at(n.symbol)!=0 || b.global_fields.at(n.symbol)!=0 ||
            scalar_owner(i,&b.node_parents,c)==0 || get_node((NodeId)b.node_parents.at(i),c).kind==NodeKind::Function) { continue; }
        Type t=get_type(n.type,c);Node value=get_node(n.left,c);
        if ((t.kind!=TypeKind::Any && (t.kind!=TypeKind::Array || get_type(t.element,c).kind!=TypeKind::Any)) || value.kind!=NodeKind::Array) { continue; }
        bool valid=true;NodeId item=value.children;
        while (item!=0) {
            Node element=get_node(item,c);
            if (element.kind==NodeKind::Spread || (get_type(element.type,c).kind!=TypeKind::Number && element.kind!=NodeKind::Undefined)) { valid=false;break; }
            item=element.next;
        }
        if (valid) { definitions.at_mut(n.symbol)=(int)i; }
    }
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind!=NodeKind::Name || definitions.at(n.symbol)==0) { continue; }
        SymbolId root=n.symbol;NodeId definition=(NodeId)definitions.at(root);
        if (scalar_owner(i,&b.node_parents,c)!=scalar_owner(definition,&b.node_parents,c)) { invalid.at_mut(root)=1;continue; }
        NodeId parent=(NodeId)b.node_parents.at(i);Node p=get_node(parent,c);
        if (p.kind==NodeKind::Index && p.left==i) {
            Node key=get_node(p.right,c);
            // A nullable numeric key would change undefined into NaN in the
            // numeric index ABI. Leave such keys on the original path.
            bool defined=il_defined(p.right,c) || (key.kind==NodeKind::Name && b.raw_slot(key.symbol)!=0);
            if (get_type(key.type,c).kind==TypeKind::Number && defined && il_local_array_write(parent,root,c,b)) { continue; }
        }
        if (p.kind==NodeKind::Property && p.left==i) {
            // Literal fields share Property's representation with reads.
            // Even a field named "length" stores and escapes the whole array.
            if (get_node((NodeId)b.node_parents.at(parent),c).kind==NodeKind::Object) { invalid.at_mut(root)=1;continue; }
            if (is_text(p.name,&text_length,c) && il_local_array_write(parent,root,c,b)) { continue; }
            NodeId call_id=(NodeId)b.node_parents.at(parent);Node call=get_node(call_id,c);
            if (call.kind==NodeKind::Call && call.left==parent) {
                if (is_text(p.name,&text_pop,c) || is_text(p.name,&text_includes,c) || is_text(p.name,&text_index_of,c)) { continue; }
                if (is_text(p.name,&text_push,c)) {
                    bool valid=true;NodeId argument=call.children;
                    while (argument!=0) {
                        if (get_node(argument,c).kind==NodeKind::Spread || !il_local_array_number(argument,root,c)) { valid=false;break; }
                        argument=get_node(argument,c).next;
                    }
                    if (valid) { continue; }
                }
            }
        }
        invalid.at_mut(root)=1;
    }
    TypeId number=get_type(numeric,c).element;
    for (size_t symbol=1;symbol<definitions.count();symbol++) {
        if (definitions.at(symbol)==0 || invalid.at(symbol)!=0) { continue; }
        NodeId definition=(NodeId)definitions.at(symbol);Node n=get_node(definition,c);
        set_node_type(definition,numeric,c);set_node_type(n.left,numeric,c);set_symbol_type(symbol,numeric,c);
    }
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind!=NodeKind::Name || definitions.at(n.symbol)==0 || invalid.at(n.symbol)!=0) { continue; }
        set_node_type(i,numeric,c);NodeId parent=(NodeId)b.node_parents.at(i);Node p=get_node(parent,c);
        if (p.kind==NodeKind::Index || is_text(p.name,&text_length,c)) { set_node_type(parent,number,c); }
        else {
            NodeId call=(NodeId)b.node_parents.at(parent);
            if (is_text(p.name,&text_pop,c) || is_text(p.name,&text_push,c) || is_text(p.name,&text_index_of,c)) { set_node_type(call,number,c); }
        }
    }
    il_infer_numeric_expressions(c);
}
