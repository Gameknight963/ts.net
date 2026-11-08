using System;
using System.Collections.Generic;

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
    sealed partial class UnicodeRegExpReader
    {
        bool SetOperator(string operation) => source.AsSpan(position).StartsWith(operation, StringComparison.Ordinal);
        int SetCharacter()
        {
            if (position >= source.Length) throw UnicodeRegExpSyntax();
            char ch = source[position];
            if (ch != '\\') {
                if ("()[]{}/-|".Contains(ch) ||
                    position + 1 < source.Length && source[position + 1] == ch && ("&!#$%*+,.:;<=>?@^~".Contains(ch) || ch == (char)96))
                    throw UnicodeRegExpSyntax();
            }
            var ranges = Atom(true);
            if (atomIsSet || ranges.Count != 1 || ranges[0].First != ranges[0].Last) throw UnicodeRegExpSyntax();
            return ranges[0].First;
        }
        List<RegExpCodePointRange> SetOperand(out int? character)
        {
            character = null;
            if (position == source.Length) throw UnicodeRegExpSyntax();
            if (source[position] == '[') return SetClass();
            if (source[position] == '\\' && position + 1 < source.Length && "dDwWsSpPq".Contains(source[position + 1])) {
                var ranges = Atom(true);
                return ignoreCase ? UnicodeRegExpFolding.Close(ranges) : ranges;
            }
            character = SetCharacter();
            return [new(character.Value, character.Value)];
        }
        List<RegExpCodePointRange> SetTerm(out bool range)
        {
            var result = SetOperand(out int? first);
            range = false;
            if (position < source.Length && source[position] == '-' && !SetOperator("--")) {
                if (first == null) throw UnicodeRegExpSyntax();
                position++;
                int last = SetCharacter();
                if (first.Value > last) throw UnicodeRegExpSyntax();
                result = [new(first.Value, last)]; range = true;
            }
            return ignoreCase ? UnicodeRegExpFolding.Close(result) : result;
        }
        List<RegExpCodePointRange> SetClass()
        {
            position++;
            bool invert = position < source.Length && source[position] == '^';
            if (invert) position++;
            var result = new List<RegExpCodePointRange>();
            if (position < source.Length && source[position] != ']') {
                result = SetTerm(out bool range);
                string operation = SetOperator("&&") ? "&&" : SetOperator("--") ? "--" : "";
                if (operation.Length != 0) {
                    if (range) throw UnicodeRegExpSyntax();
                    do {
                        position += 2;
                        var right = SetOperand(out _);
                        if (ignoreCase) right = UnicodeRegExpFolding.Close(right);
                        result = IntersectRegExpRanges(result, operation == "--" ? InvertRegExpRanges(right) : right);
                    } while (SetOperator(operation));
                }
                else {
                    while (position < source.Length && source[position] != ']') {
                        if (SetOperator("&&") || SetOperator("--")) throw UnicodeRegExpSyntax();
                        result.AddRange(SetTerm(out _));
                    }
                }
            }
            if (position == source.Length || source[position++] != ']') throw UnicodeRegExpSyntax();
            return invert ? InvertRegExpRanges(result) : MergeRegExpRanges(result);
        }
    }
}
