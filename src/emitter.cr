import "checker.cr";

void cs_type(Output& output, TypeId type, const Compiler& c) {
    if (get_type(type, c).kind == TypeKind::Number) {
        output.bytes(&text_double);
    } else if (get_type(type, c).kind == TypeKind::String) {
        output.bytes(&text_string);
    } else if (get_type(type, c).kind == TypeKind::Boolean) {
        output.bytes(&text_bool);
    } else if (get_type(type, c).kind == TypeKind::Void) {
        output.bytes(&text_void);
    } else if (get_type(type, c).kind == TypeKind::Shape) {
        output.bytes(&text_interface_prefix);
        output.integer(get_type(type, c).id);
    } else {
        output.bytes(&text_object);
    }
}

void cs_string(Output& output, TextId text, const Compiler& c) {
    output.put('"');
    for (size_t i = 0; text_byte(text, i, c) != 0; i++) {
        char ch = text_byte(text, i, c);
        if (ch == '"' || ch == '\\') {
            output.put('\\');
            output.put(ch);
        } else if (ch == '\n') {
            output.bytes(&text_escape_newline);
        } else if (ch == '\r') {
            output.bytes(&text_escape_return);
        } else if (ch == '\t') {
            output.bytes(&text_escape_tab);
        } else {
            output.put(ch);
        }
    }
    output.put('"');
}

void cs_property(Output& output, TextId name, const Compiler& c) {
    output.bytes(&text_property_prefix);
    for (size_t i = 0; text_byte(name, i, c) != 0; i++) {
        output.hex(text_byte(name, i, c));
    }
}

void emit_as(Output& output, NodeId entry, TypeId target, const Compiler& c) {
    bool checked = get_type(target, c).kind != TypeKind::Any &&
                   get_type(target, c).kind != TypeKind::Unknown &&
                   (get_type(get_node(entry, c).type, c).kind == TypeKind::Any ||
                    get_type(get_node(entry, c).type, c).kind == TypeKind::Unknown ||
                    (get_type(target, c).kind == TypeKind::Shape && get_node(entry, c).type != target) ||
                    !fits(get_node(entry, c).type, target, c));
    if (checked) {
        output.bytes(&text_js_cast);
        cs_type(output, target, c);
        output.bytes(&text_generic_call_open);
    }
    emit_expression(output, entry, c);
    if (checked) {
        output.bytes(&text_line);
        output.integer(get_token(get_node(entry, c).token, c).line);
        output.bytes(&text_checked_conversion_failed);
    }
}

