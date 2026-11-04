import "lexer.cr";

bool at(const char[]& text, const Compiler& c) {
    return is_text(get_token(c.token, c).text, text, c);
}

TokenId take(Compiler& c) {
    TokenId result = c.token;
    if (get_token(result, c).kind != TokenKind::End) {
        c.token = get_token(result, c).next;
    }
    return result;
}

bool accept(const char[]& text, Compiler& c) {
    if (!at(text, c)) {
        return false;
    }
    take(c);
    return true;
}

TokenId expect(const char[]& text, Compiler& c) {
    if (!at(text, c)) {
        fail(c.token, text, c);
    }
    return take(c);
}

TokenId identifier(Compiler& c) {
    if (get_token(c.token, c).kind != TokenKind::Name) {
        fail(c.token, &text_expected_identifier, c);
    }
    return take(c);
}

TypeId type_name(Compiler& c) {
    TokenId token = identifier(c);
    if (is_text(get_token(token, c).text, &text_number, c)) {
        return primitive(TypeKind::Number, c);
    }
    if (is_text(get_token(token, c).text, &text_string, c)) {
        return primitive(TypeKind::String, c);
    }
    if (is_text(get_token(token, c).text, &text_boolean, c)) {
        return primitive(TypeKind::Boolean, c);
    }
    if (is_text(get_token(token, c).text, &text_void, c)) {
        return primitive(TypeKind::Void, c);
    }
    if (is_text(get_token(token, c).text, &text_any, c)) {
        return primitive(TypeKind::Any, c);
    }
    if (is_text(get_token(token, c).text, &text_unknown, c)) {
        return primitive(TypeKind::Unknown, c);
    }
    TypeId type = c.types;
    while (type != 0) {
        if (get_type(type, c).name != 0 && same(get_type(type, c).name, get_token(token, c).text, c)) {
            return type;
        }
        type = get_type(type, c).next;
    }
    return new_type(TypeKind::Shape, get_token(token, c).text, c);
}

void parse_shape(Compiler& c) {
    bool alias = accept(&text_type, c);
    if (!alias) {
        expect(&text_interface, c);
    }
    TokenId name = c.token;
    TypeId type = type_name(c);
    if (get_type(type, c).kind != TypeKind::Shape || get_type(type, c).defined) {
        fail(name, &text_duplicate_or_reserved_type_name, c);
    }
    set_type_defined(type, true, c);
    if (alias) {
        expect(&text_assign, c);
    }
    expect(&text_lbrace, c);
    FieldId last = 0;
    while (!accept(&text_rbrace, c)) {
        TokenId member = identifier(c);
        if (field(type, get_token(member, c).text, c) != 0) {
            fail(member, &text_duplicate_field, c);
        }
        expect(&text_colon, c);
        FieldId entry = new_field(c);
        set_field_name(entry, get_token(member, c).text, c);
        set_field_type(entry, type_name(c), c);
        if (get_type(get_field(entry, c).type, c).kind == TypeKind::Void) {
            fail(member, &text_void_field, c);
        }
        if (last == 0) {
            set_type_fields(type, entry, c);
        } else {
            set_field_next(last, entry, c);
        }
        last = entry;
        if (!at(&text_rbrace, c)) {
            if (!accept(&text_semicolon, c) && !accept(&text_comma, c)) {
                fail(c.token, &text_expected_field_separator, c);
            }
        }
    }
    accept(&text_semicolon, c);
}

int precedence(TextId op, const Compiler& c) {
    if (is_text(op, &text_assign, c) || is_text(op, &text_add_assign, c) ||
        is_text(op, &text_subtract_assign, c) || is_text(op, &text_multiply_assign, c) ||
        is_text(op, &text_divide_assign, c)) {
        return 1;
    }
    if (is_text(op, &text_or, c)) {
        return 2;
    }
    if (is_text(op, &text_and, c)) {
        return 3;
    }
    if (is_text(op, &text_strict_equal, c) || is_text(op, &text_strict_not_equal, c)) {
        return 4;
    }
    if (is_text(op, &text_less, c) || is_text(op, &text_greater, c) || is_text(op, &text_less_equal, c) ||
        is_text(op, &text_greater_equal, c)) {
        return 5;
    }
    if (is_text(op, &text_plus, c) || is_text(op, &text_minus, c)) {
        return 6;
    }
    if (is_text(op, &text_star, c) || is_text(op, &text_slash, c) || is_text(op, &text_modulo, c)) {
        return 7;
    }
    return 0;
}

