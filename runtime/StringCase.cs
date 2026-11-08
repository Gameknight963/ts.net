using System;
using System.Globalization;
using System.Text;

public static partial class Js
{
    static CultureInfo LocaleCulture(object value)
    {
        if (value is not string && value is not TsObject) throw new InvalidOperationException("Locale must be a string or object");
        string tag = StringValue(value);
        if (tag.Length == 0 || tag.Contains('_')) throw new ArgumentOutOfRangeException(nameof(value), "Invalid locale");
        try { return CultureInfo.GetCultureInfo(tag); }
        catch (CultureNotFoundException) { throw new ArgumentOutOfRangeException(nameof(value), "Invalid or unsupported locale"); }
    }
    static CultureInfo CaseCulture(object locales)
    {
        if (IsUndefined(locales)) return CultureInfo.CurrentCulture;
        CultureInfo first = null;
        void Add(object value)
        {
            first ??= LocaleCulture(value);
        }
        if (locales is string or LocaleValue) Add(locales);
        else
        {
            var list = ToObject(locales); long length = IndexedLength(list);
            for (long index = 0; index < length; index++) if (TryIndexedValue(list, index, out object value)) { Add(value); break; }
        }
        return first ?? CultureInfo.CurrentCulture;
    }
    static string ExpandUppercase(string text)
    {
        int first = text.AsSpan().IndexOfAnyInRange('\u0080', '\uffff');
        if (first < 0) return text;
        StringBuilder result = null;
        int copied = 0;
        for (int index = first; index < text.Length; index++)
        {
            string expansion = text[index] switch
            {
                '\u00df' => "SS", '\u0131' => "I", '\u0149' => "\u02bcN", '\u01f0' => "J\u030c",
                '\ufb00' => "FF", '\ufb01' => "FI", '\ufb02' => "FL", '\ufb03' => "FFI",
                '\ufb04' => "FFL", '\ufb05' or '\ufb06' => "ST",
                _ => null
            };
            if (expansion == null) continue;
            result ??= new StringBuilder(text.Length + 8);
            result.Append(text, copied, index - copied).Append(expansion);
            copied = index + 1;
        }
        return result == null ? text : result.Append(text, copied, text.Length - copied).ToString();
    }
    static string CaseString(string text, bool upper, CultureInfo culture)
    {
        if (upper) return ExpandUppercase(text.ToUpper(culture));
        text = ApplyFinalSigma(text);
        if (culture.TwoLetterISOLanguageName is "tr" or "az") text = ApplyTurkicDotContext(text);
        else text = text.Replace("\u0130", "i\u0307", StringComparison.Ordinal);
        return text.ToLower(culture);
    }
    sealed class StringCaseFunction(bool upper, bool localized = false) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, localized ? upper ? "toLocaleUpperCase" : "toLocaleLowerCase" : upper ? "toUpperCase" : "toLowerCase", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            return CaseString(text, upper, localized ? CaseCulture(FunctionArgument(arguments, 0)) : CultureInfo.InvariantCulture);
        }
    }
}
