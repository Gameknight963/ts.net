public static partial class Js
{
    static object LocaleHourCycles(LocaleValue locale)
    {
        object cycle = locale.Keyword("hc");
        if (!IsUndefined(cycle)) return new JsArray(new[]{cycle is "true" ? "yes" : cycle});
        return new JsArray(new object[]{LocaleDefaultHourCycle(locale)});
    }
    static string LocaleDefaultHourCycle(LocaleValue locale, bool regionOverrides = true)
    {
        string region = regionOverrides ? LocaleRegionOverride(locale, "rg") ?? locale.Region ?? LocaleRegionOverride(locale, "sd") : locale.Region;
        region ??= ExpandLocaleSubtags(locale.Language, locale.Script, null).Region ?? "001";
        string selected = region == "ZZ" && locale.Language != "und" ? "h23" : LocaleHourCycleDefault(locale.Language, region);
        return selected;
    }
}
