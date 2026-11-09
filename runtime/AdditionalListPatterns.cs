using System;

public static partial class Js
{
    static string ContextualListSeparator(string locale,string separator,string next,bool conjunction)
    {
        if(locale is "es" || locale.StartsWith("es-",StringComparison.Ordinal)) {
            static char Lower(char value)=>value is >= 'A' and <= 'Z' ? (char)(value+32) : value;
            char first=next.Length==0 ? '\0' : Lower(next[0]);
            char second=next.Length<2 ? '\0' : Lower(next[1]);
            if(separator==" y " && (first=='i' || first=='h' && second=='i' && (next.Length==2 || Lower(next[2]) is not ('a' or 'e'))))return " e ";
            if(separator==" o " && (first is 'o' or '8' || first=='h' && second=='o' || next=="11" || next.StartsWith("11 ",StringComparison.Ordinal)))return " u ";
        }
        if(conjunction && (locale is "he" || locale.StartsWith("he-",StringComparison.Ordinal)) && separator==" \u05d5-") {
            char first=next.Length==0 ? '\0' : next[0];
            bool hebrew=first is >= '\u0591' and <= '\u05c7' or >= '\u05d0' and <= '\u05ea' or >= '\u05ef' and <= '\u05f4'
                or >= '\ufb1d' and <= '\ufb36' or >= '\ufb38' and <= '\ufb3c' or '\ufb3e' or >= '\ufb40' and <= '\ufb41'
                or >= '\ufb43' and <= '\ufb44' or >= '\ufb46' and <= '\ufb4f';
            if(next.Length==0 || hebrew)return " \u05d5";
        }
        return separator;
    }
    static ListPattern? AdditionalListPattern(string locale,string type,string style)
    {
        int dash=locale.IndexOf('-');
        string language=locale=="pt-PT" || dash<0 ? locale : locale[..dash];
        return language switch {
            "es" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") or ("conjunction","narrow") or ("unit","long") => new(" y ",", "," y "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" o ",", "," o "),
                ("unit","short") => new(" y ",", ",", "),
                ("unit","narrow") => new(" "," "," "),
                _ => null
            },
            "it" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") or ("conjunction","narrow") or ("unit","long") or ("unit","short") => new(" e ",", "," e "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" o ",", "," o "),
                ("unit","narrow") => new(" "," "," "),
                _ => null
            },
            "pt" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") or ("unit","long") or ("unit","short") => new(" e ",", "," e "),
                ("conjunction","narrow") => new(", ",", ",", "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" ou ",", "," ou "),
                ("unit","narrow") => new(" "," "," "),
                _ => null
            },
            "pt-PT" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") or ("unit","long") or ("unit","short") or ("unit","narrow") => new(" e ",", "," e "),
                ("conjunction","narrow") => new(", ",", ",", "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" ou ",", "," ou "),
                _ => null
            },
            "nl" => (type,style) switch {
                ("conjunction","long") or ("unit","long") => new(" en ",", "," en "),
                ("conjunction","short") => new(" & ",", "," & "),
                ("conjunction","narrow") or ("unit","short") or ("unit","narrow") => new(", ",", ",", "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" of ",", "," of "),
                _ => null
            },
            "pl" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") or ("conjunction","narrow") or ("unit","long") or ("unit","short") or ("unit","narrow") => new(" i ",", "," i "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" lub ",", "," lub "),
                _ => null
            },
            "uk" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") or ("unit","long") or ("unit","short") or ("unit","narrow") => new(" \u0456 ",", "," \u0456 "),
                ("conjunction","narrow") => new(", ",", ",", "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" \u0430\u0431\u043e ",", "," \u0430\u0431\u043e "),
                _ => null
            },
            "cs" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") or ("unit","long") => new(" a\u00a0",", "," a\u00a0"),
                ("conjunction","narrow") => new(", ",", ",", "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" nebo ",", "," nebo "),
                ("unit","short") => new(", ",", "," a\u00a0"),
                ("unit","narrow") => new(" "," "," "),
                _ => null
            },
            "tr" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") => new(" ve ",", "," ve "),
                ("conjunction","narrow") => new(", ",", ",", "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" veya ",", "," veya "),
                ("unit","long") or ("unit","short") or ("unit","narrow") => new(" "," "," "),
                _ => null
            },
            "ar" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") or ("conjunction","narrow") or ("unit","narrow") => new(" \u0648"," \u0648"," \u0648"),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" \u0623\u0648 "," \u0623\u0648 "," \u0623\u0648 "),
                ("unit","long") or ("unit","short") => new(" \u0648","\u060c \u0648","\u060c \u0648"),
                _ => null
            },
            "hi" => (type,style) switch {
                ("conjunction","long") or ("unit","long") => new(" \u0914\u0930 ",", ",", \u0914\u0930 "),
                ("conjunction","short") or ("conjunction","narrow") => new(" \u0914\u0930 ",", "," \u0914\u0930 "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" \u092f\u093e ",", "," \u092f\u093e "),
                ("unit","short") => new(", ",", ",", "),
                ("unit","narrow") => new(" ",", "," "),
                _ => null
            },
            "sv" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") => new(" och ",", "," och "),
                ("conjunction","narrow") or ("unit","long") or ("unit","short") => new(", ",", ",", "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" eller ",", "," eller "),
                ("unit","narrow") => new(" "," "," "),
                _ => null
            },
            "da" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") or ("conjunction","narrow") or ("unit","long") or ("unit","short") or ("unit","narrow") => new(" og ",", "," og "),
                ("disjunction","long") => new(" eller ",", "," eller "),
                ("disjunction","short") or ("disjunction","narrow") => new(" el. ",", "," el. "),
                _ => null
            },
            "fi" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") or ("conjunction","narrow") or ("unit","long") => new(" ja ",", "," ja "),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" tai ",", "," tai "),
                ("unit","short") => new(", ",", ",", "),
                ("unit","narrow") => new(" "," "," "),
                _ => null
            },
            "he" => (type,style) switch {
                ("conjunction","long") or ("conjunction","short") or ("conjunction","narrow") => new(" \u05d5-",", "," \u05d5-"),
                ("disjunction","long") or ("disjunction","short") or ("disjunction","narrow") => new(" \u05d0\u05d5 ",", "," \u05d0\u05d5 "),
                ("unit","long") => new(", ",", "," \u05d5-"),
                ("unit","short") => new(", ",", ",", "),
                ("unit","narrow") => new(" "," "," "),
                _ => null
            },
            _ => null
        };
    }
}
