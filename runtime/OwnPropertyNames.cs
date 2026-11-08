using System;
using System.Collections.Generic;

public abstract partial class TsObject
{
    internal virtual object OwnPropertyNames() => ForInOwnKeys();
}
public partial class JsFunction
{
    internal override object OwnPropertyNames() => Js.MergeOwnNames(this, base.ForInOwnKeys(),
        lengthSlotDeleted ? null : "length", nameSlotDeleted ? null : "name",
        ordinaryPrototype != null || this is JsClassFunction ? "prototype" : null);
}
public partial class JsArray
{
    internal override object OwnPropertyNames() => Js.MergeOwnNames(this, ArrayPropertyNames(), "length");
}
public static partial class Js
{
    internal static object MergeOwnNames(TsObject owner, object extras, params string[] initial)
    {
        var names = new List<object>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in initial) if (name != null && owner.HasOwn(name) && seen.Add(name)) names.Add(name);
        var keys = (JsArray)extras;
        for (uint index = 0; index < keys.Length(); index++)
        {
            string name = (string)keys.ElementAt(index);
            if (owner.HasOwn(name) && seen.Add(name)) names.Add(name);
        }
        return OrderOwnKeys(names.ToArray());
    }
}
