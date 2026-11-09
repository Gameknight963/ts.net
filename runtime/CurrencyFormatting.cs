using System;
using System.Collections.Generic;
using System.Globalization;

public static partial class Js
{
    // Older system ICU data omits these numbering-system-specific affixes.
    static (string Positive,string Negative) ArabicCurrencyPatterns(string symbol,string system,string minus,bool accounting,bool compact)
    {
        string body="1\u00a0"+symbol;
        if(accounting && system=="arabext" && !compact)return ("\u061c"+body,"(\u061c"+body+")");
        return ("\u200f"+body,system=="arab" || compact ? minus+"\u200f"+body : "\u200f"+minus+body);
    }
    readonly record struct CurrencyMetadata(string Symbol, int Digits);
    static CurrencyMetadata CurrencyFor(string code, CultureInfo locale, string display)
    {
        CultureInfo specific = locale;
        try { if (locale.IsNeutralCulture) specific = CultureInfo.CreateSpecificCulture(locale.Name); }
        catch (CultureNotFoundException) { }
        string localCode = null;
        try { localCode = new RegionInfo(specific.Name).ISOCurrencySymbol; }
        catch (ArgumentException) { }
        if (code == localCode)
            return new(display == "code" ? code : specific.NumberFormat.CurrencySymbol, specific.NumberFormat.CurrencyDecimalDigits);
        CurrencyMetadata metadata = CurrencyData.Values.TryGetValue(code, out var found) ? found : new(code, 2);
        if (display == "code") return new(code, metadata.Digits);
        if (display == "narrowSymbol") return metadata;
        // CLR culture data provides native symbols, rather than a translation table for
        // foreign currencies. Keep ambiguous or textual symbols unambiguous.
        string symbol = metadata.Symbol;
        if (symbol.Contains('$') && (specific.NumberFormat.CurrencySymbol.Contains('$') || code != "USD")) symbol = code;
        foreach (char character in symbol) if (char.IsLetter(character)) { symbol = code; break; }
        return new(symbol, metadata.Digits);
    }
    static class CurrencyData
    {
        static CurrencyData() { }
        internal static readonly Dictionary<string, CurrencyMetadata> Values = Build();
        static Dictionary<string, CurrencyMetadata> Build()
        {
            var result = new Dictionary<string, CurrencyMetadata>(StringComparer.Ordinal);
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                try
                {
                    string code = new RegionInfo(culture.Name).ISOCurrencySymbol;
                    result.TryAdd(code, new(culture.NumberFormat.CurrencySymbol, culture.NumberFormat.CurrencyDecimalDigits));
                }
                catch (ArgumentException) { }
            }
            return result;
        }
    }
    static string SpaceCurrencyTemplate(string template)
    {
        int index = template.IndexOf('1');
        string prefix = template[..index], suffix = template[(index + 1)..];
        if (prefix.Length > 0 && char.IsLetter(prefix[^1])) prefix += "\u00a0";
        if (suffix.Length > 0 && char.IsLetter(suffix[0])) suffix = "\u00a0" + suffix;
        return prefix + "1" + suffix;
    }
}
