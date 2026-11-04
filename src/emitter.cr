import "checker.cr";

unsafe void cs_type(FILE* output, Type* type) {
    if (type->kind == TypeKind::Number) { fputs("double", output); }
    else if (type->kind == TypeKind::String) { fputs("string", output); }
    else if (type->kind == TypeKind::Boolean) { fputs("bool", output); }
    else if (type->kind == TypeKind::Void) { fputs("void", output); }
    else if (type->kind == TypeKind::Shape) { fprintf(output, "I%d", type->id); }
    else { fputs("object", output); }
}

unsafe void cs_string(FILE* output, const char* text) {
    fputc('"', output);
    for (size_t i = 0; text[i] != 0; i++) {
        char ch = text[i];
        if (ch == '"' || ch == '\\') { fputc('\\', output); fputc(ch, output); }
        else if (ch == '\n') { fputs("\\n", output); }
        else if (ch == '\r') { fputs("\\r", output); }
        else if (ch == '\t') { fputs("\\t", output); }
        else { fputc(ch, output); }
    }
    fputc('"', output);
}

unsafe void cs_property(FILE* output, const char* name) {
    fputs("p_", output);
    for (size_t i = 0; name[i] != 0; i++) { fprintf(output, "%02x", (unsigned int)(unsigned char)name[i]); }
}

unsafe void emit_as(FILE* output, Node* entry, Type* target) {
    bool checked = target->kind != TypeKind::Any && target->kind != TypeKind::Unknown
        && (entry->type->kind == TypeKind::Any || entry->type->kind == TypeKind::Unknown
            || (target->kind == TypeKind::Shape && entry->type != target) || !fits(entry->type, target));
    if (checked) { fputs("Js.Cast<", output); cs_type(output, target); fputs(">(", output); }
    emit_expression(output, entry);
    if (checked) { fprintf(output, ", \"line %d: checked conversion failed\")", entry->token->line); }
}

