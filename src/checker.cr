import "parser.cr";

// Mutable fields require identical storage types. Width subtyping is safe:
// extra fields remain on the original object and aliases share its identity.
unsafe bool fits(Type* source, Type* target) {
    if (source == target) { return true; }
    if (source->kind != TypeKind::Shape || target->kind != TypeKind::Shape) { return false; }
    Field* required = target->fields;
    while (required != 0) {
        Field* actual = field(source, required->name);
        if (actual == 0 || actual->type != required->type) { return false; }
        required = required->next;
    }
    return true;
}

unsafe bool assignable(Type* source, Type* target) {
    if (source->kind == TypeKind::Void || target->kind == TypeKind::Void) { return false; }
    if (target->kind == TypeKind::Any || target->kind == TypeKind::Unknown) { return true; }
    if (source->kind == TypeKind::Any) { return true; }
    return fits(source, target);
}

unsafe Symbol* lookup(Compiler* c, const char* name) {
    Symbol* symbol = c->symbols;
    while (symbol != 0) {
        if (same(symbol->name, name)) { return symbol; }
        symbol = symbol->next;
    }
    return 0;
}

unsafe Symbol* bind(Compiler* c, Node* entry) {
    Symbol* previous = lookup(c, entry->name);
    if (previous != 0 && previous->scope == c->scope) { fail(c, entry->token, "duplicate binding"); }
    Symbol* symbol = (Symbol*)allocate(c->arena, sizeof(Symbol));
    symbol->name = entry->name;
    symbol->type = entry->type;
    symbol->scope = c->scope;
    symbol->id = c->next_symbol++;
    symbol->constant = entry->constant;
    if (entry->kind == NodeKind::Function) { symbol->function = entry; symbol->constant = true; }
    symbol->next = c->symbols;
    c->symbols = symbol;
    entry->symbol = symbol;
    return symbol;
}

unsafe void require_kind(Compiler* c, Node* entry, TypeKind kind, const char* message) {
    if (entry->type->kind != kind && entry->type->kind != TypeKind::Any) { fail(c, entry->token, message); }
}

unsafe void writable(Compiler* c, Node* entry) {
    if (entry->kind == NodeKind::Name) {
        if (entry->symbol->constant) { fail(c, entry->token, "cannot assign to const binding"); }
    }
    else if (entry->kind == NodeKind::Property) {
        if (entry->left->type->kind == TypeKind::String) { fail(c, entry->token, "string length is read-only"); }
    }
    else { fail(c, entry->token, "expected assignment target"); }
}

unsafe bool builtin(Node* callee, const char* owner, const char* member) {
    return callee->kind == NodeKind::Property && same(callee->name, member)
        && callee->left->kind == NodeKind::Name && same(callee->left->name, owner);
}

