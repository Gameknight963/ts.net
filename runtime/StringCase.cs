using System;
using System.Globalization;

public static partial class Js
{
    static CultureInfo CaseCulture(object locales)
    {
        if (IsUndefined(locales)) return CultureInfo.CurrentCulture;
        CultureInfo first = null;
        void Add(object value)
        {
            if (value is not string && value is not TsObject) throw new InvalidOperationException("Locale must be a string or object");
            string tag = StringValue(value);
            if (tag.Length == 0 || tag.Contains('_')) throw new ArgumentOutOfRangeException(nameof(locales), "Invalid casing locale");
            try { var culture = CultureInfo.GetCultureInfo(tag); first ??= culture; }
            catch (CultureNotFoundException) { throw new ArgumentOutOfRangeException(nameof(locales), "Invalid or unsupported casing locale"); }
        }
        if (locales is string) Add(locales);
        else
        {
            var list = ToObject(locales); long length = IndexedLength(list);
            for (long index = 0; index < length; index++) if (TryIndexedValue(list, index, out object value)) { Add(value); break; }
        }
        return first ?? CultureInfo.CurrentCulture;
    }
    sealed class StringCaseFunction(bool upper, bool localized = false) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, localized ? upper ? "toLocaleUpperCase" : "toLocaleLowerCase" : upper ? "toUpperCase" : "toLowerCase", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            if (localized) { var culture = CaseCulture(FunctionArgument(arguments, 0)); return upper ? text.ToUpper(culture) : text.ToLower(culture); }
            return upper ? text.ToUpperInvariant() : text.ToLowerInvariant();
        }
    }
}