unsafe void emit_expression(FILE* output, Node* entry) {
    NodeKind kind = entry->kind;
    if (kind == NodeKind::Number) {
        double value = strtod(entry->token->text, 0);
        if (value > 1.7976931348623157e308) { fputs("double.PositiveInfinity", output); }
        else { fprintf(output, "%.17gd", value); }
    }
    else if (kind == NodeKind::Boolean) { fputs(entry->token->text, output); }
    else if (kind == NodeKind::String) {
        const char* text = entry->token->text;
        size_t length = strlen(text);
        fputc('"', output);
        for (size_t i = 1; i + 1 < length; i++) {
            char ch = text[i];
            if (ch == '\\') {
                i++;
                if (text[i] == '\'') { fputc('\'', output); }
                else { fputc('\\', output); fputc(text[i], output); }
            }
            else if (ch == '"') { fputs("\\\"", output); }
            else { fputc(ch, output); }
        }
        fputc('"', output);
    }
    else if (kind == NodeKind::Name) { fprintf(output, "v%d", entry->symbol->id); }
    else if (kind == NodeKind::Object) {
        fprintf(output, "new O%d { ", entry->type->id);
        Node* property = entry->children;
        while (property != 0) {
            cs_property(output, property->name); fputs(" = ", output);
            emit_expression(output, property->left);
            fputs(", ", output); property = property->next;
        }
        fputs("}", output);
    }
    else if (kind == NodeKind::Property) {
        if (entry->left->type->kind == TypeKind::Any) {
            fputs("Js.Get(", output); emit_expression(output, entry->left);
            fputs(", ", output); cs_string(output, entry->name); fputs(")", output);
        }
        else if (entry->left->type->kind == TypeKind::String) {
            fputs("((double)(", output); emit_expression(output, entry->left); fputs(").Length)", output);
        }
        else { fputs("(", output); emit_expression(output, entry->left); fputs(").", output); cs_property(output, entry->name); }
    }
    else if (kind == NodeKind::Call) {
        Node* argument = entry->children;
        if (builtin(entry->left, "performance", "now")) { fputs("Js.Now(", output); }
        else if (builtin(entry->left, "console", "log")) {
            fputs("Js.Log(", output);
            while (argument != 0) {
                emit_expression(output, argument);
                argument = argument->next; if (argument != 0) { fputs(", ", output); }
            }
        }
        else {
            fprintf(output, "f%d(", entry->left->symbol->id);
            Node* parameter = entry->left->symbol->function->children;
            while (argument != 0) {
                emit_as(output, argument, parameter->type);
                argument = argument->next; parameter = parameter->next;
                if (argument != 0) { fputs(", ", output); }
            }
        }
        fputs(")", output);
    }
    else if (kind == NodeKind::Assert) { emit_as(output, entry->left, entry->type); }
    else if (kind == NodeKind::Unary) {
        fprintf(output, "(%s", entry->token->text);
        fputs("(", output);
        if (entry->left->type->kind == TypeKind::Any) {
            fputs(same(entry->token->text, "!") ? "Js.Boolean(" : "Js.Number(", output);
            emit_expression(output, entry->left); fputs(")", output);
        }
        else { emit_expression(output, entry->left); }
        fputs("))", output);
    }
    else if (kind == NodeKind::Update) {
        // C# only accepts an unparenthesized assignment target for updates.
        emit_expression(output, entry->left); fputs(entry->token->text, output);
    }
    else if (kind == NodeKind::Assign) {
        if (entry->left->kind == NodeKind::Property && entry->left->left->type->kind == TypeKind::Any) {
            fputs("Js.Set(", output); emit_expression(output, entry->left->left);
            fputs(", ", output); cs_string(output, entry->left->name);
            fputs(", ", output); emit_expression(output, entry->right); fputs(")", output);
        }
        else {
            emit_expression(output, entry->left);
            fprintf(output, " %s ", entry->token->text);
            emit_as(output, entry->right, entry->left->type);
        }
    }
    else if (kind == NodeKind::Binary) {
        const char* op = entry->token->text;
        if (same(op, "===") || same(op, "!==")) {
            if (same(op, "!==")) { fputs("!", output); }
            fputs("Js.Equal(", output); emit_expression(output, entry->left);
            fputs(", ", output); emit_expression(output, entry->right); fputs(")", output);
        }
        else if (same(op, "+") && entry->type->kind == TypeKind::Any) {
            fputs("Js.Add(", output); emit_expression(output, entry->left);
            fputs(", ", output); emit_expression(output, entry->right); fputs(")", output);
        }
        else if (same(op, "+") && entry->type->kind == TypeKind::String) {
            fputs("(Js.Text(", output); emit_expression(output, entry->left);
            fputs(") + Js.Text(", output); emit_expression(output, entry->right); fputs("))", output);
        }
        else {
            bool logical = same(op, "&&") || same(op, "||");
            fputs("(", output);
            if (entry->left->type->kind == TypeKind::Any) { fputs(logical ? "Js.Boolean(" : "Js.Number(", output); }
            fputs("(", output); emit_expression(output, entry->left); fputs(")", output);
            if (entry->left->type->kind == TypeKind::Any) { fputs(")", output); }
            fprintf(output, " %s ", op);
            if (entry->right->type->kind == TypeKind::Any) { fputs(logical ? "Js.Boolean(" : "Js.Number(", output); }
            fputs("(", output); emit_expression(output, entry->right); fputs(")", output);
            if (entry->right->type->kind == TypeKind::Any) { fputs(")", output); }
            fputs(")", output);
        }
    }
}

unsafe void emit_condition(FILE* output, Node* entry) {
    if (entry->type->kind == TypeKind::Any) { fputs("Js.Boolean(", output); }
    emit_expression(output, entry);
    if (entry->type->kind == TypeKind::Any) { fputs(")", output); }
}

unsafe void emit_variable(FILE* output, Node* entry) {
    cs_type(output, entry->type); fprintf(output, " v%d = ", entry->symbol->id);
    emit_as(output, entry->left, entry->type);
}

