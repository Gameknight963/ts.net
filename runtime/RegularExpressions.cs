using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

internal sealed class JsRegExp(string pattern, string flags, Regex engine) : TsObject
{
    internal readonly string Pattern = pattern, Flags = flags;
    internal readonly Regex Engine = engine;
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
    static Regex RegExpEngine(string pattern, RegexOptions options)
    {
        bool inClass = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char ch = pattern[i];
            if (ch == '\\')
            {
                if (++i < pattern.Length && pattern[i] is 'A' or 'G' or 'Z' or 'z' or 'k' or 'p' or 'P') throw new NotSupportedException("This regular-expression escape is not supported");
                continue;
            }
            if (ch == '[') inClass = true; else if (ch == ']') inClass = false;
            if (!inClass && ch == '(' && i + 2 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] is not (':' or '=' or '!'))
                throw new NotSupportedException("Named, lookbehind and engine-specific regex groups are not supported");
        }
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
    static JsRegExp CreateRegExp(object pattern, object flags)
    {
        string text = pattern is JsRegExp original ? original.Pattern : IsUndefined(pattern) ? "" : StringValue(pattern);
        string requested = IsUndefined(flags) ? pattern is JsRegExp regex ? regex.Flags : "" : StringValue(flags);
        var seen = new HashSet<char>();
        foreach (char flag in requested)
            if ("dgimsuvy".IndexOf(flag) < 0 || !seen.Add(flag)) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid regular-expression flags"));
        if (seen.Contains('u') && seen.Contains('v')) throw MakeThrown(new JsCaughtError("SyntaxError", "Invalid regular-expression flags"));
        foreach (char flag in "dsuv") if (seen.Contains(flag)) throw new NotSupportedException("Regular-expression flags d, s, u and v are not supported");
        var options = RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.Compiled;
        if (seen.Contains('i')) options |= RegexOptions.IgnoreCase;
        if (seen.Contains('m')) options |= RegexOptions.Multiline;
        string canonical = ""; foreach (char flag in "gimy") if (seen.Contains(flag)) canonical += flag;
        return new JsRegExp(text, canonical, RegExpEngine(text, options));
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
        result.DefineOwn("index", (double)match.Index); result.DefineOwn("input", text); result.DefineOwn("groups", Undefined);
        return result;
    }
    sealed class RegExpConstructor : NativeNamespaceFunction
    {
        internal RegExpConstructor() : base("RegExp", static args =>
            FunctionArgument(args, 0) is JsRegExp regex && IsUndefined(FunctionArgument(args, 1)) && ReferenceEquals(regex.Get("constructor"), RuntimeNamespaces.RegExp)
                ? regex : CreateRegExp(FunctionArgument(args, 0), FunctionArgument(args, 1)), 2)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype };
            UseObjectPrototype(prototype); prototype.DefineMethod("constructor", this);
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
