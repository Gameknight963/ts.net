using System;

public static partial class Js
{
    sealed class StringRegexFunction(bool search) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, search ? "search" : "match", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            object pattern = FunctionArgument(arguments, 0);
            if (TryStringProtocol(receiver, pattern, search ? SearchSymbol : MatchSymbol, Undefined, false, out var custom)) return custom;
            string text = AtString(receiver);
            var regex = CreateRegExp(IsUndefined(pattern) ? Undefined : StringValue(pattern), Undefined, false);
            return CallMethod(regex.Get(PropertyKey(search ? SearchSymbol : MatchSymbol)), regex, new object[] { text });
        }
    }
    static object MatchRegExp(string text, JsRegExp regex, bool search)
    {
        string flags = search ? null : StringValue(regex.Get("flags"));
        if (!HasBuiltinRegExpExec(regex) || search && !regex.LastIndexWritable || !search && flags != regex.Flags) return MatchCustomRegExp(text, regex, search, flags);
        bool sticky = regex.Flags.Contains('y');
        string matchingText = RegExpInput(regex, text);
        if (search)
        {
            // Searching starts at zero without changing the caller's lastIndex.
            var match = regex.Engine.Match(matchingText);
            return match.Success && (!sticky || match.Index == 0) ? (double)match.Index : -1d;
        }
        if (!regex.Flags.Contains('g')) return RegExpMatch(regex, text, false);
        regex.LastIndex = 0d;
        var result = new JsArray(0);
        int position = 0;
        while (position <= text.Length)
        {
            var match = regex.Engine.Match(matchingText, position);
            if (!match.Success || sticky && match.Index != position) break;
            result.Append(RegExpCaptureText(text, match));
            position = match.Length != 0 ? match.Index + match.Length : RegExpAdvanceIndex(text, match.Index, regex.Flags.Contains('u'));
        }
        return result.Length() == 0 ? null : result;
    }
    static bool HasBuiltinRegExpExec(JsRegExp regex)
    {
        if (regex.HasOwn("exec")) return false;
        var prototype = regex.PropertyPrototype;
        return ReferenceEquals(prototype, RegExpPrototype) && !prototype.TryOwnAccessor("exec", out _) &&
            prototype.HasOwn("exec") && ReferenceEquals(prototype.Get("exec"), RegExpMethods.Exec);
    }
    internal static TsObject ExecuteRegExp(TsObject source, string text)
    {
        object exec = source.Get("exec");
        object result = exec is JsFunction method ? method.InvokeWithReceiver(source, new object[] { text })
            : source is JsRegExp regex ? RegExpMatch(regex, text, false)
            : throw new InvalidOperationException("RegExp.exec requires a regular expression");
        return result == null ? null : result as TsObject ?? throw new InvalidOperationException("RegExp.exec must return an object or null");
    }
    static object MatchCustomRegExp(string text, TsObject regex, bool search, string flags = null)
    {
        if (search)
        {
            object previous = regex.Get("lastIndex");
            if (!SameValue(previous, 0d)) regex.Set("lastIndex", 0d);
            var match = ExecuteRegExp(regex, text);
            if (!SameValue(regex.Get("lastIndex"), previous)) regex.Set("lastIndex", previous);
            return match == null ? -1d : match.Get("index");
        }
        flags ??= StringValue(regex.Get("flags"));
        if (!flags.Contains('g')) return ExecuteRegExp(regex, text);
        bool unicode = flags.Contains('u') || flags.Contains('v');
        regex.Set("lastIndex", 0d);
        var result = new JsArray(0);
        while (true)
        {
            var match = ExecuteRegExp(regex, text);
            if (match == null) return result.Length() == 0 ? null : result;
            string value = StringValue(match.Get("0"));
            result.Append(value);
            if (value.Length == 0)
            {
                double index = Number(regex.Get("lastIndex"));
                index = double.IsNaN(index) || index <= 0 ? 0 : Math.Min(9007199254740991d, Math.Floor(index));
                int step = unicode && index < text.Length - 1 && char.IsHighSurrogate(text[(int)index]) && char.IsLowSurrogate(text[(int)index + 1]) ? 2 : 1;
                regex.Set("lastIndex", index + step);
            }
        }
    }

}
