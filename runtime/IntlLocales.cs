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
        string name = CanonicalLocaleBase(StringValue(value));
        return name.Length == 0 ? "und" : name;
    }
    static string CanonicalLocaleBase(string tag)
    {
        string[] fields = tag.Split('-');
        string language = fields[0];
        if (!(language.Length is 2 or 3 || language.Length is >= 5 and <= 8) || !AsciiLetters(language)) throw new ArgumentOutOfRangeException(nameof(tag), "Invalid locale language");
        int next = 1;
        if (next < fields.Length && fields[next].Length == 4 && AsciiLetters(fields[next])) next++;
        if (next < fields.Length && (fields[next].Length == 2 && AsciiLetters(fields[next]) || fields[next].Length == 3 && AsciiDigits(fields[next]))) next++;
        string core = LocaleCulture(string.Join('-', fields, 0, next)).Name;
        var variants = new SortedSet<string>(StringComparer.Ordinal);
        for (int index = next; index < fields.Length; index++) {
            string variant = fields[index].ToLowerInvariant();
            if (!(variant.Length is >= 5 and <= 8 || variant.Length == 4 && char.IsAsciiDigit(variant[0]))) throw new ArgumentOutOfRangeException(nameof(tag), "Invalid locale variant");
            foreach (char character in variant) if (!char.IsAsciiLetterOrDigit(character)) throw new ArgumentOutOfRangeException(nameof(tag), "Invalid locale variant");
            if (!variants.Add(variant)) throw new ArgumentOutOfRangeException(nameof(tag), "Duplicate locale variant");
        }
        return core + (variants.Count == 0 ? "" : "-" + string.Join('-', variants));
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
