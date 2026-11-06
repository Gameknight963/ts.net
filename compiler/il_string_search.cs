namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;
internal static partial class CompilerFunctions
{
    internal static bool il_string_search_name(int name, Compiler c) => is_text(name,text_index_of,c) || is_text(name,text_includes,c) || is_text(name,text_last_index_of,c);
    internal static bool il_string_search_call(int entry, Compiler c)
    {
        Node callee=get_node(get_node(entry,c).left,c);
        while(callee.kind==NodeKind.Assert) callee=get_node(callee.left,c);
        if(callee.kind is not (NodeKind.Property or NodeKind.Index)) return false;
        bool name;
        if(callee.kind==NodeKind.Property) name=il_string_search_name(callee.name,c);
        else {Node key=get_node(callee.right,c);name=key.kind==NodeKind.String && il_string_search_name(get_token(key.token,c).text,c);}
        if(!name) return false;
        int owner=get_node(callee.left,c).type;
        return owner!=0 && get_type(owner,c).kind==TypeKind.String;
    }
    internal static void il_emit_string_search(int entry, Compiler c, IlBackend b)
    {
        Node call=get_node(entry,c);int needle=call.children;
        int position=needle==0?0:get_node(needle,c).next;
        if(il_call_has_spread(entry,c)){il_spread_call(entry,c,b);return;}
        if(position!=0 && (get_node(position,c).next!=0 || (get_type(get_node(position,c).type,c).kind!=TypeKind.Number && get_node(position,c).kind!=NodeKind.Undefined))){il_value_call(entry,c,b);return;}
        int receiver=il_method_target(call.left,c,b);
        b.emit(0xfe0c,receiver);
        if(needle==0)b.call(RuntimeMember.Undefined);else il_boxed(needle,c,b);
        if(position==0 || get_node(position,c).kind==NodeKind.Undefined)b.call(RuntimeMember.EmptyNumber);else il_expression(position,c,b);
        b.emit(0x20,needle==0?0:position==0?1:2);
        TypeKind result=get_type(call.type,c).kind;
        b.call(result==TypeKind.Number?RuntimeMember.CallStringSearchNumber:result==TypeKind.Boolean?RuntimeMember.CallStringSearchBoolean:RuntimeMember.CallStringSearch);
        if(result is not (TypeKind.Number or TypeKind.Boolean))b.from_object(call.type,c);
    }
}
