using System;

public abstract partial class TsObject
{
    public virtual bool DeleteOwn(string key)
    {
        if(IsModuleNamespace || IsReadOnlyOwnProperty(key))return !HasOwn(key);
        if(extra is not null && extra.Remove(key)){accessors?.Remove(key);return true;}
        if(HasOwn(key))throw new NotSupportedException("Deleting compiler-generated fields is not implemented");
        return true;
    }
}
