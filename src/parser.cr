import "lexer.cr";

unsafe bool at(Compiler* c, const char* text) { return same(c->token->text, text); }

unsafe Token* take(Compiler* c) {
    Token* result = c->token;
    if (result->kind != TokenKind::End) { c->token = result->next; }
    return result;
}

unsafe bool accept(Compiler* c, const char* text) {
    if (!at(c, text)) { return false; }
    take(c);
    return true;
}

unsafe Token* expect(Compiler* c, const char* text) {
    if (!at(c, text)) { fail(c, c->token, text); }
    return take(c);
}

unsafe Token* identifier(Compiler* c) {
    if (c->token->kind != TokenKind::Name) { fail(c, c->token, "expected identifier"); }
    return take(c);
}

unsafe void append(Node** first, Node** last, Node* value) {
    if (*first == 0) { *first = value; } else { (*last)->next = value; }
    *last = value;
}

unsafe Type* type_name(Compiler* c) {
    Token* token = identifier(c);
    if (same(token->text, "number")) { return primitive(c, TypeKind::Number); }
    if (same(token->text, "string")) { return primitive(c, TypeKind::String); }
    if (same(token->text, "boolean")) { return primitive(c, TypeKind::Boolean); }
    if (same(token->text, "void")) { return primitive(c, TypeKind::Void); }
    if (same(token->text, "any")) { return primitive(c, TypeKind::Any); }
    if (same(token->text, "unknown")) { return primitive(c, TypeKind::Unknown); }
    Type* type = c->types;
    while (type != 0) {
        if (type->name != 0 && same(type->name, token->text)) { return type; }
        type = type->next;
    }
    return new_type(c, TypeKind::Shape, token->text);
}

unsafe void parse_shape(Compiler* c) {
    bool alias = accept(c, "type");
    if (!alias) { expect(c, "interface"); }
    Token* name = c->token;
    Type* type = type_name(c);
    if (type->kind != TypeKind::Shape || type->defined) { fail(c, name, "duplicate or reserved type name"); }
    type->defined = true;
    if (alias) { expect(c, "="); }
    expect(c, "{");
    Field* last = 0;
    while (!accept(c, "}")) {
        Token* member = identifier(c);
        if (field(type, member->text) != 0) { fail(c, member, "duplicate field"); }
        expect(c, ":");
        Field* entry = (Field*)allocate(c->arena, sizeof(Field));
        entry->name = member->text;
        entry->type = type_name(c);
        if (entry->type->kind == TypeKind::Void) { fail(c, member, "void field"); }
        if (last == 0) { type->fields = entry; } else { last->next = entry; }
        last = entry;
        if (!at(c, "}")) {
            if (!accept(c, ";") && !accept(c, ",")) { fail(c, c->token, "expected field separator"); }
        }
    }
    accept(c, ";");
}

unsafe int precedence(const char* op) {
    if (same(op, "=") || same(op, "+=") || same(op, "-=") || same(op, "*=") || same(op, "/=")) { return 1; }
    if (same(op, "||")) { return 2; }
    if (same(op, "&&")) { return 3; }
    if (same(op, "===") || same(op, "!==")) { return 4; }
    if (same(op, "<") || same(op, ">") || same(op, "<=") || same(op, ">=")) { return 5; }
    if (same(op, "+") || same(op, "-")) { return 6; }
    if (same(op, "*") || same(op, "/") || same(op, "%")) { return 7; }
    return 0;
}

unsafe Node* primary(Compiler* c) {
    Token* token = take(c);
    Node* result = 0;
    if (token->kind == TokenKind::Number) { result = node(c, NodeKind::Number, token); }
    else if (token->kind == TokenKind::String) { result = node(c, NodeKind::String, token); }
    else if (same(token->text, "true") || same(token->text, "false")) { result = node(c, NodeKind::Boolean, token); }
    else if (same(token->text, "(")) { result = expression(c, 1); expect(c, ")"); }
    else if (same(token->text, "-") || same(token->text, "+") || same(token->text, "!")) {
        result = node(c, NodeKind::Unary, token);
        result->left = expression(c, 8);
    }
    else if (same(token->text, "{")) {
        result = node(c, NodeKind::Object, token);
        Node* last = 0;
        while (!accept(c, "}")) {
            Token* name = identifier(c);
            Node* property = node(c, NodeKind::Property, name);
            property->name = name->text;
            expect(c, ":");
            property->left = expression(c, 1);
            append(&result->children, &last, property);
            if (!accept(c, ",")) { expect(c, "}"); break; }
        }
    }
    else if (token->kind == TokenKind::Name) {
        result = node(c, NodeKind::Name, token);
        result->name = token->text;
    }
    else { fail(c, token, "expected expression (syntax may be unsupported)"); }

    while (true) {
        if (accept(c, ".")) {
            Token* name = identifier(c);
            Node* member = node(c, NodeKind::Property, name);
            member->left = result;
            member->name = name->text;
            result = member;
        }
        else if (accept(c, "(")) {
            Node* call = node(c, NodeKind::Call, token);
            call->left = result;
            Node* last = 0;
            while (!accept(c, ")")) {
                append(&call->children, &last, expression(c, 1));
                if (!accept(c, ",")) { expect(c, ")"); break; }
            }
            result = call;
        }
        else if (at(c, "++") || at(c, "--")) {
            Node* update = node(c, NodeKind::Update, take(c));
            update->left = result;
            result = update;
        }
        else { break; }
    }
    return result;
}

