import "support.cr";
import "literals.cr";

// The compiler owns every store and the source bytes addressed by TextRanges.
// Context arguments come last so nested computations finish before a mutable
// borrow of Compiler is established. No record reference escapes a getter.

typedef size_t TokenId;
typedef size_t TypeId;
typedef size_t FieldId;
typedef size_t NodeId;
typedef size_t SymbolId;
typedef size_t TextId;
struct TextRange {
    size_t start;
    size_t length;
};

enum class TokenKind { End, Name, Number, String, Punctuation };
enum class TypeKind { Number, String, Boolean, Void, Any, Unknown, Shape, Array };
enum class NodeKind {
    Number,
    String,
    Boolean,
    Name,
    Object,
    Property,
    Call,
    Binary,
    Unary,
    Assert,
    Assign,
    Update,
    Block,
    Variable,
    Return,
    If,
    While,
    For,
    Expression,
    Function,
    Undefined,
    Null,
    Conditional,
    TypeOf,
    Do,
    Break,
    Continue,
    Switch,
    Case,
    Label,
    Nullish,
    Sequence,
    Array,
    Index,
    ForOf,
    Spread
};
struct Token {
    TokenKind kind;
    TextId text;
    int line;
    int column;
    TokenId next;
};
struct Type {
    TypeKind kind;
    TextId name;
    int id;
    FieldId fields;
    TypeId element;
    bool defined;
    TypeId next;
};
struct Field {
    TextId name;
    TypeId type;
    FieldId next;
    bool optional;
};
struct Node {
    NodeKind kind;
    TokenId token;
    TextId name;
    TypeId type;
    NodeId left;
    NodeId right;
    NodeId third;
    NodeId fourth;
    NodeId children;
    NodeId next;
    SymbolId symbol;
    bool constant;
};
struct Symbol {
    TextId name;
    TypeId type;
    NodeId function;
    int id;
    int scope;
    bool constant;
    SymbolId next;
};

struct Compiler {
    Source source;
    Vector<char> path;
    Store<TextRange> text_store;
    Store<Token> token_store;
    Store<Type> type_store;
    Store<Field> field_store;
    Store<Node> node_store;
    Store<Symbol> symbol_store;
    TokenId token;
    TypeId types;
    NodeId functions;
    NodeId statements;
    SymbolId symbols;
    TypeId return_type;
    int next_type;
    int next_symbol;
    int scope;
    int depth;
    int loops;

