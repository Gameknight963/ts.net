import "model.cr";
bool name_start(char ch) {
    return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_' || ch == '$';
}
bool digit(char ch) {
    return ch >= '0' && ch <= '9';
}

TokenId lex(Compiler& c) {
    TokenId first = 0;
    TokenId last = 0;
    size_t position = 0;
    int line = 1;
    int column = 1;
    if (((int)c.source.peek(0) & 255) == 239 && ((int)c.source.peek(1) & 255) == 187 &&
        ((int)c.source.peek(2) & 255) == 191) {
        position = 3;
    }
    while (true) {
        char ch = c.source.peek(position);
        if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n') {
            position++;
            column++;
            if (ch == '\n') {
                line++;
                column = 1;
            }
            continue;
        }
        if (ch == '/' && c.source.peek(position + 1) == '/') {
            while (c.source.peek(position) != 0 && c.source.peek(position) != '\n') {
                position++;
                column++;
            }
            continue;
        }
        TokenId token = new_token(c);
        set_token_line(token, line, c);
        set_token_column(token, column, c);
        set_token_kind(token, TokenKind::Punctuation, c);
        size_t start = position;
        if (ch == '/' && c.source.peek(position + 1) == '*') {
            position += 2;
            column += 2;
            while (c.source.peek(position) != 0 &&
                   !(c.source.peek(position) == '*' && c.source.peek(position + 1) == '/')) {
                if (c.source.peek(position) == '\n') {
                    line++;
                    column = 0;
                }
                position++;
                column++;
            }
            if (c.source.peek(position) == 0) {
                set_token_text(token, source_text(start, 2, c), c);
                fail(token, &text_unterminated_comment, c);
            }
            position += 2;
            column += 2;
            continue;
        }
        if (ch == 0) {
            set_token_kind(token, TokenKind::End, c);
        } else if (name_start(ch)) {
            set_token_kind(token, TokenKind::Name, c);
            do {
                position++;
            } while (name_start(c.source.peek(position)) || digit(c.source.peek(position)));
        } else if (digit(ch) || (ch == '.' && digit(c.source.peek(position + 1)))) {
            set_token_kind(token, TokenKind::Number, c);
            while (digit(c.source.peek(position))) {
                position++;
            }
            if (c.source.peek(position) == '.') {
                position++;
                while (digit(c.source.peek(position))) {
                    position++;
                }
            }
            if (c.source.peek(position) == 'e' || c.source.peek(position) == 'E') {
                position++;
                if (c.source.peek(position) == '+' || c.source.peek(position) == '-') {
                    position++;
                }
                size_t exponent = position;
                while (digit(c.source.peek(position))) {
                    position++;
                }
                if (position == exponent) {
                    set_token_text(token, source_text(start, position - start, c), c);
                    fail(token, &text_expected_exponent_digits, c);
                }
            }
        } else if (ch == '"' || ch == '\'') {
            set_token_kind(token, TokenKind::String, c);
            position++;
            while (c.source.peek(position) != ch) {
                if (c.source.peek(position) == 0 || c.source.peek(position) == '\n' ||
                    c.source.peek(position) == '\r') {
                    set_token_text(token, source_text(start, position - start, c), c);
                    fail(token, &text_unterminated_string, c);
                }
                if (c.source.peek(position) == '\\') {
                    position++;
                    char escape = c.source.peek(position);
                    if (!(escape == 'n' || escape == 'r' || escape == 't' || escape == '\\' ||
                          escape == '\'' || escape == '"')) {
                        set_token_text(token, source_text(start, position - start, c), c);
                        fail(token, &text_unsupported_string_escape, c);
                    }
                }
                position++;
            }
            position++;
        } else {
            position++;
            char following = c.source.peek(position);
            if ((ch == '=' || ch == '!') && following == '=' && c.source.peek(position + 1) == '=') {
                position += 2;
            } else if ((ch == '+' || ch == '-') && (following == ch || following == '=')) {
                position++;
            } else if ((ch == '*' || ch == '/' || ch == '<' || ch == '>' || ch == '=' || ch == '!') &&
                       following == '=') {
                position++;
            } else if ((ch == '&' || ch == '|') && following == ch) {
                position++;
            }
        }
        set_token_text(token, source_text(start, position - start, c), c);
        column += (int)(position - start);
        if (first == 0) {
            first = token;
        } else {
            set_token_next(last, token, c);
        }
        last = token;
        if (get_token(token, c).kind == TokenKind::End) {
            return first;
        }
    }
    return first;
}
