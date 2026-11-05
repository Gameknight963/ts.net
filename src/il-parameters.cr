import "il-destructure.cr";

NodeId il_flat_parameter(NodeId function, const Compiler& c) {
    Node n=get_node(function,c);NodeId parameter=n.children;
    if (parameter==0) { return 0; }
    Node p=get_node(parameter,c);
    if (p.next!=0 || p.left!=0 || p.right==0) { return 0; }
    Node pattern=get_node(p.right,c);
    if (!pattern.constant) { return 0; }
    NodeId child=pattern.children;int count=0;
    while (child!=0) {
        Node binding=get_node(child,c);
        if (binding.constant || binding.left!=0 || binding.right!=0 || get_node(binding.third,c).kind!=NodeKind::String || ++count>8) { return 0; }
        child=binding.next;
    }
    return p.right;
}

bool il_has_flat_calls(SymbolId symbol, const Compiler& c) {
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node call=get_node(i,c);
        if (call.kind!=NodeKind::Call || get_node(call.left,c).symbol!=symbol || call.children==0) { continue; }
        Node argument=get_node(call.children,c);
        if (argument.kind!=NodeKind::Object || argument.next!=0) { continue; }
        int count=0;NodeId property=argument.children;
        while (property!=0) { count++;property=get_node(property,c).next; }
        if (count<=16) { return true; }
    }
    return false;
}

bool il_flat_parameter_call(NodeId entry, const Compiler& c, IlBackend& b) {
    Node call=get_node(entry,c);SymbolId symbol=get_node(call.left,c).symbol;
    if (b.binding_methods.at(symbol)==0 || call.children==0) { return false; }
    Node literal=get_node(call.children,c);
    if (literal.kind!=NodeKind::Object || literal.next!=0) { return false; }
    NodeId property=literal.children;int count=0;
    while (property!=0) { if (++count>16) { return false; }property=get_node(property,c).next; }
    Vector<int> names=Vector<int>::create();Vector<int> slots=Vector<int>::create();Vector<int> types=Vector<int>::create();
    property=literal.children;
    // Finish every RHS expression before entering the parameter environment.
    while (property!=0) {
        Node p=get_node(property,c);TypeId type=get_field(field(literal.type,p.name,c),c).type;int slot=b.local(b.type(type,c));
        il_as(p.left,type,c,b);b.emit(0xfe0e,slot);names.push((int)p.name);slots.push(slot);types.push((int)type);property=p.next;
    }
    NodeId function=get_symbol(symbol,c).function;NodeId child=get_node(il_flat_parameter(function,c),c).children;
    while (child!=0) {
        Node binding=get_node(child,c);TextId key=get_token(get_node(binding.third,c).token,c).text;int found=-1;
        for (size_t i=0;i<names.count();i++) { if (same((TextId)names.at(i),key,c)) { found=(int)i; } }
        if (found<0) { b.empty(binding.type,c); }
        else { b.emit(0xfe0c,slots.at((size_t)found));b.convert((TypeId)types.at((size_t)found),binding.type,c); }
        child=binding.next;
    }
    int method=b.binding_methods.at(symbol);b.emit(0x28,method);b.convert(get_symbol(symbol,c).type,call.type,c);return true;
}

void il_flat_parameter_body(NodeId function, const Compiler& c, IlBackend& b) {
    Node n=get_node(function,c);int method=b.binding_methods.at(n.symbol);
    if (method==0) { return; }
    // Each emitted method owns its locals, even when it shares the source body.
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node local=get_node(i,c);
        if ((local.kind==NodeKind::Variable || local.kind==NodeKind::Binding) && local.symbol!=0 && scalar_owner(i,&b.node_parents,c)==function) {
            b.slots.at_mut(local.symbol)=-2147483647;
        }
    }
    b.begin(method);NodeId child=get_node(il_flat_parameter(function,c),c).children;int index=0;
    while (child!=0) {
        Node binding=get_node(child,c);b.slots.at_mut(binding.symbol)=-(++index);b.raw_slots.at_mut(binding.symbol)=0;child=binding.next;
    }
    il_prepare_iteration_vars(function,c,b);il_statement(n.left,n.type,c,b);
    if (get_type(n.type,c).kind!=TypeKind::Void) { b.empty(n.type,c); }
    b.op(0x2a);
}
