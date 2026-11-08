using System;

internal sealed class JsLazyIterator(JsProtocolIterator cursor, string mode, JsFunction callback, double limit) : TsObject
{
    internal JsProtocolIterator Cursor = cursor, Inner;
    internal JsFunction Callback = callback;
    internal readonly string Mode = mode;
    internal double Remaining = limit, Index;
    internal bool Done, Running;
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.LazyIteratorPrototype;
}

public static partial class Js
{
    static object EndLazyIterator(JsLazyIterator iterator, bool close)
    {
        var cursor = iterator.Cursor; var inner = iterator.Inner;
        iterator.Done = true; iterator.Cursor = iterator.Inner = null; iterator.Callback = null;
        if (close)
        {
            try { if (inner != null) CloseIteration(inner, false); }
            catch { CloseIteration(cursor, true); throw; }
            CloseIteration(cursor, false);
        }
        return IteratorResult(Undefined, true);
    }
    static object NextLazyIterator(JsLazyIterator iterator)
    {
        if (iterator.Running) throw new InvalidOperationException("Iterator helper is already running");
        if (iterator.Done) return IteratorResult(Undefined, true);
        iterator.Running = true; bool closeable = false;
        try
        {
            if (iterator.Mode == "take" && iterator.Remaining == 0) return EndLazyIterator(iterator, true);
            while (true)
            {
                closeable = false;
                if (iterator.Inner != null)
                {
                    closeable = true;
                    if (iterator.Inner.Ensure()) return IteratorResult(iterator.Inner.TakeValue(), false);
                    iterator.Inner = null; closeable = false;
                }
                if (!iterator.Cursor.Ensure()) return EndLazyIterator(iterator, false);
                if (iterator.Mode == "drop" && iterator.Remaining > 0)
                {
                    iterator.Cursor.SkipValue();
                    iterator.Remaining--;
                    continue;
                }
                object value = iterator.Cursor.TakeValue(); closeable = true;
                if (iterator.Mode == "take") iterator.Remaining--;
                if (iterator.Callback != null)
                {
                    object result = iterator.Callback.InvokeWithReceiver(Undefined, new object[] { value, iterator.Index++ });
                    if (iterator.Mode == "flatMap")
                    {
                        if (result is not TsObject) throw new InvalidOperationException("Iterator flatMap result must be an object");
                        iterator.Inner = (JsProtocolIterator)StartIteratorObject((TsObject)IteratorFrom(result));
                        continue;
                    }
                    if (iterator.Mode == "filter") { if (!Truthy(result)) continue; }
                    else value = result;
                }
                return IteratorResult(value, false);
            }
        }
        catch
        {
            if (!iterator.Done)
            {
                if (closeable) { if (iterator.Inner != null) CloseIteration(iterator.Inner, true); CloseIteration(iterator.Cursor, true); }
                iterator.Done = true; iterator.Cursor = iterator.Inner = null; iterator.Callback = null;
            }
            throw;
        }
        finally { iterator.Running = false; }
    }
    static class LazyIteratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var prototype = new JsClassInstance { ParentPrototype = NativeIteratorPrototype };
            prototype.DefineMethod("next", new CollectionMethod("next", 0, (receiver, _) => receiver is JsLazyIterator iterator ? NextLazyIterator(iterator) : throw new InvalidOperationException("Incompatible iterator helper receiver")));
            prototype.DefineMethod("return", new CollectionMethod("return", 0, (receiver, _) =>
            {
                if (receiver is not JsLazyIterator iterator) throw new InvalidOperationException("Incompatible iterator helper receiver");
                if (iterator.Running) throw new InvalidOperationException("Iterator helper is already running");
                if (iterator.Done) return IteratorResult(Undefined, true);
                iterator.Running = true;
                try { return EndLazyIterator(iterator, true); } finally { iterator.Running = false; }
            }));
            return prototype;
        }
    }
    internal static TsObject LazyIteratorPrototype => LazyIteratorMethods.Prototype;
    static void DefineIteratorLazy(JsClassInstance prototype)
    {
        foreach (string name in new[] { "map", "filter", "flatMap", "take", "drop" })
            prototype.DefineMethod(name, new CollectionMethod(name, 1, (receiver, arguments) =>
            {
                if (receiver is not TsObject source) throw new InvalidOperationException("Iterator method requires an object");
                object argument = FunctionArgument(arguments, 0); JsFunction callback = null; double limit = 0;
                try
                {
                    if (name is "map" or "filter" or "flatMap") callback = argument as JsFunction ?? throw new InvalidOperationException("Iterator callback is not callable");
                    else
                    {
                        limit = Math.Truncate(Number(argument));
                        if (double.IsNaN(limit) || limit < 0) throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid iterator limit");
                    }
                }
                catch { try { CloseIteratorObject(source); } catch { } throw; }
                return new JsLazyIterator((JsProtocolIterator)StartIteratorObject(source), name, callback, limit);
            }));
    }
}