unsafe Node* expression(Compiler* c, int minimum) {
    c->depth++;
    if (c->depth > 256) { fail(c, c->token, "expression nesting limit exceeded"); }
    Node* left = primary(c);
    while (true) {
        if (at(c, "as") && minimum <= 5) {
            Node* assertion = node(c, NodeKind::Assert, take(c));
            assertion->left = left;
            assertion->type = type_name(c);
            left = assertion;
            continue;
        }
        int level = precedence(c->token->text);
        if (level < minimum || level == 0) { break; }
        Token* op = take(c);
        NodeKind kind = level == 1 ? NodeKind::Assign : NodeKind::Binary;
        Node* binary = node(c, kind, op);
        binary->left = left;
        binary->right = expression(c, level == 1 ? level : level + 1);
        left = binary;
    }
    c->depth--;
    return left;
}

unsafe Node* variable(Compiler* c) {
    bool constant = accept(c, "const");
    if (!constant) { expect(c, "let"); }
    Token* name = identifier(c);
    Node* result = node(c, NodeKind::Variable, name);
    result->name = name->text;
    result->constant = constant;
    if (accept(c, ":")) { result->type = type_name(c); }
    expect(c, "=");
    result->left = expression(c, 1);
    return result;
}

unsafe Node* statement(Compiler* c) {
    c->depth++;
    if (c->depth > 256) { fail(c, c->token, "statement nesting limit exceeded"); }
    Token* token = c->token;
    Node* result = 0;
    if (accept(c, "{")) {
        result = node(c, NodeKind::Block, token);
        Node* last = 0;
        while (!accept(c, "}")) {
            if (c->token->kind == TokenKind::End) { fail(c, c->token, "unterminated block"); }
            append(&result->children, &last, statement(c));
        }
    }
    else if (at(c, "const") || at(c, "let")) { result = variable(c); expect(c, ";"); }
    else if (accept(c, "return")) {
        result = node(c, NodeKind::Return, token);
        if (!at(c, ";")) { result->left = expression(c, 1); }
        expect(c, ";");
    }
    else if (accept(c, "if")) {
        result = node(c, NodeKind::If, token);
        expect(c, "("); result->left = expression(c, 1); expect(c, ")");
        result->right = statement(c);
        if (accept(c, "else")) { result->third = statement(c); }
    }
    else if (accept(c, "while")) {
        result = node(c, NodeKind::While, token);
        expect(c, "("); result->left = expression(c, 1); expect(c, ")");
        result->right = statement(c);
    }
    else if (accept(c, "for")) {
        result = node(c, NodeKind::For, token);
        expect(c, "(");
        result->left = variable(c); expect(c, ";");
        result->right = expression(c, 1); expect(c, ";");
        result->third = expression(c, 1); expect(c, ")");
        result->fourth = statement(c);
    }
    else {
        result = node(c, NodeKind::Expression, token);
        result->left = expression(c, 1);
        expect(c, ";");
    }
    c->depth--;
    return result;
}

unsafe void parse(Compiler* c) {
    Node* last_function = 0;
    Node* last_statement = 0;
    while (c->token->kind != TokenKind::End) {
        if (at(c, "type") || at(c, "interface")) { parse_shape(c); }
        else if (accept(c, "function")) {
            Token* name = identifier(c);
            Node* function = node(c, NodeKind::Function, name);
            function->name = name->text;
            expect(c, "(");
            Node* last = 0;
            while (!accept(c, ")")) {
                Token* parameter = identifier(c);
                Node* entry = node(c, NodeKind::Variable, parameter);
                entry->name = parameter->text;
                expect(c, ":"); entry->type = type_name(c);
                append(&function->children, &last, entry);
                if (!accept(c, ",")) { expect(c, ")"); break; }
            }
            expect(c, ":"); function->type = type_name(c);
            if (!at(c, "{")) { fail(c, c->token, "expected function body"); }
            function->left = statement(c);
            append(&c->functions, &last_function, function);
        }
        else { append(&c->statements, &last_statement, statement(c)); }
    }
    Type* type = c->types;
    while (type != 0) {
        if (type->kind == TypeKind::Shape && !type->defined) { fail(c, c->token, "undefined type name"); }
        type = type->next;
    }
}