    static Self create(const char[]& path) {
        Vector<char> copied = Vector<char>::create();
        for (size_t i = 0; i < path.length; i++) {
            copied.push(path[i]);
        }
        return (Self){.source = Source::read(path),
                      .path = move(copied),
                      .text_store = Store<TextRange>::create(),
                      .token_store = Store<Token>::create(),
                      .type_store = Store<Type>::create(),
                      .field_store = Store<Field>::create(),
                      .node_store = Store<Node>::create(),
                      .symbol_store = Store<Symbol>::create(),
                      .token = 0,
                      .types = 0,
                      .functions = 0,
                      .statements = 0,
                      .symbols = 0,
                      .return_type = 0,
                      .next_type = 0,
                      .next_symbol = 0,
                      .scope = 0,
                      .depth = 0,
                      .loops = 0};
    }
};
Token get_token(TokenId id, const Compiler& c) {
    return c.token_store.get(id);
}
void set_token_kind(TokenId id, TokenKind value, Compiler& c) {
    c.token_store.at_mut(id).kind = value;
}
void set_token_text(TokenId id, TextId value, Compiler& c) {
    c.token_store.at_mut(id).text = value;
}
void set_token_line(TokenId id, int value, Compiler& c) {
    c.token_store.at_mut(id).line = value;
}
void set_token_column(TokenId id, int value, Compiler& c) {
    c.token_store.at_mut(id).column = value;
}
void set_token_next(TokenId id, TokenId value, Compiler& c) {
    c.token_store.at_mut(id).next = value;
}
TokenId new_token(Compiler& c) {
    Token record = {.kind = TokenKind::End, .text = 0, .line = 0, .column = 0, .next = 0};
    return c.token_store.add(record);
}
Type get_type(TypeId id, const Compiler& c) {
    return c.type_store.get(id);
}
void set_type_kind(TypeId id, TypeKind value, Compiler& c) {
    c.type_store.at_mut(id).kind = value;
}
void set_type_name(TypeId id, TextId value, Compiler& c) {
    c.type_store.at_mut(id).name = value;
}
void set_type_id(TypeId id, int value, Compiler& c) {
    c.type_store.at_mut(id).id = value;
}
void set_type_fields(TypeId id, FieldId value, Compiler& c) {
    c.type_store.at_mut(id).fields = value;
}
void set_type_defined(TypeId id, bool value, Compiler& c) {
    c.type_store.at_mut(id).defined = value;
}
void set_type_next(TypeId id, TypeId value, Compiler& c) {
    c.type_store.at_mut(id).next = value;
}
TypeId new_type_record(Compiler& c) {
    Type record = {.kind = TypeKind::Number, .name = 0, .id = 0, .fields = 0,.element=0, .defined = false, .next = 0};
    return c.type_store.add(record);
}
Field get_field(FieldId id, const Compiler& c) {
    return c.field_store.get(id);
}
void set_field_name(FieldId id, TextId value, Compiler& c) {
    c.field_store.at_mut(id).name = value;
}
void set_field_type(FieldId id, TypeId value, Compiler& c) {
    c.field_store.at_mut(id).type = value;
}
void set_field_next(FieldId id, FieldId value, Compiler& c) {
    c.field_store.at_mut(id).next = value;
}
FieldId new_field(Compiler& c) {
    Field record = {.name = 0, .type = 0, .next = 0, .optional=false};
    return c.field_store.add(record);
}
Node get_node(NodeId id, const Compiler& c) {
    return c.node_store.get(id);
}
void set_node_kind(NodeId id, NodeKind value, Compiler& c) {
    c.node_store.at_mut(id).kind = value;
}
void set_node_token(NodeId id, TokenId value, Compiler& c) {
    c.node_store.at_mut(id).token = value;
}
void set_node_name(NodeId id, TextId value, Compiler& c) {
    c.node_store.at_mut(id).name = value;
}
void set_node_type(NodeId id, TypeId value, Compiler& c) {
    c.node_store.at_mut(id).type = value;
}
void set_node_left(NodeId id, NodeId value, Compiler& c) {
    c.node_store.at_mut(id).left = value;
}
void set_node_right(NodeId id, NodeId value, Compiler& c) {
    c.node_store.at_mut(id).right = value;
}
void set_node_third(NodeId id, NodeId value, Compiler& c) {
    c.node_store.at_mut(id).third = value;
}
void set_node_fourth(NodeId id, NodeId value, Compiler& c) {
    c.node_store.at_mut(id).fourth = value;
}
void set_node_children(NodeId id, NodeId value, Compiler& c) {
    c.node_store.at_mut(id).children = value;
}
void set_node_next(NodeId id, NodeId value, Compiler& c) {
    c.node_store.at_mut(id).next = value;
}
void set_node_symbol(NodeId id, SymbolId value, Compiler& c) {
    c.node_store.at_mut(id).symbol = value;
}
void set_node_constant(NodeId id, bool value, Compiler& c) {
    c.node_store.at_mut(id).constant = value;
}
NodeId new_node(Compiler& c) {
    Node record = {.kind = NodeKind::Number,
                   .token = 0,
                   .name = 0,
                   .type = 0,
                   .left = 0,
                   .right = 0,
                   .third = 0,
                   .fourth = 0,
                   .children = 0,
                   .next = 0,
                   .symbol = 0,
                   .constant = false};
    return c.node_store.add(record);
}
Symbol get_symbol(SymbolId id, const Compiler& c) {
    return c.symbol_store.get(id);
}
void set_symbol_name(SymbolId id, TextId value, Compiler& c) {
    c.symbol_store.at_mut(id).name = value;
}
void set_symbol_type(SymbolId id, TypeId value, Compiler& c) {
    c.symbol_store.at_mut(id).type = value;
}
void set_symbol_function(SymbolId id, NodeId value, Compiler& c) {
    c.symbol_store.at_mut(id).function = value;
}
void set_symbol_id(SymbolId id, int value, Compiler& c) {
    c.symbol_store.at_mut(id).id = value;
}
void set_symbol_scope(SymbolId id, int value, Compiler& c) {
    c.symbol_store.at_mut(id).scope = value;
}
void set_symbol_constant(SymbolId id, bool value, Compiler& c) {
    c.symbol_store.at_mut(id).constant = value;
}
void set_symbol_next(SymbolId id, SymbolId value, Compiler& c) {
    c.symbol_store.at_mut(id).next = value;
}
SymbolId new_symbol(Compiler& c) {
    Symbol record = {.name = 0, .type = 0, .function = 0, .id = 0, .scope = 0, .constant = false, .next = 0};
    return c.symbol_store.add(record);
}

