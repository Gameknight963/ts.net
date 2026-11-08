using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    struct DateFormatOutput(List<(string Type, string Value)> parts)
    {
        readonly StringBuilder text = new();
        readonly int start = parts?.Count ?? 0;
        internal int Length => text.Length;
        internal void Append(string type, string value) {
            if (value.Length == 0) return;
            text.Append(value);
            if (parts == null) return;
            if (type == "literal" && parts.Count != 0 && parts[^1].Type == "literal")
                parts[^1] = (type, parts[^1].Value + value);
            else parts.Add((type, value));
        }
        internal void Append(string type, ReadOnlySpan<char> value) {
            if (parts == null) text.Append(value);
            else Append(type, value.ToString());
        }
        internal void Prepend(string type, string value) {
            if (value.Length == 0) return;
            text.Insert(0, value);
            parts?.Insert(start, (type, value));
        }
        public override string ToString() => text.ToString();
    }
    static string NativeDateFormat(DateTimeOffset value, string format, CultureInfo culture, List<(string Type, string Value)> parts)
    {
        string plain = value.ToString(format, culture);
        if (parts == null) return plain;
        string pattern = format.Length == 1 ? culture.DateTimeFormat.GetAllDateTimePatterns(format[0])[0] : format;
        // Mark fields inside the complete native pattern so genitive month names
        // and calendar-specific contextual formatting retain their native context.
        char marker = '\ufdd0';
        while (plain.Contains(marker) || pattern.Contains(marker)) marker++;
        var tagged = new StringBuilder();
        var types = new List<string>();
        for (int index = 0; index < pattern.Length;) {
            char ch = pattern[index++];
            if (ch is '\'' or '"') {
                tagged.Append(ch);
                while (index < pattern.Length) {
                    char literal = pattern[index++]; tagged.Append(literal);
                    if (literal == '\\' && index < pattern.Length) tagged.Append(pattern[index++]);
                    else if (literal == ch) break;
                }
                continue;
            }
            if (ch == '\\' && index < pattern.Length) {tagged.Append(ch).Append(pattern[index++]); continue;}
            int count = 1;
            while (index < pattern.Length && pattern[index] == ch) {count++; index++;}
            string type = ch switch {
                'y' => "year", 'M' => "month", 'd' => count >= 3 ? "weekday" : "day", 'g' => "era",
                'h' or 'H' => "hour", 'm' => "minute", 's' => "second", 'f' or 'F' => "fractionalSecond",
                't' => "dayPeriod", 'z' or 'K' => "timeZoneName", _ => null
            };
            if (type != null) {tagged.Append('\'').Append(marker).Append('\''); types.Add(type);}
            tagged.Append(ch, count);
            if (type != null) tagged.Append('\'').Append(marker).Append('\'');
        }
        string[] values = value.ToString(tagged.ToString(), culture).Split(marker);
        var output = new DateFormatOutput(parts);
        for (int index = 0; index < values.Length; index++)
            output.Append((index & 1) == 0 ? "literal" : types[index / 2], values[index]);
        return plain;
    }
}
