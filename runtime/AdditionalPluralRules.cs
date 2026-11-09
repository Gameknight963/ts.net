using System;

public static partial class Js
{
    static bool AdditionalPluralLanguage(string language)=>language is "ar" or "pl" or "uk" or "cs" or "sk" or "sl" or "lt" or "lv" or "he" or "ro";
    // https://unicode.org/reports/tr35/tr35-numbers.html#Language_Plural_Rules
    // Operands come from the rounded decimal text, retaining visible trailing zeros.
    static string AdditionalPluralCategory(string locale,string type,string text)
    {
        int point=text.IndexOf('.');
        ReadOnlySpan<char> integer=point<0 ? text.AsSpan() : text.AsSpan(0,point);
        while(integer.Length>1 && integer[0]=='0')integer=integer[1..];
        ReadOnlySpan<char> fraction=point<0 ? default : text.AsSpan(point+1);
        bool integral=true;foreach(char digit in fraction)if(digit!='0'){integral=false;break;}
        bool one=integer.SequenceEqual("1"),zero=integer.SequenceEqual("0"),two=integer.SequenceEqual("2");
        int last=integer[^1]-'0',pair=integer.Length>1 ? (integer[^2]-'0')*10+last : last;
        if(type=="ordinal")return !integral ? "other" : locale=="uk" ? last==3 && pair!=13 ? "few" : "other" : locale=="ro" && one ? "one" : "other";
        switch(locale) {
            case "ar":
                if(!integral)return "other";
                return zero ? "zero" : one ? "one" : two ? "two" : pair is >=3 and <=10 ? "few" : pair is >=11 and <=99 ? "many" : "other";
            case "pl":
                if(fraction.Length!=0)return "other";
                return one ? "one" : last is >=2 and <=4 && pair is not (>=12 and <=14) ? "few" : "many";
            case "uk":
                if(fraction.Length!=0)return "other";
                return last==1 && pair!=11 ? "one" : last is >=2 and <=4 && pair is not (>=12 and <=14) ? "few" : "many";
            case "cs":case "sk":
                if(fraction.Length!=0)return "many";
                return one ? "one" : integer.Length==1 && last is >=2 and <=4 ? "few" : "other";
            case "sl":
                return fraction.Length!=0 ? "few" : pair==1 ? "one" : pair==2 ? "two" : pair is 3 or 4 ? "few" : "other";
            case "lt":
                if(!integral)return "many";
                return last==1 && pair is not (>=11 and <=19) ? "one" : last is >=2 and <=9 && pair is not (>=11 and <=19) ? "few" : "other";
            case "lv":
                int fractionLast=fraction.Length==0 ? 0 : fraction[^1]-'0';
                int fractionPair=fraction.Length>1 ? (fraction[^2]-'0')*10+fractionLast : fractionLast;
                if(integral && (last==0 || pair is >=11 and <=19) || fraction.Length==2 && fractionPair is >=11 and <=19)return "zero";
                return integral && last==1 && pair!=11 || fraction.Length==2 && fractionLast==1 && fractionPair!=11 || fraction.Length!=2 && fractionLast==1 ? "one" : "other";
            case "he":
                return fraction.Length==0 && one || fraction.Length!=0 && zero ? "one" : fraction.Length==0 && two ? "two" : "other";
            case "ro":
                return fraction.Length==0 && one ? "one" : fraction.Length!=0 || zero || !one && pair is >=1 and <=19 ? "few" : "other";
            default:throw new InvalidOperationException("Unknown plural language");
        }
    }
    static string AdditionalPluralRange(string locale,string first,string last)=>locale switch {
        "uk" or "lt"=>last,
        "pl" or "cs" or "sk"=>last=="one" && first is "one" or "few" ? "other" : last,
        "sl"=>last=="one" ? "few" : last,
        "lv"=>last=="one" ? "one" : "other",
        "he"=>"other",
        "ro"=>last=="few" || first=="few" && last=="one" ? "few" : "other",
        "ar"=>last is "few" or "many" ? last : first=="zero" && last is "one" or "two" ? "zero" : "other",
        _=>throw new InvalidOperationException("Unknown plural language")
    };
    static object[] AdditionalPluralCategories(string locale,string type)=>type=="ordinal"
        ? locale=="uk" ? new object[]{"few","other"} : new object[]{"one","other"}
        : locale switch {
            "ar"=>new object[]{"zero","one","two","few","many","other"},
            "lv"=>new object[]{"zero","one","other"},
            "he"=>new object[]{"one","two","other"},
            "sl"=>new object[]{"one","two","few","other"},
            "ro"=>new object[]{"one","few","other"},
            _=>new object[]{"one","few","many","other"}
        };
}
