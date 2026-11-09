using System;

public static partial class Js
{
    static object LocaleCalendars(LocaleValue locale)
    {
        object calendar=locale.Keyword("ca");
        if(!IsUndefined(calendar))return new JsArray(new[]{calendar});
        string region=LocaleRegionOverride(locale,"rg") ?? locale.Region ?? LocaleRegionOverride(locale,"sd");
        region??=ExpandLocaleSubtags(locale.Language,locale.Script,null).Region ?? "001";
        // Regional calendar preferences, independent of DateTime's available calendars.
        // https://unicode.org/reports/tr35/tr35-dates.html#Calendar_Preference_Data
        var result=new JsArray(0);
        void Add(string name)=>result.Append(name);
        if(region is "AF" or "IR")Add("persian");
        if(region=="TH")Add("buddhist");
        Add("gregory");
        switch(region) {
            case "CN":case "CX":case "HK":case "MO":case "SG": Add("chinese");break;
            case "TW":Add("roc");Add("chinese");break;
            case "ET":Add("ethiopic");break;
            case "IN":Add("indian");break;
            case "JP":Add("japanese");break;
            case "KR":Add("dangi");break;
            case "EG":Add("coptic");break;
            case "IL":Add("hebrew");break;
        }
        if(region is "AE" or "BH" or "KW" or "QA" or "SA")Add("islamic-umalqura");
        bool islamic=region is "BD" or "DJ" or "DZ" or "EH" or "ER" or "ID" or "IQ" or "JO" or "KM" or "LB" or "LY" or "MA" or "MR" or "MY" or "NE" or "OM" or "PK" or "PS" or "SD" or "SY" or "TD" or "TN" or "YE"
            or "AE" or "BH" or "KW" or "QA" or "AF" or "IR" or "EG" or "IL" or "SA";
        if(islamic)Add("islamic");
        if(region=="SA")Add("islamic-rgsa");
        else if(islamic || region is "AL" or "AZ" or "MV" or "TJ" or "TM" or "TR" or "UZ" or "XK") {
            Add("islamic-civil");Add("islamic-tbla");
        }
        return result;
    }
}
