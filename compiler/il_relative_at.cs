namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    // Leave the selected function on the stack and retain its receiver for
    // built-ins that require JavaScript method-call semantics.
    internal static int il_method_target(int calleeId, Compiler c, IlBackend b)
    {
        Node callee = get_node(calleeId, c);
        while (callee.kind==NodeKind.Assert) {callee=get_node(callee.left,c);}
        if (callee.kind is not (NodeKind.Property or NodeKind.Index)) { il_boxed(calleeId,c,b); return -1; }
        il_boxed(callee.left,c,b); b.op(0x25); int receiver=b.local(-5); b.emit(0xfe0e,receiver);
        if (callee.kind==NodeKind.Property) { il_text(callee.name,c,b); b.call(RuntimeMember.DynamicGet); }
        else { il_boxed(callee.right,c,b); b.call(RuntimeMember.IndexGet); }
        return receiver;
    }
    internal static bool il_at_call(int entry, Compiler c)
    {
        Node callee=get_node(get_node(entry,c).left,c);
        while(callee.kind==NodeKind.Assert){callee=get_node(callee.left,c);}
        return callee.kind==NodeKind.Property && is_text(callee.name,text_at_method,c) ||
            callee.kind==NodeKind.Index && get_node(callee.right,c).kind==NodeKind.String &&
            is_text(get_token(get_node(callee.right,c).token,c).text,text_at_method,c);
    }
    internal static void il_emit_at(int entry, Compiler c, IlBackend b)
    {
        Node call=get_node(entry,c);int receiver=il_method_target(call.left,c,b);
        int target=b.local(-5);b.emit(0xfe0e,target);
        int argument=call.children;
        if (!il_call_has_spread(entry,c) && argument!=0 && get_node(argument,c).next==0 && get_type(get_node(argument,c).type,c).kind==TypeKind.Number) {
            b.emit(0xfe0c,target);b.emit(0xfe0c,receiver);il_expression(argument,c,b);
            if (get_type(call.type,c).kind==TypeKind.Number) { b.call(RuntimeMember.CallMethodNumber1); }
            else { b.call(RuntimeMember.CallAtIndex);b.from_object(call.type,c); }
            return;
        }
        if (argument==0) {
            b.emit(0xfe0c,target);b.emit(0xfe0c,receiver);b.call(RuntimeMember.CallMethod0);il_function_result(call.type,c,b);return;
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
