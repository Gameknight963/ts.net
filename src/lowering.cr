import "model.cr";

bool fits(TypeId source, TypeId target, const Compiler& c) {
    if (source == target) {
        return true;
    }
    if (get_type(source, c).kind != TypeKind::Shape || get_type(target, c).kind != TypeKind::Shape) {
        return false;
    }
    FieldId required = get_type(target, c).fields;
    while (required != 0) {
        FieldId actual = field(source, get_field(required, c).name, c);
        if (actual == 0 || get_field(actual, c).type != get_field(required, c).type) {
            return false;
        }
        required = get_field(required, c).next;
    }
    return true;
}

bool builtin(NodeId callee, const char[]& owner, const char[]& member, const Compiler& c) {
    return get_node(callee, c).kind == NodeKind::Property && is_text(get_node(callee, c).name, member, c) &&
           get_node(get_node(callee, c).left, c).kind == NodeKind::Name &&
           is_text(get_node(get_node(callee, c).left, c).name, owner, c);
}