unsafe void emit_statement(FILE* output, Node* entry, Type* return_type) {
    if (entry->kind == NodeKind::Block) {
        fputs("{\n", output);
        Node* child = entry->children;
        while (child != 0) { emit_statement(output, child, return_type); child = child->next; }
        fputs("}\n", output);
    }
    else if (entry->kind == NodeKind::Variable) { emit_variable(output, entry); fputs(";\n", output); }
    else if (entry->kind == NodeKind::Return) {
        fputs("return", output);
        if (entry->left != 0) { fputs(" ", output); emit_as(output, entry->left, return_type); }
        fputs(";\n", output);
    }
    else if (entry->kind == NodeKind::If || entry->kind == NodeKind::While) {
        fputs(entry->kind == NodeKind::If ? "if (" : "while (", output);
        emit_condition(output, entry->left); fputs(") {\n", output);
        emit_statement(output, entry->right, return_type); fputs("}\n", output);
        if (entry->third != 0) {
            fputs("else {\n", output); emit_statement(output, entry->third, return_type); fputs("}\n", output);
        }
    }
    else if (entry->kind == NodeKind::For) {
        fputs("for (", output); emit_variable(output, entry->left); fputs("; ", output);
        emit_condition(output, entry->right); fputs("; ", output);
        // Discarding a non-statement expression is legal in TS, so use a helper.
        if (entry->third->kind == NodeKind::Assign || entry->third->kind == NodeKind::Update || entry->third->kind == NodeKind::Call) {
            emit_expression(output, entry->third);
        }
        else { fputs("Js.Discard(", output); emit_expression(output, entry->third); fputs(")", output); }
        fputs(") {\n", output); emit_statement(output, entry->fourth, return_type); fputs("}\n", output);
    }
    else if (entry->kind == NodeKind::Expression) {
        Node* value = entry->left;
        if (value->kind == NodeKind::Assign || value->kind == NodeKind::Update || value->kind == NodeKind::Call) {
            emit_expression(output, value);
        }
        else { fputs("Js.Discard(", output); emit_expression(output, value); fputs(")", output); }
        fputs(";\n", output);
    }
}

unsafe void emit_shapes(Compiler* c, FILE* output) {
    Type* type = c->types;
    while (type != 0) {
        if (type->kind == TypeKind::Shape) {
            fprintf(output, "interface I%d {\n", type->id);
            Field* member = type->fields;
            while (member != 0) {
                cs_type(output, member->type); fputs(" ", output); cs_property(output, member->name);
                fputs(" { get; set; }\n", output); member = member->next;
            }
            fputs("}\n", output);
        }
        type = type->next;
    }
    type = c->types;
    while (type != 0) {
        if (type->kind == TypeKind::Shape) {
            fprintf(output, "sealed class O%d : TsObject", type->id);
            Type* target = c->types;
            while (target != 0) {
                if (fits(type, target)) { fprintf(output, ", I%d", target->id); }
                target = target->next;
            }
            fputs(" {\n", output);
            Field* member = type->fields;
            while (member != 0) {
                fputs("public ", output); cs_type(output, member->type); fputs(" ", output);
                cs_property(output, member->name); fputs(" { get; set; }\n", output);
                member = member->next;
            }
            fputs("public override object Get(string key) { switch (key) {\n", output);
            member = type->fields;
            while (member != 0) {
                fputs("case ", output); cs_string(output, member->name); fputs(": return ", output);
                cs_property(output, member->name); fputs(";\n", output); member = member->next;
            }
            fputs("default: return base.Get(key); } }\n", output);
            fputs("public override object Set(string key, object value) { switch (key) {\n", output);
            member = type->fields;
            while (member != 0) {
                fputs("case ", output); cs_string(output, member->name); fputs(": ", output);
                cs_property(output, member->name); fputs(" = Js.Cast<", output); cs_type(output, member->type);
                fputs(">(value, \"property write violates static field type\"); return value;\n", output);
                member = member->next;
            }
            fputs("default: return base.Set(key, value); } }\n}\n", output);
        }
        type = type->next;
    }
}

unsafe void emit_program(Compiler* c, const char* path) {
    FILE* output = fopen(path, "wb");
    if (output == 0) { printf("tsnetc: cannot write %s\n", path); exit(1); }
    fputs("// Generated by the Crust tsnetc prototype.\nusing System;\n", output);
    emit_shapes(c, output);
    fputs("static class Program {\n", output);
    Node* function = c->functions;
    while (function != 0) {
        fputs("static ", output); cs_type(output, function->type);
        fprintf(output, " f%d(", function->symbol->id);
        Node* parameter = function->children;
        while (parameter != 0) {
            cs_type(output, parameter->type); fprintf(output, " v%d", parameter->symbol->id);
            parameter = parameter->next; if (parameter != 0) { fputs(", ", output); }
        }
        fputs(")\n", output); emit_statement(output, function->left, function->type);
        function = function->next;
    }
    fputs("public static void Main() {\n", output);
    Node* entry = c->statements;
    while (entry != 0) { emit_statement(output, entry, 0); entry = entry->next; }
    fputs("}\n}\n", output);
    bool failed = ferror(output) != 0;
    if (fclose(output) != 0) { failed = true; }
    if (failed) { puts("tsnetc: output write failed"); exit(1); }
}
