using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    static List<RegExpCodePointRange> IntersectRegExpRanges(List<RegExpCodePointRange> left, List<RegExpCodePointRange> right)
    {
        left = MergeRegExpRanges(left); right = MergeRegExpRanges(right);
        var result = new List<RegExpCodePointRange>();
        int a = 0, b = 0;
        while (a < left.Count && b < right.Count) {
            int first = Math.Max(left[a].First, right[b].First), last = Math.Min(left[a].Last, right[b].Last);
            if (first <= last) result.Add(new(first, last));
            if (left[a].Last < right[b].Last) a++; else b++;
        }
        return result;
    }
    sealed class RegExpCharacterSet
    {
        internal List<RegExpCodePointRange> Ranges = new();
        // Six hex digits per code point preserve lone-surrogate boundaries.
        internal readonly HashSet<string> Strings = new(StringComparer.Ordinal);
        internal bool MayContainStrings;
        internal bool IgnoreCase;
        internal void Union(RegExpCharacterSet other)
        {
            Ranges.AddRange(other.Ranges); Strings.UnionWith(other.Strings);
            MayContainStrings |= other.MayContainStrings;
        }
        internal void Intersect(RegExpCharacterSet other, bool subtract)
        {
            Ranges = IntersectRegExpRanges(Ranges, subtract ? InvertRegExpRanges(other.Ranges) : other.Ranges);
            if (subtract) Strings.ExceptWith(other.Strings);
            else {Strings.IntersectWith(other.Strings); MayContainStrings &= other.MayContainStrings;}
        }
    }
    sealed class RegExpStringNode
    {
        internal bool Terminal;
        internal readonly SortedDictionary<int, RegExpStringNode> Children = new();
    }
    static string RegExpStringNodeAtom(RegExpStringNode node, bool ignoreCase)
    {
        string Point(int point) {
                var ranges = new List<RegExpCodePointRange>{new(point, point)};
                return RegExpCodePointAtom(ignoreCase ? UnicodeRegExpFolding.Close(ranges) : ranges);
        }
        var prefix = new StringBuilder();
        while (!node.Terminal && node.Children.Count == 1) {
            using var child = node.Children.GetEnumerator(); child.MoveNext();
            prefix.Append(Point(child.Current.Key)); node = child.Current.Value;
        }
        if (node.Children.Count == 0) return prefix.ToString();
        var alternatives = new List<string>();
        foreach (var child in node.Children)
            alternatives.Add(Point(child.Key) + RegExpStringNodeAtom(child.Value, ignoreCase));
        // A prefix is tried only after all its longer alternatives.
        if (node.Terminal) alternatives.Add("");
        return prefix + "(?:" + string.Join('|', alternatives) + ")";
    }
    static string SetExpressionAtom(RegExpCharacterSet set)
    {
        var root = new RegExpStringNode();
        foreach (string value in set.Strings) {
            if (value.Length == 0) continue;
            var node = root;
            for (int index = 0; index < value.Length; index += 6) {
                int point = int.Parse(value.AsSpan(index, 6), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                if (!node.Children.TryGetValue(point, out var child)) {
                    child = new(); node.Children.Add(point, child);
                }
                node = child;
            }
            node.Terminal = true;
        }
        var alternatives = new List<string>();
        if (root.Children.Count != 0) alternatives.Add(RegExpStringNodeAtom(root, set.IgnoreCase));
        if (set.Ranges.Count != 0) alternatives.Add(RegExpCodePointAtom(set.Ranges));
        if (set.Strings.Contains("")) alternatives.Add("");
        return alternatives.Count == 0 ? "(?!)" : "(?:" + string.Join('|', alternatives) + ")";
    }
    sealed partial class UnicodeRegExpReader
    {
        bool TryStringProperty(out RegExpCharacterSet result)
        {
            result = null;
            if (!unicodeSets || position + 3 >= source.Length || source[position] != '\\' ||
                source[position + 1] is not ('p' or 'P') || source[position + 2] != '{') return false;
            int end = source.IndexOf('}', position + 3);
            if (end < 0) return false;
            string property = source[(position + 3)..end];
            if (!IsRegExpStringProperty(property)) return false;
            if (source[position + 1] == 'P') throw UnicodeRegExpSyntax();
            position = end + 1;
            result = UnicodeRegExpStringProperties.Select(property, ignoreCase);
            return true;
        }
        bool SetOperator(string operation) => source.AsSpan(position).StartsWith(operation, StringComparison.Ordinal);
        int SetCharacter()
        {
            if (position >= source.Length) throw UnicodeRegExpSyntax();
            char ch = source[position];
            if (ch == '\\' && position + 1 < source.Length && "dDwWsSpPq".Contains(source[position + 1]))
                throw UnicodeRegExpSyntax();
            if (ch != '\\') {
                if ("()[]{}/-|".Contains(ch) ||
                    position + 1 < source.Length && source[position + 1] == ch && ("&!#$%*+,.:;<=>?@^~".Contains(ch) || ch == (char)96))
                    throw UnicodeRegExpSyntax();
            }
            var ranges = Atom(true);
            if (atomIsSet || ranges.Count != 1 || ranges[0].First != ranges[0].Last) throw UnicodeRegExpSyntax();
            return ranges[0].First;
        }
        RegExpCharacterSet SetStrings()
        {
            position += 2;
            if (position == source.Length || source[position++] != '{') throw UnicodeRegExpSyntax();
            var result = new RegExpCharacterSet { IgnoreCase = ignoreCase };
            while (true) {
                var points = new List<int>();
                var key = new StringBuilder();
                while (position < source.Length && source[position] is not ('|' or '}')) {
                    int point = SetCharacter();
                    if (ignoreCase) point = UnicodeRegExpFolding.Canonical(point);
                    points.Add(point); key.Append(point.ToString("X6", CultureInfo.InvariantCulture));
                }
                if (position == source.Length) throw UnicodeRegExpSyntax();
                if (points.Count == 1) result.Ranges.Add(new(points[0], points[0]));
                else {result.Strings.Add(key.ToString()); result.MayContainStrings = true;}
                if (source[position++] == '}') break;
            }
            if (ignoreCase) result.Ranges = UnicodeRegExpFolding.Close(result.Ranges);
            return result;
        }
        RegExpCharacterSet SetOperand(out int? character)
        {
            character = null;
            if (position == source.Length) throw UnicodeRegExpSyntax();
            if (source[position] == '[') return SetClass();
            if (TryStringProperty(out var property)) return property;
            if (source[position] == '\\' && position + 1 < source.Length && source[position + 1] == 'q') return SetStrings();
            if (source[position] == '\\' && position + 1 < source.Length && "dDwWsSpP".Contains(source[position + 1])) {
                var ranges = Atom(true);
                return new() {Ranges = ignoreCase ? UnicodeRegExpFolding.Close(ranges) : ranges, IgnoreCase = ignoreCase};
            }
            character = SetCharacter();
            return new() {Ranges = [new(character.Value, character.Value)], IgnoreCase = ignoreCase};
        }
        RegExpCharacterSet SetTerm(out bool range)
        {
            var result = SetOperand(out int? first);
            range = false;
            if (position < source.Length && source[position] == '-' && !SetOperator("--")) {
                if (first == null) throw UnicodeRegExpSyntax();
                position++;
                int last = SetCharacter();
                if (first.Value > last) throw UnicodeRegExpSyntax();
                result = new() {Ranges = [new(first.Value, last)], IgnoreCase = ignoreCase}; range = true;
            }
            if (ignoreCase) result.Ranges = UnicodeRegExpFolding.Close(result.Ranges);
            return result;
        }
        RegExpCharacterSet SetClass()
        {
            position++;
            bool invert = position < source.Length && source[position] == '^';
            if (invert) position++;
            var result = new RegExpCharacterSet {IgnoreCase = ignoreCase};
            if (position < source.Length && source[position] != ']') {
                result = SetTerm(out bool range);
                string operation = SetOperator("&&") ? "&&" : SetOperator("--") ? "--" : "";
                if (operation.Length != 0) {
                    if (range) throw UnicodeRegExpSyntax();
                    do {
                        position += 2;
                        var right = SetOperand(out _);
                        if (ignoreCase) right.Ranges = UnicodeRegExpFolding.Close(right.Ranges);
                        result.Intersect(right, operation == "--");
                    } while (SetOperator(operation));
                }
                else {
                    while (position < source.Length && source[position] != ']') {
                        if (SetOperator("&&") || SetOperator("--")) throw UnicodeRegExpSyntax();
                        result.Union(SetTerm(out _));
                    }
                }
            }
            if (position == source.Length || source[position++] != ']') throw UnicodeRegExpSyntax();
            if (invert && result.MayContainStrings) throw UnicodeRegExpSyntax();
            result.Ranges = invert ? InvertRegExpRanges(result.Ranges) : MergeRegExpRanges(result.Ranges);
            return result;
        }
    }
}
