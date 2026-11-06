using System;
using System.Collections.Generic;

public abstract partial class TsObject
{
    // Reuse the descriptor-map slot to mark namespace objects, without adding
    // storage or a lookup table to ordinary objects.
    private sealed class ModuleNamespaceAccessors : Dictionary<string, JsAccessor>
    {
        internal ModuleNamespaceAccessors() { }
        internal ModuleNamespaceAccessors(Dictionary<string, JsAccessor> source) : base(source) { }
    }
    private bool IsModuleNamespace => accessors is ModuleNamespaceAccessors;
    internal void SealModuleNamespace() => accessors = accessors == null
        ? new ModuleNamespaceAccessors() : new ModuleNamespaceAccessors(accessors);
    private object ModuleNamespaceKeys()
    {
        var names = new List<string>();
        if (extra != null) foreach (var name in extra.Keys) names.Add(name);
        names.Sort(StringComparer.Ordinal);
        return new JsArray(names.ToArray());
    }
}

public static partial class Js
{
    public static void SealModuleNamespace(object value) => ((TsObject)value).SealModuleNamespace();
}
