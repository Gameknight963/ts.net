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
            var regex = pattern as JsRegExp ?? CreateRegExp(pattern, Undefined);
            return MatchRegExp(text, regex, search);
        }
    }
    static object MatchRegExp(string text, JsRegExp regex, bool search)
    {
        bool sticky = regex.Flags.Contains('y');
        if (search)
        {
            // Searching starts at zero without changing the caller's lastIndex.
            var match = regex.Engine.Match(text);
            return match.Success && (!sticky || match.Index == 0) ? (double)match.Index : -1d;
        }
        if (!regex.Flags.Contains('g')) return RegExpMatch(regex, text, false);
        regex.LastIndex = 0d;
        var result = new JsArray(0);
        int position = 0;
        while (position <= text.Length)
        {
            var match = regex.Engine.Match(text, position);
            if (!match.Success || sticky && match.Index != position) break;
            result.Append(match.Value);
            position = match.Index + Math.Max(1, match.Length);
        }
        return result.Length() == 0 ? null : result;
    }

}
