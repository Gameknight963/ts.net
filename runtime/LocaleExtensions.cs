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
                if (singleton.Length != 1 || !char.IsAsciiLetterOrDigit(singleton[0])) throw new ArgumentOutOfRangeException(nameof(parts), "Invalid locale extension");
                var values = new List<string>();
                while (start < parts.Length && (singleton == "x" || parts[start].Length != 1))
                {
                    string part = parts[start++];
                    if (part.Length < (singleton == "x" ? 1 : 2) || part.Length > 8) throw new ArgumentOutOfRangeException(nameof(parts), "Invalid locale extension subtag");
                    foreach (char ch in part) if (!char.IsAsciiLetterOrDigit(ch)) throw new ArgumentOutOfRangeException(nameof(parts), "Invalid locale extension subtag");
                    values.Add(part.ToLowerInvariant());
                }
                if (values.Count == 0) throw new ArgumentOutOfRangeException(nameof(parts), "Empty locale extension");
                if (singleton != "u") {
                    if (!others.TryAdd(singleton, values)) throw new ArgumentOutOfRangeException(nameof(parts), "Duplicate locale extension");
                    continue;
                }
                if (unicodeSeen) throw new ArgumentOutOfRangeException(nameof(parts), "Duplicate Unicode extension");
                unicodeSeen = true;
                int index = 0;
                while (index < values.Count && values[index].Length != 2)
                    attributes.Add(values[index++]);
                while (index < values.Count) {
                    string key = values[index++];
                    if (!char.IsAsciiLetter(key[1])) throw new ArgumentOutOfRangeException(nameof(parts), "Invalid Unicode locale key");
                    var types = new List<string>();
                    while (index < values.Count && values[index].Length != 2) types.Add(values[index++]);
                    string type = string.Join('-', types);
                    Keywords.TryAdd(key, type == "true" ? "" : type);
                }
            }
        }
        internal void Option(TsObject options, string property, string key, string[] allowed = null, bool weekday = false)
        {
            object value = options.Get(property);
            if (IsUndefined(value)) return;
            string text = StringValue(value);
            if (weekday) text = text switch { "0" or "7" => "sun", "1" => "mon", "2" => "tue", "3" => "wed", "4" => "thu", "5" => "fri", "6" => "sat", _ => text };
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