NodeId primary(Compiler& c) {
    TokenId token = take(c);
    NodeId result = 0;
    if (get_token(token, c).kind == TokenKind::Number) {
        result = node(NodeKind::Number, token, c);
    } else if (get_token(token, c).kind == TokenKind::String) {
        result = node(NodeKind::String, token, c);
    } else if (is_text(get_token(token, c).text, &text_true, c) ||
               is_text(get_token(token, c).text, &text_false, c)) {
        result = node(NodeKind::Boolean, token, c);
    } else if (is_text(get_token(token, c).text, &text_lparen, c)) {
        result = expression(1, c);
        expect(&text_rparen, c);
    } else if (is_text(get_token(token, c).text, &text_minus, c) ||
               is_text(get_token(token, c).text, &text_plus, c) ||
               is_text(get_token(token, c).text, &text_not, c)) {
        result = node(NodeKind::Unary, token, c);
        set_node_left(result, expression(8, c), c);
    } else if (is_text(get_token(token, c).text, &text_lbrace, c)) {
        result = node(NodeKind::Object, token, c);
        NodeId last = 0;
        while (!accept(&text_rbrace, c)) {
            TokenId name = identifier(c);
            NodeId property = node(NodeKind::Property, name, c);
            set_node_name(property, get_token(name, c).text, c);
            expect(&text_colon, c);
            set_node_left(property, expression(1, c), c);
            set_node_children(result, append(get_node(result, c).children, &last, property, c), c);
            if (!accept(&text_comma, c)) {
                expect(&text_rbrace, c);
                break;
            }
        }
    } else if (get_token(token, c).kind == TokenKind::Name) {
        result = node(NodeKind::Name, token, c);
        set_node_name(result, get_token(token, c).text, c);
    } else {
        fail(token, &text_expected_expression_syntax_may_be_unsupported, c);
    }
    while (true) {
        if (accept(&text_dot, c)) {
            TokenId name = identifier(c);
            NodeId member = node(NodeKind::Property, name, c);
            set_node_left(member, result, c);
            set_node_name(member, get_token(name, c).text, c);
            result = member;
        } else if (accept(&text_lparen, c)) {
            NodeId call = node(NodeKind::Call, token, c);
            set_node_left(call, result, c);
            NodeId last = 0;
            while (!accept(&text_rparen, c)) {
                set_node_children(call, append(get_node(call, c).children, &last, expression(1, c), c), c);
                if (!accept(&text_comma, c)) {
                    expect(&text_rparen, c);
                    break;
                }
            }
            result = call;
        } else if (at(&text_increment, c) || at(&text_decrement, c)) {
            NodeId update = node(NodeKind::Update, take(c), c);
            set_node_left(update, result, c);
            result = update;
        } else {
            break;
        }
    }
    return result;
}

NodeId expression(int minimum, Compiler& c) {
    c.depth++;
    if (c.depth > 256) {
        fail(c.token, &text_expression_nesting_limit_exceeded, c);
    }
    NodeId left = primary(c);
    while (true) {
        if (at(&text_as, c) && minimum <= 5) {
            NodeId assertion = node(NodeKind::Assert, take(c), c);
            set_node_left(assertion, left, c);
            set_node_type(assertion, type_name(c), c);
            left = assertion;
            continue;
        }
        int level = precedence(get_token(c.token, c).text, c);
        if (level < minimum || level == 0) {
            break;
        }
        TokenId op = take(c);
        NodeKind kind = level == 1 ? NodeKind::Assign : NodeKind::Binary;
        NodeId binary = node(kind, op, c);
        set_node_left(binary, left, c);
        set_node_right(binary, expression(level == 1 ? level : level + 1, c), c);
        left = binary;
    }
    c.depth--;
    return left;
}

