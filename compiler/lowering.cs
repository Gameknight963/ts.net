namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;



internal static partial class CompilerFunctions
{
    internal static bool fits_nested(int source, int target, Vector<int> sources, Vector<int> targets, Compiler c)
    {
        if (source == target)
        {
            return true;
        }
        TypeKind source_kind = get_type(source, c).kind; TypeKind target_kind = get_type(target, c).kind;
        // Object-backed field views accept typed storage through generated adapters.
        // Top-level scalar conversions still need to box their values.
        if ((target_kind == TypeKind.Any || target_kind == TypeKind.Unknown) && source_kind != TypeKind.Void) { return true; }
        // An any-valued field can satisfy a typed view through a checked getter.
        // Keep top-level conversions separate so scalars still box and validate.
        if (source_kind == TypeKind.Any && target_kind != TypeKind.Void) { return true; }
        // Array references share the same ABI; element reads retain their checks.
        if (source_kind == TypeKind.Array && target_kind == TypeKind.Array) { return true; }
        if (source_kind != TypeKind.Shape || target_kind != TypeKind.Shape)
        {
            return false;
        }
        for (int i = 0; i < sources.count(); i++) { if (sources.at(i) == (int)source && targets.at(i) == (int)target) { return true; } }
        sources.push((int)source); targets.push((int)target);
        int required = get_type(target, c).fields;
        while (required != 0)
        {
            int actual = field(source, get_field(required, c).name, c);
            if ((actual == 0 && !get_field(required, c).optional) || (actual != 0 && !fits_nested(get_field(actual, c).type, get_field(required, c).type, sources, targets, c)))
            {
                return false;
            }
            required = get_field(required, c).next;
        }
        return true;
    }
    internal static bool fits(int source, int target, Compiler c)
    {
        if (source == target) { return true; }
        if (get_type(source, c).kind != TypeKind.Shape || get_type(target, c).kind != TypeKind.Shape) { return false; }
        Vector<int> sources = new Vector<int>(); Vector<int> targets = new Vector<int>();
        return fits_nested(source, target, sources, targets, c);
    }
    internal static bool builtin(int callee, TextBytes owner, TextBytes member, Compiler c)
    {
        return get_node(callee, c).kind == NodeKind.Property && is_text(get_node(callee, c).name, member, c) &&
               get_node(get_node(callee, c).left, c).kind == NodeKind.Name && get_node(get_node(callee, c).left, c).symbol == 0 &&
               is_text(get_node(get_node(callee, c).left, c).name, owner, c);
    }
    internal static bool builtin_has_own(int callee, Compiler c)
    {
        return builtin(callee, text_object_constructor, text_has_own, c) && get_node(callee, c).constant;
    }
}
