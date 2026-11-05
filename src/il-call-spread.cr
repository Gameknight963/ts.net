import "il-parameters.cr";

bool il_call_has_spread(NodeId entry, const Compiler& c) {
    NodeId argument=get_node(entry,c).children;
    while (argument!=0) { if (get_node(argument,c).kind==NodeKind::Spread) { return true; }argument=get_node(argument,c).next; }
    return false;
}

TypeId il_call_number_type(NodeId entry, const Compiler& c) {
    Node call=get_node(entry,c);Node callee=get_node(call.left,c);
    if (il_array_call(entry,c)) {
        TypeId owner=get_node(callee.left,c).type;
        return il_number_array(owner,c) ? get_type(owner,c).element : 0;
    }
    if (callee.symbol==0 || get_symbol(callee.symbol,c).function==0) { return 0; }
    NodeId parameter=get_node(get_symbol(callee.symbol,c).function,c).children;TypeId number=0;
    while (parameter!=0) {
        Node p=get_node(parameter,c);
        if (p.third!=0) {
            TypeId rest=get_node(p.third,c).type;
            if (!il_number_array(rest,c)) { return 0; }number=get_type(rest,c).element;
        } else {
            if (get_type(p.type,c).kind!=TypeKind::Number) { return 0; }number=p.type;
        }
        parameter=p.next;
    }
    return number;
}

void il_append_call_argument(NodeId entry, int array, TypeId number, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);
    if (n.kind==NodeKind::Spread && get_node(n.left,c).kind==NodeKind::Array) {
        NodeId child=get_node(n.left,c).children;
        while (child!=0) { il_append_call_argument(child,array,number,c,b);child=get_node(child,c).next; }
        return;
    }
    b.emit(0xfe0c,array);
    if (n.kind==NodeKind::Spread) { il_expression(n.left,c,b);b.emit(0x20,0);b.call(RuntimeMember::ArraySpread); }
    else if (number!=0) { il_as(entry,number,c,b);b.call(RuntimeMember::NumberPush);b.op(0x26); }
    else { il_boxed(entry,c,b);b.call(RuntimeMember::ArrayAppend); }
}

void il_spread_call(NodeId entry, const Compiler& c, IlBackend& b) {
    Node call=get_node(entry,c);Node callee=get_node(call.left,c);int receiver=-1;
    bool array_call=il_array_call(entry,c);
    if (array_call) { il_expression(callee.left,c,b);receiver=b.local(-5);b.emit(0xfe0e,receiver); }
    TypeId numeric=il_call_number_type(entry,c);
    b.emit(0x20,0);b.emit(0x73,b.ref(numeric!=0 ? RuntimeMember::NumberArrayConstructor : RuntimeMember::ArrayConstructor));
    int array=b.local(-5);b.emit(0xfe0e,array);NodeId argument=call.children;
    while (argument!=0) { il_append_call_argument(argument,array,numeric,c,b);argument=get_node(argument,c).next; }
    if (builtin(call.left,&text_console,&text_log,c)) { b.emit(0xfe0c,array);b.call(RuntimeMember::LogArguments);return; }
    if (builtin(call.left,&text_performance,&text_now,c)) { b.call(RuntimeMember::Now);b.call(RuntimeMember::WrapNumber);return; }
    if (array_call) {
        b.emit(0xfe0c,receiver);
        if (is_text(callee.name,&text_push,c)) {
            b.emit(0xfe0c,array);b.call(RuntimeMember::PushArguments);b.call(RuntimeMember::WrapNumber);
            if (get_type(call.type,c).kind!=TypeKind::Number) { b.call(RuntimeMember::BoxNumber); }
        } else if (il_number_array(get_node(callee.left,c).type,c)) { b.call(RuntimeMember::NumberPop); }
        else { b.call(RuntimeMember::ArrayPop);b.from_object(call.type,c); }
        return;
    }
    SymbolId symbol=callee.symbol;
    if (symbol==0 || get_symbol(symbol,c).function==0) { fail(call.token,"function value calls need lowering",c); }
    NodeId parameter=get_node(get_symbol(symbol,c).function,c).children;int index=0;
    while (parameter!=0) {
        Node p=get_node(parameter,c);b.emit(0xfe0c,array);b.emit(0x20,index);
        if (p.third!=0) { b.emit(0x20,il_number_array(get_node(p.third,c).type,c) ? 1 : 0);b.call(RuntimeMember::CallRest); }
        else if (get_type(p.type,c).kind==TypeKind::Number) { b.call(RuntimeMember::ForOfNumber); }
        else { b.call(RuntimeMember::ForOfValue);b.from_object(p.type,c); }
        index++;parameter=p.next;
    }
    b.emit(0x28,(int)b.function(symbol));b.convert(get_symbol(symbol,c).type,call.type,c);
}
