using System;
using System.Collections.Generic;
using System.Globalization;

public static partial class Js
{
    static class IntlValueLists
    {
        internal static readonly string[] Units = "acre bit byte celsius centimeter day degree fahrenheit fluid-ounce foot gallon gigabit gigabyte gram hectare hour inch kilobit kilobyte kilogram kilometer liter megabit megabyte meter microsecond mile mile-scandinavian milliliter millimeter millisecond minute month nanosecond ounce percent petabyte pound second stone terabit terabyte week yard year".Split(' ');
        internal static readonly string[] NumberingSystems = "adlm ahom bali beng bhks brah cakm cham deva diak fullwide gara gong gonm gujr gukh guru hanidec hmng hmnp java kali kawi khmr knda krai lana lanatham laoo latn lepc limb mathbold mathdbl mathmono mathsanb mathsans mlym modi mong mroo mtei mymr mymrepka mymrpao mymrshan mymrtlng nagm newa nkoo olck onao orya osma outlined rohg saur segment shrd sind sinh sora sund sunu takr talu tamldec telu thai tibt tirh tnsa tols vaii wara wcho".Split(' ');
        internal static readonly Lazy<string[]> Calendars = new(() => {
            var names = new SortedSet<string>(StringComparer.Ordinal) {"iso8601"};
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
                foreach (var calendar in culture.OptionalCalendars)
                    if (DateLocaleSettings.NativeCalendarName(calendar) is string name) names.Add(name);
            return [..names];
        });
        internal static readonly Lazy<string[]> Currencies = new(() => {
            var names = new SortedSet<string>(CurrencyData.Values.Keys, StringComparer.Ordinal);
            names.Remove("XXX");
            return [..names];
        });
        internal static readonly Lazy<string[]> Collations = new(() => {
            if(NativeCollation.Available)return NativeCollation.Values();
            var names = new List<string>();
            foreach (var (locale, name) in new[]{("de-DE", "phonebk"), ("es-ES", "trad"), ("zh-CN", "stroke"), ("zh-TW", "zhuyin")}) {
                try {CollationSettings.NativeComparer(CultureInfo.GetCultureInfo(locale), "sort", null, name); names.Add(name);}
                catch (NotSupportedException) { }
            }
            names.Sort(StringComparer.Ordinal);
            return names.ToArray();
        });
        internal static readonly Lazy<string[]> TimeZones = new(() => {
            var names = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var region in LocaleTimeZoneData.Regions) if (region.Key != "001") foreach (string zone in region.Value) names.Add(zone);
            names.RemoveWhere(name => !NativeTimeZoneAvailable(name));
            return [..names];
        });
    }
    static object IntlSupportedValues(object keyValue)
    {
        string key = StringValue(keyValue);
        string[] names = key switch {
            "unit" => IntlValueLists.Units, "numberingSystem" => IntlValueLists.NumberingSystems,
            "calendar" => IntlValueLists.Calendars.Value, "currency" => IntlValueLists.Currencies.Value,
            "collation" => IntlValueLists.Collations.Value, "timeZone" => IntlValueLists.TimeZones.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(keyValue), "Invalid Intl value key")
        };
        var result = new JsArray(0);
        foreach (string name in names) result.Append(name);
        return result;
    }
}
