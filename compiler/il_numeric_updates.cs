namespace Tsnet.Compiler;

internal static partial class CompilerFunctions
{
    static bool il_numeric_update(int entry, bool discardBox, Compiler c, IlBackend b)
    {
        Node update = get_node(entry, c), target = get_node(update.left, c);
        if (target.kind is not (NodeKind.Property or NodeKind.Index) || target.fourth != 0 ||
            get_type(target.type, c).kind != TypeKind.Number) return false;
        int operation = get_token(update.token, c).text;
        double delta;
        if (update.kind == NodeKind.Update) {
            if (!is_text(operation, text_increment, c) && !is_text(operation, text_decrement, c)) return false;
            delta = is_text(operation, text_increment, c) ? 1 : -1;
        } else {
            if (update.kind != NodeKind.Assign || get_node(update.right, c).kind != NodeKind.Number ||
                !is_text(operation, text_add_assign, c) && !is_text(operation, text_subtract_assign, c)) return false;
            delta = number(get_token(get_node(update.right, c).token, c).text, c);
            if (is_text(operation, text_subtract_assign, c)) delta = -delta;
        }
        int classUpdate = il_class_field_update(update.left, c, b);
        if (classUpdate == 0)
        {
            Type owner = get_type(get_node(target.left, c).type, c);
            if (owner.kind != TypeKind.Shape || owner.element == 0 || get_type(owner.element, c).kind != TypeKind.Number ||
                target.kind == NodeKind.Property && field(get_node(target.left, c).type, target.name, c) != 0) return false;
        }
        il_boxed(target.left, c, b);
        if (classUpdate == 0)
        {
            if (target.kind == NodeKind.Property) il_text(target.name, c, b); else il_boxed(target.right, c, b);
        }
        b.plan.number(b.method, delta);
        b.emit(0x20, update.kind == NodeKind.Update && !update.constant ? 1 : 0);
        if (classUpdate != 0) b.emit(0x28, classUpdate);
        else b.call(target.kind == NodeKind.Property ? RuntimeMember.AddNumberProperty : RuntimeMember.AddNumberIndex);
        if (!(discardBox && get_type(update.type, c).kind == TypeKind.Any)) b.convert(target.type, update.type, c);
        return true;
    }
}
