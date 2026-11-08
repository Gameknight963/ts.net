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
        if (locales is LocaleValue locale) return LocaleCulture(locale.BaseName);
        if (locales is string tag)
        {
            // Ordinary tags keep the cached native-culture lookup. Extension
            // tags need separate parsing, since they do not select casing rules.
            for (int dash = tag.IndexOf('-'); dash >= 0;)
            {
                int next = tag.IndexOf('-', dash + 1);
                if ((next < 0 ? tag.Length : next) - dash - 1 == 1)
                    return LocaleCulture(new LocaleValue(tag, Undefined).BaseName);
                dash = next;
            }
            return LocaleCulture(tag);
        }
        var list = ToObject(locales);
        long length = IndexedLength(list);
        for (long index = 0; index < length; index++)
            if (TryIndexedValue(list, index, out object value))
                return value is string or LocaleValue ? CaseCulture(value) : LocaleCulture(new LocaleValue(value, Undefined).BaseName);
        return CultureInfo.CurrentCulture;
    }
    static string UppercaseWithIota(char first) => string.Create(2, first, static (output, value) => { output[0] = value; output[1] = '\u0399'; });
    static string ExpandUppercase(string text)
    {
        int first = text.AsSpan().IndexOfAnyInRange('\u0080', '\uffff');
        if (first < 0) return text;
        StringBuilder result = null;
        int copied = 0;
        for (int index = first; index < text.Length; index++)
        {
            char mapped = RecentCase(text[index], true);
            string expansion = mapped switch
            {
                '\u00df' => "SS", '\u0131' => "I", '\u0149' => "\u02bcN", '\u01f0' => "J\u030c",
                '\ufb00' => "FF", '\ufb01' => "FI", '\ufb02' => "FL", '\ufb03' => "FFI",
                '\ufb04' => "FFL", '\ufb05' or '\ufb06' => "ST",
                '\u0390' or '\u1fd3' => "\u0399\u0308\u0301", '\u03b0' or '\u1fe3' => "\u03a5\u0308\u0301",
                '\u1e96' => "H\u0331", '\u1e97' => "T\u0308", '\u1e98' => "W\u030a", '\u1e99' => "Y\u030a", '\u1e9a' => "A\u02be",
                '\u1f50' => "\u03a5\u0313", '\u1f52' => "\u03a5\u0313\u0300", '\u1f54' => "\u03a5\u0313\u0301", '\u1f56' => "\u03a5\u0313\u0342",
                >= '\u1f88' and <= '\u1f8f' => UppercaseWithIota((char)(text[index] - 0x80)),
                >= '\u1f98' and <= '\u1f9f' => UppercaseWithIota((char)(text[index] - 0x70)),
                >= '\u1fa8' and <= '\u1faf' => UppercaseWithIota((char)(text[index] - 0x40)),
                '\u1fb2' => "\u1fba\u0399", '\u1fb4' => "\u0386\u0399", '\u1fb6' => "\u0391\u0342", '\u1fb7' => "\u0391\u0342\u0399", '\u1fbc' => "\u0391\u0399",
                '\u1fc2' => "\u1fca\u0399", '\u1fc4' => "\u0389\u0399", '\u1fc6' => "\u0397\u0342", '\u1fc7' => "\u0397\u0342\u0399", '\u1fcc' => "\u0397\u0399",
                '\u1fd2' => "\u0399\u0308\u0300", '\u1fd6' => "\u0399\u0342", '\u1fd7' => "\u0399\u0308\u0342",
                '\u1fe2' => "\u03a5\u0308\u0300", '\u1fe4' => "\u03a1\u0313", '\u1fe6' => "\u03a5\u0342", '\u1fe7' => "\u03a5\u0308\u0342",
                '\u1ff2' => "\u1ffa\u0399", '\u1ff4' => "\u038f\u0399", '\u1ff6' => "\u03a9\u0342", '\u1ff7' => "\u03a9\u0342\u0399", '\u1ffc' => "\u03a9\u0399",
                '\u0587' => "\u0535\u0552", '\ufb13' => "\u0544\u0546", '\ufb14' => "\u0544\u0535", '\ufb15' => "\u0544\u053b", '\ufb16' => "\u054e\u0546", '\ufb17' => "\u0544\u053d",
                _ => null
            };
            if (expansion == null && mapped == text[index]) continue;
            result ??= new StringBuilder(text.Length + 8);
            result.Append(text, copied, index - copied);
            if (expansion == null) result.Append(mapped); else result.Append(expansion);
            copied = index + 1;
        }
        return result == null ? text : result.Append(text, copied, text.Length - copied).ToString();
    }
    static string CaseString(string text, bool upper, CultureInfo culture)
    {
        if (culture.TwoLetterISOLanguageName == "lt") text = ApplyLithuanianCasing(text, upper);
        if (upper) return ExpandUppercase(text.ToUpper(culture));
        text = ApplyFinalSigma(text);
        if (culture.TwoLetterISOLanguageName is "tr" or "az") text = ApplyTurkicDotContext(text);
        else text = text.Replace("\u0130", "i\u0307", StringComparison.Ordinal);
        return RecentLowercase(text.ToLower(culture));
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
