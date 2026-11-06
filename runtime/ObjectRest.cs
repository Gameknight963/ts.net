using System;
using System.Collections.Generic;
using System.Globalization;

public static partial class Js
{
    public static void CopyObjectRest(object destination, object source, object[] excludedKeys)
    {
        RequireObject(source);
        var target = (TsObject)destination;
        HashSet<string> excluded = null;
        if (excludedKeys.Length != 0) { excluded = new(StringComparer.Ordinal); foreach (object key in excludedKeys) excluded.Add((string)key); }
        if (source is TsObject value)
        {
            var keys = (JsArray)value.OwnKeys();
            for (uint index = 0; index < keys.Length(); index++)
            {
                string key = (string)keys.ElementAt(index);
                if (excluded?.Contains(key) == true || !value.HasOwn(key)) continue;
                target.DefineOwn(key, value.Get(key));
            }
        }
        else if (source is string text)
        {
            for (int index = 0; index < text.Length; index++)
            {
                string key = index.ToString(CultureInfo.InvariantCulture);
                if (excluded?.Contains(key) != true) target.DefineOwn(key, text[index].ToString());
            }
        }
    }
}
