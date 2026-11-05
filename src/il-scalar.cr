import "il-model.cr";

NodeId scalar_owner(NodeId entry, const Vector<int>& parents, const Compiler& c) {
    NodeId current=entry;
    while (current!=0) {
        if (get_node(current,c).kind==NodeKind::Function) { return current; }
        current=(NodeId)parents.at(current);
    }
    return 0;
}

bool scalar_literal(NodeId entry, const Compiler& c) {
    Node n=get_node(entry,c);
    if (n.kind!=NodeKind::Object || get_type(n.type,c).kind!=TypeKind::Shape) { return false; }
    int count=0;FieldId member=get_type(n.type,c).fields;
    while (member!=0) {
        TypeKind kind=get_type(get_field(member,c).type,c).kind;
        if (kind==TypeKind::Void) { return false; }
        count++;member=get_field(member,c).next;
    }
    if (count==0 || count>8) { return false; }
    int properties=0;NodeId property=n.children;
    while (property!=0) {
        if (field(n.type,get_node(property,c).name,c)==0) { return false; }
        properties++;property=get_node(property,c).next;
    }
    return properties==count;
}

void il_prove_scalars(const Compiler& c, IlBackend& b) {
    Vector<int> parents=zero_ids(c.node_store.count()+1);
    Vector<int> definitions=zero_ids(c.symbol_store.count()+1);
    Vector<int> invalid=zero_ids(c.symbol_store.count()+1);
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        // Jump targets are control-flow edges, not lexical parents.
        if (n.kind==NodeKind::Break || n.kind==NodeKind::Continue) { continue; }
        if (n.left!=0) { parents.at_mut(n.left)=(int)i; }
        if (n.right!=0) { parents.at_mut(n.right)=(int)i; }
        if (n.third!=0) { parents.at_mut(n.third)=(int)i; }
        if (n.fourth!=0) { parents.at_mut(n.fourth)=(int)i; }
        NodeId child=n.children;
        while (child!=0) { parents.at_mut(child)=(int)i;child=get_node(child,c).next; }
        if (n.kind==NodeKind::Variable && n.constant && get_type(n.type,c).kind==TypeKind::Shape && scalar_literal(n.left,c)) {
            b.scalar_roots.at_mut(n.symbol)=(int)n.symbol;definitions.at_mut(n.symbol)=(int)i;
        }
    }
    // Immutable aliases share the same field locals, including narrower views.
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind!=NodeKind::Variable || !n.constant || get_type(n.type,c).kind!=TypeKind::Shape) { continue; }
        Node value=get_node(n.left,c);
        if (value.kind!=NodeKind::Name) { continue; }
        SymbolId root=(SymbolId)b.scalar_roots.at(value.symbol);
        if (root!=0 && fits(value.type,n.type,c)) { b.scalar_roots.at_mut(n.symbol)=(int)root; }
    }
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind!=NodeKind::Name) { continue; }
        SymbolId root=(SymbolId)b.scalar_roots.at(n.symbol);
        if (root==0) { continue; }
        NodeId definition=(NodeId)definitions.at(root);
        if (scalar_owner(i,&parents,c)!=scalar_owner(definition,&parents,c)) { invalid.at_mut(root)=1;continue; }
        Node parent=get_node((NodeId)parents.at(i),c);
        if (parent.kind==NodeKind::Variable && parent.left==i && b.scalar_roots.at(parent.symbol)==(int)root) { continue; }
        if (parent.kind==NodeKind::Property && parent.left==i) {
            NodeId parent_id=(NodeId)parents.at(i);
            // Literal members share Property's representation with field reads.
            // Storing the whole object as a literal member is an escape.
            Node container=get_node((NodeId)parents.at(parent_id),c);
            TypeId actual=get_node(get_node(definition,c).left,c).type;
            FieldId original=field(actual,parent.name,c);FieldId view=field(n.type,parent.name,c);
            if (container.kind!=NodeKind::Object && original!=0 && view!=0) {
                TypeId original_type=get_field(original,c).type;TypeId view_type=get_field(view,c).type;
                if (original_type==view_type || (get_type(original_type,c).kind==TypeKind::Array && get_type(view_type,c).kind==TypeKind::Array)) { continue; }
            }
        }
        // Calls, captures, identity tests, rebinding and dynamic views escape.
        invalid.at_mut(root)=1;
    }
    for (size_t i=1;i<b.scalar_roots.count();i++) {
        SymbolId root=(SymbolId)b.scalar_roots.at(i);
        if (root!=0 && invalid.at(root)!=0) { b.scalar_roots.at_mut(i)=0; }
    }
    b.node_parents=move(parents);
}

ScalarField il_scalar_field(NodeId entry, const Compiler& c, const IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind==NodeKind::Property && get_node(n.left,c).kind==NodeKind::Name) {
        SymbolId root=(SymbolId)b.scalar_roots.at(get_node(n.left,c).symbol);
        if (root!=0) {
            for (size_t i=0;i<b.scalar_fields.count();i++) {
                ScalarField value=b.scalar_fields.at(i);
                if (value.root==root && same(value.name,n.name,c)) { return value; }
            }
        }
    }
    return (ScalarField){.root=0,.name=0,.type=0,.slot=-1};
}

void il_scalar_initialize(NodeId entry, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);Node value=get_node(n.left,c);
    FieldId member=get_type(value.type,c).fields;
    while (member!=0) {
        Field f=get_field(member,c);int slot=b.local(b.type(f.type,c));
        bool found=false;
        for (size_t i=0;i<b.scalar_fields.count();i++) {
            ScalarField storage=b.scalar_fields.at(i);
            if (storage.root==n.symbol && same(storage.name,f.name,c)) {
                b.scalar_fields.at_mut(i).slot=slot;found=true;break;
            }
        }
        if (!found) { b.scalar_fields.push((ScalarField){.root=n.symbol,.name=f.name,.type=f.type,.slot=slot}); }
        member=f.next;
    }
    NodeId property=value.children;
    while (property!=0) {
        Node p=get_node(property,c);FieldId f=field(value.type,p.name,c);
        il_as(p.left,get_field(f,c).type,c,b);
        for (size_t i=0;i<b.scalar_fields.count();i++) {
            ScalarField storage=b.scalar_fields.at(i);
            if (storage.root==n.symbol && same(storage.name,p.name,c)) { b.emit(0xfe0e,storage.slot);break; }
        }
        property=p.next;
    }
}


