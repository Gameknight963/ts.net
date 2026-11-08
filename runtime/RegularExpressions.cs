using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

internal sealed class JsRegExp(string pattern, string flags, Regex engine, string[] namedGroups) : TsObject
{
    internal string Pattern = pattern, Flags = flags;
    internal Regex Engine = engine;
    internal string[] NamedGroups = namedGroups;
    internal object LastIndex = 0d;
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.RegExpPrototype;
    public override bool HasOwn(string key) => key == "lastIndex" || base.HasOwn(key);
    public override object Get(string key) => key == "lastIndex" ? LastIndex : base.Get(key);
    public override object Set(string key, object value) { if (key == "lastIndex") { LastIndex = value; return value; } return base.Set(key, value); }
    public override bool DeleteOwn(string key) => key != "lastIndex" && base.DeleteOwn(key);
    internal override bool OwnPropertyConfigurable(string key) => key != "lastIndex" && base.OwnPropertyConfigurable(key);
    internal override object OwnPropertyNames() => Js.MergeOwnNames(this, base.OwnKeys(), "lastIndex");
    internal override object ForInOwnKeys() => Js.ForInNames(base.OwnKeys(), "lastIndex");
    internal override bool ForInEnumerable(string key) => key != "lastIndex";
}

public static partial class Js
{
    internal static TsObject RegExpPrototype => (TsObject)RuntimeNamespaces.RegExp.Get("prototype");
    static readonly Dictionary<(string, RegexOptions), Regex> regexEngines = new();
    static Regex RegExpEngine(string pattern, RegexOptions options, bool dotAll, out string[] namedGroups)
    {
        bool inClass = false;
        bool currentDotAll = dotAll, currentMultiline = (options & RegexOptions.Multiline) != 0;
        var modes = new Stack<(bool DotAll, bool Multiline)>();
        var names = new List<string>();
        string prefix = "tsnetCapture";
        while (pattern.Contains(prefix, StringComparison.Ordinal)) prefix += "X";
        int captures = 0;
        bool namedBackReferences = pattern.Contains("\\k", StringComparison.Ordinal) && RegExpHasNamedCapture(pattern);
        var rewritten = new StringBuilder(pattern.Length);
        for (int i = 0; i < pattern.Length; i++)
        {
            char ch = pattern[i];
            if (ch == '\\')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == 'c')
                {
                    if (i + 2 < pattern.Length && (char.IsAsciiLetter(pattern[i + 2]) || inClass && (char.IsAsciiDigit(pattern[i + 2]) || pattern[i + 2] == '_')))
                    {
                        rewritten.Append("\\x").Append(((int)pattern[i + 2] % 32).ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
                        i += 2; continue;
                    }
                    // Invalid legacy control escapes retain the backslash.
                    rewritten.Append("\\\\c"); i++; continue;
                }
                if (i + 1 < pattern.Length && pattern[i + 1] == 'k')
                {
                    if (!namedBackReferences) { rewritten.Append('k'); i++; continue; }
                    if (inClass || i + 2 >= pattern.Length || pattern[i + 2] != '<')
                        throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid named back reference"));
                }
                if (i + 1 < pattern.Length && pattern[i + 1] is 'x' or 'u')
                {
                    int digits = pattern[i + 1] == 'x' ? 2 : 4;
                    bool valid = pattern.Length - i - 2 >= digits;
                    for (int digit = 0; valid && digit < digits; digit++) valid = char.IsAsciiHexDigit(pattern[i + 2 + digit]);
                    if (!valid) { rewritten.Append(pattern[++i]); continue; }
                }
                if (i + 1 < pattern.Length && pattern[i + 1] is 'a' or 'e' or 'A' or 'G' or 'Z' or 'z' or 'p' or 'P')
                {
                    // These are identity escapes without u/v, but .NET assigns
                    // them control-character, anchor, or Unicode-property meanings.
                    rewritten.Append(pattern[++i]); continue;
                }
                rewritten.Append(ch); i++;
                if (i < pattern.Length) rewritten.Append(pattern[i]);
                continue;
            }
            if (!inClass && ch == '[' && i + 1 < pattern.Length && pattern[i + 1] == ']')
            { rewritten.Append("(?!)"); i++; continue; }
            if (!inClass && ch == '[' && i + 2 < pattern.Length && pattern[i + 1] == '^' && pattern[i + 2] == ']')
            { rewritten.Append("[\\s\\S]"); i += 2; continue; }
            if (ch == '[') inClass = true; else if (ch == ']') inClass = false;
            if (!inClass && ch == '(')
            {
                modes.Push((currentDotAll, currentMultiline));
                if (i + 2 < pattern.Length && pattern[i + 1] == '?' && RegExpModifierGroup(pattern, i + 2))
                {
                    bool enable = true;
                    for (int flag = i + 2; pattern[flag] != ':'; flag++)
                        if (pattern[flag] == '-') enable = false;
                        else if (pattern[flag] == 's') currentDotAll = enable;
                        else if (pattern[flag] == 'm') currentMultiline = enable;
                }
            }
            else if (!inClass && ch == ')' && modes.Count != 0) (currentDotAll, currentMultiline) = modes.Pop();
            if (!inClass && ch == '.') { rewritten.Append(currentDotAll ? "[\\s\\S]" : "[^\\n\\r\\u2028\\u2029]"); continue; }
            if (!inClass && ch == '^') { rewritten.Append(currentMultiline ? "(?<![^\\n\\r\\u2028\\u2029])" : "\\A"); continue; }
            if (!inClass && ch == '$') { rewritten.Append(currentMultiline ? "(?![^\\n\\r\\u2028\\u2029])" : "\\z"); continue; }
            bool lookbehind = i + 3 < pattern.Length && pattern[i + 2] == '<' && pattern[i + 3] is '=' or '!';
            bool named = !inClass && ch == '(' && i + 3 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] == '<' && !lookbehind;
            if (named)
            {
                int end = pattern.IndexOf('>', i + 3);
                if (end < 0) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid named capture"));
                string name = pattern[(i + 3)..end];
                if (name.Length == 0 || char.IsDigit(name[0])) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid capture name"));
                if (names.Contains(name)) throw new NotSupportedException("Duplicate named captures are not supported");
                names.Add(name); captures++;
            }
            if (!inClass && ch == '(' && (i + 1 >= pattern.Length || pattern[i + 1] != '?'))
            {
                // .NET otherwise numbers unnamed groups before named groups.
                rewritten.Append("(?<").Append(prefix).Append(++captures).Append('>');
                continue;
            }
            if (!inClass && ch == '(' && i + 2 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] is not (':' or '=' or '!') && !lookbehind && !named)
                if (!RegExpModifierGroup(pattern, i + 2))
                    throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid regular-expression group"));
            rewritten.Append(ch);
        }
        namedGroups = names.ToArray();
        pattern = rewritten.ToString();
        lock (regexEngines)
        {
            if (regexEngines.TryGetValue((pattern, options), out var cached)) return cached;
            Regex result;
            try { result = new Regex(pattern, options); }
            catch (ArgumentException error) { throw MakeThrown(new JsCaughtError("SyntaxError", error.Message)); }
            if (regexEngines.Count >= 64) regexEngines.Clear();
            regexEngines.Add((pattern, options), result); return result;
        }
    }
    static bool RegExpHasNamedCapture(string pattern)
    {
        bool inClass = false;
        for (int index = 0; index < pattern.Length; index++)
        {
            char character = pattern[index];
            if (character == '\\') { index++; continue; }
            if (character == '[') inClass = true;
            else if (character == ']') inClass = false;
            else if (!inClass && character == '(' && index + 3 < pattern.Length && pattern[index + 1] == '?' && pattern[index + 2] == '<' && pattern[index + 3] is not ('=' or '!')) return true;
        }
        return false;
    }
    static bool RegExpModifierGroup(string pattern, int start)
    {
        int flags = 0;
        bool subtract = false;
        for (int index = start; index < pattern.Length; index++)
        {
            char ch = pattern[index];
            if (ch == ':') return flags != 0;
            if (ch == '-') {
                if (subtract) return false;
                subtract = true; continue;
            }
            int flag = ch switch { 'i' => 1, 'm' => 2, 's' => 4, _ => 0 };
            if (flag == 0 || (flags & flag) != 0) return false;
            flags |= flag;
        }
        return false;
    }
    static JsRegExp CreateRegExp(object pattern, object flags)
    {
        string text = pattern is JsRegExp original ? original.Pattern : IsUndefined(pattern) ? "" : StringValue(pattern);
        string requested = IsUndefined(flags) ? pattern is JsRegExp regex ? regex.Flags : "" : StringValue(flags);
        var seen = new HashSet<char>();
        foreach (char flag in requested)
            if ("dgimsuvy".IndexOf(flag) < 0 || !seen.Add(flag)) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid regular-expression flags"));
        if (seen.Contains('u') && seen.Contains('v')) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid regular-expression flags"));
        foreach (char flag in "uv") if (seen.Contains(flag)) throw new NotSupportedException("Regular-expression flags u and v are not supported");
        var options = RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.Compiled;
        if (seen.Contains('i')) options |= RegexOptions.IgnoreCase;
        if (seen.Contains('m')) options |= RegexOptions.Multiline;
        string canonical = ""; foreach (char flag in "dgimsy") if (seen.Contains(flag)) canonical += flag;
        var engine = RegExpEngine(text, options, seen.Contains('s'), out var names);
        return new JsRegExp(text, canonical, engine, names);
    }
    static string RegExpSource(string pattern)
    {
        if (pattern.Length == 0) return "(?:)";
        var result = new StringBuilder(); int slashes = 0;
        foreach (char ch in pattern)
        {
            if (ch == '/' && slashes % 2 == 0) result.Append('\\');
            result.Append(ch switch { '\n' => "\\n", '\r' => "\\r", '\u2028' => "\\u2028", '\u2029' => "\\u2029", _ => ch.ToString() });
            slashes = ch == '\\' ? slashes + 1 : 0;
        }
        return result.ToString();
    }
    internal static object RegExpMatch(JsRegExp regex, object input, bool test)
    {
        string text = StringValue(input);
        bool global = regex.Flags.Contains('g'), sticky = regex.Flags.Contains('y');
        double position = Number(regex.LastIndex);
        position = double.IsNaN(position) || position <= 0 ? 0 : Math.Truncate(position);
        if (!global && !sticky) position = 0;
        if (position > text.Length) { regex.LastIndex = 0d; return test ? BoxBoolean(false) : null; }
        if (test)
        {
            if (!global && !sticky) return BoxBoolean(regex.Engine.IsMatch(text));
            var matches = regex.Engine.EnumerateMatches(text.AsSpan(), (int)position);
            if (!matches.MoveNext() || sticky && matches.Current.Index != position) { regex.LastIndex = 0d; return BoxBoolean(false); }
            regex.LastIndex = (double)(matches.Current.Index + matches.Current.Length); return BoxBoolean(true);
        }
        Match match = regex.Engine.Match(text, (int)position);
        if (!match.Success || sticky && match.Index != position) { if (global || sticky) regex.LastIndex = 0d; return test ? BoxBoolean(false) : null; }
        if (global || sticky) regex.LastIndex = (double)(match.Index + match.Length);
        if (test) return BoxBoolean(true);
        var result = new JsArray(0);
        foreach (Group group in match.Groups) result.Append(group.Success ? group.Value : Undefined);
        result.DefineOwn("index", (double)match.Index); result.DefineOwn("input", text); result.DefineOwn("groups", RegExpNamedGroups(regex, match));
        if (regex.Flags.Contains('d'))
        {
            var indices = new JsArray(0);
            foreach (Group group in match.Groups)
                indices.Append(group.Success ? new JsArray(new object[] { (double)group.Index, (double)(group.Index + group.Length) }) : Undefined);
            indices.DefineOwn("groups", RegExpNamedGroups(regex, match, indices));
            result.DefineOwn("indices", indices);
        }
        return result;
    }
    static object RegExpNamedGroups(JsRegExp regex, Match match, JsArray indices = null)
    {
        if (regex.NamedGroups.Length == 0) return Undefined;
        var result = new JsDynamicObject(); result.SetPrototype(null);
        foreach (string name in regex.NamedGroups)
        {
            var group = match.Groups[name];
            result.DefineOwn(name, !group.Success ? Undefined : indices != null
                ? indices.Get(regex.Engine.GroupNumberFromName(name).ToString(System.Globalization.CultureInfo.InvariantCulture))
                : group.Value);
        }
        return result;
    }
    sealed class RegExpConstructor : NativeNamespaceFunction
    {
        internal RegExpConstructor() : base("RegExp", static args =>
            FunctionArgument(args, 0) is JsRegExp regex && IsUndefined(FunctionArgument(args, 1)) && ReferenceEquals(regex.Get("constructor"), RuntimeNamespaces.RegExp)
                ? regex : CreateRegExp(FunctionArgument(args, 0), FunctionArgument(args, 1)), 2)
        {
            Member("escape", new JsFunction((FunctionBody)(static args => EscapeRegExp(FunctionArgument(args, 0))), null, "escape", 1));
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype };
            UseObjectPrototype(prototype); prototype.DefineMethod("constructor", this);
            DefineRegExpProtocols(prototype);
            prototype.DefineMethod("compile", new CollectionMethod("compile", 2, (receiver, args) =>
            {
                if (receiver is not JsRegExp target)
                    throw MakeThrown(new JsCaughtError("TypeError", "RegExp.compile requires a regular expression"));
                object pattern = FunctionArgument(args, 0), flags = FunctionArgument(args, 1);
                if (pattern is JsRegExp && !IsUndefined(flags))
                    throw MakeThrown(new JsCaughtError("TypeError", "Cannot supply flags when compiling a regular expression"));
                // Validate and compile first: invalid input must leave the receiver unchanged.
                var replacement = CreateRegExp(pattern, flags);
                target.Pattern = replacement.Pattern;
                target.Flags = replacement.Flags;
                target.Engine = replacement.Engine;
                target.NamedGroups = replacement.NamedGroups;
                target.LastIndex = 0d;
                return target;
            }));
            foreach (bool test in new[] { false, true })
                prototype.DefineMethod(test ? "test" : "exec", new CollectionMethod(test ? "test" : "exec", 1, (receiver, args) => receiver is JsRegExp regex ? RegExpMatch(regex, FunctionArgument(args, 0), test) : throw new InvalidOperationException("RegExp method requires a regular expression")));
            prototype.DefineMethod("toString", new CollectionMethod("toString", 0, (receiver, _) =>
            {
                var source = ReflectTarget(receiver); string pattern = StringValue(source.Get("source")), flags = StringValue(source.Get("flags")); return "/" + pattern + "/" + flags;
            }));
            prototype.DefineClassAccessor("source", new CollectionMethod("get source", 0, (receiver, _) => receiver is JsRegExp regex ? RegExpSource(regex.Pattern) : ReferenceEquals(receiver, prototype) ? "(?:)" : throw new InvalidOperationException("RegExp source requires a regular expression")), 1);
            foreach (var item in new[] { ("global",'g'), ("ignoreCase",'i'), ("multiline",'m'), ("sticky",'y'), ("hasIndices",'d'), ("dotAll",'s'), ("unicode",'u'), ("unicodeSets",'v') })
                prototype.DefineClassAccessor(item.Item1, new CollectionMethod("get " + item.Item1, 0, (receiver, _) => receiver is JsRegExp regex ? BoxBoolean(regex.Flags.Contains(item.Item2)) : ReferenceEquals(receiver, prototype) ? Undefined : throw new InvalidOperationException("RegExp flag requires a regular expression")), 1);
            prototype.DefineClassAccessor("flags", new CollectionMethod("get flags", 0, (receiver, _) =>
            {
                var source = ReflectTarget(receiver); string result = "";
                foreach (var item in new[] { ("hasIndices",'d'), ("global",'g'), ("ignoreCase",'i'), ("multiline",'m'), ("dotAll",'s'), ("unicode",'u'), ("unicodeSets",'v'), ("sticky",'y') }) if (Truthy(source.Get(item.Item1))) result += item.Item2;
                return result;
            }), 1);
        }
        public override object Construct(object[] arguments) => CreateRegExp(FunctionArgument(arguments, 0), FunctionArgument(arguments, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments) { var result = (TsObject)Construct(arguments); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
}