void emit_expression(Output& output, NodeId entry, const Compiler& c) {
    NodeKind kind = get_node(entry, c).kind;
    if (kind == NodeKind::Number) {
        double value = number(get_token(get_node(entry, c).token, c).text, c);
        if (value > 1.7976931348623157e308) {
            output.bytes(&text_double_positiveinfinity);
        } else {
            output.number(value);
            output.bytes(&text_number_suffix);
        }
    } else if (kind == NodeKind::Boolean) {
        write(get_token(get_node(entry, c).token, c).text, output, c);
    } else if (kind == NodeKind::String) {
        TextId text = get_token(get_node(entry, c).token, c).text;
        size_t length = text_size(text, c);
        output.put('"');
        for (size_t i = 1; i + 1 < length; i++) {
            char ch = text_byte(text, i, c);
            if (ch == '\\') {
                i++;
                if (text_byte(text, i, c) == '\'') {
                    output.put('\'');
                } else {
                    output.put('\\');
                    output.put(text_byte(text, i, c));
                }
            } else if (ch == '"') {
                output.bytes(&text_escaped_quote);
            } else {
                output.put(ch);
            }
        }
        output.put('"');
    } else if (kind == NodeKind::Name) {
        output.bytes(&text_variable_prefix);
        output.integer(get_symbol(get_node(entry, c).symbol, c).id);
    } else if (kind == NodeKind::Object) {
        output.bytes(&text_new_o);
        output.integer(get_type(get_node(entry, c).type, c).id);
        output.bytes(&text_object_initializer_open);
        NodeId property = get_node(entry, c).children;
        while (property != 0) {
            cs_property(output, get_node(property, c).name, c);
            output.bytes(&text_spaced_assign);
            emit_expression(output, get_node(property, c).left, c);
            output.bytes(&text_argument_separator);
            property = get_node(property, c).next;
        }
        output.bytes(&text_rbrace);
    } else if (kind == NodeKind::Property) {
        if (get_type(get_node(get_node(entry, c).left, c).type, c).kind == TypeKind::Any) {
            output.bytes(&text_js_get);
            emit_expression(output, get_node(entry, c).left, c);
            output.bytes(&text_argument_separator);
            cs_string(output, get_node(entry, c).name, c);
            output.bytes(&text_rparen);
        } else if (get_type(get_node(get_node(entry, c).left, c).type, c).kind == TypeKind::String) {
            output.bytes(&text_string_length_prefix);
            emit_expression(output, get_node(entry, c).left, c);
            output.bytes(&text_string_length_suffix);
        } else {
            output.bytes(&text_lparen);
            emit_expression(output, get_node(entry, c).left, c);
            output.bytes(&text_member_access);
            cs_property(output, get_node(entry, c).name, c);
        }
    } else if (kind == NodeKind::Call) {
        NodeId argument = get_node(entry, c).children;
        if (builtin(get_node(entry, c).left, &text_performance, &text_now, c)) {
            output.bytes(&text_js_now);
        } else if (builtin(get_node(entry, c).left, &text_console, &text_log, c)) {
            output.bytes(&text_js_log);
            while (argument != 0) {
                emit_expression(output, argument, c);
                argument = get_node(argument, c).next;
                if (argument != 0) {
                    output.bytes(&text_argument_separator);
                }
            }
        } else {
            output.bytes(&text_function_prefix);
            output.integer(get_symbol(get_node(get_node(entry, c).left, c).symbol, c).id);
            output.bytes(&text_lparen);
            NodeId parameter =
                get_node(get_symbol(get_node(get_node(entry, c).left, c).symbol, c).function, c).children;
            while (argument != 0) {
                emit_as(output, argument, get_node(parameter, c).type, c);
                argument = get_node(argument, c).next;
                parameter = get_node(parameter, c).next;
                if (argument != 0) {
                    output.bytes(&text_argument_separator);
                }
            }
        }
        output.bytes(&text_rparen);
    } else if (kind == NodeKind::Assert) {
        emit_as(output, get_node(entry, c).left, get_node(entry, c).type, c);
    } else if (kind == NodeKind::Unary) {
        output.bytes(&text_lparen);
        write(get_token(get_node(entry, c).token, c).text, output, c);
        output.bytes(&text_lparen);
        if (get_type(get_node(get_node(entry, c).left, c).type, c).kind == TypeKind::Any) {
            if (is_text(get_token(get_node(entry, c).token, c).text, &text_not, c)) {
                output.bytes(&text_js_boolean);
            } else {
                output.bytes(&text_js_number);
            }
            emit_expression(output, get_node(entry, c).left, c);
            output.bytes(&text_rparen);
        } else {
            emit_expression(output, get_node(entry, c).left, c);
        }
        output.bytes(&text_close_two_parens);
    } else if (kind == NodeKind::Update) {
        emit_expression(output, get_node(entry, c).left, c);
        write(get_token(get_node(entry, c).token, c).text, output, c);
    } else if (kind == NodeKind::Assign) {
        if (get_node(get_node(entry, c).left, c).kind == NodeKind::Property &&
            get_type(get_node(get_node(get_node(entry, c).left, c).left, c).type, c).kind == TypeKind::Any) {
            output.bytes(&text_js_set);
            emit_expression(output, get_node(get_node(entry, c).left, c).left, c);
            output.bytes(&text_argument_separator);
            cs_string(output, get_node(get_node(entry, c).left, c).name, c);
            output.bytes(&text_argument_separator);
            emit_expression(output, get_node(entry, c).right, c);
            output.bytes(&text_rparen);
        } else {
            emit_expression(output, get_node(entry, c).left, c);
            output.bytes(&text_space);
            write(get_token(get_node(entry, c).token, c).text, output, c);
            output.bytes(&text_space);
            emit_as(output, get_node(entry, c).right, get_node(get_node(entry, c).left, c).type, c);
        }
    } else if (kind == NodeKind::Binary) {
        TextId op = get_token(get_node(entry, c).token, c).text;
        if (is_text(op, &text_strict_equal, c) || is_text(op, &text_strict_not_equal, c)) {
            if (is_text(op, &text_strict_not_equal, c)) {
                output.bytes(&text_not);
            }
            output.bytes(&text_js_equal);
            emit_expression(output, get_node(entry, c).left, c);
            output.bytes(&text_argument_separator);
            emit_expression(output, get_node(entry, c).right, c);
            output.bytes(&text_rparen);
        } else if (is_text(op, &text_plus, c) && get_type(get_node(entry, c).type, c).kind == TypeKind::Any) {
            output.bytes(&text_js_add);
            emit_expression(output, get_node(entry, c).left, c);
            output.bytes(&text_argument_separator);
            emit_expression(output, get_node(entry, c).right, c);
            output.bytes(&text_rparen);
        } else if (is_text(op, &text_plus, c) &&
                   get_type(get_node(entry, c).type, c).kind == TypeKind::String) {
            output.bytes(&text_concatenation_start);
            emit_expression(output, get_node(entry, c).left, c);
            output.bytes(&text_concatenation_next);
            emit_expression(output, get_node(entry, c).right, c);
            output.bytes(&text_close_two_parens);
        } else {
            bool logical = is_text(op, &text_and, c) || is_text(op, &text_or, c);
            output.bytes(&text_lparen);
            if (get_type(get_node(get_node(entry, c).left, c).type, c).kind == TypeKind::Any) {
                if (logical) {
                    output.bytes(&text_js_boolean);
                } else {
                    output.bytes(&text_js_number);
                }
            }
            output.bytes(&text_lparen);
            emit_expression(output, get_node(entry, c).left, c);
            output.bytes(&text_rparen);
            if (get_type(get_node(get_node(entry, c).left, c).type, c).kind == TypeKind::Any) {
                output.bytes(&text_rparen);
            }
            output.bytes(&text_space);
            write(op, output, c);
            output.bytes(&text_space);
            if (get_type(get_node(get_node(entry, c).right, c).type, c).kind == TypeKind::Any) {
                if (logical) {
                    output.bytes(&text_js_boolean);
                } else {
                    output.bytes(&text_js_number);
                }
            }
            output.bytes(&text_lparen);
            emit_expression(output, get_node(entry, c).right, c);
            output.bytes(&text_rparen);
            if (get_type(get_node(get_node(entry, c).right, c).type, c).kind == TypeKind::Any) {
                output.bytes(&text_rparen);
            }
            output.bytes(&text_rparen);
        }
    }
}

