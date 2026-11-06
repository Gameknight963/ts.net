namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal struct TextRange
{
    public int start;
    public int length;
}

internal enum TokenKind { End, Name, Number, String, Punctuation }

internal enum TypeKind { Number, String, Boolean, Void, Any, Unknown, Shape, Array }

internal enum NodeKind
{
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
    Spread,
    Destructure,
    Binding,
    FunctionValue,
    NumberIsNaN,
    NumberIsFinite,
    NumberIsInteger,
    NumberIsSafeInteger,
    NumberIsNaNFunction,
    NumberIsFiniteFunction,
    NumberIsIntegerFunction,
    NumberIsSafeIntegerFunction,
    ObjectKeysFunction,
    ObjectValuesFunction,
    ObjectEntriesFunction,
    Throw, Try, CatchValue, BuiltinError, Construct, ClassValue, ClassStatic, ClassField, ResolveThis, SuperBase, SuperCall, SuperInitializer, OptionalChain, ChainProperty, ChainIndex, ChainCall, BuiltinNamespace, HasProperty, ForIn, Arguments, AsyncAwait, SavedCall, AsyncPropertyKey, AsyncBindingScope, DestructuringState, DestructuringRead, DestructuringRest, AsyncResume, AsyncIterationStart, AsyncIterationNext, AsyncIterationValue, AsyncTry, AsyncTryEnd, ForAwaitNext, ForAwaitAccept, ForAwaitNeedsClose, ForAwaitClose, RequireIteratorResult, AsyncIteratorTry
}

internal struct Token
{
    public TokenKind kind;
    public int text;
    public int line;
    public int column;
    public int next;
}

internal struct Type
{
    public TypeKind kind;
    // Nonzero names identify class families, shared by generic instantiations.
    public int name;
    public int id;
    public int fields;
    public int element;
    public bool defined;
    public int next;
}

internal struct Field
{
    public int name;
    public int type;
    public int next;
    public bool optional;
}

internal struct Node
{
    public NodeKind kind;
    public int token;
    public int name;
    public int type;
    public int left;
    public int right;
    public int third;
    public int fourth;
    public int children;
    public int next;
    public int symbol;
    public bool constant;
}

internal struct Symbol
{
    public int name;
    public int type;
    public int function;
    public int id;
    public int scope;
    public bool constant;
    public int next;
}

internal sealed class Compiler
{
    public Source source;
    public Vector<byte> path;
    public Store<TextRange> text_store;
    public Store<Token> token_store;
    public Store<Type> type_store;
    public Store<Field> field_store;
    public Store<Node> node_store;
    public Store<Symbol> symbol_store;
    public int token;
    public int types;
    public int functions;
    public int statements;
    public int symbols;
    public int return_type;
    public int next_type;
    public int next_symbol;
    public int scope;
    public int depth;
    public int loops;
    public static Compiler create(TextBytes path)
    {
        Vector<byte> copied = new Vector<byte>();
        for (int i = 0; i < path.length; i++)
        {
            copied.push(path[i]);
        }
        return new Compiler
        {
            source = Source.read(path),
            path = identity(copied),
            text_store = new Store<TextRange>(),
            token_store = new Store<Token>(),
            type_store = new Store<Type>(),
            field_store = new Store<Field>(),
            node_store = new Store<Node>(),
            symbol_store = new Store<Symbol>(),
            token = 0,
            types = 0,
            functions = 0,
            statements = 0,
            symbols = 0,
            return_type = 0,
            next_type = 0,
            next_symbol = 0,
            scope = 0,
            depth = 0,
            loops = 0
        };
    }
}

