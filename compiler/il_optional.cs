namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    internal static bool il_chain_number_property(Node step, int receiver, Compiler c, IlBackend b)
    {
        if (step.left != 0) return false;
        int owner = get_node(step.fourth, c).type; var kind = get_type(owner, c).kind;
        if (kind == TypeKind.Shape)
        {
            if (get_type(owner, c).name != 0) return false;
            int member = field(owner, step.name, c);
            if (member == 0 || get_type(get_field(member, c).type, c).kind != TypeKind.Number) return false;
            b.emit(0xfe0c, receiver); il_shape_read(owner, member, b); b.convert(get_field(member, c).type, step.type, c); return true;
        }
        if (!is_text(step.name, text_length, c)) return false;
        if (kind == TypeKind.Array) { b.emit(0xfe0c, receiver); b.call(RuntimeMember.ArrayLength); b.call(RuntimeMember.WrapNumber); return true; }
        if (kind == TypeKind.String) { b.emit(0xfe0c, receiver); b.call(RuntimeMember.Length); b.op(0x6c); b.call(RuntimeMember.WrapNumber); return true; }
        return false;
    }
    // A nullish optional step skips the entire remaining chain. Keep the
    // property receiver separately so calls and parenthesized references retain this.
    internal static int il_optional_chain(int entry, bool reference, Compiler c, IlBackend b)
    {
        Node root = get_node(entry, c);
        bool callableBase = root.children != 0 && get_node(root.children, c).kind == NodeKind.ChainCall || reference && root.children == 0;
        int incoming = -1;
        if (callableBase) incoming = il_method_target(root.left, c, b); else il_boxed(root.left, c, b);
        int value = b.local(-5); b.emit(0xfe0e, value);
        int receiver = b.local(-5); if (incoming >= 0) b.emit(0xfe0c, incoming); else b.call(RuntimeMember.Undefined); b.emit(0xfe0e, receiver);
        int result = b.local(reference ? -5 : b.type(root.type, c)), missing = b.label(), done = b.label();
        for (int child = root.children; child != 0; child = get_node(child, c).next)
        {
            Node step = get_node(child, c); bool final = step.next == 0; bool typedNumber = final && !root.constant && !reference && get_type(root.type, c).kind == TypeKind.Number;
            if (step.constant) { b.emit(0xfe0c, value); b.call(RuntimeMember.IsNullish); b.emit(0x3a, missing); }
            if (step.kind == NodeKind.ChainProperty || step.kind == NodeKind.ChainIndex)
            {
                b.emit(0xfe0c, value); b.emit(0xfe0e, receiver);
                if (typedNumber && step.kind == NodeKind.ChainProperty && il_chain_number_property(step, receiver, c, b)) { b.emit(0xfe0e, result); b.emit(0x38, done); continue; }
                b.emit(0xfe0c, receiver);
                if (final && root.constant)
                {
                    if (step.kind == NodeKind.ChainProperty) il_text(step.name, c, b); else il_boxed(step.right, c, b);
                    b.call(RuntimeMember.DeleteProperty); b.call(RuntimeMember.WrapBoolean); b.box(root.type, c);
                    b.emit(0xfe0e, value); continue;
                }
                if (step.kind == NodeKind.ChainProperty) { if (step.left != 0) { il_boxed(step.left, c, b); b.call(RuntimeMember.PrivateGet); } else { il_text(step.name, c, b); b.call(RuntimeMember.DynamicGet); } } else { il_boxed(step.right, c, b); b.call(RuntimeMember.IndexGet); }
            }
            else
            {
                int count = 0; bool numeric = typedNumber; bool spread = false;
                for (int arg = step.children; arg != 0; arg = get_node(arg, c).next) { Node argument = get_node(arg, c); count++; spread |= argument.kind == NodeKind.Spread; numeric &= argument.kind != NodeKind.Spread && (get_type(argument.type, c).kind == TypeKind.Number || argument.kind == NodeKind.Undefined); }
                b.emit(0xfe0c, value); b.emit(0xfe0c, receiver);
                if (numeric && !spread && count <= 2)
                {
                    for (int arg = step.children; arg != 0; arg = get_node(arg, c).next) il_as(arg, root.type, c, b);
                    b.call(count == 0 ? RuntimeMember.CallMethodNumber0 : count == 1 ? RuntimeMember.CallMethodNumber1 : RuntimeMember.CallMethodNumber2);
                    b.emit(0xfe0e, result); b.emit(0x38, done); continue;
                }
                if (spread)
                {
                    b.emit(0x20, 0); b.emit(0x73, b.RuntimeToken(RuntimeMember.ArrayConstructor)); int args = b.local(-5); b.emit(0xfe0e, args);
                    for (int arg = step.children; arg != 0; arg = get_node(arg, c).next) il_append_call_argument(arg, args, 0, c, b);
                    b.emit(0xfe0c, args); b.call(RuntimeMember.CallMethodArguments);
                }
                else if (count == 0) b.call(RuntimeMember.CallMethod0);
                else
                {
                    b.emit(0x20, count); b.emit(0x8d, b.RuntimeToken(RuntimeMember.ObjectType)); int index = 0;
                    for (int arg = step.children; arg != 0; arg = get_node(arg, c).next) { b.op(0x25); b.emit(0x20, index++); il_boxed(arg, c, b); b.op(0xa2); }
                    b.call(RuntimeMember.CallMethod);
                }
                b.call(RuntimeMember.Undefined); b.emit(0xfe0e, receiver);
            }
            b.emit(0xfe0e, value);
        }
        b.emit(0xfe0c, value); if (!reference) b.from_object(root.type, c); b.emit(0xfe0e, result); b.emit(0x38, done);
        b.mark(missing); if (root.constant) { b.emit(0x20, 1); b.call(RuntimeMember.WrapBoolean); } else if (reference) b.call(RuntimeMember.Undefined); else b.empty(root.type, c); b.emit(0xfe0e, result);
        b.mark(done); b.emit(0xfe0c, result); return receiver;
    }
}
