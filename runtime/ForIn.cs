using System;
using System.Collections.Generic;

public abstract partial class TsObject
{
    internal virtual object ForInOwnKeys() => OwnKeys();
    internal virtual bool ForInEnumerable(string key)
    {
        // Namespace descriptors read their live binding, unlike ordinary
        // accessor descriptors. This preserves TDZ errors in module cycles.
        if (IsModuleNamespace) _ = Get(key);
        return true;
    }
}

public partial class JsFunction
{
    internal override object ForInOwnKeys() => HasOwn("prototype") ? Js.ForInNames(base.ForInOwnKeys(), "length", "name", "prototype") : Js.ForInNames(base.ForInOwnKeys(), "length", "name");
    internal override bool ForInEnumerable(string key) => (key != "prototype" || ordinaryPrototype == null) && (key is not ("length" or "name") || base.HasOwn(key)) && base.ForInEnumerable(key);
}

public partial class JsArray
{
    internal override object ForInOwnKeys() => Js.ForInNames(ArrayPropertyNames(), "length");
    internal override bool ForInEnumerable(string key) => key != "length" && NamedPropertyEnumerable(key);
}

internal sealed class JsForIn
{
    TsObject current;
    JsArray keys;
    uint position;
    readonly HashSet<string> visited = new(StringComparer.Ordinal);
    readonly HashSet<TsObject> prototypes = new();
    internal JsForIn(object value)
    {
        current = value is TsObject obj ? obj : Js.IsNullish(value) ? null : new Primitive(value);
    }
    internal object Next()
    {
        while (current != null)
        {
            if (keys == null)
            {
                if (!prototypes.Add(current)) return Js.Undefined;
                keys = (JsArray)current.ForInOwnKeys(); position = 0;
            }
            while (position < keys.Length())
            {
                string key = (string)keys.ElementAt(position++);
                if (Js.IsSymbolKey(key) || !current.HasOwn(key) || !visited.Add(key)) continue;
                if (current.ForInEnumerable(key)) return key;
            }
            current = current.PropertyPrototype; keys = null;
        }
        return Js.Undefined;
    }
    sealed class Primitive(object value) : TsObject
    {
        internal override TsObject PropertyPrototype => Js.PrimitivePrototype(value);
        public override bool HasOwn(string key) => value is string text && (key == "length" || JsArray.Index(key, out uint index) && index < text.Length);
        internal override bool ForInEnumerable(string key) => key != "length";
        internal override object ForInOwnKeys()
        {
            var result = (JsArray)Js.ObjectKeys(value);
            if (value is string) result.Append("length");
            return result;
        }
    }
}

public static partial class Js
{
    internal static object ForInNames(object names, params string[] hidden)
    {
        var keys = (JsArray)names;
        foreach (string name in hidden)
        {
            bool found = false;
            for (uint i = 0; i < keys.Length(); i++) if ((string)keys.ElementAt(i) == name) { found = true; break; }
            if (!found) keys.Append(name);
        }
        return keys;
    }
    public static object ForInStart(object value) => new JsForIn(value);
    public static object ForInNext(object iterator) => ((JsForIn)iterator).Next();
}
