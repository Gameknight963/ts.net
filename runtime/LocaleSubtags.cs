using System;
using System.Collections.Generic;

public static partial class Js
{
    static (string Language, string Script, string Region) ExpandLocaleSubtags(string language, string script, string region)
    {
        string alias = LocaleSubtagLanguageAlias(language);
        if (alias != language) {
            string[] aliasFields = alias.Split('-'); language = aliasFields[0];
            for (int i = 1; i < aliasFields.Length; i++) {if (aliasFields[i].Length == 4) script ??= aliasFields[i]; else region ??= aliasFields[i];}
        }
        if (region != null && LocaleSubtagRegionAliases(region) is string replacements) {
            string likely = null;
            if (script != null) LikelyLocaleSubtags.Values.TryGetValue(language + "-" + script, out likely);
            if (likely == null) LikelyLocaleSubtags.Values.TryGetValue(language, out likely);
            string preferred = likely?.Split('-')[2];
            string[] candidates = replacements.Split(' ');
            region = preferred != null && Array.IndexOf(candidates, preferred) >= 0 ? preferred : candidates[0];
        }
        if (language != "und" && script != null && region != null) return (language, script, region);
        string selectedScript = script == "Zzzz" ? null : script, selectedRegion = region == "ZZ" ? null : region;
        if (language != "und" && !LikelyLocaleSubtags.Values.ContainsKey(language)) return (language, script, region);
        string selected = null;
        if (selectedScript != null && selectedRegion != null) LikelyLocaleSubtags.Values.TryGetValue(language + "-" + selectedScript + "-" + selectedRegion, out selected);
        if (selected == null && selectedScript != null) LikelyLocaleSubtags.Values.TryGetValue(language + "-" + selectedScript, out selected);
        if (selected == null && language == "und" && selectedScript != null) LikelyLocaleSubtags.Values.TryGetValue("und", out selected);
        if (selected == null && selectedRegion != null) LikelyLocaleSubtags.Values.TryGetValue(language + "-" + selectedRegion, out selected);
        if (selected == null) LikelyLocaleSubtags.Values.TryGetValue(language, out selected);
        if (selected == null) return (language, script, region);
        string[] fields = selected.Split('-');
        return (fields[0], selectedScript ?? fields[1], selectedRegion ?? fields[2]);
    }
    static object LocaleSubtags(LocaleValue locale, bool minimize)
    {
        var expanded = ExpandLocaleSubtags(locale.Language, minimize && locale.Script == "Zzzz" ? null : locale.Script, minimize && locale.Region == "ZZ" ? null : locale.Region);
        var selected = expanded;
        if (minimize) {
            if (ExpandLocaleSubtags(expanded.Language, null, null) == expanded) selected = (expanded.Language, null, null);
            else if (expanded.Region != null && ExpandLocaleSubtags(expanded.Language, null, expanded.Region) == expanded) selected = (expanded.Language, null, expanded.Region);
            else if (expanded.Script != null && ExpandLocaleSubtags(expanded.Language, expanded.Script, null) == expanded) selected = (expanded.Language, expanded.Script, null);
        }
        var fields = new List<string>{selected.Language};
        if (selected.Script != null) fields.Add(selected.Script);
        if (selected.Region != null) fields.Add(selected.Region);
        string[] original = locale.BaseName.Split('-');
        int start = 1 + (locale.Script == null ? 0 : 1) + (locale.Region == null ? 0 : 1);
        for (int index = start; index < original.Length; index++) fields.Add(original[index]);
        return new LocaleValue(string.Join('-', fields) + locale.Name[locale.BaseName.Length..], Undefined);
    }
}
