namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal sealed class ExceptionFrame(int scope)
{
    internal readonly int Scope = scope;
    internal readonly List<ExceptionExit> Exits = [];
}
internal sealed record ExceptionExit(int Label, bool Return, int TargetNode, int TargetLabel, int Value);
internal sealed partial class IlBackend
{
    internal readonly List<ExceptionFrame> exception_frames = [];
    internal int caught_exception = -1;
}
internal static partial class CompilerFunctions
{
    internal static void il_exception_jump(int targetNode, int targetLabel, Compiler c, IlBackend b)
    {
        if (b.exception_frames.Count == 0 || il_inside(targetNode, b.exception_frames[^1].Scope, b)) { b.emit(0x38, targetLabel); return; }
        int label = b.label(); b.exception_frames[^1].Exits.Add(new(label, false, targetNode, targetLabel, -1)); b.emit(0xdd, label);
    }
    internal static void il_exception_return(int returnType, Compiler c, IlBackend b)
    {
        if (b.exception_frames.Count == 0) { b.op(0x2a); return; }
        int storage = b.raw_function_return ? -3 : b.type(returnType, c);
        int slot = -1; if (storage != -1) { slot = b.local(storage); b.emit(0xfe0e, slot); }
        int label = b.label(); b.exception_frames[^1].Exits.Add(new(label, true, 0, 0, slot)); b.emit(0xdd, label);
    }
    internal static void il_try(int entry, int returnType, Compiler c, IlBackend b)
    {
        Node node = get_node(entry, c);
        var frame = new ExceptionFrame(entry); b.exception_frames.Add(frame);
        int normal = b.label(), start = b.label(), end = b.label(), handler = b.label(), handlerEnd = b.label();
        int outerStart = 0, outerEnd = 0, outerHandler = 0, outerHandlerEnd = 0, pending = -1;
        int type = b.RuntimeToken(RuntimeMember.ExceptionType);
        if (node.third != 0) { outerStart = b.label(); outerEnd = b.label(); outerHandler = b.label(); outerHandlerEnd = b.label(); b.mark(outerStart); }
        b.mark(start); il_statement(node.left, returnType, c, b); b.emit(0xdd, normal); b.mark(end);
        if (node.right != 0)
        {
            b.mark(handler); int previous = b.caught_exception; b.caught_exception = b.local(type); b.emit(0xfe0e, b.caught_exception);
            il_statement(node.right, returnType, c, b); b.emit(0xdd, normal); b.caught_exception = previous; b.mark(handlerEnd);
            b.plan.catch_region(b.method, start, end, handler, handlerEnd, type);
        }
        if (node.third != 0)
        {
            b.mark(outerEnd); b.mark(outerHandler); pending = b.local(type); b.emit(0xfe0e, pending);
            int thrown = b.label(); b.emit(0xdd, thrown); b.mark(outerHandlerEnd);
            b.plan.catch_region(b.method, outerStart, outerEnd, outerHandler, outerHandlerEnd, type);
            // The finalizer runs outside its own handler, so it may override a
            // pending return, jump, or exception without executing twice.
            b.exception_frames.RemoveAt(b.exception_frames.Count - 1);
            b.mark(thrown); il_statement(node.third, returnType, c, b); b.emit(0xfe0c, pending); b.op(0x7a);
        }
        else b.exception_frames.RemoveAt(b.exception_frames.Count - 1);
        foreach (var exit in frame.Exits)
        {
            b.mark(exit.Label); if (node.third != 0) il_statement(node.third, returnType, c, b);
            if (exit.Return) { if (exit.Value >= 0) b.emit(0xfe0c, exit.Value); il_exception_return(returnType, c, b); }
            else il_exception_jump(exit.TargetNode, exit.TargetLabel, c, b);
        }
        b.mark(normal); if (node.third != 0) il_statement(node.third, returnType, c, b);
    }
}