void emit_condition(Output& output, NodeId entry, const Compiler& c) {
    if (get_type(get_node(entry, c).type, c).kind == TypeKind::Any) {
        output.bytes(&text_js_boolean);
    }
    emit_expression(output, entry, c);
    if (get_type(get_node(entry, c).type, c).kind == TypeKind::Any) {
        output.bytes(&text_rparen);
    }
}

void emit_variable(Output& output, NodeId entry, const Compiler& c) {
    cs_type(output, get_node(entry, c).type, c);
    output.bytes(&text_variable_declaration_prefix);
    output.integer(get_symbol(get_node(entry, c).symbol, c).id);
    output.bytes(&text_spaced_assign);
    emit_as(output, get_node(entry, c).left, get_node(entry, c).type, c);
}

void emit_statement(Output& output, NodeId entry, TypeId return_type, const Compiler& c) {
    if (get_node(entry, c).kind == NodeKind::Block) {
        output.bytes(&text_block_open);
        NodeId child = get_node(entry, c).children;
        while (child != 0) {
            emit_statement(output, child, return_type, c);
            child = get_node(child, c).next;
        }
        output.bytes(&text_block_close);
    } else if (get_node(entry, c).kind == NodeKind::Variable) {
        emit_variable(output, entry, c);
        output.bytes(&text_statement_end);
    } else if (get_node(entry, c).kind == NodeKind::Return) {
        output.bytes(&text_return);
        if (get_node(entry, c).left != 0) {
            output.bytes(&text_space);
            emit_as(output, get_node(entry, c).left, return_type, c);
        }
        output.bytes(&text_statement_end);
    } else if (get_node(entry, c).kind == NodeKind::If || get_node(entry, c).kind == NodeKind::While) {
        if (get_node(entry, c).kind == NodeKind::If) {
            output.bytes(&text_if_2);
        } else {
            output.bytes(&text_while_2);
        }
        emit_condition(output, get_node(entry, c).left, c);
        output.bytes(&text_control_body_open);
        emit_statement(output, get_node(entry, c).right, return_type, c);
        output.bytes(&text_block_close);
        if (get_node(entry, c).third != 0) {
            output.bytes(&text_else_2);
            emit_statement(output, get_node(entry, c).third, return_type, c);
            output.bytes(&text_block_close);
        }
    } else if (get_node(entry, c).kind == NodeKind::For) {
        output.bytes(&text_for_2);
        emit_variable(output, get_node(entry, c).left, c);
        output.bytes(&text_for_separator);
        emit_condition(output, get_node(entry, c).right, c);
        output.bytes(&text_for_separator);
        if (get_node(get_node(entry, c).third, c).kind == NodeKind::Assign ||
            get_node(get_node(entry, c).third, c).kind == NodeKind::Update ||
            get_node(get_node(entry, c).third, c).kind == NodeKind::Call) {
            emit_expression(output, get_node(entry, c).third, c);
        } else {
            output.bytes(&text_js_discard);
            emit_expression(output, get_node(entry, c).third, c);
            output.bytes(&text_rparen);
        }
        output.bytes(&text_control_body_open);
        emit_statement(output, get_node(entry, c).fourth, return_type, c);
        output.bytes(&text_block_close);
    } else if (get_node(entry, c).kind == NodeKind::Expression) {
        NodeId value = get_node(entry, c).left;
        if (get_node(value, c).kind == NodeKind::Assign || get_node(value, c).kind == NodeKind::Update ||
            get_node(value, c).kind == NodeKind::Call) {
            emit_expression(output, value, c);
        } else {
            output.bytes(&text_js_discard);
            emit_expression(output, value, c);
            output.bytes(&text_rparen);
        }
        output.bytes(&text_statement_end);
    }
}

