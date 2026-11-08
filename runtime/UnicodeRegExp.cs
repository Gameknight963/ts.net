using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    readonly record struct RegExpCodePointRange(int First, int Last);
    static Exception UnicodeRegExpSyntax() => MakeThrown(new JsCaughtError("SyntaxError", "Invalid Unicode regular expression"));
    static List<RegExpCodePointRange> MergeRegExpRanges(List<RegExpCodePointRange> ranges)
    {
        ranges.Sort(static (a, b) => a.First.CompareTo(b.First));
        var result = new List<RegExpCodePointRange>();
        foreach (var range in ranges) {
            if (result.Count != 0 && range.First <= result[^1].Last + 1)
                result[^1] = new(result[^1].First, Math.Max(result[^1].Last, range.Last));
            else result.Add(range);
        }
        return result;
    }
    static List<RegExpCodePointRange> InvertRegExpRanges(List<RegExpCodePointRange> ranges)
    {
        var result = new List<RegExpCodePointRange>(); int start = 0;
        foreach (var range in MergeRegExpRanges(ranges)) {
            if (start < range.First) result.Add(new(start, range.First - 1));
            start = range.Last + 1;
        }
        if (start <= 0x10ffff) result.Add(new(start, 0x10ffff));
        return result;
    }
    static string RegExpCodePointAtom(List<RegExpCodePointRange> ranges)
    {
        var alternatives = new List<string>(); var basic = new StringBuilder();
        string Unit(int value) => "\\u" + value.ToString("X4", CultureInfo.InvariantCulture);
        string Units(int first, int last) => first == last ? Unit(first) : "[" + Unit(first) + "-" + Unit(last) + "]";
        void Basic(int first, int last) {if (first <= last) {basic.Append(Unit(first)); if (first != last) basic.Append('-').Append(Unit(last));}}
        // A surrogate pair is one character. Lone-surrogate branches must not match half a pair.
        foreach (var range in MergeRegExpRanges(ranges)) {
            Basic(range.First, Math.Min(range.Last, 0xd7ff));
            Basic(Math.Max(range.First, 0xe000), Math.Min(range.Last, 0xffff));
            int first = Math.Max(range.First, 0xd800), last = Math.Min(range.Last, 0xdbff);
            if (first <= last) alternatives.Add(Units(first, last) + "(?![\\uDC00-\\uDFFF])");
            first = Math.Max(range.First, 0xdc00); last = Math.Min(range.Last, 0xdfff);
            if (first <= last) alternatives.Add("(?<![\\uD800-\\uDBFF])" + Units(first, last));
            first = Math.Max(range.First, 0x10000); last = range.Last;
            if (first > last) continue;
            int highFirst = 0xd800 + ((first - 0x10000) >> 10), highLast = 0xd800 + ((last - 0x10000) >> 10);
            int lowFirst = 0xdc00 + ((first - 0x10000) & 1023), lowLast = 0xdc00 + ((last - 0x10000) & 1023);
            if (highFirst == highLast) alternatives.Add(Unit(highFirst) + Units(lowFirst, lowLast));
            else {
                alternatives.Add(Unit(highFirst) + Units(lowFirst, 0xdfff));
                if (highFirst + 1 < highLast) alternatives.Add(Units(highFirst + 1, highLast - 1) + "[\\uDC00-\\uDFFF]");
                alternatives.Add(Unit(highLast) + Units(0xdc00, lowLast));
            }
        }
        if (basic.Length != 0) alternatives.Add("[" + basic + "]");
        return alternatives.Count == 0 ? "(?!)" : "(?:" + string.Join('|', alternatives) + ")";
    }
    static List<RegExpCodePointRange> UnicodeRegExpClassEscape(char kind)
    {
        var ranges = char.ToLowerInvariant(kind) switch {
            'd' => new List<RegExpCodePointRange>{new('0', '9')},
            'w' => new List<RegExpCodePointRange>{new('0', '9'), new('A', 'Z'), new('_', '_'), new('a', 'z')},
            _ => new List<RegExpCodePointRange>{new(9, 13), new(32, 32), new(0xa0, 0xa0), new(0x1680, 0x1680), new(0x2000, 0x200a), new(0x2028, 0x2029), new(0x202f, 0x202f), new(0x205f, 0x205f), new(0x3000, 0x3000), new(0xfeff, 0xfeff)}
        };
        return char.IsUpper(kind) ? InvertRegExpRanges(ranges) : ranges;
    }
    static readonly string UnicodeRegExpDot = RegExpCodePointAtom(InvertRegExpRanges([new(10, 10), new(13, 13), new(0x2028, 0x2029)]));
    static readonly string UnicodeRegExpDotAll = RegExpCodePointAtom([new(0, 0x10ffff)]);
    static int RegExpAdvanceIndex(string text, int index, bool unicode) =>
        index + (unicode && index < text.Length - 1 && char.IsHighSurrogate(text[index]) && char.IsLowSurrogate(text[index + 1]) ? 2 : 1);
    static int RegExpStartIndex(string text, int index, bool unicode) =>
        unicode && index > 0 && index < text.Length && char.IsLowSurrogate(text[index]) && char.IsHighSurrogate(text[index - 1]) ? index - 1 : index;

    sealed class UnicodeRegExpReader(string source)
    {
        int position;
        int Hex(int digits)
        {
            if (source.Length - position < digits) throw UnicodeRegExpSyntax();
            int value = 0;
            for (int count = 0; count < digits; count++) {
                char digit = source[position++];
                if (!char.IsAsciiHexDigit(digit)) throw UnicodeRegExpSyntax();
                value = value * 16 + (digit <= '9' ? digit - '0' : char.ToLowerInvariant(digit) - 'a' + 10);
            }
            return value;
        }
        int UnicodeEscape()
        {
            if (position < source.Length && source[position] == '{') {
                int begin = ++position;
                while (position < source.Length && char.IsAsciiHexDigit(source[position])) position++;
                if (position == begin || position == source.Length || source[position] != '}') throw UnicodeRegExpSyntax();
                var digits = source.AsSpan(begin, position++ - begin).TrimStart('0');
                if (digits.Length == 0) return 0;
                if (!int.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int value) || value < 0 || value > 0x10ffff) throw UnicodeRegExpSyntax();
                return value;
            }
            int first = Hex(4);
            if (first is >= 0xd800 and <= 0xdbff && source.Length - position >= 6 && source[position] == '\\' && source[position + 1] == 'u' && source[position + 2] != '{') {
                int saved = position; position += 2; int low = Hex(4);
                if (low is >= 0xdc00 and <= 0xdfff) return 0x10000 + ((first - 0xd800) << 10) + low - 0xdc00;
                position = saved;
            }
            return first;
        }
        List<RegExpCodePointRange> Atom(bool characterClass)
        {
            if (position == source.Length) throw UnicodeRegExpSyntax();
            int value = source[position++];
            if (value == '\\') {
                if (position == source.Length) throw UnicodeRegExpSyntax();
                char escape = source[position++];
                if (escape is 'd' or 'D' or 'w' or 'W' or 's' or 'S') return UnicodeRegExpClassEscape(escape);
                if (escape is 'p' or 'P') throw new NotSupportedException("Unicode regular-expression properties are not supported yet");
                value = escape switch {
                    'u' => UnicodeEscape(), 'x' => Hex(2), 'n' => 10, 'r' => 13, 't' => 9, 'f' => 12, 'v' => 11,
                    'b' when characterClass => 8,
                    '0' when position == source.Length || !char.IsAsciiDigit(source[position]) => 0,
                    'c' when position < source.Length && char.IsAsciiLetter(source[position]) => source[position++] % 32,
                    _ => -1
                };
                if (value < 0) {
                    if ("^$\\.*+?()[]{}|/".Contains(escape) || characterClass && escape == '-') value = escape;
                    else throw UnicodeRegExpSyntax();
                }
            }
            else if (value is >= 0xd800 and <= 0xdbff && position < source.Length && char.IsLowSurrogate(source[position]))
                value = 0x10000 + ((value - 0xd800) << 10) + source[position++] - 0xdc00;
            return [new(value, value)];
        }
        string CharacterClass()
        {
            position++;
            bool invert = position < source.Length && source[position] == '^';
            if (invert) position++;
            var ranges = new List<RegExpCodePointRange>();
            while (position < source.Length && source[position] != ']') {
                var first = Atom(true);
                if (position < source.Length - 1 && source[position] == '-' && source[position + 1] != ']') {
                    position++; var last = Atom(true);
                    if (first.Count != 1 || last.Count != 1 || first[0].First != first[0].Last || last[0].First != last[0].Last || first[0].First > last[0].First) throw UnicodeRegExpSyntax();
                    ranges.Add(new(first[0].First, last[0].First));
                }
                else ranges.AddRange(first);
            }
            if (position == source.Length) throw UnicodeRegExpSyntax();
            position++;
            return RegExpCodePointAtom(invert ? InvertRegExpRanges(ranges) : ranges);
        }
        int CaptureCount()
        {
            int captures = 0; bool characterClass = false;
            for (int index = 0; index < source.Length; index++) {
                char ch = source[index];
                if (ch == '\\') {index++; continue;}
                if (ch == '[') characterClass = true;
                else if (ch == ']') characterClass = false;
                else if (!characterClass && ch == '(' && (index + 1 == source.Length || source[index + 1] != '?' || index + 3 < source.Length && source[index + 2] == '<' && source[index + 3] is not ('=' or '!'))) captures++;
            }
            return captures;
        }
        internal string Rewrite()
        {
            var result = new StringBuilder(); int captures = CaptureCount();
            var assertions = new Stack<bool>(); bool previousAssertion = false;
            while (position < source.Length) {
                char ch = source[position];
                if (previousAssertion && ch is '*' or '+' or '?' or '{') throw UnicodeRegExpSyntax();
                previousAssertion = false;
                if (ch == '[') {result.Append(CharacterClass()); continue;}
                if (ch == '\\') {
                    if (position + 1 < source.Length && source[position + 1] is 'b' or 'B') {result.Append(source, position, 2); position += 2; previousAssertion = true; continue;}
                    if (position + 1 < source.Length && source[position + 1] is >= '1' and <= '9') {
                        int begin = ++position;
                        while (position < source.Length && char.IsAsciiDigit(source[position])) position++;
                        if (!int.TryParse(source.AsSpan(begin, position - begin), out int reference) || reference > captures) throw UnicodeRegExpSyntax();
                        result.Append('\\').Append(source, begin, position - begin); continue;
                    }
                    if (position + 2 < source.Length && source[position + 1] == 'k' && source[position + 2] == '<') {
                        int end = source.IndexOf('>', position + 3);
                        if (end < 0 || !RegExpHasNamedCapture(source)) throw UnicodeRegExpSyntax();
                        result.Append(source, position, end - position + 1); position = end + 1; continue;
                    }
                    result.Append(RegExpCodePointAtom(Atom(false))); continue;
                }
                if (ch == '(') {
                    bool assertion = position + 2 < source.Length && source[position + 1] == '?' && (source[position + 2] is '=' or '!' || position + 3 < source.Length && source[position + 2] == '<' && source[position + 3] is '=' or '!');
                    assertions.Push(assertion);
                    if (position + 2 < source.Length && source[position + 1] == '?' && RegExpModifierGroup(source, position + 2)) {
                        int end = source.IndexOf(':', position + 2);
                        if (source.AsSpan(position, end - position).Contains('i')) throw new NotSupportedException("Unicode case-insensitive regular expressions are not supported yet");
                        result.Append(source, position, end - position + 1); position = end + 1; continue;
                    }
                    if (position + 3 < source.Length && source[position + 1] == '?' && source[position + 2] == '<' && source[position + 3] is not ('=' or '!')) {
                        int end = source.IndexOf('>', position + 3); if (end < 0) throw UnicodeRegExpSyntax();
                        result.Append(source, position, end - position + 1); position = end + 1; continue;
                    }
                }
                if (ch == ')') previousAssertion = assertions.Count != 0 && assertions.Pop();
                if (ch is '^' or '$') previousAssertion = true;
                if (ch == '{') {
                    int begin = position++;
                    if (position == source.Length || !char.IsAsciiDigit(source[position])) throw UnicodeRegExpSyntax();
                    while (position < source.Length && char.IsAsciiDigit(source[position])) position++;
                    if (position < source.Length && source[position] == ',') {position++; while (position < source.Length && char.IsAsciiDigit(source[position])) position++;}
                    if (position == source.Length || source[position++] != '}') throw UnicodeRegExpSyntax();
                    result.Append(source, begin, position - begin); continue;
                }
                if (ch is '}' or ']') throw UnicodeRegExpSyntax();
                if (char.IsSurrogate(ch)) {result.Append(RegExpCodePointAtom(Atom(false))); continue;}
                result.Append(ch); position++;
            }
            return "(?:" + result + ")";
        }
    }
    static List<string> UnicodeRegExpCaptureNames(string pattern, string prefix)
    {
        var names = new List<string>(); bool characterClass = false;
        for (int index = 0; index < pattern.Length; index++) {
            char ch = pattern[index];
            if (ch == '\\') {index++; continue;}
            if (ch == '[') characterClass = true;
            else if (ch == ']') characterClass = false;
            else if (!characterClass && ch == '(') {
                if (index + 1 == pattern.Length || pattern[index + 1] != '?') names.Add(prefix + (names.Count + 1));
                else if (index + 3 < pattern.Length && pattern[index + 2] == '<' && pattern[index + 3] is not ('=' or '!')) {
                    int end = pattern.IndexOf('>', index + 3); if (end < 0) throw UnicodeRegExpSyntax();
                    names.Add(RegExpEngineCaptureName(ReadRegExpCaptureName(pattern[(index + 3)..end]))); index = end;
                }
            }
        }
        return names;
    }
}
