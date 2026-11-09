public static partial class Js
{
    static object LocaleCollations(LocaleValue locale)
    {
        object requested = locale.Keyword("co");
        if (!IsUndefined(requested)) return new JsArray(new[]{requested is "true" ? "yes" : requested});
        if(NativeCollation.Available) {var available=new JsArray(0);foreach(string name in NativeCollation.Values(locale.BaseName))available.Append(name);return available;}
        var result = new JsArray(0);
        foreach (string name in IntlValueLists.Collations.Value) {
            bool applicable = locale.Language switch {"de" => name == "phonebk", "es" => name == "trad", "zh" => name is "stroke" or "zhuyin", _ => false};
            if (!applicable) continue;
            try {CollationSettings.NativeComparer(LocaleCulture(locale.BaseName), "sort", null, name); result.Append(name);}
            catch (System.NotSupportedException) { }
        }
        return result;
    }
}
