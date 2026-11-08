using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

public static partial class Js
{
    sealed class RegExpAlternative(int id)
    {
        internal readonly int Id = id;
        internal int Branch;
    }
    sealed record RegExpNamedOccurrence(string PhysicalName, (int Group, int Branch)[] Path);
    sealed class RegExpNamedCaptures
    {
        internal static readonly RegExpNamedCaptures Empty = new();
        internal readonly List<string> Names = new();
        internal readonly Dictionary<int, string> AtPosition = new();
        internal readonly Dictionary<string, List<RegExpNamedOccurrence>> ByName = new(StringComparer.Ordinal);
    }
    static bool RegExpSeparateAlternatives((int Group, int Branch)[] left, (int Group, int Branch)[] right)
    {
        int count = Math.Min(left.Length, right.Length);
        for (int index = 0; index < count && left[index].Group == right[index].Group; index++)
            if (left[index].Branch != right[index].Branch) return true;
        return false;
    }
    static RegExpNamedCaptures ReadRegExpNamedCaptures(string pattern)
    {
        if (!pattern.Contains("(?<", StringComparison.Ordinal) || !RegExpHasNamedCapture(pattern))
            return RegExpNamedCaptures.Empty;
        var result = new RegExpNamedCaptures();
        var path = new List<RegExpAlternative>{new(0)};
        int nextGroup = 1; bool characterClass = false;
        for (int index = 0; index < pattern.Length; index++) {
            char ch = pattern[index];
            if (ch == '\\') {index++; continue;}
            if (ch == '[') {characterClass = true; continue;}
            if (ch == ']') {characterClass = false; continue;}
            if (characterClass) continue;
            if (ch == '|') {path[^1].Branch++; continue;}
            if (ch == ')') {if (path.Count > 1) path.RemoveAt(path.Count - 1); continue;}
            if (ch != '(') continue;
            if (index + 3 < pattern.Length && pattern[index + 1] == '?' && pattern[index + 2] == '<' && pattern[index + 3] is not ('=' or '!')) {
                int end = pattern.IndexOf('>', index + 3);
                if (end < 0) throw InvalidCaptureName();
                string name = ReadRegExpCaptureName(pattern[(index + 3)..end]);
                var alternatives = new (int Group, int Branch)[path.Count];
                for (int part = 0; part < alternatives.Length; part++) alternatives[part] = (path[part].Id, path[part].Branch);
                if (!result.ByName.TryGetValue(name, out var previous)) {
                    previous = new(); result.ByName.Add(name, previous); result.Names.Add(name);
                }
                foreach (var occurrence in previous)
                    if (!RegExpSeparateAlternatives(occurrence.Path, alternatives))
                        throw MakeThrown(new JsCaughtError("SyntaxError", "Duplicate capture name"));
                string physical = RegExpEngineCaptureName(name);
                if (previous.Count != 0) physical += "Occurrence" + (previous.Count + 1).ToString(CultureInfo.InvariantCulture);
                previous.Add(new(physical, alternatives)); result.AtPosition.Add(index, physical);
                index = end;
            }
            path.Add(new(nextGroup++));
        }
        return result;
    }
    internal static int[][] RegExpDuplicateCaptureIndices(Regex engine, string[] names)
    {
        int[][] result = null;
        for (int index = 0; index < names.Length; index++) {
            string physical = RegExpEngineCaptureName(names[index]);
            int second = engine.GroupNumberFromName(physical + "Occurrence2");
            if (second < 0) continue;
            var candidates = new List<int>{engine.GroupNumberFromName(physical), second};
            for (int occurrence = 3; ; occurrence++) {
                int number = engine.GroupNumberFromName(physical + "Occurrence" + occurrence.ToString(CultureInfo.InvariantCulture));
                if (number < 0) break;
                candidates.Add(number);
            }
            result ??= new int[names.Length][]; result[index] = candidates.ToArray();
        }
        return result;
    }
    static int RegExpNamedCaptureNumber(JsRegExp regex, Match match, int index)
    {
        var candidates = regex.DuplicateCaptureIndices?[index];
        if (candidates != null)
            foreach (int number in candidates) if (match.Groups[number].Success) return number;
        return regex.NamedCaptureIndices[index];
    }
}