unsafe Type* check_expression(Compiler* c, Node* entry) {
    if (entry->kind == NodeKind::Number) { entry->type = primitive(c, TypeKind::Number); }
    else if (entry->kind == NodeKind::String) { entry->type = primitive(c, TypeKind::String); }
    else if (entry->kind == NodeKind::Boolean) { entry->type = primitive(c, TypeKind::Boolean); }
    else if (entry->kind == NodeKind::Name) {
        Symbol* symbol = lookup(c, entry->name);
        if (symbol == 0) { fail(c, entry->token, "unknown binding"); }
        if (symbol->function != 0) { fail(c, entry->token, "function values are not implemented yet"); }
        entry->symbol = symbol;
        entry->type = symbol->type;
    }
    else if (entry->kind == NodeKind::Object) {
        Type* type = new_type(c, TypeKind::Shape, 0);
        type->defined = true;
        Field* last = 0;
        Node* property = entry->children;
        while (property != 0) {
            if (field(type, property->name) != 0) { fail(c, property->token, "duplicate object property"); }
            Field* member = (Field*)allocate(c->arena, sizeof(Field));
            member->name = property->name;
            member->type = check_expression(c, property->left);
            if (member->type->kind == TypeKind::Void) { fail(c, property->token, "void object property"); }
            if (last == 0) { type->fields = member; } else { last->next = member; }
            last = member;
            property = property->next;
        }
        entry->type = type;
    }
    else if (entry->kind == NodeKind::Property) {
        Type* receiver = check_expression(c, entry->left);
        if (receiver->kind == TypeKind::Any) { entry->type = primitive(c, TypeKind::Any); }
        else if (receiver->kind == TypeKind::String && same(entry->name, "length")) { entry->type = primitive(c, TypeKind::Number); }
        else {
            if (receiver->kind != TypeKind::Shape) { fail(c, entry->token, "property access requires object or any"); }
            Field* member = field(receiver, entry->name);
            if (member == 0) { fail(c, entry->token, "object has no such property"); }
            entry->type = member->type;
        }
    }
    else if (entry->kind == NodeKind::Call) {
        Node* callee = entry->left;
        if (builtin(callee, "performance", "now")) {
            if (lookup(c, "performance") != 0 || entry->children != 0) { fail(c, callee->token, "performance.now requires no arguments and no shadowing"); }
            entry->type = primitive(c, TypeKind::Number);
        }
        else if (builtin(callee, "console", "log")) {
            if (lookup(c, "console") != 0) { fail(c, callee->token, "shadowing console is not implemented"); }
            Node* argument = entry->children;
            while (argument != 0) {
                if (check_expression(c, argument)->kind == TypeKind::Void) { fail(c, argument->token, "void log argument"); }
                argument = argument->next;
            }
            entry->type = primitive(c, TypeKind::Void);
        }
        else {
            if (callee->kind != NodeKind::Name) { fail(c, callee->token, "only direct function calls are implemented"); }
            Symbol* symbol = lookup(c, callee->name);
            if (symbol == 0 || symbol->function == 0) { fail(c, callee->token, "expected declared function"); }
            callee->symbol = symbol;
            Node* parameter = symbol->function->children;
            Node* argument = entry->children;
            while (parameter != 0 && argument != 0) {
                Type* actual = check_expression(c, argument);
                if (!assignable(actual, parameter->type)) { fail(c, argument->token, "incompatible function argument"); }
                parameter = parameter->next; argument = argument->next;
            }
            if (parameter != 0 || argument != 0) { fail(c, entry->token, "wrong number of arguments"); }
            entry->type = symbol->type;
        }
    }
    else if (entry->kind == NodeKind::Assert) {
        Type* actual = check_expression(c, entry->left);
        TypeKind target = entry->type->kind;
        if (actual->kind == TypeKind::Void || target == TypeKind::Void) { fail(c, entry->token, "void assertion"); }
        if (actual->kind != TypeKind::Any && actual->kind != TypeKind::Unknown
            && target != TypeKind::Any && target != TypeKind::Unknown
            && !fits(actual, entry->type) && !fits(entry->type, actual)) {
            fail(c, entry->token, "assertion between incompatible types");
        }
    }
    else if (entry->kind == NodeKind::Unary) {
        check_expression(c, entry->left);
        if (same(entry->token->text, "!")) {
            require_kind(c, entry->left, TypeKind::Boolean, "condition must be boolean");
            entry->type = primitive(c, TypeKind::Boolean);
        }
        else {
            require_kind(c, entry->left, TypeKind::Number, "numeric operand required");
            entry->type = primitive(c, TypeKind::Number);
        }
    }
    else if (entry->kind == NodeKind::Update) {
        check_expression(c, entry->left); writable(c, entry->left);
        if (entry->left->type->kind != TypeKind::Number) { fail(c, entry->token, "updates currently require a static number"); }
        entry->type = entry->left->type;
    }
    else if (entry->kind == NodeKind::Assign || entry->kind == NodeKind::Binary) {
        Type* left = check_expression(c, entry->left);
        Type* right = check_expression(c, entry->right);
        const char* op = entry->token->text;
        if (left->kind == TypeKind::Void || right->kind == TypeKind::Void) { fail(c, entry->token, "void operand"); }
        if (entry->kind == NodeKind::Assign) {
            writable(c, entry->left);
            if (!assignable(right, left)) { fail(c, entry->token, "incompatible assignment"); }
            if (!same(op, "=")) {
                if (left->kind != TypeKind::Number || right->kind != TypeKind::Number) {
                    fail(c, entry->token, "compound assignment currently requires static numbers");
                }
            }
            entry->type = left;
        }
        else if (same(op, "===") || same(op, "!==")) { entry->type = primitive(c, TypeKind::Boolean); }
        else if (same(op, "&&") || same(op, "||")) {
            require_kind(c, entry->left, TypeKind::Boolean, "logical operand must be boolean");
            require_kind(c, entry->right, TypeKind::Boolean, "logical operand must be boolean");
            entry->type = primitive(c, TypeKind::Boolean);
        }
        else if (same(op, "+") && (left->kind == TypeKind::Any || right->kind == TypeKind::Any)) {
            if (left->kind == TypeKind::Unknown || right->kind == TypeKind::Unknown) { fail(c, entry->token, "unknown operand requires assertion"); }
            entry->type = primitive(c, TypeKind::Any);
        }
        else if (same(op, "+") && (left->kind == TypeKind::String || right->kind == TypeKind::String)) {
            if (left->kind == TypeKind::Unknown || right->kind == TypeKind::Unknown) { fail(c, entry->token, "unknown operand requires assertion"); }
            entry->type = primitive(c, TypeKind::String);
        }
        else {
            require_kind(c, entry->left, TypeKind::Number, "numeric operand required");
            require_kind(c, entry->right, TypeKind::Number, "numeric operand required");
            bool comparison = same(op, "<") || same(op, ">") || same(op, "<=") || same(op, ">=");
            entry->type = primitive(c, comparison ? TypeKind::Boolean : TypeKind::Number);
        }
    }
    if (entry->type == 0) { fail(c, entry->token, "unimplemented expression"); }
    return entry->type;
}

