public static partial class Js
{
    private sealed class AsyncIterationState
    {
        internal object Source;
        internal bool Keys;
        internal uint Position;
        internal object Current;
        internal bool Finished;
        internal bool NeedsClose;
    }

    public static object BeginAsyncIteration(object source, bool keys)
    {
        if (keys) source = ForInStart(source);
        else source = Iteration.Start(source);
        return new AsyncIterationState { Source = source, Keys = keys };
    }

    public static bool NextAsyncIteration(object state)
    {
        var cursor = (AsyncIterationState)state;
        cursor.NeedsClose = false;
        if (cursor.Keys)
        {
            cursor.Current = ForInNext(cursor.Source);
            return !IsUndefined(cursor.Current);
        }
        if (cursor.Finished || cursor.Position >= Iteration.Length(cursor.Source)) { cursor.Finished = true; return false; }
        int position = unchecked((int)cursor.Position);
        cursor.Current = Iteration.Value(cursor.Source, position);
        cursor.Position += (uint)Iteration.Step(cursor.Source, position);
        cursor.NeedsClose = true;
        return true;
    }

    public static object AsyncIterationValue(object state) => ((AsyncIterationState)state).Current;
    public static object CloseAsyncIteration(object state)
    {
        var cursor = (AsyncIterationState)state; cursor.NeedsClose = false;
        CloseIterationAt(cursor.Source, unchecked((int)cursor.Position), false); return Undefined;
    }
}
