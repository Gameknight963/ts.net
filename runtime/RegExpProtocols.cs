using System;

public static partial class Js
{
    sealed class RegExpProtocolFunction(string name, int kind) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("RegExp method requires a receiver")), null, "[Symbol." + name + "]", kind is 2 or 4 ? 2 : 1)
    {
        internal object Execute(object receiver, object input, object second)
        {
            if (kind is 0 or 2 or 3 && receiver is not JsRegExp)
            {
                if (receiver is not TsObject source) throw new InvalidOperationException("RegExp method requires an object");
                string converted = StringValue(input);
                if (kind == 2)
                {
                    var function = second as JsFunction;
                    string template = function == null ? StringValue(second) : null;
                    return ReplaceCustomRegExp(converted, source, function, template, StringValue(source.Get("flags")));
                }
                return MatchCustomRegExp(converted, source, kind == 3);
            }
            if (receiver is not JsRegExp regex) throw new InvalidOperationException("RegExp receiver required");
            string text = StringValue(input);
            return kind switch
            {
                0 => MatchRegExp(text, regex, false),
                1 => MatchAllRegExp(text, regex),
                2 => ReplaceRegExp(text, regex, second, false),
                3 => MatchRegExp(text, regex, true),
                _ => SplitRegExp(text, regex, IsUndefined(second) ? uint.MaxValue : unchecked((uint)ToInt32(Number(second))))
            };
        }
        public override object InvokeWithReceiver(object receiver, object[] arguments) => Execute(receiver, FunctionArgument(arguments, 0), FunctionArgument(arguments, 1));
    }
    static void DefineRegExpProtocols(JsClassInstance prototype)
    {
        prototype.DefineMethod(PropertyKey(MatchSymbol), new RegExpProtocolFunction("match", 0));
        prototype.DefineMethod(PropertyKey(MatchAllSymbol), new RegExpProtocolFunction("matchAll", 1));
        prototype.DefineMethod(PropertyKey(ReplaceSymbol), new RegExpProtocolFunction("replace", 2));
        prototype.DefineMethod(PropertyKey(SearchSymbol), new RegExpProtocolFunction("search", 3));
        prototype.DefineMethod(PropertyKey(SplitSymbol), new RegExpProtocolFunction("split", 4));
    }
}
