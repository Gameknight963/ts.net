using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

public static partial class Js
{
    static readonly ConditionalWeakTable<Regex, Lazy<Regex>> RegExpCaptureTestEngines = new();
    static bool RegExpHasCaptureReferences(string pattern)
    {
        // Conservative: escaped literals may disable the fast path too.
        for (int index = 0; index + 1 < pattern.Length; index++)
            if (pattern[index] == '\\' && (pattern[index + 1] == 'k' || pattern[index + 1] is >= '1' and <= '9')) return true;
        return false;
    }
    static void RegisterRegExpCaptureTestEngine(Regex engine, string original, RegexOptions options)
    {
        // With no backreferences, capture state cannot change whether or where
        // a match succeeds. Native test calls do not expose the capture values.
        if (!RegExpHasCaptureReferences(original))
            RegExpCaptureTestEngines.Add(engine, new(() => new Regex(original, options)));
    }
    static Regex RegExpTestEngine(Regex engine) =>
        RegExpCaptureTestEngines.TryGetValue(engine, out var test) ? test.Value : engine;
    sealed class RegExpCaptureFrame(string header, bool reverse, string name = null)
    {
        internal readonly string Header = header;
        internal readonly bool Reverse = reverse;
        internal readonly StringBuilder Text = new();
        internal readonly List<string> Captures = name == null ? new() : new() {name};
        internal int AtomStart = -1;
        internal List<string> AtomCaptures;
        internal bool AtomConsumes, BeforeAtomConsumes, SequenceConsumes, AllAlternativesConsume = true;
        internal bool SafeHistory = true;
        internal bool Consumes => AllAlternativesConsume && SequenceConsumes && !Header.StartsWith("(?=", StringComparison.Ordinal) &&
            !Header.StartsWith("(?!", StringComparison.Ordinal) && !Header.StartsWith("(?<=", StringComparison.Ordinal) && !Header.StartsWith("(?<!", StringComparison.Ordinal);
        internal void Append(string atom, List<string> captures = null, bool consumes = true)
        {
            AtomStart = Text.Length; AtomCaptures = captures; Text.Append(atom);
            BeforeAtomConsumes = SequenceConsumes; AtomConsumes = consumes; SequenceConsumes |= consumes;
        }
    }
    static string ResetRegExpRepeatedCaptures(string pattern)
    {
        var frames = new Stack<RegExpCaptureFrame>();
        var current = new RegExpCaptureFrame("", false);
        for (int position = 0; position < pattern.Length;) {
            int begin = position;
            char ch = pattern[position++];
            if (ch == '\\') {
                bool consumes = true;
                if (position < pattern.Length && pattern[position] is 'b' or 'B' or 'k' or >= '1' and <= '9') consumes = false;
                if (position + 1 < pattern.Length && pattern[position] == 'k' && pattern[position + 1] == '<') {
                    int end = pattern.IndexOf('>', position + 2);
                    if (end >= 0) position = end;
                }
                if (position < pattern.Length) position++;
                if (!consumes) while (position < pattern.Length && char.IsAsciiDigit(pattern[position])) position++;
                current.Append(pattern[begin..position], consumes: consumes); continue;
            }
            if (ch == '[') {
                while (position < pattern.Length) {
                    char unit = pattern[position++];
                    if (unit == '\\' && position < pattern.Length) position++;
                    else if (unit == ']') break;
                }
                current.Append(pattern[begin..position]); continue;
            }
            if (ch == '(') {
                bool reverse = current.Reverse; string name = null;
                if (position < pattern.Length && pattern[position] == '?') {
                    position++;
                    if (position == pattern.Length) return pattern;
                    if (pattern[position] == '<') {
                        position++;
                        if (position == pattern.Length) return pattern;
                        if (pattern[position] is '=' or '!') {reverse = true; position++;}
                        else {
                            int end = pattern.IndexOf('>', position);
                            if (end < 0) return pattern;
                            name = pattern[position..end]; position = end + 1;
                        }
                    }
                    else if (pattern[position] is '=' or '!') {reverse = false; position++;}
                    else if (pattern[position] == ':') position++;
                    else if (pattern[position] == '(') {
                        // Numeric and named backreferences use capture conditionals.
                        int end = pattern.IndexOf(')', position + 1);
                        if (end < 0) return pattern;
                        position = end + 1;
                    }
                    else {
                        int end = pattern.IndexOf(':', position);
                        if (end < 0) return pattern;
                        position = end + 1;
                    }
                }
                frames.Push(current); current = new(pattern[begin..position], reverse, name);
                continue;
            }
            if (ch == ')') {
                if (frames.Count == 0) return pattern;
                var finished = current; current = frames.Pop();
                current.Append(finished.Header + finished.Text + ")", finished.Captures, finished.Consumes);
                current.SafeHistory &= finished.SafeHistory;
                current.Captures.AddRange(finished.Captures); continue;
            }
            bool quantifier = ch is '*' or '+' or '?';
            bool minimumPositive = ch == '+';
            if (ch == '{') {
                int scan = position;
                while (scan < pattern.Length && char.IsAsciiDigit(pattern[scan])) scan++;
                if (scan > position) {
                    if (scan < pattern.Length && pattern[scan] == ',') {
                        scan++; while (scan < pattern.Length && char.IsAsciiDigit(pattern[scan])) scan++;
                    }
                    if (scan < pattern.Length && pattern[scan] == '}') {
                        int end = pattern.IndexOfAny([',', '}'], begin + 1);
                        minimumPositive = int.TryParse(pattern.AsSpan(begin + 1, end - begin - 1), out int minimum) && minimum > 0;
                        position = scan + 1; quantifier = true;
                    }
                }
            }
            if (quantifier) {
                if (position < pattern.Length && pattern[position] == '?') position++;
                if (current.AtomStart >= 0 && current.AtomCaptures is {Count: > 0}) {
                    // CLR empty iterations have separate capture semantics. Leave
                    // those patterns untouched until their repetition is lowered too.
                    if (!current.AtomConsumes) current.SafeHistory = false;
                    var reset = new StringBuilder();
                    foreach (string capture in current.AtomCaptures)
                        reset.Append("(?(").Append(capture).Append(")(?<-").Append(capture).Append(">))");
                    string atom = current.Text.ToString(current.AtomStart, current.Text.Length - current.AtomStart);
                    current.Text.Length = current.AtomStart;
                    current.Text.Append("(?:");
                    // Lookbehind evaluates its atoms from right to left.
                    if (!current.Reverse) current.Text.Append(reset);
                    current.Text.Append(atom);
                    if (current.Reverse) current.Text.Append(reset);
                    current.Text.Append(')');
                }
                current.Text.Append(pattern, begin, position - begin);
                current.SequenceConsumes = current.BeforeAtomConsumes || minimumPositive && current.AtomConsumes;
                current.AtomStart = -1; current.AtomCaptures = null; continue;
            }
            if (ch is '|' or '^' or '$') {
                current.Text.Append(ch); current.AtomStart = -1; current.AtomCaptures = null;
                if (ch == '|') {current.AllAlternativesConsume &= current.SequenceConsumes; current.SequenceConsumes = false;}
            }
            else current.Append(ch.ToString());
        }
        return frames.Count == 0 && current.SafeHistory ? current.Text.ToString() : pattern;
    }
}
