using System;
using System.Globalization;
using System.Text;

public static partial class Js
{
    static string WithoutComparisonPunctuation(string text)
    {
        StringBuilder result = null;
        int offset = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsPunctuation(rune) || Rune.IsWhiteSpace(rune))
            {
                result ??= new StringBuilder(text.Length).Append(text, 0, offset);
            }
            else if (result != null) result.Append(text, offset, rune.Utf16SequenceLength);
            offset += rune.Utf16SequenceLength;
        }
        return result?.ToString() ?? text;
    }
    sealed class StringLocaleCompareFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, "localeCompare", 1)
    {
        double Execute(object receiver, object[] arguments)
        {
            string text = AtString(receiver), other = StringValue(FunctionArgument(arguments, 0));
            var culture = CaseCulture(FunctionArgument(arguments, 1));
            object optionValue = FunctionArgument(arguments, 2);
            if (culture.Name.Contains("-u-", StringComparison.OrdinalIgnoreCase) || culture.CompareInfo.Name.Contains('_'))
                return SelectCollationSettings(FunctionArgument(arguments, 1), optionValue).Compare(text, other);
            return IsUndefined(optionValue) ? Math.Sign(culture.CompareInfo.Compare(text, other))
                : new CollationSettings(culture, optionValue).Compare(text, other);
        }
        public override object InvokeWithReceiver(object receiver, object[] arguments) => Execute(receiver, arguments);
        public override double? InvokeMethodNumber0(object receiver) => Execute(receiver, Array.Empty<object>());
        public override double? InvokeMethodNumber1(object receiver, double? first) => Execute(receiver, new[] { BoxNumber(first) });
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Execute(receiver, new[] { BoxNumber(first), BoxNumber(second) });
    }
}
