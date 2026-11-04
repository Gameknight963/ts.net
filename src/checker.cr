import "parser.cr";

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

bool assignable(TypeId source, TypeId target, const Compiler& c) {
    if (get_type(source, c).kind == TypeKind::Void || get_type(target, c).kind == TypeKind::Void) {
        return false;
    }
    if (get_type(target, c).kind == TypeKind::Any || get_type(target, c).kind == TypeKind::Unknown) {
        return true;
    }
    if (get_type(source, c).kind == TypeKind::Any) {
        return true;
    }
    return fits(source, target, c);
}

SymbolId lookup(TextId name, const Compiler& c) {
    SymbolId symbol = c.symbols;
    while (symbol != 0) {
        if (same(get_symbol(symbol, c).name, name, c)) {
            return symbol;
        }
        symbol = get_symbol(symbol, c).next;
    }
    return 0;
}

SymbolId bind(NodeId entry, Compiler& c) {
    SymbolId previous = lookup(get_node(entry, c).name, c);
    if (previous != 0 && get_symbol(previous, c).scope == c.scope) {
        fail(get_node(entry, c).token, &text_duplicate_binding, c);
    }
    SymbolId symbol = new_symbol(c);
    set_symbol_name(symbol, get_node(entry, c).name, c);
    set_symbol_type(symbol, get_node(entry, c).type, c);
    set_symbol_scope(symbol, c.scope, c);
    set_symbol_id(symbol, c.next_symbol++, c);
    set_symbol_constant(symbol, get_node(entry, c).constant, c);
    if (get_node(entry, c).kind == NodeKind::Function) {
        set_symbol_function(symbol, entry, c);
        set_symbol_constant(symbol, true, c);
    }
    set_symbol_next(symbol, c.symbols, c);
    c.symbols = symbol;
    set_node_symbol(entry, symbol, c);
    return symbol;
}

void require_kind(NodeId entry, TypeKind kind, const char[]& message, const Compiler& c) {
    if (get_type(get_node(entry, c).type, c).kind != kind &&
        get_type(get_node(entry, c).type, c).kind != TypeKind::Any) {
        fail(get_node(entry, c).token, message, c);
    }
}

void writable(NodeId entry, const Compiler& c) {
    if (get_node(entry, c).kind == NodeKind::Name) {
        if (get_symbol(get_node(entry, c).symbol, c).constant) {
            fail(get_node(entry, c).token, &text_cannot_assign_to_const_binding, c);
        }
    } else if (get_node(entry, c).kind == NodeKind::Property) {
        if (get_type(get_node(get_node(entry, c).left, c).type, c).kind == TypeKind::String) {
            fail(get_node(entry, c).token, &text_string_length_is_read_only, c);
        }
    } else {
        fail(get_node(entry, c).token, &text_expected_assignment_target, c);
    }
}

bool builtin(NodeId callee, const char[]& owner, const char[]& member, const Compiler& c) {
    return get_node(callee, c).kind == NodeKind::Property && is_text(get_node(callee, c).name, member, c) &&
           get_node(get_node(callee, c).left, c).kind == NodeKind::Name &&
           is_text(get_node(get_node(callee, c).left, c).name, owner, c);
}

