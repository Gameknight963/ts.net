public static partial class Js
{
    private sealed class AsyncIterationState
    {
        internal object Source;
        internal bool Keys;
        internal uint Position;
        internal object Current;
    }

    public static object BeginAsyncIteration(object source, bool keys)
    {
        if (keys) source = ForInStart(source);
        else _ = Iteration.Length(source);
        return new AsyncIterationState { Source = source, Keys = keys };
    }

    public static bool NextAsyncIteration(object state)
    {
        var cursor = (AsyncIterationState)state;
        if (cursor.Keys)
        {
            cursor.Current = ForInNext(cursor.Source);
            return !IsUndefined(cursor.Current);
        }
        if (cursor.Position >= Iteration.Length(cursor.Source)) return false;
        int position = unchecked((int)cursor.Position);
        cursor.Current = Iteration.Value(cursor.Source, position);
        cursor.Position += (uint)Iteration.Step(cursor.Source, position);
        return true;
    }

    public static object AsyncIterationValue(object state) => ((AsyncIterationState)state).Current;
}