internal static partial class CompilerFunctions
{
    internal static Token get_token(int id, Compiler c)
    {
        return c.token_store.get(id);
    }
    internal static void set_token_kind(int id, TokenKind value, Compiler c)
    {
        c.token_store.at_mut(id).kind = value;
    }
    internal static void set_token_text(int id, int value, Compiler c)
    {
        c.token_store.at_mut(id).text = value;
    }
    internal static void set_token_line(int id, int value, Compiler c)
    {
        c.token_store.at_mut(id).line = value;
    }
    internal static void set_token_column(int id, int value, Compiler c)
    {
        c.token_store.at_mut(id).column = value;
    }
    internal static void set_token_next(int id, int value, Compiler c)
    {
        c.token_store.at_mut(id).next = value;
    }
    internal static int new_token(Compiler c)
    {
        Token record = new Token { kind = TokenKind.End, text = 0, line = 0, column = 0, next = 0 };
        return c.token_store.add(record);
    }
    internal static Type get_type(int id, Compiler c)
    {
        return c.type_store.get(id);
    }
    internal static void set_type_kind(int id, TypeKind value, Compiler c)
    {
        c.type_store.at_mut(id).kind = value;
    }
    internal static void set_type_name(int id, int value, Compiler c)
    {
        c.type_store.at_mut(id).name = value;
    }
    internal static void set_type_id(int id, int value, Compiler c)
    {
        c.type_store.at_mut(id).id = value;
    }
    internal static void set_type_fields(int id, int value, Compiler c)
    {
        c.type_store.at_mut(id).fields = value;
    }
    internal static void set_type_defined(int id, bool value, Compiler c)
    {
        c.type_store.at_mut(id).defined = value;
    }
    internal static void set_type_next(int id, int value, Compiler c)
    {
        c.type_store.at_mut(id).next = value;
    }
    internal static int new_type_record(Compiler c)
    {
        Type record = new Type { kind = TypeKind.Number, name = 0, id = 0, fields = 0, element = 0, defined = false, next = 0 };
        return c.type_store.add(record);
    }
    internal static Field get_field(int id, Compiler c)
    {
        return c.field_store.get(id);
    }
    internal static void set_field_name(int id, int value, Compiler c)
    {
        c.field_store.at_mut(id).name = value;
    }
    internal static void set_field_type(int id, int value, Compiler c)
    {
        c.field_store.at_mut(id).type = value;
    }
    internal static void set_field_next(int id, int value, Compiler c)
    {
        c.field_store.at_mut(id).next = value;
    }
    internal static int new_field(Compiler c)
    {
        Field record = new Field { name = 0, type = 0, next = 0, optional = false };
        return c.field_store.add(record);
    }
    internal static Node get_node(int id, Compiler c)
    {
        return c.node_store.get(id);
    }
    internal static void set_node_kind(int id, NodeKind value, Compiler c)
    {
        c.node_store.at_mut(id).kind = value;
    }
    internal static void set_node_token(int id, int value, Compiler c)
    {
        c.node_store.at_mut(id).token = value;
    }
    internal static void set_node_name(int id, int value, Compiler c)
    {
        c.node_store.at_mut(id).name = value;
    }
    internal static void set_node_type(int id, int value, Compiler c)
    {
        c.node_store.at_mut(id).type = value;
    }
    internal static void set_node_left(int id, int value, Compiler c)
    {
        c.node_store.at_mut(id).left = value;
    }
    internal static void set_node_right(int id, int value, Compiler c)
    {
        c.node_store.at_mut(id).right = value;
    }
    internal static void set_node_third(int id, int value, Compiler c)
    {
        c.node_store.at_mut(id).third = value;
    }
    internal static void set_node_fourth(int id, int value, Compiler c)
    {
        c.node_store.at_mut(id).fourth = value;
    }
    internal static void set_node_children(int id, int value, Compiler c)
    {
        c.node_store.at_mut(id).children = value;
    }
    internal static void set_node_next(int id, int value, Compiler c)
    {
        c.node_store.at_mut(id).next = value;
    }
    internal static void set_node_symbol(int id, int value, Compiler c)
    {
        c.node_store.at_mut(id).symbol = value;
    }
    internal static void set_node_constant(int id, bool value, Compiler c)
    {
        c.node_store.at_mut(id).constant = value;
    }
    internal static int new_node(Compiler c)
    {
        Node record = new Node
        {
            kind = NodeKind.Number,
            token = 0,
            name = 0,
            type = 0,
            left = 0,
            right = 0,
            third = 0,
            fourth = 0,
            children = 0,
            next = 0,
            symbol = 0,
            constant = false
        };
        return c.node_store.add(record);
    }
    internal static Symbol get_symbol(int id, Compiler c)
    {
        return c.symbol_store.get(id);
    }
    internal static void set_symbol_name(int id, int value, Compiler c)
    {
        c.symbol_store.at_mut(id).name = value;
    }
    internal static void set_symbol_type(int id, int value, Compiler c)
    {
        c.symbol_store.at_mut(id).type = value;
    }
    internal static void set_symbol_function(int id, int value, Compiler c)
    {
        c.symbol_store.at_mut(id).function = value;
    }
    internal static void set_symbol_id(int id, int value, Compiler c)
    {
        c.symbol_store.at_mut(id).id = value;
    }
    internal static void set_symbol_scope(int id, int value, Compiler c)
    {
        c.symbol_store.at_mut(id).scope = value;
    }
    internal static void set_symbol_constant(int id, bool value, Compiler c)
    {
        c.symbol_store.at_mut(id).constant = value;
    }
    internal static void set_symbol_next(int id, int value, Compiler c)
    {
        c.symbol_store.at_mut(id).next = value;
    }
    internal static int new_symbol(Compiler c)
    {
        Symbol record = new Symbol { name = 0, type = 0, function = 0, id = 0, scope = 0, constant = false, next = 0 };
        return c.symbol_store.add(record);
    }
    internal static int source_text(int start, int length, Compiler c)
    {
        if (start > c.source.length() || length > c.source.length() - start)
        {
            print("tsnetc: invalid source range");
            exit(1);
        }
        TextRange range = new TextRange { start = start, length = length };
        return (int)c.text_store.add(range);
    }
    internal static int text_size(int id, Compiler c)
    {
        if (id == 0)
        {
            return 0;
        }
        return c.text_store.get((int)id).length;
    }
    internal static byte text_byte(int id, int index, Compiler c)
    {
        if (index >= text_size(id, c))
        {
            return 0;
        }
        TextRange range = c.text_store.get((int)id);
        return c.source.peek(range.start + index);
    }
    internal static bool same(int left, int right, Compiler c)
    {
        int length = text_size(left, c);
        if (length != text_size(right, c))
        {
            return false;
        }
        for (int i = 0; i < length; i++)
        {
            if (text_byte(left, i, c) != text_byte(right, i, c))
            {
                return false;
            }
        }
        return true;
    }
    internal static void write(int text, Output output, Compiler c)
    {
        for (int i = 0; i < text_size(text, c); i++)
        {
            output.put(text_byte(text, i, c));
        }
    }
    internal static double number(int text, Compiler c)
    {
        Vector<byte> copied = new Vector<byte>();
        for (int i = 0; i < text_size(text, c); i++)
        {
            copied.push(text_byte(text, i, c));
        }
        copied.push(0);
        return native_number(copied.view());
    }
    internal static void fail(int token, TextBytes message, Compiler c)
    {
        Output output = Output.console();
        output.bytes(c.path.view());
        output.put('(');
        output.integer(get_token(token, c).line);
        output.put(',');
        output.integer(get_token(token, c).column);
        output.bytes(text_error_tsnet);
        output.bytes(message);
        output.bytes(text_at);
        write(get_token(token, c).text, output, c);
        output.bytes(text_diagnostic_end);
        exit(1);
    }
    internal static int node(NodeKind kind, int token, Compiler c)
    {
        int result = new_node(c);
        set_node_kind(result, kind, c);
        set_node_token(result, token, c);
        return result;
    }
    internal static int new_type(TypeKind kind, int name, Compiler c)
    {
        int result = new_type_record(c);
        set_type_kind(result, kind, c);
        set_type_name(result, name, c);
        int id = c.next_type++;
        set_type_id(result, id, c);
        set_type_next(result, c.types, c);
        c.types = result;
        return result;
    }
    internal static int primitive(TypeKind kind, Compiler c)
    {
        int current = c.types;
        while (current != 0)
        {
            if (get_type(current, c).kind == kind)
            {
                return current;
            }
            current = get_type(current, c).next;
        }
        return new_type(kind, 0, c);
    }
    internal static int field(int type, int name, Compiler c)
    {
        int current = get_type(type, c).fields;
        while (current != 0)
        {
            if (same(get_field(current, c).name, name, c))
            {
                return current;
            }
            current = get_field(current, c).next;
        }
        return 0;
    }
    internal static int append(int first, ref int last, int value, Compiler c)
    {
        if (first == 0)
        {
            first = value;
        }
        else
        {
            set_node_next(last, value, c);
        }
        last = value;
        return first;
    }
    internal static bool is_text(int text, TextBytes expected, Compiler c)
    {
        int length = expected.length;
        if (length != 0 && expected[length - 1] == 0)
        {
            length--;
        }
        if (length != text_size(text, c))
        {
            return false;
        }
        for (int i = 0; i < length; i++)
        {
            if (text_byte(text, i, c) != expected[i])
            {
                return false;
            }
        }
        return true;
    }
}
