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
    if (il_has_object_spread(entry,c)) {
        Node spread=get_node(n.children,c);
        if (spread.kind!=NodeKind::Spread || spread.next!=0 || !il_same_spread_fields(get_node(spread.left,c).type,n.type,c)) { return false; }
        member=get_type(n.type,c).fields;
        while (member!=0) { Field f=get_field(member,c);if (f.optional) { return false; }member=f.next; }
        return true;
    }
    int properties=0;NodeId property=n.children;
    while (property!=0) {
        if (field(n.type,get_node(property,c).name,c)==0) { return false; }
        properties++;property=get_node(property,c).next;
    }
    return properties==count;
}

bool scalar_numeric_value(NodeId entry, const Vector<int>& definitions, const IlBackend& b, const Compiler& c) {
    Node n=get_node(entry,c);
    if (n.type!=0 && get_type(n.type,c).kind==TypeKind::Number) { return true; }
    if (n.kind==NodeKind::Undefined) { return true; }
    if (n.kind==NodeKind::Binary && is_text(get_token(n.token,c).text,&text_plus,c)) {
        return scalar_numeric_value(n.left,definitions,b,c) && scalar_numeric_value(n.right,definitions,b,c);
    }
    if (n.kind==NodeKind::Property && get_node(n.left,c).kind==NodeKind::Name) {
        SymbolId root=(SymbolId)b.scalar_roots.at(get_node(n.left,c).symbol);
        if (root==0) { return false; }
        Node declaration=get_node((NodeId)definitions.at(root),c);
        FieldId member=field(get_node(declaration.left,c).type,n.name,c);
        return member!=0 && get_type(get_field(member,c).type,c).kind==TypeKind::Number;
    }
    return false;
}

bool scalar_own_call(NodeId entry, const Compiler& c) {
    Node call=get_node(entry,c);
    if (call.kind!=NodeKind::Call || !builtin_has_own(call.left,c) || call.children==0) { return false; }
    Node receiver=get_node(call.children,c);
    return receiver.kind==NodeKind::Name && receiver.next!=0 && get_node(receiver.next,c).kind==NodeKind::String && get_node(receiver.next,c).next==0;
}

void il_prove_scalars(const Compiler& c, IlBackend& b) {
    Vector<int> parents=zero_ids(c.node_store.count()+1);
    Vector<int> definitions=zero_ids(c.symbol_store.count()+1);
    Vector<int> invalid=zero_ids(c.symbol_store.count()+1);
    Vector<int> declarations=zero_ids(c.symbol_store.count()+1);
    Vector<int> parameters=zero_ids(c.symbol_store.count()+1);
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind==NodeKind::Variable || n.kind==NodeKind::Binding) { declarations.at_mut(n.symbol)++; }
        if (n.kind==NodeKind::Function) {
            NodeId parameter=n.children;
            while (parameter!=0) { Node p=get_node(parameter,c);parameters.at_mut(p.symbol)=1;parameter=p.next; }
        }
    }
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
        if (n.kind==NodeKind::Variable && n.left!=0 && parameters.at(n.symbol)==0 && declarations.at(n.symbol)==1 && (get_type(n.type,c).kind==TypeKind::Shape || get_type(n.type,c).kind==TypeKind::Any) && scalar_literal(n.left,c)) {
            b.scalar_roots.at_mut(n.symbol)=(int)n.symbol;definitions.at_mut(n.symbol)=(int)i;
        }
    }
    // Aliases share field locals; any later rebinding invalidates their root.
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind!=NodeKind::Variable || n.left==0 || parameters.at(n.symbol)!=0 || declarations.at(n.symbol)!=1 || (get_type(n.type,c).kind!=TypeKind::Shape && get_type(n.type,c).kind!=TypeKind::Any)) { continue; }
        Node value=get_node(n.left,c);
        if (value.kind!=NodeKind::Name) { continue; }
        SymbolId root=(SymbolId)b.scalar_roots.at(value.symbol);
        if (root!=0 && (get_type(n.type,c).kind==TypeKind::Any || fits(value.type,n.type,c))) { b.scalar_roots.at_mut(n.symbol)=(int)root; }
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
        if (parent.kind==NodeKind::Call && parent.children==i && get_node(definition,c).constant && scalar_own_call((NodeId)parents.at(i),c)) {
            // The source can have a different set of own keys at runtime.
            // Keep presence queries on a real spread object.
            if (!il_has_object_spread(get_node(definition,c).left,c)) { continue; }
        }
        if (parent.kind==NodeKind::Property && parent.left==i) {
            NodeId parent_id=(NodeId)parents.at(i);
            // Literal members share Property's representation with field reads.
            // Storing the whole object as a literal member is an escape.
            Node container=get_node((NodeId)parents.at(parent_id),c);
            TypeId actual=get_node(get_node(definition,c).left,c).type;
            FieldId original=field(actual,parent.name,c);FieldId view=0;
            TypeKind owner_kind=get_type(n.type,c).kind;
            if (owner_kind!=TypeKind::Any) { view=field(n.type,parent.name,c); }
            bool wide=owner_kind==TypeKind::Any;
            if (view!=0) {
                TypeKind view_kind=get_type(get_field(view,c).type,c).kind;
                wide=view_kind==TypeKind::Any || view_kind==TypeKind::Unknown;
            }
            if (wide && original!=0 && get_type(get_field(original,c).type,c).kind==TypeKind::Number) {
                // Unknown writes keep the checked setter's existing failure path.
                if (container.kind==NodeKind::Assign && container.left==parent_id && (is_text(get_token(container.token,c).text,&text_assign,c) || is_text(get_token(container.token,c).text,&text_and_assign,c) || is_text(get_token(container.token,c).text,&text_or_assign,c) || is_text(get_token(container.token,c).text,&text_nullish_assign,c) || is_text(get_token(container.token,c).text,&text_add_assign,c)) && !scalar_numeric_value(container.right,&definitions,b,c)) { invalid.at_mut(root)=1;continue; }
                view=original;
            }
            if (container.kind!=NodeKind::Object && original!=0 && view!=0) {
                TypeId original_type=get_field(original,c).type;TypeId view_type=get_field(view,c).type;
                if (original_type==view_type || (get_type(original_type,c).kind==TypeKind::Array && get_type(view_type,c).kind==TypeKind::Array)) { continue; }
            }
        }
        // Calls, captures, identity tests, rebinding and unknown fields escape.
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
    if (il_has_object_spread(n.left,c)) { il_scalar_spread_initialize(entry,c,b);return; }
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

