using System;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static partial class Js
{
    static void AppendReplacement(StringBuilder result, string template, string text, string match, int position, Match captures = null, JsRegExp regex = null)
    {
        int literal = 0;
        for (int index = 0; index + 1 < template.Length; index++)
        {
            if (template[index] != '$') continue;
            char token = template[index + 1];
            if (captures != null && regex.NamedGroups.Length != 0 && token == '<')
            {
                int end = template.IndexOf('>', index + 2);
                if (end >= 0)
                {
                    result.Append(template, literal, index - literal);
                    string name = template[(index + 2)..end];
                    int captureIndex = Array.IndexOf(regex.NamedGroups, name);
                    if (captureIndex >= 0) result.Append(RegExpCaptureText(text, captures.Groups[regex.NamedCaptureIndices[captureIndex]]));
                    index = end; literal = end + 1; continue;
                }
            }
            if (captures != null && token >= '0' && token <= '9')
            {
                int capture = token - '0', digits = 1;
                if (index + 2 < template.Length && template[index + 2] >= '0' && template[index + 2] <= '9')
                {
                    int pair = capture * 10 + template[index + 2] - '0';
                    if (pair > 0 && pair < captures.Groups.Count) { capture = pair; digits = 2; }
                }
                if (capture > 0 && capture < captures.Groups.Count)
                {
                    result.Append(template, literal, index - literal);
                    result.Append(RegExpCaptureText(text, captures.Groups[capture]));
                    index += digits; literal = index + 1; continue;
                }
            }
            if (token != '$' && token != '&' && token != '`' && token != '\'') continue;
            result.Append(template, literal, index - literal);
            switch (token)
            {
                case '$': result.Append('$'); break;
                case '&': result.Append(match); break;
                case '`': result.Append(text, 0, position); break;
                case '\'': result.Append(text, position + match.Length, text.Length - position - match.Length); break;
            }
            index++; literal = index + 1;
        }
        result.Append(template, literal, template.Length - literal);
    }
    static object ReplaceRegExp(string text, JsRegExp regex, object replacement, bool all)
    {
        var function = replacement as JsFunction;
        string template = function == null ? StringValue(replacement) : null;
        string flags = StringValue(regex.Get("flags"));
        bool global = flags.Contains('g'), sticky = regex.Flags.Contains('y');
        if (all && !global) throw new InvalidOperationException("replaceAll requires a global regular expression");
        if (!HasBuiltinRegExpExec(regex) || flags != regex.Flags) return ReplaceCustomRegExp(text, regex, function, template, flags);
        if (global) regex.LastIndex = 0d;
        double start = Number(regex.LastIndex);
        start = double.IsNaN(start) || start <= 0 ? 0 : Math.Truncate(start);
        if (!sticky) start = 0;
        if (start <= text.Length) start = RegExpStartIndex(text, (int)start, regex.Flags.Contains('u'));
        string matchingText = RegExpInput(regex, text);
        var matches = new List<Match>();
        if (start <= text.Length)
        {
            Match match = regex.Engine.Match(matchingText, (int)start);
            while (match.Success && (!sticky || match.Index == start))
            {
                matches.Add(match);
                start = match.Index + match.Length;
                if (sticky || global) regex.LastIndex = start;
                if (!global) break;
                if (match.Length == 0) start = RegExpAdvanceIndex(text, (int)start, regex.Flags.Contains('u'));
                if (start > text.Length) break;
                match = regex.Engine.Match(matchingText, (int)start);
            }
        }
        if (global || sticky && matches.Count == 0) regex.LastIndex = 0d;
        if (matches.Count == 0) return text;
        var result = new StringBuilder(text.Length); int copied = 0;
        foreach (Match match in matches)
        {
            result.Append(text, copied, match.Index - copied);
            if (function != null)
            {
                var args = new object[match.Groups.Count + 2 + (regex.NamedGroups.Length == 0 ? 0 : 1)];
                for (int i = 0; i < match.Groups.Count; i++) args[i] = match.Groups[i].Success ? RegExpCaptureText(text, match.Groups[i]) : Undefined;
                args[match.Groups.Count] = (double)match.Index; args[match.Groups.Count + 1] = text;
                if (regex.NamedGroups.Length != 0) args[^1] = RegExpNamedGroups(regex, match, text: text);
                result.Append(StringValue(function.InvokeWithReceiver(Undefined, args)));
            }
            else AppendReplacement(result, template, text, RegExpCaptureText(text, match), match.Index, match, regex);
            copied = match.Index + match.Length;
        }
        result.Append(text, copied, text.Length - copied); return result.ToString();
    }
    sealed class StringReplacementFunction(bool all) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, all ? "replaceAll" : "replace", 2)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            object searchValue = FunctionArgument(arguments, 0);
            if (IsNullish(receiver)) throw new InvalidOperationException("String method requires a receiver");
            if (all) RequireGlobalRegExp(searchValue, "replaceAll");
            if (TryStringProtocol(receiver, searchValue, ReplaceSymbol, FunctionArgument(arguments, 1), true, out var custom)) return custom;
            string text = AtString(receiver);
            string search = StringValue(searchValue);
            object replacement = FunctionArgument(arguments, 1);
            var function = replacement as JsFunction;
            // Replacement conversion is observable even when no match exists.
            string template = function == null ? StringValue(replacement) : null;
            int position = text.IndexOf(search, StringComparison.Ordinal);
            if (position < 0) return text;
            var result = new StringBuilder(text.Length);
            int copied = 0;
            while (true)
            {
                result.Append(text, copied, position - copied);
                if (function != null)
                    result.Append(StringValue(function.InvokeWithReceiver(Undefined, new object[] { search, (double)position, text })));
                else AppendReplacement(result, template, text, search, position);
                copied = position + search.Length;
                if (!all || search.Length == 0 && position == text.Length) break;
                position = text.IndexOf(search, position + Math.Max(search.Length, 1), StringComparison.Ordinal);
                if (position < 0) break;
            }
            result.Append(text, copied, text.Length - copied); return result.ToString();
        }
    }
}
