public static partial class Js
{
    static string LocaleHourCycleDefault(string language, string region)
    {
        string special = (language, region) switch {
            ("fr", "CA") => "h23",
            ("ku", "SY") => "h23",
            _ => null
        };
        if (special != null) return special;
        return region switch {
            "AE" or "AG" or "AL" or "AR" or "AS" or "AU" or "BB" or "BD" or "BH" or "BM" or "BN" or "BO" or "BS" or "BT" or "CA" or "CL" or "CO" or "CR" or "CT" or "CU" or "CY" or "DJ" or "DM" or "DO" or "DZ" or "EC" or "EG" or "EH" or "ER" or "ET" or "FJ" or "FM" or "GD" or "GH" or "GM" or "GR" or "GT" or "GU" or "GY" or "HK" or "HN" or "IN" or "IQ" or "JM" or "JO" or "JT" or "KH" or "KI" or "KN" or "KP" or "KR" or "KW" or "KY" or "LB" or "LC" or "LR" or "LS" or "LY" or "MH" or "MI" or "MO" or "MP" or "MR" or "MW" or "MX" or "MY" or "NA" or "NH" or "NI" or "NT" or "NZ" or "OM" or "PA" or "PC" or "PE" or "PG" or "PH" or "PK" or "PR" or "PS" or "PU" or "PW" or "PY" or "PZ" or "QA" or "SA" or "SB" or "SD" or "SG" or "SL" or "SO" or "SS" or "SV" or "SY" or "SZ" or "TC" or "TD" or "TN" or "TO" or "TT" or "TW" or "UM" or "US" or "UY" or "VC" or "VE" or "VG" or "VI" or "VU" or "WK" or "WS" or "YD" or "YE" or "ZM" or "ZZ" => "h12",
            _ => "h23"
        };
    }
}
