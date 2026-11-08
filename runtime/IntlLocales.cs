using System;
using System.Collections.Generic;

public static partial class Js
{
    static string IntlLocaleMatcher(TsObject options)
    {
        object value = options == null ? Undefined : options.Get("localeMatcher");
        string matcher = IsUndefined(value) ? "best fit" : StringValue(value);
        if (matcher is not ("lookup" or "best fit")) throw new ArgumentOutOfRangeException(nameof(options), "Invalid locale matcher");
        return matcher;
    }
    static string LocaleIdentifier(object value)
    {
        if (value is LocaleValue locale) return locale.Name;
        string name = LocaleCulture(value).Name;
        return name.Length == 0 ? "und" : name;
    }
    static object CanonicalLocales(object locales)
    {
        var result = new JsArray(0);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(object value)
        {
            string name = value is LocaleValue locale ? locale.Name : new LocaleValue(value, Undefined).Name;
            if (seen.Add(name)) result.Append(name);
        }
        if (IsUndefined(locales)) return result;
        if (locales is string or LocaleValue) Add(locales);
        else
        {
            var list = ToObject(locales);
            long length = IndexedLength(list);
            for (long index = 0; index < length; index++)
                if (TryIndexedValue(list, index, out object value)) Add(value);
        }
        return result;
    }
}
