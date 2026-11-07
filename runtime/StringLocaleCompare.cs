using System;
using System.Globalization;

public static partial class Js
{
    sealed class StringLocaleCompareFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, "localeCompare", 1)
    {
        double Execute(object receiver, object[] arguments)
        {
            string text = AtString(receiver), other = StringValue(FunctionArgument(arguments, 0));
            var culture = CaseCulture(FunctionArgument(arguments, 1));
            CompareOptions flags = CompareOptions.None;
            object optionValue = FunctionArgument(arguments, 2);
            if (!IsUndefined(optionValue))
            {
                var options = ToObject(optionValue);
                foreach (string key in new[] { "numeric", "caseFirst", "ignorePunctuation", "usage", "collation", "localeMatcher" })
                    if (!IsUndefined(options.Get(key))) throw new NotSupportedException("localeCompare options other than sensitivity are not supported");
                object sensitivity = options.Get("sensitivity");
                if (!IsUndefined(sensitivity)) flags = StringValue(sensitivity) switch
                {
                    "base" => CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace,
                    "accent" => CompareOptions.IgnoreCase,
                    "case" => CompareOptions.IgnoreNonSpace,
                    "variant" => CompareOptions.None,
                    _ => throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid comparison sensitivity")
                };
            }
            return Math.Sign(culture.CompareInfo.Compare(text, other, flags));
        }
        public override object InvokeWithReceiver(object receiver, object[] arguments) => Execute(receiver, arguments);
        public override double? InvokeMethodNumber0(object receiver) => Execute(receiver, Array.Empty<object>());
        public override double? InvokeMethodNumber1(object receiver, double? first) => Execute(receiver, new[] { BoxNumber(first) });
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Execute(receiver, new[] { BoxNumber(first), BoxNumber(second) });
    }
}