TextId source_text(size_t start, size_t length, Compiler& c) {
    if (start > c.source.length() || length > c.source.length() - start) {
        print("tsnetc: invalid source range");
        exit(1);
    }
    TextRange range = {.start = start, .length = length};
    return (TextId)c.text_store.add(range);
}
size_t text_size(TextId id, const Compiler& c) {
    if (id == 0) {
        return 0;
    }
    return c.text_store.get((size_t)id).length;
}
char text_byte(TextId id, size_t index, const Compiler& c) {
    if (index >= text_size(id, c)) {
        return 0;
    }
    TextRange range = c.text_store.get((size_t)id);
    return c.source.peek(range.start + index);
}
bool same(TextId left, TextId right, const Compiler& c) {
    size_t length = text_size(left, c);
    if (length != text_size(right, c)) {
        return false;
    }
    for (size_t i = 0; i < length; i++) {
        if (text_byte(left, i, c) != text_byte(right, i, c)) {
            return false;
        }
    }
    return true;
}
void write(TextId text, Output& output, const Compiler& c) {
    for (size_t i = 0; i < text_size(text, c); i++) {
        output.put(text_byte(text, i, c));
    }
}
double number(TextId text, const Compiler& c) {
    Vector<char> copied = Vector<char>::create();
    for (size_t i = 0; i < text_size(text, c); i++) {
        copied.push(text_byte(text, i, c));
    }
    copied.push(0);
    return native_number(copied.view());
}
void fail(TokenId token, const char[]& message, const Compiler& c) {
    Output output = Output::console();
    output.bytes(c.path.view());
    output.put('(');
    output.integer(get_token(token, c).line);
    output.put(',');
    output.integer(get_token(token, c).column);
    output.bytes(&text_error_tsnet);
    output.bytes(message);
    output.bytes(&text_at);
    write(get_token(token, c).text, &output, c);
    output.bytes(&text_diagnostic_end);
    exit(1);
}

NodeId node(NodeKind kind, TokenId token, Compiler& c) {
    NodeId result = new_node(c);
    set_node_kind(result, kind, c);
    set_node_token(result, token, c);
    return result;
}
TypeId new_type(TypeKind kind, TextId name, Compiler& c) {
    TypeId result = new_type_record(c);
    set_type_kind(result, kind, c);
    set_type_name(result, name, c);
    int id = c.next_type++;
    set_type_id(result, id, c);
    set_type_next(result, c.types, c);
    c.types = result;
    return result;
}
TypeId primitive(TypeKind kind, Compiler& c) {
    TypeId current = c.types;
    while (current != 0) {
        if (get_type(current, c).kind == kind) {
            return current;
        }
        current = get_type(current, c).next;
    }
    return new_type(kind, 0, c);
}
FieldId field(TypeId type, TextId name, const Compiler& c) {
    FieldId current = get_type(type, c).fields;
    while (current != 0) {
        if (same(get_field(current, c).name, name, c)) {
            return current;
        }
        current = get_field(current, c).next;
    }
    return 0;
}
NodeId append(NodeId first, NodeId &last, NodeId value, Compiler& c) {
    if (first == 0) {
        first = value;
    } else {
        set_node_next(last, value, c);
    }
    last = value;
    return first;
}

bool is_text(TextId text, const char[]& expected, const Compiler& c) {
    size_t length = expected.length;
    if (length != 0 && expected[length - 1] == 0) {
        length--;
    }
    if (length != text_size(text, c)) {
        return false;
    }
    for (size_t i = 0; i < length; i++) {
        if (text_byte(text, i, c) != expected[i]) {
            return false;
        }
    }
    return true;
}
