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
            if (!IsUndefined(FunctionArgument(arguments, 2))) throw new NotSupportedException("localeCompare options are not supported");
            return Math.Sign(culture.CompareInfo.Compare(text, other, CompareOptions.None));
        }
        public override object InvokeWithReceiver(object receiver, object[] arguments) => Execute(receiver, arguments);
        public override double? InvokeMethodNumber0(object receiver) => Execute(receiver, Array.Empty<object>());
        public override double? InvokeMethodNumber1(object receiver, double? first) => Execute(receiver, new[] { BoxNumber(first) });
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Execute(receiver, new[] { BoxNumber(first), BoxNumber(second) });
    }
}
