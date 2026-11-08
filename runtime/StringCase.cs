using System;
using System.Globalization;

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
    static string CaseString(string text, bool upper, CultureInfo culture)
    {
        if (upper) return text.ToUpper(culture).Replace("\u00df", "SS", StringComparison.Ordinal).Replace('\u0131', 'I');
        if (culture.TwoLetterISOLanguageName is not ("tr" or "az"))
            text = text.Replace("\u0130", "i\u0307", StringComparison.Ordinal);
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
