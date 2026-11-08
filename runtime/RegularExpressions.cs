using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

internal sealed class JsRegExp(string pattern, string flags, Regex engine, string[] namedGroups) : JsDescriptorObject
{
    internal string Pattern = pattern, Flags = flags;
    internal bool FoldInput = Js.RegExpNeedsFoldInput(pattern, flags);
    internal Regex Engine = engine;
    internal string[] NamedGroups = namedGroups;
    internal int[] NamedCaptureIndices = Js.RegExpCaptureIndices(engine, namedGroups);
    object lastIndex = 0d;
    bool lastIndexWritable = true;
    internal bool LastIndexWritable => lastIndexWritable;
    internal object LastIndex
    {
        get => lastIndex;
        set { if (!lastIndexWritable) throw new InvalidOperationException("RegExp lastIndex is read-only"); lastIndex = value; }
    }
    internal override bool IsReadOnlyOwnProperty(string key) => key == "lastIndex" ? !lastIndexWritable : base.IsReadOnlyOwnProperty(key);
    internal override object OwnPropertyDescriptor(string key) => key == "lastIndex" ? Js.DataDescriptor(lastIndex, lastIndexWritable, false, false) : base.OwnPropertyDescriptor(key);
    internal override void Define(Descriptor descriptor)
    {
        if (descriptor.Key == "lastIndex") { lastIndex = descriptor.Value; lastIndexWritable = descriptor.Writable; }
        else base.Define(descriptor);
    }
    public override object SetOwnValue(string key, object value) => key == "lastIndex" ? Set(key, value) : base.SetOwnValue(key, value);
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.RegExpPrototype;
    public override bool HasOwn(string key) => key == "lastIndex" || base.HasOwn(key);
    public override object Get(string key) => key == "lastIndex" ? LastIndex : base.Get(key);
    public override object Set(string key, object value) { if (key == "lastIndex") { LastIndex = value; return value; } return base.Set(key, value); }
    public override bool DeleteOwn(string key) => key != "lastIndex" && base.DeleteOwn(key);
    internal override bool OwnPropertyConfigurable(string key) => key != "lastIndex" && base.OwnPropertyConfigurable(key);
    internal override object OwnPropertyNames() => Js.MergeOwnNames(this, base.ForInOwnKeys(), "lastIndex");
    internal override object ForInOwnKeys() => Js.ForInNames(base.ForInOwnKeys(), "lastIndex");
    internal override bool ForInEnumerable(string key) => key != "lastIndex" && base.ForInEnumerable(key);
}

