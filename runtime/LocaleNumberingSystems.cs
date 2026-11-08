public static partial class Js
{
    static object LocaleNumberingSystems(LocaleValue locale)
    {
        object requested = locale.Keyword("nu");
        if (!IsUndefined(requested)) return new JsArray(new[]{requested});
        string language = locale.Language, script = locale.Script;
        string system = (language, script) switch {
            ("ff", "Adlm") => "adlm", ("pa" or "uz", "Arab") => "arabext",
            ("as" or "bn" or "mni", null or "Beng") => "beng",
            ("bh" or "bho" or "mr" or "ne" or "sa", null or "Deva") => "deva",
            ("dz", null or "Tibt") => "tibt",
            ("fa" or "ks" or "ps" or "lrc", null or "Arab") => "arabext",
            ("my", null or "Mymr") => "mymr",
            ("sd" or "ckb", null or "Arab") => "arab",
            ("sat", null or "Olck") => "olck",
            ("ar", null or "Arab") => locale.Region is "BH" or "DJ" or "EG" or "ER" or "IL" or "IQ" or "JO" or "KM" or "KW" or "LB" or "MR" or "NT" or "OM" or "PS" or "QA" or "SA" or "SD" or "SO" or "SS" or "SY" or "TD" or "YD" or "YE" ? "arab" : "latn",
            _ => "latn"
        };
        return new JsArray(new object[]{system});
    }
}
