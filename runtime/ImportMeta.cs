using System.Collections.Generic;

public static partial class Js
{
    static readonly Dictionary<string, TsObject> importMetadata = new();
    static class ImportMetaFunction
    {
        internal static readonly JsFunction Value = new((FunctionBody)(static arguments =>
        {
            string url = (string)FunctionArgument(arguments, 0);
            lock (importMetadata)
            {
                if (importMetadata.TryGetValue(url, out var cached)) return cached;
                var result = new JsDynamicObject(); result.SetPrototype(null); result.DefineOwn("url", url);
                importMetadata.Add(url, result); return result;
            }
        }), null, "importMeta", 1);
    }
}