// Retain the proven numeric representation through local any views. The
// nullable field representation still distinguishes undefined from NaN.
void il_infer_scalar_values(Compiler& c, const IlBackend& b) {
    Vector<int> shapes=zero_ids(c.symbol_store.count()+1);
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind==NodeKind::Variable && n.left!=0 && b.scalar_roots.at(n.symbol)==(int)n.symbol) {
            shapes.at_mut(n.symbol)=(int)get_node(n.left,c).type;
        }
    }
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind!=NodeKind::Property || n.type==0 || get_type(n.type,c).kind!=TypeKind::Any || get_node(n.left,c).kind!=NodeKind::Name) { continue; }
        SymbolId root=(SymbolId)b.scalar_roots.at(get_node(n.left,c).symbol);
        if (root==0 || shapes.at(root)==0) { continue; }
        FieldId member=field((TypeId)shapes.at(root),n.name,c);
        if (member!=0 && get_type(get_field(member,c).type,c).kind==TypeKind::Number) { set_node_type(i,get_field(member,c).type,c); }
    }
    // Parents precede their expression children in the frontend snapshot.
    for (size_t i=c.node_store.count();i>0;i--) {
        Node n=get_node(i,c);
        if (n.type==0 || get_type(n.type,c).kind!=TypeKind::Any) { continue; }
        if (n.kind==NodeKind::Binary && is_text(get_token(n.token,c).text,&text_plus,c) && get_type(get_node(n.left,c).type,c).kind==TypeKind::Number && get_type(get_node(n.right,c).type,c).kind==TypeKind::Number) {
            set_node_type(i,get_node(n.left,c).type,c);
        }
        if ((n.kind==NodeKind::Assign || n.kind==NodeKind::Update) && get_node(n.left,c).kind==NodeKind::Property && get_type(get_node(n.left,c).type,c).kind==TypeKind::Number) {
            set_node_type(i,get_node(n.left,c).type,c);
        }
    }
}

bool il_scalar_own(NodeId entry, const Compiler& c, IlBackend& b) {
    if (!scalar_own_call(entry,c)) { return false; }
    Node receiver=get_node(get_node(entry,c).children,c);
    SymbolId root=(SymbolId)b.scalar_roots.at(receiver.symbol);
    if (root==0) { return false; }
    TextId key=get_token(get_node(receiver.next,c).token,c).text;bool present=false;
    for (size_t i=0;i<b.scalar_fields.count();i++) {
        ScalarField member=b.scalar_fields.at(i);
        if (member.root==root && same(member.name,key,c)) { present=true; }
    }
    b.emit(0x20,present ? 1 : 0);b.call(RuntimeMember::WrapBoolean);return true;
}
