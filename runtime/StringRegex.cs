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
        if (!HasBuiltinRegExpExec(regex)) return MatchCustomRegExp(text, regex, search);
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
    static bool HasBuiltinRegExpExec(JsRegExp regex)
    {
        if (regex.HasOwn("exec")) return false;
        var prototype = regex.PropertyPrototype;
        return ReferenceEquals(prototype, RegExpPrototype) && !prototype.TryOwnAccessor("exec", out _) &&
            prototype.HasOwn("exec") && ReferenceEquals(prototype.Get("exec"), RegExpMethods.Exec);
    }
    static TsObject ExecuteRegExp(TsObject source, string text)
    {
        object exec = source.Get("exec");
        object result = exec is JsFunction method ? method.InvokeWithReceiver(source, new object[] { text })
            : source is JsRegExp regex ? RegExpMatch(regex, text, false)
            : throw new InvalidOperationException("RegExp.exec requires a regular expression");
        return result == null ? null : result as TsObject ?? throw new InvalidOperationException("RegExp.exec must return an object or null");
    }
    static object MatchCustomRegExp(string text, JsRegExp regex, bool search)
    {
        if (search)
        {
            object previous = regex.LastIndex;
            if (!SameValue(previous, 0d)) regex.LastIndex = 0d;
            var match = ExecuteRegExp(regex, text);
            if (!SameValue(regex.LastIndex, previous)) regex.LastIndex = previous;
            return match == null ? -1d : match.Get("index");
        }
        if (!regex.Flags.Contains('g')) return ExecuteRegExp(regex, text);
        regex.LastIndex = 0d;
        var result = new JsArray(0);
        while (true)
        {
            var match = ExecuteRegExp(regex, text);
            if (match == null) return result.Length() == 0 ? null : result;
            string value = StringValue(match.Get("0"));
            result.Append(value);
            if (value.Length == 0)
            {
                double index = Number(regex.LastIndex);
                regex.LastIndex = (double.IsNaN(index) || index <= 0 ? 0 : Math.Min(9007199254740991d, Math.Floor(index))) + 1;
            }
        }
    }

}