NodeId variable(Compiler& c) {
    bool constant = accept(&text_const, c);
    if (!constant) {
        expect(&text_let, c);
    }
    TokenId name = identifier(c);
    NodeId result = node(NodeKind::Variable, name, c);
    set_node_name(result, get_token(name, c).text, c);
    set_node_constant(result, constant, c);
    if (accept(&text_colon, c)) {
        set_node_type(result, type_name(c), c);
    }
    expect(&text_assign, c);
    set_node_left(result, expression(1, c), c);
    return result;
}

NodeId statement(Compiler& c) {
    c.depth++;
    if (c.depth > 256) {
        fail(c.token, &text_statement_nesting_limit_exceeded, c);
    }
    TokenId token = c.token;
    NodeId result = 0;
    if (accept(&text_lbrace, c)) {
        result = node(NodeKind::Block, token, c);
        NodeId last = 0;
        while (!accept(&text_rbrace, c)) {
            if (get_token(c.token, c).kind == TokenKind::End) {
                fail(c.token, &text_unterminated_block, c);
            }
            set_node_children(result, append(get_node(result, c).children, &last, statement(c), c), c);
        }
    } else if (at(&text_const, c) || at(&text_let, c)) {
        result = variable(c);
        expect(&text_semicolon, c);
    } else if (accept(&text_return, c)) {
        result = node(NodeKind::Return, token, c);
        if (!at(&text_semicolon, c)) {
            set_node_left(result, expression(1, c), c);
        }
        expect(&text_semicolon, c);
    } else if (accept(&text_if, c)) {
        result = node(NodeKind::If, token, c);
        expect(&text_lparen, c);
        set_node_left(result, expression(1, c), c);
        expect(&text_rparen, c);
        set_node_right(result, statement(c), c);
        if (accept(&text_else, c)) {
            set_node_third(result, statement(c), c);
        }
    } else if (accept(&text_while, c)) {
        result = node(NodeKind::While, token, c);
        expect(&text_lparen, c);
        set_node_left(result, expression(1, c), c);
        expect(&text_rparen, c);
        set_node_right(result, statement(c), c);
    } else if (accept(&text_for, c)) {
        result = node(NodeKind::For, token, c);
        expect(&text_lparen, c);
        set_node_left(result, variable(c), c);
        expect(&text_semicolon, c);
        set_node_right(result, expression(1, c), c);
        expect(&text_semicolon, c);
        set_node_third(result, expression(1, c), c);
        expect(&text_rparen, c);
        set_node_fourth(result, statement(c), c);
    } else {
        result = node(NodeKind::Expression, token, c);
        set_node_left(result, expression(1, c), c);
        expect(&text_semicolon, c);
    }
    c.depth--;
    return result;
}

void parse(Compiler& c) {
    NodeId last_function = 0;
    NodeId last_statement = 0;
    while (get_token(c.token, c).kind != TokenKind::End) {
        if (at(&text_type, c) || at(&text_interface, c)) {
            parse_shape(c);
        } else if (accept(&text_function, c)) {
            TokenId name = identifier(c);
            NodeId function = node(NodeKind::Function, name, c);
            set_node_name(function, get_token(name, c).text, c);
            expect(&text_lparen, c);
            NodeId last = 0;
            while (!accept(&text_rparen, c)) {
                TokenId parameter = identifier(c);
                NodeId entry = node(NodeKind::Variable, parameter, c);
                set_node_name(entry, get_token(parameter, c).text, c);
                expect(&text_colon, c);
                set_node_type(entry, type_name(c), c);
                set_node_children(function, append(get_node(function, c).children, &last, entry, c), c);
                if (!accept(&text_comma, c)) {
                    expect(&text_rparen, c);
                    break;
                }
            }
            expect(&text_colon, c);
            set_node_type(function, type_name(c), c);
            if (!at(&text_lbrace, c)) {
                fail(c.token, &text_expected_function_body, c);
            }
            set_node_left(function, statement(c), c);
            c.functions = append(c.functions, &last_function, function, c);
        } else {
            c.statements = append(c.statements, &last_statement, statement(c), c);
        }
    }
    TypeId type = c.types;
    while (type != 0) {
        if (get_type(type, c).kind == TypeKind::Shape && !get_type(type, c).defined) {
            fail(c.token, &text_undefined_type_name, c);
        }
        type = get_type(type, c).next;
    }
}
