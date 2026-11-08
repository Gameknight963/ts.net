using System;

public static partial class Js
{
    static object SplitRegExpProtocol(string text, TsObject regex, object limitValue)
    {
        JsFunction species = RegExpSpeciesConstructor(regex);
        string flags = StringValue(regex.Get("flags"));
        bool unicode = flags.Contains('u') || flags.Contains('v');
        string stickyFlags = flags.Contains('y') ? flags : flags + "y";
        TsObject splitter;
        if ((species == null || ReferenceEquals(species, RuntimeNamespaces.RegExp)) && regex is JsRegExp original && flags == original.Flags)
        {
            bool recognized = IsRegExpValue(regex);
            splitter = flags == original.Flags ? new JsRegExp(original.Pattern, stickyFlags, original.Engine, original.NamedGroups) : CreateRegExp(regex, stickyFlags, recognized);
        }
        else splitter = species == null ? CreateRegExp(regex, stickyFlags)
            : species.Construct(new object[] { regex, stickyFlags }) as TsObject ?? throw new InvalidOperationException("RegExp species returned a primitive");
        uint limit = IsUndefined(limitValue) ? uint.MaxValue : unchecked((uint)ToInt32(Number(limitValue)));
        if (splitter is JsRegExp native && native.Flags.Contains('y') && HasBuiltinRegExpExec(native)) return SplitRegExp(text, native, limit);
        var result = new JsArray(0);
        if (limit == 0) return result;
        if (text.Length == 0)
        {
            if (ExecuteRegExp(splitter, text) == null) result.Append(text);
            return result;
        }
        int copied = 0, search = 0;
        while (search < text.Length)
        {
            splitter.Set("lastIndex", (double)search);
            TsObject match = ExecuteRegExp(splitter, text);
            if (match != null)
            {
                double position = Number(splitter.Get("lastIndex"));
                int end = double.IsNaN(position) || position <= 0 ? 0 : (int)Math.Min(Math.Floor(position), text.Length);
                if (end != copied)
                {
                    result.Append(text.Substring(copied, search - copied));
                    if (result.Length() == limit) return result;
                    copied = end;
                    long length = IndexedLength(match);
                    for (long index = 1; index < length; index++)
                    {
                        result.Append(IndexedValue(match, index));
                        if (result.Length() == limit) return result;
                    }
                    search = copied;
                    continue;
                }
            }
            search += unicode && search < text.Length - 1 && char.IsHighSurrogate(text[search]) && char.IsLowSurrogate(text[search + 1]) ? 2 : 1;
        }
        result.Append(text.Substring(copied));
        return result;
    }
    static object SplitRegExp(string text, JsRegExp regex, uint limit)
    {
        var result = new JsArray(0);
        if (limit == 0) return result;
        if (text.Length == 0)
        {
            if (!regex.Engine.IsMatch(text)) result.Append(text);
            return result;
        }
        int copied = 0, search = 0;
        while (search < text.Length)
        {
            var match = regex.Engine.Match(text, search);
            if (!match.Success || match.Index == text.Length) break;
            int end = match.Index + match.Length;
            if (end == copied) { search = RegExpAdvanceIndex(text, match.Index, regex.Flags.Contains('u')); continue; }
            result.Append(text.Substring(copied, match.Index - copied));
            if (result.Length() == limit) return result;
            for (int group = 1; group < match.Groups.Count; group++)
            {
                result.Append(match.Groups[group].Success ? match.Groups[group].Value : Undefined);
                if (result.Length() == limit) return result;
            }
            copied = end; search = end;
        }
        result.Append(text.Substring(copied)); return result;
    }
    sealed class StringSplitFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, "split", 2)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            object separator = FunctionArgument(arguments, 0), limitValue = FunctionArgument(arguments, 1);
            if (TryStringProtocol(receiver, separator, SplitSymbol, limitValue, true, out var custom)) return custom;
            string text = AtString(receiver);
            uint limit = IsUndefined(limitValue) ? uint.MaxValue : unchecked((uint)ToInt32(Number(limitValue)));
            string pattern = StringValue(separator);
            var result = new JsArray(0);
            if (limit == 0) return result;
            if (IsUndefined(separator)) { result.Append(text); return result; }
            if (pattern.Length == 0)
            {
                uint length = Math.Min(limit, (uint)text.Length);
                for (int index = 0; index < length; index++) result.Append(Iteration.CodeUnit(text[index]));
                return result;
            }
            int start = 0;
            while (true)
            {
                int match = text.IndexOf(pattern, start, StringComparison.Ordinal);
                if (match < 0) break;
                result.Append(text.Substring(start, match - start));
                if (result.Length() == limit) return result;
                start = match + pattern.Length;
            }
            result.Append(text.Substring(start)); return result;
        }
    }
}