TypeId check_expression(NodeId entry, Compiler& c) {
    if (get_node(entry, c).kind == NodeKind::Number) {
        set_node_type(entry, primitive(TypeKind::Number, c), c);
    } else if (get_node(entry, c).kind == NodeKind::String) {
        set_node_type(entry, primitive(TypeKind::String, c), c);
    } else if (get_node(entry, c).kind == NodeKind::Boolean) {
        set_node_type(entry, primitive(TypeKind::Boolean, c), c);
    } else if (get_node(entry, c).kind == NodeKind::Name) {
        SymbolId symbol = lookup(get_node(entry, c).name, c);
        if (symbol == 0) {
            fail(get_node(entry, c).token, &text_unknown_binding, c);
        }
        if (get_symbol(symbol, c).function != 0) {
            fail(get_node(entry, c).token, &text_function_values_are_not_implemented_yet, c);
        }
        set_node_symbol(entry, symbol, c);
        set_node_type(entry, get_symbol(symbol, c).type, c);
    } else if (get_node(entry, c).kind == NodeKind::Object) {
        TypeId type = new_type(TypeKind::Shape, 0, c);
        set_type_defined(type, true, c);
        FieldId last = 0;
        NodeId property = get_node(entry, c).children;
        while (property != 0) {
            if (field(type, get_node(property, c).name, c) != 0) {
                fail(get_node(property, c).token, &text_duplicate_object_property, c);
            }
            FieldId member = new_field(c);
            set_field_name(member, get_node(property, c).name, c);
            set_field_type(member, check_expression(get_node(property, c).left, c), c);
            if (get_type(get_field(member, c).type, c).kind == TypeKind::Void) {
                fail(get_node(property, c).token, &text_void_object_property, c);
            }
            if (last == 0) {
                set_type_fields(type, member, c);
            } else {
                set_field_next(last, member, c);
            }
            last = member;
            property = get_node(property, c).next;
        }
        set_node_type(entry, type, c);
    } else if (get_node(entry, c).kind == NodeKind::Property) {
        TypeId receiver = check_expression(get_node(entry, c).left, c);
        if (get_type(receiver, c).kind == TypeKind::Any) {
            set_node_type(entry, primitive(TypeKind::Any, c), c);
        } else if (get_type(receiver, c).kind == TypeKind::String &&
                   is_text(get_node(entry, c).name, &text_length, c)) {
            set_node_type(entry, primitive(TypeKind::Number, c), c);
        } else {
            if (get_type(receiver, c).kind != TypeKind::Shape) {
                fail(get_node(entry, c).token, &text_property_access_requires_object_or_any, c);
            }
            FieldId member = field(receiver, get_node(entry, c).name, c);
            if (member == 0) {
                fail(get_node(entry, c).token, &text_object_has_no_such_property, c);
            }
            set_node_type(entry, get_field(member, c).type, c);
        }
    } else if (get_node(entry, c).kind == NodeKind::Call) {
        NodeId callee = get_node(entry, c).left;
        if (builtin(callee, &text_performance, &text_now, c)) {
            if (lookup_named(&text_performance, c) != 0 || get_node(entry, c).children != 0) {
                fail(get_node(callee, c).token, &text_invalid_performance_call, c);
            }
            set_node_type(entry, primitive(TypeKind::Number, c), c);
        } else if (builtin(callee, &text_console, &text_log, c)) {
            if (lookup_named(&text_console, c) != 0) {
                fail(get_node(callee, c).token, &text_shadowing_console_is_not_implemented, c);
            }
            NodeId argument = get_node(entry, c).children;
            while (argument != 0) {
                if (get_type(check_expression(argument, c), c).kind == TypeKind::Void) {
                    fail(get_node(argument, c).token, &text_void_log_argument, c);
                }
                argument = get_node(argument, c).next;
            }
            set_node_type(entry, primitive(TypeKind::Void, c), c);
        } else {
            if (get_node(callee, c).kind != NodeKind::Name) {
                fail(get_node(callee, c).token, &text_only_direct_function_calls_are_implemented, c);
            }
            SymbolId symbol = lookup(get_node(callee, c).name, c);
            if (symbol == 0 || get_symbol(symbol, c).function == 0) {
                fail(get_node(callee, c).token, &text_expected_declared_function, c);
            }
            set_node_symbol(callee, symbol, c);
            NodeId parameter = get_node(get_symbol(symbol, c).function, c).children;
            NodeId argument = get_node(entry, c).children;
            while (parameter != 0 && argument != 0) {
                TypeId actual = check_expression(argument, c);
                if (!assignable(actual, get_node(parameter, c).type, c)) {
                    fail(get_node(argument, c).token, &text_incompatible_function_argument, c);
                }
                parameter = get_node(parameter, c).next;
                argument = get_node(argument, c).next;
            }
            if (parameter != 0 || argument != 0) {
                fail(get_node(entry, c).token, &text_wrong_number_of_arguments, c);
            }
            set_node_type(entry, get_symbol(symbol, c).type, c);
        }
    } else if (get_node(entry, c).kind == NodeKind::Assert) {
        TypeId actual = check_expression(get_node(entry, c).left, c);
        TypeKind target = get_type(get_node(entry, c).type, c).kind;
        if (get_type(actual, c).kind == TypeKind::Void || target == TypeKind::Void) {
            fail(get_node(entry, c).token, &text_void_assertion, c);
        }
        if (get_type(actual, c).kind != TypeKind::Any && get_type(actual, c).kind != TypeKind::Unknown &&
            target != TypeKind::Any && target != TypeKind::Unknown &&
            !fits(actual, get_node(entry, c).type, c) && !fits(get_node(entry, c).type, actual, c)) {
            fail(get_node(entry, c).token, &text_assertion_between_incompatible_types, c);
        }
    } else if (get_node(entry, c).kind == NodeKind::Unary) {
        check_expression(get_node(entry, c).left, c);
        if (is_text(get_token(get_node(entry, c).token, c).text, &text_not, c)) {
            require_kind(get_node(entry, c).left, TypeKind::Boolean, &text_condition_must_be_boolean, c);
            set_node_type(entry, primitive(TypeKind::Boolean, c), c);
        } else {
            require_kind(get_node(entry, c).left, TypeKind::Number, &text_numeric_operand_required, c);
            set_node_type(entry, primitive(TypeKind::Number, c), c);
        }
    } else if (get_node(entry, c).kind == NodeKind::Update) {
        check_expression(get_node(entry, c).left, c);
        writable(get_node(entry, c).left, c);
        if (get_type(get_node(get_node(entry, c).left, c).type, c).kind != TypeKind::Number) {
            fail(get_node(entry, c).token, &text_updates_currently_require_a_static_number, c);
        }
        set_node_type(entry, get_node(get_node(entry, c).left, c).type, c);
    } else if (get_node(entry, c).kind == NodeKind::Assign || get_node(entry, c).kind == NodeKind::Binary) {
        TypeId left = check_expression(get_node(entry, c).left, c);
        TypeId right = check_expression(get_node(entry, c).right, c);
        TextId op = get_token(get_node(entry, c).token, c).text;
        if (get_type(left, c).kind == TypeKind::Void || get_type(right, c).kind == TypeKind::Void) {
            fail(get_node(entry, c).token, &text_void_operand, c);
        }
        if (get_node(entry, c).kind == NodeKind::Assign) {
            writable(get_node(entry, c).left, c);
            if (!assignable(right, left, c)) {
                fail(get_node(entry, c).token, &text_incompatible_assignment, c);
            }
            if (!is_text(op, &text_assign, c)) {
                if (get_type(left, c).kind != TypeKind::Number ||
                    get_type(right, c).kind != TypeKind::Number) {
                    fail(get_node(entry, c).token,
                         &text_compound_assignment_currently_requires_static_numbers, c);
                }
            }
            set_node_type(entry, left, c);
        } else if (is_text(op, &text_strict_equal, c) || is_text(op, &text_strict_not_equal, c)) {
            set_node_type(entry, primitive(TypeKind::Boolean, c), c);
        } else if (is_text(op, &text_and, c) || is_text(op, &text_or, c)) {
            require_kind(get_node(entry, c).left, TypeKind::Boolean, &text_logical_operand_must_be_boolean,
                         c);
            require_kind(get_node(entry, c).right, TypeKind::Boolean, &text_logical_operand_must_be_boolean,
                         c);
            set_node_type(entry, primitive(TypeKind::Boolean, c), c);
        } else if (is_text(op, &text_plus, c) &&
                   (get_type(left, c).kind == TypeKind::Any || get_type(right, c).kind == TypeKind::Any)) {
            if (get_type(left, c).kind == TypeKind::Unknown || get_type(right, c).kind == TypeKind::Unknown) {
                fail(get_node(entry, c).token, &text_unknown_operand_requires_assertion, c);
            }
            set_node_type(entry, primitive(TypeKind::Any, c), c);
        } else if (is_text(op, &text_plus, c) && (get_type(left, c).kind == TypeKind::String ||
                                                  get_type(right, c).kind == TypeKind::String)) {
            if (get_type(left, c).kind == TypeKind::Unknown || get_type(right, c).kind == TypeKind::Unknown) {
                fail(get_node(entry, c).token, &text_unknown_operand_requires_assertion, c);
            }
            set_node_type(entry, primitive(TypeKind::String, c), c);
        } else {
            require_kind(get_node(entry, c).left, TypeKind::Number, &text_numeric_operand_required, c);
            require_kind(get_node(entry, c).right, TypeKind::Number, &text_numeric_operand_required, c);
            bool comparison = is_text(op, &text_less, c) || is_text(op, &text_greater, c) ||
                              is_text(op, &text_less_equal, c) || is_text(op, &text_greater_equal, c);
            set_node_type(entry, primitive(comparison ? TypeKind::Boolean : TypeKind::Number, c), c);
        }
    }
    if (get_node(entry, c).type == 0) {
        fail(get_node(entry, c).token, &text_unimplemented_expression, c);
    }
    return get_node(entry, c).type;
}

