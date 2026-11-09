namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    // Leave the selected function on the stack and retain its receiver for
    // built-ins that require JavaScript method-call semantics.
    internal static int il_method_target(int calleeId, Compiler c, IlBackend b)
    {
        Node callee = get_node(calleeId, c);
        while (callee.kind==NodeKind.Assert) {calleeId=callee.left;callee=get_node(calleeId,c);}
        if(callee.kind==NodeKind.OptionalChain)return il_optional_chain(calleeId,true,c,b);
        if (callee.kind is not (NodeKind.Property or NodeKind.Index)) { il_boxed(calleeId,c,b); return -1; }
        il_boxed(callee.left,c,b); b.op(0x25); int receiver=b.local(-5); b.emit(0xfe0e,receiver);
        if (callee.kind==NodeKind.Property) { if (callee.fourth != 0) { il_boxed(callee.fourth, c, b); b.call(RuntimeMember.PrivateGet); } else { il_text(callee.name,c,b); b.call(RuntimeMember.DynamicGet); } }
        else if (il_direct_index(calleeId, c, b)) { il_integer(callee.right, c, b); b.call(RuntimeMember.ArrayIndexGetAt); }
        else { il_boxed(callee.right,c,b); b.call(RuntimeMember.IndexGet); }
        return receiver;
    }
    internal static bool il_index_method_name(int name, Compiler c) => is_text(name,text_at_method,c) ||
        is_text(name,text_substring,c) || is_text(name,text_slice,c) || is_text(name,text_char_at,c) || is_text(name,text_char_code_at,c) || is_text(name,text_code_point_at,c);
    internal static bool il_index_method_call(int entry, Compiler c)
    {
        Node callee=get_node(get_node(entry,c).left,c);
        while(callee.kind==NodeKind.Assert){callee=get_node(callee.left,c);}
        int name = callee.kind==NodeKind.Property ? callee.name : callee.kind==NodeKind.Index && get_node(callee.right,c).kind==NodeKind.String ? get_token(get_node(callee.right,c).token,c).text : 0;
        // Keep existing array-slice lowering; primitive strings select the method path.
        if (name!=0 && is_text(name,text_slice,c) && get_type(get_node(callee.left,c).type,c).kind!=TypeKind.String) return false;
        return callee.kind==NodeKind.Property && il_index_method_name(callee.name,c) ||
            callee.kind==NodeKind.Index && get_node(callee.right,c).kind==NodeKind.String &&
            il_index_method_name(get_token(get_node(callee.right,c).token,c).text,c);
    }
    internal static void il_emit_index_method(int entry, Compiler c, IlBackend b)
    {
        Node call=get_node(entry,c);int receiver=il_method_target(call.left,c,b);
        int target=b.local(-5);b.emit(0xfe0e,target);
        int argument=call.children;
        if (!il_call_has_spread(entry,c) && argument!=0 && get_node(argument,c).next==0 && get_type(get_node(argument,c).type,c).kind==TypeKind.Number) {
            b.emit(0xfe0c,target);b.emit(0xfe0c,receiver);il_expression(argument,c,b);
            if (get_type(call.type,c).kind==TypeKind.Number) { b.call(RuntimeMember.CallMethodNumber1); }
            else { b.call(RuntimeMember.CallMethodIndex);b.from_object(call.type,c); }
            return;
        }
        if (!il_call_has_spread(entry,c) && argument!=0 && get_node(argument,c).next!=0 &&
            get_node(get_node(argument,c).next,c).next==0 && get_type(get_node(argument,c).type,c).kind==TypeKind.Number &&
            get_type(get_node(get_node(argument,c).next,c).type,c).kind==TypeKind.Number && get_type(call.type,c).kind!=TypeKind.Number) {
            b.emit(0xfe0c,target);b.emit(0xfe0c,receiver);il_expression(argument,c,b);il_expression(get_node(argument,c).next,c,b);
            b.call(RuntimeMember.CallMethodIndices);b.from_object(call.type,c);return;
        }
        if (argument==0) {
            b.emit(0xfe0c,target);b.emit(0xfe0c,receiver);b.call(get_type(call.type,c).kind==TypeKind.Number ? RuntimeMember.CallMethodNumber0 : RuntimeMember.CallMethod0);
            if(get_type(call.type,c).kind!=TypeKind.Number){il_function_result(call.type,c,b);}return;
        }
        if (il_call_has_spread(entry,c)) {
            b.emit(0x20,0);b.emit(0x73,b.RuntimeToken(RuntimeMember.ArrayConstructor));int arguments=b.local(-5);b.emit(0xfe0e,arguments);
            while (argument!=0) {il_append_call_argument(argument,arguments,0,c,b);argument=get_node(argument,c).next;}
            b.emit(0xfe0c,target);b.emit(0xfe0c,receiver);b.emit(0xfe0c,arguments);b.call(RuntimeMember.CallMethodArguments);il_function_result(call.type,c,b);return;
        }
        int count=0;while(argument!=0){count++;argument=get_node(argument,c).next;}
        b.emit(0xfe0c,target);b.emit(0xfe0c,receiver);b.emit(0x20,count);b.emit(0x8d,b.RuntimeToken(RuntimeMember.ObjectType));
        argument=call.children;int index=0;
        while(argument!=0){b.op(0x25);b.emit(0x20,index++);il_boxed(argument,c,b);b.op(0xa2);argument=get_node(argument,c).next;}
        b.call(RuntimeMember.CallMethod);il_function_result(call.type,c,b);
    }
}
