import "il-parameters.cr";

SymbolId il_direct_function(NodeId entry, const Compiler& c, const IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind==NodeKind::FunctionValue) { return n.symbol; }
    if (n.kind!=NodeKind::Name || n.symbol==0) { return 0; }
    if (get_symbol(n.symbol,c).function!=0) { return n.symbol; }
    return (SymbolId)b.function_targets.at(n.symbol);
}

void il_prepare_function_values(int program, const Compiler& c, IlBackend& b) {
    Vector<int> owners=zero_ids(c.symbol_store.count()+1);
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if ((n.kind==NodeKind::Variable || n.kind==NodeKind::Binding) && n.symbol!=0) { owners.at_mut(n.symbol)=(int)scalar_owner(i,&b.node_parents,c); }
        if (n.kind==NodeKind::Variable && n.left!=0 && n.fourth==0 && b.written_symbols.at(n.symbol)==0 && get_node((NodeId)b.node_parents.at(i),c).kind!=NodeKind::Function) {
            Node value=get_node(n.left,c);SymbolId target=0;
            if (value.kind==NodeKind::FunctionValue) { target=value.symbol; }
            if (value.kind==NodeKind::Name && get_symbol(value.symbol,c).function!=0) { target=value.symbol; }
            b.function_targets.at_mut(n.symbol)=(int)target;
        }
    }
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind!=NodeKind::Name || n.symbol==0) { continue; }
        if (owners.at(n.symbol)!=0 && owners.at(n.symbol)!=(int)scalar_owner(i,&b.node_parents,c)) { fail(n.token,"capturing local bindings needs lowering",c); }
        Node parent=get_node((NodeId)b.node_parents.at(i),c);
        bool direct=parent.kind==NodeKind::Call && parent.left==i;
        if (!direct) { b.function_values_needed.at_mut(n.symbol)=1; }
        if (get_symbol(n.symbol,c).function!=0) {
            if (b.written_symbols.at(n.symbol)!=0) { fail(n.token,"reassigning function declarations needs lowering",c); }
            if (!direct && get_node(get_symbol(n.symbol,c).function,c).constant) { fail(n.token,"named function expression self values need lowering",c); }
            if (!direct && b.function_caches.at(n.symbol)==0) {
                Vector<char> name=numbered_name('c',get_symbol(n.symbol,c).id);
                b.function_caches.at_mut(n.symbol)=b.plan.field(program,0x11,-5,name.view());
            }
        }
    }
}

int il_numeric_function(SymbolId symbol, const Compiler& c) {
    Node f=get_node(get_symbol(symbol,c).function,c);
    if (get_type(f.type,c).kind!=TypeKind::Number) { return -1; }
    int count=0;NodeId parameter=f.children;
    while (parameter!=0) {
        Node p=get_node(parameter,c);
        if (p.third!=0 || get_type(p.type,c).kind!=TypeKind::Number || ++count>2) { return -1; }
        parameter=p.next;
    }
    return count;
}

bool il_function_returns(NodeId entry, const Compiler& c) {
    Node n=get_node(entry,c);
    if (n.kind==NodeKind::Return) { return true; }
    if (n.kind==NodeKind::Block) {
        NodeId child=n.children;if (child==0) { return false; }
        while (get_node(child,c).next!=0) { child=get_node(child,c).next; }
        return il_function_returns(child,c);
    }
    if (n.kind==NodeKind::If && n.third!=0) { return il_function_returns(n.right,c) && il_function_returns(n.third,c); }
    return false;
}

bool il_raw_numeric_function(NodeId entry, const Compiler& c, const IlBackend& b) {
    Node f=get_node(entry,c);
    if (il_numeric_function(f.symbol,c)<0 || !il_function_returns(f.left,c)) { return false; }
    NodeId parameter=f.children;
    while (parameter!=0) { Node p=get_node(parameter,c);if (p.left!=0 || b.written_symbols.at(p.symbol)!=0) { return false; }parameter=p.next; }
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (scalar_owner(i,&b.node_parents,c)!=entry) { continue; }
        if (n.kind==NodeKind::Return && (n.left==0 || !il_defined(n.left,c) || get_type(get_node(n.left,c).type,c).kind!=TypeKind::Number)) { return false; }
        if (n.kind!=NodeKind::Name) { continue; }
        parameter=f.children;bool argument=false;
        while (parameter!=0) { Node p=get_node(parameter,c);if (p.symbol==n.symbol) { argument=true;break; }parameter=p.next; }
        if (!argument) { continue; }
        Node parent=get_node((NodeId)b.node_parents.at(i),c);
        // Arithmetic converts undefined to NaN. Returning, testing, storing,
        // or forwarding the original parameter would distinguish the two.
        if (parent.type==0 || get_type(parent.type,c).kind!=TypeKind::Number) { return false; }
        if (parent.kind==NodeKind::Binary || parent.kind==NodeKind::Unary) { continue; }
        if (parent.kind==NodeKind::Assign && !is_text(get_token(parent.token,c).text,&text_assign,c) && !il_logical_assignment(get_token(parent.token,c).text,c)) { continue; }
        return false;
    }
    return true;
}