bool check_statement(NodeId entry, Compiler& c) {
    if (get_node(entry, c).kind == NodeKind::Block) {
        SymbolId saved = c.symbols;
        c.scope++;
        bool returns = false;
        NodeId child = get_node(entry, c).children;
        while (child != 0) {
            if (check_statement(child, c)) {
                returns = true;
            }
            child = get_node(child, c).next;
        }
        c.symbols = saved;
        c.scope--;
        return returns;
    }
    if (get_node(entry, c).kind == NodeKind::Variable) {
        TypeId actual = check_expression(get_node(entry, c).left, c);
        if (get_node(entry, c).type == 0) {
            set_node_type(entry, actual, c);
        }
        if (!assignable(actual, get_node(entry, c).type, c)) {
            fail(get_node(entry, c).token, &text_incompatible_variable_initializer, c);
        }
        bind(entry, c);
    } else if (get_node(entry, c).kind == NodeKind::Return) {
        if (c.return_type == 0) {
            fail(get_node(entry, c).token, &text_return_outside_function, c);
        }
        if (get_node(entry, c).left == 0) {
            if (get_type(c.return_type, c).kind != TypeKind::Void) {
                fail(get_node(entry, c).token, &text_missing_return_value, c);
            }
        } else if (!assignable(check_expression(get_node(entry, c).left, c), c.return_type, c)) {
            fail(get_node(entry, c).token, &text_incompatible_return_value, c);
        }
        return true;
    } else if (get_node(entry, c).kind == NodeKind::If || get_node(entry, c).kind == NodeKind::While) {
        check_expression(get_node(entry, c).left, c);
        require_kind(get_node(entry, c).left, TypeKind::Boolean, &text_condition_must_be_boolean, c);
        SymbolId saved = c.symbols;
        c.scope++;
        bool first = check_statement(get_node(entry, c).right, c);
        c.symbols = saved;
        c.scope--;
        bool second = false;
        if (get_node(entry, c).third != 0) {
            c.scope++;
            second = check_statement(get_node(entry, c).third, c);
            c.symbols = saved;
            c.scope--;
        }
        return get_node(entry, c).kind == NodeKind::If && first && second;
    } else if (get_node(entry, c).kind == NodeKind::For) {
        SymbolId saved = c.symbols;
        c.scope++;
        check_statement(get_node(entry, c).left, c);
        check_expression(get_node(entry, c).right, c);
        require_kind(get_node(entry, c).right, TypeKind::Boolean, &text_condition_must_be_boolean, c);
        check_expression(get_node(entry, c).third, c);
        check_statement(get_node(entry, c).fourth, c);
        c.symbols = saved;
        c.scope--;
    } else if (get_node(entry, c).kind == NodeKind::Expression) {
        check_expression(get_node(entry, c).left, c);
    } else {
        fail(get_node(entry, c).token, &text_unimplemented_statement, c);
    }
    return false;
}