// Return whether every path returns. Loops conservatively may execute zero times.
unsafe bool check_statement(Compiler* c, Node* entry) {
    if (entry->kind == NodeKind::Block) {
        Symbol* saved = c->symbols; c->scope++;
        bool returns = false;
        Node* child = entry->children;
        while (child != 0) {
            if (check_statement(c, child)) { returns = true; }
            child = child->next;
        }
        c->symbols = saved; c->scope--;
        return returns;
    }
    if (entry->kind == NodeKind::Variable) {
        Type* actual = check_expression(c, entry->left);
        if (entry->type == 0) { entry->type = actual; }
        if (!assignable(actual, entry->type)) { fail(c, entry->token, "incompatible variable initializer"); }
        bind(c, entry);
    }
    else if (entry->kind == NodeKind::Return) {
        if (c->return_type == 0) { fail(c, entry->token, "return outside function"); }
        if (entry->left == 0) {
            if (c->return_type->kind != TypeKind::Void) { fail(c, entry->token, "missing return value"); }
        }
        else if (!assignable(check_expression(c, entry->left), c->return_type)) { fail(c, entry->token, "incompatible return value"); }
        return true;
    }
    else if (entry->kind == NodeKind::If || entry->kind == NodeKind::While) {
        check_expression(c, entry->left);
        require_kind(c, entry->left, TypeKind::Boolean, "condition must be boolean");
        Symbol* saved = c->symbols; c->scope++;
        bool first = check_statement(c, entry->right);
        c->symbols = saved; c->scope--;
        bool second = false;
        if (entry->third != 0) {
            c->scope++; second = check_statement(c, entry->third);
            c->symbols = saved; c->scope--;
        }
        return entry->kind == NodeKind::If && first && second;
    }
    else if (entry->kind == NodeKind::For) {
        Symbol* saved = c->symbols; c->scope++;
        check_statement(c, entry->left);
        check_expression(c, entry->right);
        require_kind(c, entry->right, TypeKind::Boolean, "condition must be boolean");
        check_expression(c, entry->third);
        check_statement(c, entry->fourth);
        c->symbols = saved; c->scope--;
    }
    else if (entry->kind == NodeKind::Expression) { check_expression(c, entry->left); }
    else { fail(c, entry->token, "unimplemented statement"); }
    return false;
}

unsafe void check_program(Compiler* c) {
    Node* function = c->functions;
    while (function != 0) { bind(c, function); function = function->next; }
    Symbol* functions = c->symbols;
    function = c->functions;
    while (function != 0) {
        c->scope++;
        Node* parameter = function->children;
        while (parameter != 0) {
            if (parameter->type->kind == TypeKind::Void) { fail(c, parameter->token, "void parameter"); }
            bind(c, parameter); parameter = parameter->next;
        }
        c->return_type = function->type;
        bool returns = check_statement(c, function->left);
        if (!returns && function->type->kind != TypeKind::Void) { fail(c, function->token, "function must return on every path"); }
        c->symbols = functions; c->scope--;
        function = function->next;
    }
    c->return_type = 0;
    c->scope++;
    Node* entry = c->statements;
    while (entry != 0) { check_statement(c, entry); entry = entry->next; }
}
