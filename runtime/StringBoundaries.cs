using System;
using System.Text;

public static partial class Js
{
    sealed class StringBoundaryFunction(string name, bool backwards) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, name, 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            object needle = FunctionArgument(arguments, 0);
            if (IsRegExpValue(needle)) throw new InvalidOperationException("String boundary methods do not accept regular expressions");
            string search = StringValue(needle);
            object position = FunctionArgument(arguments, 1);
            int bound = backwards && IsUndefined(position) ? text.Length : StringRangeBound(Number(position), text.Length, true);
            int start = backwards ? bound - search.Length : bound;
            return BoxBoolean(start >= 0 && search.Length <= text.Length - start && text.AsSpan(start, search.Length).SequenceEqual(search));
        }
    }
    sealed class StringConcatFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, "concat", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            if (arguments.Length == 0) return text;
            if (arguments.Length == 1) return string.Concat(text, StringValue(arguments[0]));
            var result = new StringBuilder(text);
            foreach (object argument in arguments) result.Append(StringValue(argument));
            return result.ToString();
        }
    }
    static void DefineStringBoundaries(JsBoxedPrimitive prototype)
    {
        prototype.DefineHidden("at", RelativeAtFunctions.String);
        prototype.DefineHidden("slice", StringRangeFunctions.Slice);
        prototype.DefineHidden("substring", StringRangeFunctions.Substring);
        prototype.DefineHidden("charAt", CharacterFunctions.CharAt);
        prototype.DefineHidden("charCodeAt", CharacterFunctions.CharCodeAt);
        prototype.DefineHidden("codePointAt", CharacterFunctions.CodePointAt);
        prototype.DefineHidden("indexOf", StringSearchFunctions.IndexOf);
        prototype.DefineHidden("includes", StringSearchFunctions.Includes);
        prototype.DefineHidden("lastIndexOf", StringSearchFunctions.LastIndexOf);
        prototype.DefineHidden("isWellFormed", new StringWellFormedFunction(false));
        prototype.DefineHidden("toWellFormed", new StringWellFormedFunction(true));
        prototype.DefineHidden("normalize", new StringNormalizationFunction());
        prototype.DefineHidden("replace", new StringReplacementFunction(false));
        prototype.DefineHidden("replaceAll", new StringReplacementFunction(true));
        prototype.DefineHidden("localeCompare", new StringLocaleCompareFunction());
        prototype.DefineHidden("toLocaleUpperCase", new StringCaseFunction(true, true));
        prototype.DefineHidden("toLocaleLowerCase", new StringCaseFunction(false, true));
        prototype.DefineHidden("toUpperCase", new StringCaseFunction(true));
        prototype.DefineHidden("toLowerCase", new StringCaseFunction(false));
        prototype.DefineHidden("matchAll", new StringMatchAllFunction());
        prototype.DefineHidden("match", new StringRegexFunction(false));
        prototype.DefineHidden("search", new StringRegexFunction(true));
        prototype.DefineHidden("split", new StringSplitFunction());
        prototype.DefineHidden("startsWith", new StringBoundaryFunction("startsWith", false));
        prototype.DefineHidden("endsWith", new StringBoundaryFunction("endsWith", true));
        prototype.DefineHidden("concat", new StringConcatFunction());
    }
}