public static partial class Js
{
    const string RegExpWhitespace = "\\u0009-\\u000d\\u0020\\u00a0\\u1680\\u2000-\\u200a\\u2028-\\u2029\\u202f\\u205f\\u3000\\ufeff";
    const string RegExpNonWhitespace = "\\u0000-\\u0008\\u000e-\\u001f\\u0021-\\u009f\\u00a1-\\u167f\\u1681-\\u1fff\\u200b-\\u2027\\u202a-\\u202e\\u2030-\\u205e\\u2060-\\u2fff\\u3001-\\ufefe\\uff00-\\uffff";
    internal static TsObject RegExpPrototype => (TsObject)RuntimeNamespaces.RegExp.Get("prototype");
    static readonly Dictionary<(string, RegexOptions), Regex> regexEngines = new();
    static Regex RegExpEngine(string pattern, RegexOptions options, bool dotAll, bool unicode, out string[] namedGroups)
    {
        bool inClass = false;
        bool previousClassSet = false;
        bool currentDotAll = dotAll, currentMultiline = (options & RegexOptions.Multiline) != 0;
        var modes = new Stack<(bool DotAll, bool Multiline)>();
        var names = new List<string>();
        string prefix = "tsnetCapture";
        while (pattern.Contains(prefix, StringComparison.Ordinal)) prefix += "X";
        var unicodeCaptures = unicode ? UnicodeRegExpCaptureNames(pattern, prefix) : null;
        int captures = 0;
        bool namedBackReferences = pattern.Contains("\\k", StringComparison.Ordinal) && RegExpHasNamedCapture(pattern);
        var rewritten = new StringBuilder(pattern.Length);
        for (int i = 0; i < pattern.Length; i++)
        {
            char ch = pattern[i];
            if (ch == '\\')
            {
                if (unicode && !inClass && i + 1 < pattern.Length && pattern[i + 1] is >= '1' and <= '9') {
                    int begin = ++i; while (i + 1 < pattern.Length && char.IsAsciiDigit(pattern[i + 1])) i++;
                    int reference = int.Parse(pattern.AsSpan(begin, i - begin + 1), System.Globalization.CultureInfo.InvariantCulture);
                    string capture = unicodeCaptures[reference - 1];
                    // A nonparticipating group, including a forward reference, matches the empty string.
                    rewritten.Append("(?(").Append(capture).Append(")\\k<").Append(capture).Append(">|)"); continue;
                }

                previousClassSet = inClass && i + 1 < pattern.Length && pattern[i + 1] is 'd' or 'D' or 'w' or 'W' or 's' or 'S';
                if (i + 1 < pattern.Length && pattern[i + 1] is 's' or 'S')
                {
                    if (!inClass) rewritten.Append('[');
                    rewritten.Append(pattern[++i] == 's' ? RegExpWhitespace : RegExpNonWhitespace);
                    if (!inClass) rewritten.Append(']');
                    continue;
                }
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
                    int end = pattern.IndexOf('>', i + 3);
                    if (end < 0) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid named back reference"));
                    string name = ReadRegExpCaptureName(pattern[(i + 3)..end]);
                    string capture = RegExpEngineCaptureName(name);
                    if (unicode) rewritten.Append("(?(").Append(capture).Append(")\\k<").Append(capture).Append(">|)");
                    else rewritten.Append("\\k<").Append(capture).Append('>');
                    i = end; continue;
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
            if (inClass && ch == '-' && (previousClassSet || i + 2 < pattern.Length && pattern[i + 1] == '\\' && pattern[i + 2] is 'd' or 'D' or 'w' or 'W' or 's' or 'S'))
            { rewritten.Append("\\-"); previousClassSet = false; continue; }
            previousClassSet = false;
            if (inClass && ch == '[') { rewritten.Append("\\["); continue; }
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
            if (!inClass && ch == '.') { rewritten.Append(unicode ? currentDotAll ? UnicodeRegExpDotAll : UnicodeRegExpDot : currentDotAll ? "[\\s\\S]" : "[^\\n\\r\\u2028\\u2029]"); continue; }
            if (!inClass && ch == '^') { rewritten.Append(currentMultiline ? "(?<![^\\n\\r\\u2028\\u2029])" : "\\A"); continue; }
            if (!inClass && ch == '$') { rewritten.Append(currentMultiline ? "(?![^\\n\\r\\u2028\\u2029])" : "\\z"); continue; }
            bool lookbehind = i + 3 < pattern.Length && pattern[i + 2] == '<' && pattern[i + 3] is '=' or '!';
            bool named = !inClass && ch == '(' && i + 3 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] == '<' && !lookbehind;
            if (named)
            {
                int end = pattern.IndexOf('>', i + 3);
                if (end < 0) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid named capture"));
                string name = ReadRegExpCaptureName(pattern[(i + 3)..end]);
                if (names.Contains(name)) throw new NotSupportedException("Duplicate named captures are not supported");
                names.Add(name); captures++;
                rewritten.Append("(?<").Append(RegExpEngineCaptureName(name)).Append('>');
                i = end; continue;
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
            if (!unicode && regexEngines.TryGetValue((pattern, options), out var cached)) return cached;
            Regex result;
            try { result = new Regex(pattern, options); }
            catch (ArgumentException error) { throw MakeThrown(new JsCaughtError("SyntaxError", error.Message)); }
            if (!unicode) {
                if (regexEngines.Count >= 64) regexEngines.Clear();
                regexEngines.Add((pattern, options), result);
            }
            return result;
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
    static JsRegExp CreateRegExp(object pattern, object flags, bool? recognized = null)
    {
        var input = RegExpInputs(pattern, flags, recognized);
        return CompileRegExp(input.Source, input.Flags);
    }
    static (object Source, object Flags) RegExpInputs(object pattern, object flags, bool? recognized = null)
    {
        bool regexLike = recognized ?? IsRegExpValue(pattern);
        object source = pattern;
        if (pattern is JsRegExp original)
        {
            source = original.Pattern;
            if (IsUndefined(flags)) flags = original.Flags;
        }
        else if (regexLike)
        {
            source = Get(pattern, "source");
            if (IsUndefined(flags)) flags = Get(pattern, "flags");
        }
        return (source, flags);
    }
    static JsRegExp CompileRegExp(object source, object flags)
    {
        string text = IsUndefined(source) ? "" : StringValue(source);
        string requested = IsUndefined(flags) ? "" : StringValue(flags);
        var seen = new HashSet<char>();
        foreach (char flag in requested)
            if ("dgimsuvy".IndexOf(flag) < 0 || !seen.Add(flag)) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid regular-expression flags"));
        if (seen.Contains('u') && seen.Contains('v')) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid regular-expression flags"));
        if (seen.Contains('v')) throw new NotSupportedException("Regular-expression flag v is not supported");
        bool unicode = seen.Contains('u');
        var options = RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.Compiled;
        if (seen.Contains('i')) options |= RegexOptions.IgnoreCase;
        if (seen.Contains('m')) options |= RegexOptions.Multiline;
        string canonical = ""; foreach (char flag in "dgimsuy") if (seen.Contains(flag)) canonical += flag;
        if (unicode) {
            var compiled = UnicodeRegExpEngines.Compile(text, options, seen.Contains('s'));
            return new JsRegExp(text, canonical, compiled.Engine, compiled.Names);
        }
        var engine = RegExpEngine(text, options, seen.Contains('s'), false, out var names);
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
        string matchingText = RegExpInput(regex, text);
        bool global = regex.Flags.Contains('g'), sticky = regex.Flags.Contains('y');
        double position = Number(regex.LastIndex);
        position = double.IsNaN(position) || position <= 0 ? 0 : Math.Truncate(position);
        if (!global && !sticky) position = 0;
        if (position > text.Length) { regex.LastIndex = 0d; return test ? BoxBoolean(false) : null; }
        position = RegExpStartIndex(text, (int)position, regex.Flags.Contains('u'));
        if (test)
        {
            if (!global && !sticky) return BoxBoolean(regex.Engine.IsMatch(matchingText));
            var matches = regex.Engine.EnumerateMatches(matchingText.AsSpan(), (int)position);
            if (!matches.MoveNext() || sticky && matches.Current.Index != position) { regex.LastIndex = 0d; return BoxBoolean(false); }
            regex.LastIndex = (double)(matches.Current.Index + matches.Current.Length); return BoxBoolean(true);
        }
        Match match = regex.Engine.Match(matchingText, (int)position);
        if (!match.Success || sticky && match.Index != position) { if (global || sticky) regex.LastIndex = 0d; return test ? BoxBoolean(false) : null; }
        if (global || sticky) regex.LastIndex = (double)(match.Index + match.Length);
        if (test) return BoxBoolean(true);
        var result = new JsArray(0);
        foreach (Group group in match.Groups) result.Append(group.Success ? RegExpCaptureText(text, group) : Undefined);
        result.DefineOwn("index", (double)match.Index); result.DefineOwn("input", text); result.DefineOwn("groups", RegExpNamedGroups(regex, match, text: text));
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
    static object RegExpNamedGroups(JsRegExp regex, Match match, JsArray indices = null, string text = null)
    {
        if (regex.NamedGroups.Length == 0) return Undefined;
        var result = new JsDynamicObject(); result.SetPrototype(null);
        for (int index = 0; index < regex.NamedGroups.Length; index++)
        {
            string name = regex.NamedGroups[index];
            int number = regex.NamedCaptureIndices[index];
            var group = match.Groups[number];
            result.DefineOwn(name, !group.Success ? Undefined : indices != null
                ? indices.Get(number.ToString(System.Globalization.CultureInfo.InvariantCulture))
                : text == null ? group.Value : RegExpCaptureText(text, group));
        }
        return result;
    }
    sealed class RegExpConstructor : NativeNamespaceFunction
    {
        internal RegExpConstructor() : base("RegExp", static args =>
        {
            object pattern = FunctionArgument(args, 0), flags = FunctionArgument(args, 1);
            bool regexLike = IsRegExpValue(pattern);
            return regexLike && IsUndefined(flags) && ReferenceEquals(Js.Get(pattern, "constructor"), RuntimeNamespaces.RegExp)
                ? pattern : CreateRegExp(pattern, flags, regexLike);
        }, 2)
        {
            string speciesKey = PropertyKey(SpeciesSymbol);
            Member(speciesKey, Undefined); DefineAccessor(speciesKey, new ConstructorSpeciesGetter(), 1);
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
                var replacement = CreateRegExp(pattern, flags, false);
                target.Pattern = replacement.Pattern;
                target.Flags = replacement.Flags;
                target.FoldInput = replacement.FoldInput;
                target.Engine = replacement.Engine;
                target.NamedGroups = replacement.NamedGroups;
                target.NamedCaptureIndices = replacement.NamedCaptureIndices;
                target.LastIndex = 0d;
                return target;
            }));
            prototype.DefineMethod("exec", RegExpMethods.Exec);
            prototype.DefineMethod("test", new CollectionMethod("test", 1, RegExpTest));
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
        internal override object InitializeDerived(object instance, object[] arguments, JsFunction newTarget)
        {
            var input = RegExpInputs(FunctionArgument(arguments, 0), FunctionArgument(arguments, 1));
            var prototype = ConstructionPrototype(newTarget);
            var result = CompileRegExp(input.Source, input.Flags);
            result.SetPrototype(prototype);
            return result;
        }
    }
}
