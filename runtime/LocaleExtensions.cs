using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class LocaleExtensions
    {
        internal readonly SortedDictionary<string, string> Keywords = new(StringComparer.Ordinal);
        readonly SortedSet<string> attributes = new(StringComparer.Ordinal);
        readonly SortedDictionary<string, List<string>> others = new(StringComparer.Ordinal);
        internal LocaleExtensions(string[] parts, int start)
        {
            bool unicodeSeen = false;
            while (start < parts.Length)
            {
                string singleton = parts[start++].ToLowerInvariant();
                var values = new List<string>();
                while (start < parts.Length && (singleton == "x" || parts[start].Length != 1)) values.Add(parts[start++].ToLowerInvariant());
                if (values.Count == 0) throw new ArgumentOutOfRangeException(nameof(parts), "Empty locale extension");
                if (singleton != "u") {
                    if (!others.TryAdd(singleton, values)) throw new ArgumentOutOfRangeException(nameof(parts), "Duplicate locale extension");
                    continue;
                }
                if (unicodeSeen) throw new ArgumentOutOfRangeException(nameof(parts), "Duplicate Unicode extension");
                unicodeSeen = true;
                int index = 0;
                while (index < values.Count && values[index].Length != 2)
                    if (!attributes.Add(values[index++])) throw new ArgumentOutOfRangeException(nameof(parts), "Duplicate locale attribute");
                while (index < values.Count) {
                    string key = values[index++];
                    var types = new List<string>();
                    while (index < values.Count && values[index].Length != 2) types.Add(values[index++]);
                    string type = string.Join('-', types);
                    if (!Keywords.TryAdd(key, type == "true" ? "" : type)) throw new ArgumentOutOfRangeException(nameof(parts), "Duplicate locale keyword");
                }
            }
        }
        internal void Option(TsObject options, string property, string key, string[] allowed = null)
        {
            object value = options.Get(property);
            if (IsUndefined(value)) return;
            string text = StringValue(value);
            if (allowed != null ? Array.IndexOf(allowed, text) < 0 : !ValidType(text))
                throw new ArgumentOutOfRangeException(nameof(options), "Invalid locale " + property);
            text = text.ToLowerInvariant();
            Keywords[key] = text == "true" ? "" : text;
        }
        static bool ValidType(string text)
        {
            foreach (string part in text.Split('-')) {
                if (part.Length is < 3 or > 8) return false;
                foreach (char character in part) if (!char.IsAsciiLetterOrDigit(character)) return false;
            }
            return true;
        }
        internal string Suffix()
        {
            var extensions = new SortedDictionary<string, List<string>>(others, StringComparer.Ordinal);
            if (attributes.Count != 0 || Keywords.Count != 0) {
                var unicode = new List<string>(attributes);
                foreach (var pair in Keywords) {
                    unicode.Add(pair.Key);
                    if (pair.Value.Length != 0) unicode.Add(pair.Value);
                }
                extensions["u"] = unicode;
            }
            var result = new List<string>();
            foreach (var pair in extensions) { result.Add(pair.Key); result.AddRange(pair.Value); }
            return result.Count == 0 ? "" : "-" + string.Join('-', result);
        }
    }
}
