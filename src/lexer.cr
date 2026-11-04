import "model.cr";

bool name_start(char ch) {
    return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_' || ch == '$';
}
bool digit(char ch) { return ch >= '0' && ch <= '9'; }

unsafe Token* lex(Compiler* c, char* source) {
    Token* first = 0;
    Token* last = 0;
    size_t position = 0;
    int line = 1;
    int column = 1;
    if (strncmp(source, "\xef\xbb\xbf", 3) == 0) { position = 3; }
    while (true) {
        char ch = source[position];
        if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n') {
            position++;
            column++;
            if (ch == '\n') { line++; column = 1; }
            continue;
        }
        if (ch == '/' && source[position + 1] == '/') {
            while (source[position] != 0 && source[position] != '\n') { position++; column++; }
            continue;
        }
        Token* token = (Token*)allocate(c->arena, sizeof(Token));
        token->line = line;
        token->column = column;
        token->kind = TokenKind::Punctuation;
        size_t start = position;
        if (ch == '/' && source[position + 1] == '*') {
            position += 2; column += 2;
            while (source[position] != 0 && !(source[position] == '*' && source[position + 1] == '/')) {
                if (source[position] == '\n') { line++; column = 0; }
                position++; column++;
            }
            if (source[position] == 0) { token->text = copy_text(c->arena, "/*", 2); fail(c, token, "unterminated comment"); }
            position += 2; column += 2;
            continue;
        }
        if (ch == 0) { token->kind = TokenKind::End; }
        else if (name_start(ch)) {
            token->kind = TokenKind::Name;
            do { position++; } while (name_start(source[position]) || digit(source[position]));
        }
        else if (digit(ch) || (ch == '.' && digit(source[position + 1]))) {
            token->kind = TokenKind::Number;
            while (digit(source[position])) { position++; }
            if (source[position] == '.') {
                position++;
                while (digit(source[position])) { position++; }
            }
            if (source[position] == 'e' || source[position] == 'E') {
                position++;
                if (source[position] == '+' || source[position] == '-') { position++; }
                size_t exponent = position;
                while (digit(source[position])) { position++; }
                if (position == exponent) { token->text = copy_text(c->arena, source + start, position - start); fail(c, token, "expected exponent digits"); }
            }
        }
        else if (ch == '"' || ch == '\'') {
            token->kind = TokenKind::String;
            position++;
            while (source[position] != ch) {
                if (source[position] == 0 || source[position] == '\n' || source[position] == '\r') {
                    token->text = copy_text(c->arena, source + start, position - start);
                    fail(c, token, "unterminated string");
                }
                if (source[position] == '\\') {
                    position++;
                    char escape = source[position];
                    if (!(escape == 'n' || escape == 'r' || escape == 't' || escape == '\\' || escape == '\'' || escape == '"')) {
                        token->text = copy_text(c->arena, source + start, position - start);
                        fail(c, token, "unsupported string escape");
                    }
                }
                position++;
            }
            position++;
        }
        else {
            position++;
            char following = source[position];
            if ((ch == '=' || ch == '!') && following == '=' && source[position + 1] == '=') { position += 2; }
            else if ((ch == '+' || ch == '-') && (following == ch || following == '=')) { position++; }
            else if ((ch == '*' || ch == '/' || ch == '<' || ch == '>' || ch == '=' || ch == '!') && following == '=') { position++; }
            else if ((ch == '&' || ch == '|') && following == ch) { position++; }
        }
        token->text = copy_text(c->arena, source + start, position - start);
        column += (int)(position - start);
        if (first == 0) { first = token; } else { last->next = token; }
        last = token;
        if (token->kind == TokenKind::End) { return first; }
    }
    return first;
}
