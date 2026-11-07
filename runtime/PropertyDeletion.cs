using System;

public abstract partial class TsObject
{
    public virtual bool DeleteOwn(string key)
    {
        if(IsModuleNamespace || IsReadOnlyOwnProperty(key))return !HasOwn(key);
        if(extra is not null && extra.Remove(key)){accessors?.Remove(key);return true;}
        if(HasOwn(key))throw new NotSupportedException("Dynamic deletion of a fixed-layout field is unsupported; use a literal-key delete or a dictionary-backed object");
        return true;
    }
}

public static partial class Js
{
    public static bool DeleteSuperProperty(object key)
    {
        // The expression supplying key has run; a super reference throws
        // before its key can be coerced or any property can be removed.
        throw MakeThrown(new JsCaughtError("ReferenceError", "Cannot delete a super property"));
    }
    public static bool DeleteProperty(object receiver, object key)
    {
        RequireObject(receiver);
        string name = PropertyKey(key);
        if (!ToObject(receiver).DeleteOwn(name)) throw new InvalidOperationException("Property is not configurable");
        return true;
    }
}
