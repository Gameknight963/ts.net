using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    sealed class DateLocaleFields
    {
        readonly string year, month, day, weekday, era;
        internal bool HasFields => year != null || month != null || day != null || weekday != null || era != null;
        internal DateLocaleFields(TsObject options)
        {
            string Read(string name, params string[] choices)
            {
                object value = options.Get(name);
                if (IsUndefined(value)) return null;
                string text = StringValue(value);
                if (Array.IndexOf(choices, text) < 0) throw new ArgumentOutOfRangeException(name, "Invalid date component width");
                return text;
            }
            year = Read("year", "numeric", "2-digit");
            month = Read("month", "numeric", "2-digit", "long", "short", "narrow");
            day = Read("day", "numeric", "2-digit");
            weekday = Read("weekday", "long", "short", "narrow");
            era = Read("era", "long", "short", "narrow");
        }
        internal string Format(DateTimeOffset value, CultureInfo culture)
        {
            bool textual = month is "long" or "short" or "narrow";
            string pattern = textual ? culture.DateTimeFormat.LongDatePattern : culture.DateTimeFormat.ShortDatePattern;
            var parts = new List<(char Field, string Literal)>();
            var literal = new StringBuilder();
            for (int index = 0; index < pattern.Length;)
            {
                char ch = pattern[index++];
                if (ch is '\'' or '"')
                {
                    while (index < pattern.Length && pattern[index] != ch) literal.Append(pattern[index++]);
                    if (index < pattern.Length) index++;
                    continue;
                }
                if (ch == '\\' && index < pattern.Length) { literal.Append(pattern[index++]); continue; }
                if (ch is not ('y' or 'M' or 'd' or 'g')) { literal.Append(ch == '/' ? culture.DateTimeFormat.DateSeparator : ch.ToString()); continue; }
                int count = 1;
                while (index < pattern.Length && pattern[index] == ch) { count++; index++; }
                if (ch == 'd' && count >= 3) ch = 'w';
                parts.Add((ch, literal.ToString())); literal.Clear();
            }
            var output = new StringBuilder();
            bool skipped = false;
            foreach (var part in parts)
            {
                string width = part.Field switch { 'y' => year, 'M' => month, 'd' => day, 'w' => weekday, 'g' => era, _ => null };
                if (width == null) { skipped = true; continue; }
                if (output.Length != 0) output.Append(skipped ? textual ? " " : culture.DateTimeFormat.DateSeparator : part.Literal);
                output.Append(Field(part.Field, width, value, culture));
                skipped = false;
            }
            if (!skipped && output.Length != 0) output.Append(literal);
            // Short native date patterns generally omit weekday and era.
            bool Contains(char field) => parts.Exists(part => part.Field == field);
            if (weekday != null && !Contains('w'))
            {
                string text = Field('w', weekday, value, culture);
                if (output.Length != 0) text += ", ";
                output.Insert(0, text);
            }
            if (era != null && !Contains('g'))
            {
                if (output.Length != 0) output.Append(' ');
                output.Append(Field('g', era, value, culture));
            }
            return output.ToString();
        }
        static string Field(char field, string width, DateTimeOffset value, CultureInfo culture)
        {
            string pattern = field switch
            {
                'y' => width == "2-digit" ? "yy" : "yyyy",
                'M' => width switch { "numeric" => "%M", "2-digit" => "MM", "short" => "MMM", _ => "MMMM" },
                'd' => width == "2-digit" ? "dd" : "%d",
                'w' => width == "short" ? "ddd" : "dddd",
                _ => "gg"
            };
            string result = value.ToString(pattern, culture);
            return width == "narrow" ? StringInfo.GetNextTextElement(result) : result;
        }
    }
}
