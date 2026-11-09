using System;
using System.Globalization;
using System.Numerics;

public static partial class Js
{
    sealed partial class NumberFormatValue
    {
        static bool RangeNegative(object value)=>value switch{double number=>double.IsNegative(number),BigInteger integer=>integer.Sign<0,LocaleDecimal exact=>exact.Negative,_=>false};
        static bool RangeZero(object value)=>value switch{double number=>number==0,BigInteger integer=>integer.IsZero,LocaleDecimal exact=>exact.Digits.IsZero,_=>false};
        string RegionalRangeText(object start,object end,NumberRangePatterns patterns)
        {
            string left=Text(start),right=Text(end);
            if(left==right && (RangeNegative(start)==RangeNegative(end) || RangeZero(start) && RangeZero(end)))return patterns.Approximately.Replace("{0}",left,StringComparison.Ordinal);
            var info=settings.culture.NumberFormat;
            foreach(string sign in new[]{info.NegativeSign,info.PositiveSign}) {
                if(sign.Length==0)continue;
                if(!left.StartsWith(sign,StringComparison.Ordinal) || !right.StartsWith(sign,StringComparison.Ordinal))continue;
                bool bidi=char.GetUnicodeCategory(sign[0])==UnicodeCategory.Format || char.GetUnicodeCategory(sign[^1])==UnicodeCategory.Format;
                if(!bidi)continue;
                int first=Math.Min(patterns.Range.IndexOf("{0}",StringComparison.Ordinal),patterns.Range.IndexOf("{1}",StringComparison.Ordinal));
                return patterns.Range[..first]+sign+patterns.Range[first..].Replace("{0}",left[sign.Length..],StringComparison.Ordinal).Replace("{1}",right[sign.Length..],StringComparison.Ordinal);
            }
            return patterns.Range.Replace("{0}",left,StringComparison.Ordinal).Replace("{1}",right,StringComparison.Ordinal);
        }
        object RegionalRange(object start,object end,bool parts)
        {
            if(settings.style!="decimal" || settings.notation!="standard")throw new NotSupportedException("Regional number ranges currently support standard decimal notation");
            var patterns=NativeNumberRangePatterns.For(settings.culture.Name,settings.numberingSystem);
            if(!parts)return RegionalRangeText(start,end,patterns);
            var left=(JsArray)Parts(start);var right=(JsArray)Parts(end);
            string Type(JsArray list,uint index)=>(string)((TsObject)list.ElementAt(index)).Get("type");
            string Value(JsArray list,uint index)=>(string)((TsObject)list.ElementAt(index)).Get("value");
            bool equal=left.Length()==right.Length() && (RangeNegative(start)==RangeNegative(end) || RangeZero(start) && RangeZero(end));
            for(uint i=0;equal && i<left.Length();i++)equal=Type(left,i)==Type(right,i) && Value(left,i)==Value(right,i);
            uint sharedPrefix=0;bool hasBidi=false,hasSign=false,prefixCopied=false;
            if(!equal) {
                while(sharedPrefix<left.Length() && sharedPrefix<right.Length() && Type(left,sharedPrefix)==Type(right,sharedPrefix) && Value(left,sharedPrefix)==Value(right,sharedPrefix)) {
                    string type=Type(left,sharedPrefix),value=Value(left,sharedPrefix);if(type is not ("literal" or "minusSign" or "plusSign"))break;
                    hasSign|=type is "minusSign" or "plusSign";
                    if(type=="literal")foreach(char ch in value)hasBidi|=char.GetUnicodeCategory(ch)==UnicodeCategory.Format;
                    sharedPrefix++;
                }
                // Directional markers make these multi-character sign affixes;
                // they remain shared when the endpoint signs are identical.
                if(!hasBidi || !hasSign)sharedPrefix=0;
            }
            var result=new JsArray(0);JsDynamicObject previous=null;
            void Add(string type,string value,string source) {
                if(value.Length==0)return;
                if(type=="literal" && previous!=null && Equals(previous.Get("type"),type) && Equals(previous.Get("source"),source)){previous.Set("value",(string)previous.Get("value")+value);return;}
                previous=new JsDynamicObject();previous.DefineOwn("type",type);previous.DefineOwn("value",value);previous.DefineOwn("source",source);result.Append(previous);
            }
            void Copy(JsArray list,string source) {
                if(!prefixCopied){for(uint i=0;i<sharedPrefix;i++)Add(Type(left,i),Value(left,i),"shared");prefixCopied=true;}
                for(uint i=sharedPrefix;i<list.Length();i++) {
                    string type=Type(list,i),value=Value(list,i);bool separatorMark=source=="endRange" && i==0 && type=="literal";
                    if(separatorMark)foreach(char ch in value)if(char.GetUnicodeCategory(ch)!=UnicodeCategory.Format){separatorMark=false;break;}
                    Add(type,value,separatorMark ? "shared" : source);
                }
            }
            void Literal(string literal) {
                if(!equal){Add("literal",literal,"shared");return;}
                bool IsLiteral(char ch)=>char.IsWhiteSpace(ch) || char.GetUnicodeCategory(ch)==UnicodeCategory.Format;
                for(int i=0;i<literal.Length;) {
                    bool space=IsLiteral(literal[i]);int limit=i+1;while(limit<literal.Length && IsLiteral(literal[limit])==space)limit++;
                    Add(space ? "literal" : "approximatelySign",literal[i..limit],"shared");i=limit;
                }
            }
            string pattern=equal ? patterns.Approximately : patterns.Range;
            for(int i=0;i<pattern.Length;) {
                if(pattern.AsSpan(i).StartsWith("{0}",StringComparison.Ordinal)){Copy(left,equal ? "shared" : "startRange");i+=3;}
                else if(pattern.AsSpan(i).StartsWith("{1}",StringComparison.Ordinal)){Copy(right,"endRange");i+=3;}
                else {int limit=i+1;while(limit<pattern.Length && pattern[limit]!='{')limit++;Literal(pattern[i..limit]);i=limit;}
            }
            return result;
        }
    }
}