void il_raw_function_body(NodeId entry, const Compiler& c, IlBackend& b) {
    Node f=get_node(entry,c);int method=b.raw_functions.at(f.symbol);if (method==0) { return; }
    b.begin(method);b.raw_function_return=true;NodeId parameter=f.children;int index=0;
    while (parameter!=0) { Node p=get_node(parameter,c);b.slots.at_mut(p.symbol)=-(++index);b.raw_slots.at_mut(p.symbol)=-3;parameter=p.next; }
    il_prepare_iteration_vars(entry,c,b);il_statement(f.left,f.type,c,b);
    b.plan.number(b.method,0);b.op(0x2a);b.raw_function_return=false;
    parameter=f.children;while (parameter!=0) { Node p=get_node(parameter,c);b.raw_slots.at_mut(p.symbol)=0;parameter=p.next; }
}

void il_function_object(SymbolId symbol, const Compiler& c, IlBackend& b) {
    int adapter=b.function_adapters.at(symbol);b.op(0x14);b.emit(0xfe06,adapter);b.emit(0x73,b.ref(RuntimeMember::FunctionBodyConstructor));
    int numeric=il_numeric_function(symbol,c);
    if (numeric<0) { b.op(0x14); }
    else {
        int raw=b.raw_functions.at(symbol);b.op(0x14);b.emit(0xfe06,raw!=0 ? raw : b.function(symbol));
        RuntimeMember constructor=numeric==0 ? RuntimeMember::NumberFunction0Constructor : numeric==1 ? RuntimeMember::NumberFunction1Constructor : RuntimeMember::NumberFunction2Constructor;
        if (raw!=0) { constructor=numeric==0 ? RuntimeMember::RawNumberFunction0Constructor : numeric==1 ? RuntimeMember::RawNumberFunction1Constructor : RuntimeMember::RawNumberFunction2Constructor; }
        b.emit(0x73,b.ref(constructor));
    }
    il_text(get_symbol(symbol,c).name,c,b);
    int length=0;NodeId parameter=get_node(get_symbol(symbol,c).function,c).children;
    while (parameter!=0) { Node p=get_node(parameter,c);if (p.left!=0 || p.third!=0) { break; }length++;parameter=p.next; }
    b.emit(0x20,length);b.emit(0x73,b.ref(RuntimeMember::FunctionConstructor));
}

void il_function_reference(SymbolId symbol, const Compiler& c, IlBackend& b) {
    int field=b.function_caches.at(symbol);int done=b.label();
    if (field==0) { fail(get_node(get_symbol(symbol,c).function,c).token,"function value cache missing",c); }
    b.emit(0x7e,field);b.op(0x25);b.emit(0x3a,done);b.op(0x26);
    il_function_object(symbol,c,b);b.op(0x25);b.emit(0x80,field);b.mark(done);
}

void il_function_adapter(NodeId entry, const Compiler& c, IlBackend& b) {
    Node f=get_node(entry,c);int adapter=b.function_adapters.at(f.symbol);b.begin(adapter);
    NodeId parameter=f.children;int index=0;
    while (parameter!=0) {
        Node p=get_node(parameter,c);b.emit(0xfe09,0);b.emit(0x20,index++);
        if (p.third!=0) { b.emit(0x20,il_number_array(p.type,c) ? 1 : 0);b.call(RuntimeMember::FunctionRest); }
        else { b.call(RuntimeMember::FunctionArgument); }
        b.from_object(p.type,c);parameter=p.next;
    }
    b.emit(0x28,b.function(f.symbol));
    if (get_type(f.type,c).kind==TypeKind::Void) { b.call(RuntimeMember::Undefined); }
    else { b.box(f.type,c); }
    b.op(0x2a);
}

void il_function_result(TypeId type, const Compiler& c, IlBackend& b) {
    if (get_type(type,c).kind==TypeKind::Void) { b.op(0x26); }
    else { b.from_object(type,c); }
}

void il_value_call(NodeId entry, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);int count=0;bool numeric=get_type(n.type,c).kind==TypeKind::Number;NodeId argument=n.children;
    while (argument!=0) { Node value=get_node(argument,c);count++;if (get_type(value.type,c).kind!=TypeKind::Number && value.kind!=NodeKind::Undefined) { numeric=false; }argument=value.next; }
    il_boxed(n.left,c,b);
    if (numeric && count<=2) {
        TypeId number=n.type;
        argument=n.children;
        while (argument!=0) { il_as(argument,number,c,b);argument=get_node(argument,c).next; }
        b.call(count==0 ? RuntimeMember::CallFunctionNumber0 : count==1 ? RuntimeMember::CallFunctionNumber1 : RuntimeMember::CallFunctionNumber2);return;
    }
    b.emit(0x20,count);b.emit(0x8d,b.ref(RuntimeMember::ObjectType));argument=n.children;int index=0;
    while (argument!=0) { b.op(0x25);b.emit(0x20,index++);il_boxed(argument,c,b);b.op(0xa2);argument=get_node(argument,c).next; }
    b.call(RuntimeMember::CallFunction);il_function_result(n.type,c,b);
}
