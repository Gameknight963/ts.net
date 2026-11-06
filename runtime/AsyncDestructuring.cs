using System.Collections.Generic;

public static partial class Js
{
    private sealed class DestructuringState
    {
        internal object Source;
        internal bool ObjectPattern;
        internal int Position;
        internal bool Finished;
        internal List<object> Excluded;
    }

    public static object BeginDestructuring(object source, bool objectPattern)
    {
        if (objectPattern) RequireObject(source);
        else _ = Iteration.Length(source);
        return new DestructuringState { Source = source, ObjectPattern = objectPattern };
    }

    public static object ReadDestructuring(object state, object key)
    {
        var cursor = (DestructuringState)state;
        if (cursor.ObjectPattern)
        {
            string name = PropertyKey(key);
            (cursor.Excluded ??= new()).Add(name);
            return IndexGet(cursor.Source, name);
        }
        if (cursor.Finished || cursor.Position >= Iteration.Length(cursor.Source))
        {
            cursor.Finished = true;
            return Undefined;
        }
        object value = Iteration.Value(cursor.Source, cursor.Position);
        cursor.Position += Iteration.Step(cursor.Source, cursor.Position);
        return value;
    }

    public static object RestDestructuring(object state, object destination)
    {
        var cursor = (DestructuringState)state;
        if (cursor.ObjectPattern)
            CopyObjectRest(destination, cursor.Source, cursor.Excluded?.ToArray() ?? System.Array.Empty<object>());
        else
        {
            var array = (JsArray)destination;
            while (!cursor.Finished && cursor.Position < Iteration.Length(cursor.Source))
            {
                object value = ReadDestructuring(cursor, Undefined);
                if (array is NumberArray numbers) numbers.PushNumber(Cast<double?>(value, "Iteration value violates number binding type"));
                else array.Append(value);
            }
            cursor.Finished = true;
        }
        return destination;
    }
}
