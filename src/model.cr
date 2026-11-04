import "support.cr";

enum class TokenKind { End, Name, Number, String, Punctuation };
struct Token {
    TokenKind kind;
    char* text;
    int line;
    int column;
    Token* next;
};

enum class TypeKind { Number, String, Boolean, Void, Any, Unknown, Shape };
struct Field { char* name; Type* type; Field* next; };
struct Type {
    TypeKind kind;
    char* name;
    int id;
    Field* fields;
    bool defined;
    Type* next;
};

enum class NodeKind {
    Number, String, Boolean, Name, Object, Property, Call, Binary, Unary,
    Assert, Assign, Update, Block, Variable, Return, If, While, For, Expression,
    Function
};

struct Node {
    NodeKind kind;
    Token* token;
    char* name;
    Type* type;
    Node* left;
    Node* right;
    Node* third;
    Node* fourth;
    Node* children;
    Node* next;
    Symbol* symbol;
    bool constant;
};

struct Symbol {
    char* name;
    Type* type;
    Node* function;
    int id;
    int scope;
    bool constant;
    Symbol* next;
};

struct Compiler {
    Arena* arena;
    const char* path;
    Token* token;
    Type* types;
    Node* functions;
    Node* statements;
    Symbol* symbols;
    int next_type;
    int next_symbol;
    int scope;
    int depth;
    int loops;
    Type* return_type;
};

unsafe void fail(Compiler* c, Token* token, const char* message) {
    printf("%s(%d,%d): error TSNET: %s [at '%s']\n",
        c->path, token->line, token->column, message, token->text);
    exit(1);
}

unsafe Node* node(Compiler* c, NodeKind kind, Token* token) {
    Node* result = (Node*)allocate(c->arena, sizeof(Node));
    result->kind = kind;
    result->token = token;
    return result;
}

unsafe Type* new_type(Compiler* c, TypeKind kind, char* name) {
    Type* result = (Type*)allocate(c->arena, sizeof(Type));
    result->kind = kind;
    result->name = name;
    result->id = c->next_type++;
    result->next = c->types;
    c->types = result;
    return result;
}

unsafe Type* primitive(Compiler* c, TypeKind kind) {
    Type* current = c->types;
    while (current != 0) {
        if (current->kind == kind) { return current; }
        current = current->next;
    }
    return new_type(c, kind, 0);
}

unsafe Field* field(Type* type, const char* name) {
    Field* current = type->fields;
    while (current != 0) {
        if (same(current->name, name)) { return current; }
        current = current->next;
    }
    return 0;
}
