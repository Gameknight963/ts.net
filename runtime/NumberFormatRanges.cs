using System;
using System.Text;
using System.Numerics;

public static partial class Js
{
    sealed partial class NumberFormatValue
    {
        internal object Range(object start, object end, bool parts)
        {
            if (IsUndefined(start) || IsUndefined(end)) throw new InvalidOperationException("Range endpoints are required");
            object Convert(object value) {
                value = IntlNumericValue(value);
                return value is LocaleDecimal or BigInteger or double ? value : Number(value);
            }
            start = Convert(start); end = Convert(end);
            if (start is double a && double.IsNaN(a) || end is double b && double.IsNaN(b)) throw new ArgumentOutOfRangeException(nameof(start), "Range endpoints cannot be NaN");
            if (settings.culture.Name is not ("en" or "en-US")) throw new NotSupportedException("Number ranges currently support US English");
            var left = (JsArray)Parts(start); var right = (JsArray)Parts(end);
            string Type(JsArray list, int index) => (string)((TsObject)list.ElementAt((uint)index)).Get("type");
            string Value(JsArray list, int index) => (string)((TsObject)list.ElementAt((uint)index)).Get("value");
            int leftEnd = (int)left.Length(), rightEnd = (int)right.Length(), leftStart = 0, rightStart = 0;
            var result = parts ? new JsArray(0) : null;
            var text = parts ? null : new StringBuilder();
            void Add(string type, string value, string source) {
                if (!parts) { text.Append(value); return; }
                if (type == "literal" && result.Length() != 0) {
                    var previous = (TsObject)result.ElementAt((uint)result.Length() - 1);
                    if ((string)previous.Get("type") == "literal") {previous.Set("value", (string)previous.Get("value") + value); previous.Set("source", "shared"); return;}
                }
                var part = new JsDynamicObject(); part.DefineOwn("type", type); part.DefineOwn("value", value); part.DefineOwn("source", source); result.Append(part);
            }
            void Copy(JsArray list, int from, int to, string source) {for (int i = from; i < to; i++) Add(Type(list, i), Value(list, i), source);}
            bool equal = leftEnd == rightEnd;
            for (int i = 0; equal && i < leftEnd; i++) equal = Type(left, i) == Type(right, i) && Value(left, i) == Value(right, i);
            if (equal) {
                if (!(Type(left, 0) == "literal" && Value(left, 0) == "(")) Add("approximatelySign", "~", "shared");
                Copy(left, 0, leftEnd, "shared");
                return parts ? result : text.ToString();
            }
            bool accounting = leftEnd > 2 && rightEnd > 2 && Type(left, 0) == "literal" && Value(left, 0) == "(" && Type(right, 0) == "literal" && Value(right, 0) == "(";
            int accountingSuffix = accounting ? leftEnd - 1 : leftEnd;
            bool negativeAffix = !settings.CurrencyName && (settings.style == "currency" || settings.style == "percent" && settings.notation != "compact") && Type(left, 0) == "minusSign" && Type(right, 0) == "minusSign";
            bool positiveAffix = !settings.CurrencyName && settings.style == "currency" && Type(left, 0) == "plusSign" && Type(right, 0) == "plusSign";
            bool codeAffix = settings.style == "currency" && settings.currencyDisplay == "code" && Type(left, 0) == "currency" && Type(right, 0) == "currency";
            if (accounting || negativeAffix || positiveAffix || codeAffix) {
                while (leftStart < leftEnd && rightStart < rightEnd && Type(left, leftStart) is ("literal" or "currency" or "minusSign" or "plusSign") && Type(left, leftStart) == Type(right, rightStart) && Value(left, leftStart) == Value(right, rightStart)) {
                    if (!accounting && Type(left, leftStart) == "literal") {rightStart++; break;}
                    Add(Type(left, leftStart), Value(left, leftStart), "shared"); leftStart++; rightStart++;
                }
                if (accounting) {leftEnd--; rightEnd--;}
            }
            bool sharedPercent = settings.style == "percent" && (negativeAffix || settings.notation == "compact");
            if (sharedPercent) {leftEnd--; rightEnd--;}
            bool sharedUnit = settings.style == "unit" || settings.CurrencyName;
            (string Gap, string Name) unit = default;
            if (sharedUnit) {
                unit = settings.CurrencyName ? (" ", settings.CurrencyNameAffix("2")) : settings.UnitAffix("2");
                leftEnd--; rightEnd--;
                if (leftEnd > leftStart && Type(left, leftEnd - 1) == "literal") leftEnd--;
                if (rightEnd > rightStart && Type(right, rightEnd - 1) == "literal") rightEnd--;
            }
            int sharedCompact = -1;
            if (settings.notation == "compact" && settings.LongCompact && leftEnd > leftStart && rightEnd > rightStart && Type(left, leftEnd - 1) == "compact" && Type(right, rightEnd - 1) == "compact" && Value(left, leftEnd - 1) == Value(right, rightEnd - 1)) {
                sharedCompact = leftEnd - 1; leftEnd--; rightEnd--;
                if (leftEnd > leftStart && Type(left, leftEnd - 1) == "literal") leftEnd--;
                if (rightEnd > rightStart && Type(right, rightEnd - 1) == "literal") rightEnd--;
            }
            bool Spaced(JsArray list, int from, int to) {
                for (int i = from; i < to; i++) if (Type(list, i) is not ("integer" or "group" or "decimal" or "fraction" or "infinity") && !(Type(list, i) == "literal" && Value(list, i) == "\u00a0")) return true;
                return false;
            }
            Copy(left, leftStart, leftEnd, "startRange");
            Add("literal", Spaced(left, leftStart, leftEnd) ? " \u2013 " : "\u2013", "shared");
            Copy(right, rightStart, rightEnd, "endRange");
            if (sharedCompact >= 0) {Add("literal", " ", "shared"); Add("compact", Value(left, sharedCompact), "shared");}
            if (sharedUnit) {if (unit.Gap.Length != 0) Add("literal", unit.Gap, "shared"); Add(settings.CurrencyName ? "currency" : "unit", unit.Name, "shared");}
            if (sharedPercent) Add(settings.notation == "compact" ? "unit" : "percentSign", settings.culture.NumberFormat.PercentSymbol, "shared");
            if (accounting) Copy(left, accountingSuffix, accountingSuffix + 1, "shared");
            return parts ? result : text.ToString();
        }
    }
}
