import "il-iteration.cr";

void il_finish_binding(NodeId entry, const Compiler& c, IlBackend& b) {
    Node binding=get_node(entry,c);
    if (binding.left!=0) {
        int defined=b.label();b.op(0x25);b.missing(binding.type,c);b.emit(0x39,defined);b.op(0x26);
        il_as(binding.left,binding.type,c,b);b.mark(defined);
    }
    if (binding.right!=0) {
        b.box(binding.type,c);int nested=b.local(-5);b.emit(0xfe0e,nested);il_binding_pattern(binding.right,nested,c,b);
    } else {
        if (b.iteration_vars.at(binding.symbol)==0) { b.slots.at_mut(binding.symbol)=b.local(b.type(binding.type,c)); }
        b.store(binding.symbol);
    }
}

bool il_literal_bindings(NodeId entry, const Compiler& c, IlBackend& b) {
    Node pattern=get_node(entry,c);Node literal=get_node(pattern.left,c);
    if (literal.kind!=NodeKind::Array) { return false; }
    NodeId child=pattern.children;
    while (child!=0) {
        Node binding=get_node(child,c);
        if (binding.kind==NodeKind::Binding && (binding.constant || binding.right!=0)) { return false; }
        child=binding.next;
    }
    child=literal.children;int count=0;
    while (child!=0) {
        if (get_node(child,c).kind==NodeKind::Spread || ++count>16) { return false; }
        child=get_node(child,c).next;
    }
    TypeId element=get_type(literal.type,c).element;Vector<int> slots=Vector<int>::create();Vector<int> types=Vector<int>::create();child=literal.children;
    // Save all values first: later RHS expressions can mutate earlier bindings.
    while (child!=0) {
        Node value=get_node(child,c);TypeId storage=element;
        if (get_type(element,c).kind==TypeKind::Any || get_type(element,c).kind==TypeKind::Unknown) { storage=value.type; }
        int slot=b.local(b.type(storage,c));
        if (value.kind==NodeKind::Undefined && value.constant) { b.empty(storage,c); }
        else { il_as(child,storage,c,b); }
        b.emit(0xfe0e,slot);slots.push(slot);types.push((int)storage);child=value.next;
    }
    child=pattern.children;size_t index=0;
    while (child!=0) {
        Node binding=get_node(child,c);
        if (binding.kind==NodeKind::Binding) {
            if (index<slots.count()) { b.emit(0xfe0c,slots.at(index));b.convert((TypeId)types.at(index),binding.type,c); }
            else { b.empty(binding.type,c); }
            il_finish_binding(child,c,b);
        }
        index++;child=binding.next;
    }
    return true;
}

void il_binding_read(TypeId type, int source, int position, int finished, const Compiler& c, IlBackend& b) {
    int missing=b.label();int done=b.label();
    b.emit(0xfe0c,finished);b.emit(0x3a,missing);
    b.emit(0xfe0c,position);b.op(0x76);b.emit(0xfe0c,source);b.call(RuntimeMember::ForOfLength);b.op(0xfe04);b.emit(0x39,missing);
    b.emit(0xfe0c,source);b.emit(0xfe0c,position);
    if (get_type(type,c).kind==TypeKind::Number) { b.call(RuntimeMember::ForOfNumber); }
    else { b.call(RuntimeMember::ForOfValue);b.from_object(type,c); }
    b.emit(0xfe0c,position);b.emit(0xfe0c,source);b.emit(0xfe0c,position);b.call(RuntimeMember::ForOfStep);b.op(0x58);b.emit(0xfe0e,position);
    b.emit(0x38,done);b.mark(missing);
    b.emit(0x20,1);b.emit(0xfe0e,finished);b.empty(type,c);b.mark(done);
}

void il_binding_rest(TypeId type, int source, int position, int finished, const Compiler& c, IlBackend& b) {
    bool numeric=il_number_array(type,c);TypeId element=get_type(type,c).element;
    b.emit(0x20,0);b.emit(0x73,b.ref(numeric ? RuntimeMember::NumberArrayConstructor : RuntimeMember::ArrayConstructor));
    int array=b.local(-5);b.emit(0xfe0e,array);int start=b.label();int done=b.label();b.mark(start);
    b.emit(0xfe0c,finished);b.emit(0x3a,done);
    b.emit(0xfe0c,position);b.op(0x76);b.emit(0xfe0c,source);b.call(RuntimeMember::ForOfLength);b.op(0xfe04);b.emit(0x39,done);
    b.emit(0xfe0c,array);il_binding_read(element,source,position,finished,c,b);
    if (numeric) { b.call(RuntimeMember::NumberPush);b.op(0x26); }
    else { b.box(element,c);b.call(RuntimeMember::ArrayAppend); }
    b.emit(0x38,start);b.mark(done);b.emit(0x20,1);b.emit(0xfe0e,finished);b.emit(0xfe0c,array);
}

void il_binding_pattern(NodeId entry, int source, const Compiler& c, IlBackend& b) {
    Node n=get_node(entry,c);
    // Even an empty pattern must require an iterable value.
    b.emit(0xfe0c,source);b.call(RuntimeMember::ForOfLength);b.op(0x26);
    int position=b.local(-6);int finished=b.local(-2);
    b.emit(0x20,0);b.emit(0xfe0e,position);b.emit(0x20,0);b.emit(0xfe0e,finished);
    NodeId child=n.children;
    while (child!=0) {
        Node binding=get_node(child,c);
        if (binding.kind==NodeKind::Undefined) {
            il_binding_read(binding.type,source,position,finished,c,b);b.op(0x26);
        } else {
            if (binding.constant) { il_binding_rest(binding.type,source,position,finished,c,b); }
            else { il_binding_read(binding.type,source,position,finished,c,b); }
            il_finish_binding(child,c,b);
        }
        child=binding.next;
    }
}
