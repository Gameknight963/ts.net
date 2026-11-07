using System;
using System.Globalization;

public static partial class Js
{
    sealed class NumberLocaleFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Number method requires a receiver")), null, "toLocaleString", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            object primitive = receiver is JsBoxedPrimitive boxed ? boxed.Value : receiver;
            if (primitive is not double value) throw new InvalidOperationException("Number method has an incompatible receiver");
            var culture = CaseCulture(FunctionArgument(arguments, 0));
            int minimum = 0, maximum = 3; bool grouping = true;
            object optionValue = FunctionArgument(arguments, 1);
            if (!IsUndefined(optionValue))
            {
                var options = ToObject(optionValue);
                foreach (string key in new[] { "style", "currency", "notation", "signDisplay", "minimumIntegerDigits", "minimumSignificantDigits", "maximumSignificantDigits", "roundingMode", "roundingPriority", "roundingIncrement", "trailingZeroDisplay", "compactDisplay", "unit", "numberingSystem", "localeMatcher" })
                    if (!IsUndefined(options.Get(key))) throw new NotSupportedException("Number locale formatting option " + key + " is not supported");
                int Digits(string key, int fallback)
                {
                    object setting = options.Get(key); if (IsUndefined(setting)) return fallback;
                    double count = Number(setting);
                    if (!double.IsFinite(count) || count < 0 || count > 100) throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid fraction digits");
                    return (int)Math.Floor(count);
                }
                minimum = Digits("minimumFractionDigits", 0);
                maximum = Digits("maximumFractionDigits", Math.Max(3, minimum));
                if (minimum > maximum) throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid fraction digits");
                object group = options.Get("useGrouping"); if (!IsUndefined(group)) grouping = Truthy(group);
            }
            string result = value.ToString((grouping ? "N" : "F") + maximum.ToString(CultureInfo.InvariantCulture), culture);
            if (!double.IsFinite(value) || maximum == 0) return result;
            int trim = 0;
            while (trim < maximum - minimum && result[result.Length - trim - 1] == '0') trim++;
            if (trim == maximum) trim += culture.NumberFormat.NumberDecimalSeparator.Length;
            return trim == 0 ? result : result[..^trim];
        }
    }
}