void emit_shapes(Output& output, const Compiler& c) {
    TypeId type = c.types;
    while (type != 0) {
        if (get_type(type, c).kind == TypeKind::Shape) {
            output.bytes(&text_interface_i);
            output.integer(get_type(type, c).id);
            output.bytes(&text_type_body_open);
            FieldId member = get_type(type, c).fields;
            while (member != 0) {
                cs_type(output, get_field(member, c).type, c);
                output.bytes(&text_space);
                cs_property(output, get_field(member, c).name, c);
                output.bytes(&text_get_set);
                member = get_field(member, c).next;
            }
            output.bytes(&text_block_close);
        }
        type = get_type(type, c).next;
    }
    type = c.types;
    while (type != 0) {
        if (get_type(type, c).kind == TypeKind::Shape) {
            output.bytes(&text_sealed_class_o);
            output.integer(get_type(type, c).id);
            output.bytes(&text_tsobject);
            TypeId target = c.types;
            while (target != 0) {
                if (fits(type, target, c)) {
                    output.bytes(&text_interface_separator);
                    output.integer(get_type(target, c).id);
                }
                target = get_type(target, c).next;
            }
            output.bytes(&text_type_body_open);
            FieldId member = get_type(type, c).fields;
            while (member != 0) {
                output.bytes(&text_public);
                cs_type(output, get_field(member, c).type, c);
                output.bytes(&text_space);
                cs_property(output, get_field(member, c).name, c);
                output.bytes(&text_get_set);
                member = get_field(member, c).next;
            }
            output.bytes(&text_public_override_object_get_string_key_switch);
            member = get_type(type, c).fields;
            while (member != 0) {
                output.bytes(&text_case);
                cs_string(output, get_field(member, c).name, c);
                output.bytes(&text_return_2);
                cs_property(output, get_field(member, c).name, c);
                output.bytes(&text_statement_end);
                member = get_field(member, c).next;
            }
            output.bytes(&text_default_return_base_get_key);
            output.bytes(&text_public_override_object_set_string_key_object);
            member = get_type(type, c).fields;
            while (member != 0) {
                output.bytes(&text_case);
                cs_string(output, get_field(member, c).name, c);
                output.bytes(&text_case_separator);
                cs_property(output, get_field(member, c).name, c);
                output.bytes(&text_js_cast_2);
                cs_type(output, get_field(member, c).type, c);
                output.bytes(&text_value_property_write_violates_static_field_type);
                member = get_field(member, c).next;
            }
            output.bytes(&text_default_return_base_set_key_value);
        }
        type = get_type(type, c).next;
    }
}

void emit_program(const char[]& path, const Compiler& c) {
    Output output = Output::open(path);
    output.bytes(&text_generated_by_the_crust_tsnetc_prototype_using);
    emit_shapes(&output, c);
    output.bytes(&text_static_class_program);
    NodeId function = c.functions;
    while (function != 0) {
        output.bytes(&text_static);
        cs_type(&output, get_node(function, c).type, c);
        output.bytes(&text_function_declaration_prefix);
        output.integer(get_symbol(get_node(function, c).symbol, c).id);
        output.bytes(&text_lparen);
        NodeId parameter = get_node(function, c).children;
        while (parameter != 0) {
            cs_type(&output, get_node(parameter, c).type, c);
            output.bytes(&text_variable_declaration_prefix);
            output.integer(get_symbol(get_node(parameter, c).symbol, c).id);
            parameter = get_node(parameter, c).next;
            if (parameter != 0) {
                output.bytes(&text_argument_separator);
            }
        }
        output.bytes(&text_signature_end);
        emit_statement(&output, get_node(function, c).left, get_node(function, c).type, c);
        function = get_node(function, c).next;
    }
    output.bytes(&text_public_static_void_main);
    NodeId entry = c.statements;
    while (entry != 0) {
        emit_statement(&output, entry, 0, c);
        entry = get_node(entry, c).next;
    }
    output.bytes(&text_program_end);
    if (!output.finish()) {
        print("tsnetc: output write failed");
        exit(1);
    }
}