void check_program(Compiler& c) {
    NodeId function = c.functions;
    while (function != 0) {
        bind(function, c);
        function = get_node(function, c).next;
    }
    SymbolId functions = c.symbols;
    function = c.functions;
    while (function != 0) {
        c.scope++;
        NodeId parameter = get_node(function, c).children;
        while (parameter != 0) {
            if (get_type(get_node(parameter, c).type, c).kind == TypeKind::Void) {
                fail(get_node(parameter, c).token, &text_void_parameter, c);
            }
            bind(parameter, c);
            parameter = get_node(parameter, c).next;
        }
        c.return_type = get_node(function, c).type;
        bool returns = check_statement(get_node(function, c).left, c);
        if (!returns && get_type(get_node(function, c).type, c).kind != TypeKind::Void) {
            fail(get_node(function, c).token, &text_function_must_return_on_every_path, c);
        }
        c.symbols = functions;
        c.scope--;
        function = get_node(function, c).next;
    }
    c.return_type = 0;
    c.scope++;
    NodeId entry = c.statements;
    while (entry != 0) {
        check_statement(entry, c);
        entry = get_node(entry, c).next;
    }
}

SymbolId lookup_named(const char[]& name, const Compiler& c) {
    SymbolId current = c.symbols;
    while (current != 0) {
        if (is_text(get_symbol(current, c).name, name, c)) {
            return current;
        }
        current = get_symbol(current, c).next;
    }
    return 0;
}
