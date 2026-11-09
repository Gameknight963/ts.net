using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public static partial class Js
{
    sealed class DurationFormatValue:JsDescriptorObject
    {
        static readonly string[] Fields=["years","months","weeks","days","hours","minutes","seconds","milliseconds","microseconds","nanoseconds"];
        static readonly int[] ReadOrder=[3,4,8,7,5,1,9,6,2,0];
        readonly string locale,numberingSystem,style,separator;
        readonly string[] styles=new string[10],displays=new string[10];
        readonly NumberFormatValue[] numbers=new NumberFormatValue[10];
        readonly ListFormatValue list;
        readonly int fractionalDigits=-1;
        internal override TsObject PropertyPrototype=>TryExplicitPrototype(out var value) ? value : (TsObject)IntlDurationFormat.Constructor.Get("prototype");
        static bool Numeric(string style)=>style is "numeric" or "2-digit";
        internal DurationFormatValue(object locales,object optionValue)
        {
            locales=CanonicalLocales(locales);
            var options=IsUndefined(optionValue) ? null : ToObject(optionValue);
            object Read(string key)=>options==null ? Undefined : options.Get(key);
            string Option(string key,string fallback){object value=Read(key);return IsUndefined(value) ? fallback : StringValue(value);}
            var numberOptions=new JsDynamicObject();
            object matcher=Read("localeMatcher");if(!IsUndefined(matcher))numberOptions.Set("localeMatcher",StringValue(matcher));
            IntlLocaleMatcher(numberOptions);
            object system=Read("numberingSystem");if(!IsUndefined(system))numberOptions.Set("numberingSystem",system);
            var settings=new NumberLocaleSettings(locales,numberOptions);locale=settings.localeName.Length==0 ? "en-US" : settings.localeName;numberingSystem=settings.numberingSystem;
            separator=settings.culture.DateTimeFormat.TimeSeparator;
            style=Option("style","short");if(style is not ("long" or "short" or "narrow" or "digital"))throw new ArgumentOutOfRangeException(nameof(optionValue),"Invalid duration style");
            bool previousNumeric=false;
            for(int i=0;i<Fields.Length;i++) {
                string fallback=previousNumeric ? "numeric" : style=="digital" ? i<4 ? "short" : "numeric" : style;
                string selected=Option(Fields[i],fallback);
                if(selected is not ("long" or "short" or "narrow") && !(i>=4 && selected=="numeric") && !(i is >=4 and <=6 && selected=="2-digit"))
                    throw new ArgumentOutOfRangeException(nameof(optionValue),"Invalid duration unit style");
                if(previousNumeric && !Numeric(selected))throw new ArgumentOutOfRangeException(nameof(optionValue),"Numeric duration units must remain numeric");
                if(previousNumeric && i is 5 or 6)selected="2-digit";
                styles[i]=selected;displays[i]=Option(Fields[i]+"Display",Numeric(selected) && i<=6 ? "always" : "auto");
                if(displays[i] is not ("always" or "auto"))throw new ArgumentOutOfRangeException(nameof(optionValue),"Invalid duration display");
                if(i>=7 && Numeric(selected) && displays[i]=="always")throw new ArgumentOutOfRangeException(nameof(optionValue),"Fractional duration units cannot be displayed separately");
                previousNumeric=Numeric(selected);
            }
            object digits=Read("fractionalDigits");
            if(!IsUndefined(digits)) {
                double count=Number(digits);if(double.IsNaN(count) || count<0 || count>9)throw new ArgumentOutOfRangeException(nameof(optionValue),"Invalid duration fractional digits");
                fractionalDigits=(int)Math.Floor(count);
            }
            for(int i=0;i<Fields.Length;i++) {
                if(i>=7 && Numeric(styles[i]))continue;
                var number=new JsDynamicObject();number.Set("numberingSystem",numberingSystem);
                number.Set("minimumFractionDigits",0d);number.Set("maximumFractionDigits",0d);
                if(Numeric(styles[i])){number.Set("useGrouping",false);number.Set("minimumIntegerDigits",styles[i]=="2-digit" ? 2d : 1d);}
                else{number.Set("style","unit");number.Set("unit",Fields[i][..^1]);number.Set("unitDisplay",styles[i]);}
                int last=i;while(last+1<Fields.Length && last+1>=7 && Numeric(styles[last+1]))last++;
                if(last>i) {
                    number.Set("minimumFractionDigits",fractionalDigits<0 ? 0d : (double)fractionalDigits);
                    number.Set("maximumFractionDigits",fractionalDigits<0 ? (last-i)*3d : (double)fractionalDigits);
                    number.Set("roundingMode","trunc");
                }
                numbers[i]=new NumberFormatValue(locale,number);
            }
            var listOptions=new JsDynamicObject();listOptions.Set("type","unit");listOptions.Set("style",style=="digital" ? "short" : style);list=new ListFormatValue(locale,listOptions);
        }
        static BigInteger[] Values(object input,out bool negative)
        {
            if(input is string)throw new ArgumentOutOfRangeException(nameof(input),"Duration strings are not supported");
            if(input is not TsObject source)throw new InvalidOperationException("Duration must be an object");
            var result=new BigInteger[10];bool present=false,mixed=false;int sign=0;
            foreach(int index in ReadOrder) {
                object value=source.Get(Fields[index]);if(IsUndefined(value))continue;
                present=true;double number=Number(value);
                if(double.IsNaN(number))number=0;
                if(!double.IsFinite(number) || !double.IsInteger(number))throw new ArgumentOutOfRangeException(nameof(input),"Duration fields must be finite integers");
                int current=number<0 ? -1 : number>0 ? 1 : 0;
                if(current!=0 && sign!=0 && current!=sign)mixed=true;
                if(current!=0)sign=current;result[index]=new BigInteger(Math.Abs(number));
            }
            if(!present)throw new InvalidOperationException("Duration must contain a time or date field");
            if(mixed)throw new ArgumentOutOfRangeException(nameof(input),"Duration fields must have the same sign");
            if(result[0]>=4294967296 || result[1]>=4294967296 || result[2]>=4294967296)
                throw new ArgumentOutOfRangeException(nameof(input),"Duration date fields exceed their limit");
            BigInteger nanoseconds=(((result[3]*24+result[4])*60+result[5])*60+result[6])*1000000000+result[7]*1000000+result[8]*1000+result[9];
            if(nanoseconds>=(BigInteger.One<<53)*1000000000)throw new ArgumentOutOfRangeException(nameof(input),"Duration time fields exceed their limit");
            negative=sign<0;return result;
        }
        internal object Format(object input,bool parts)
        {
            var values=Values(input,out bool negative);bool signUsed=false;
            var components=parts ? new List<JsArray>() : null;var texts=new JsArray(0);
            var group=parts ? new JsArray(0) : null;var groupText=new StringBuilder();
            void Flush(){if(groupText.Length==0)return;if(parts){components.Add(group);group=new JsArray(0);}texts.Append(groupText.ToString());groupText.Clear();}
            void Add(JsArray source,string unit) {
                for(uint index=0;index<source.Length();index++) {
                    var part=(TsObject)source.ElementAt(index);part.DefineOwn("unit",unit);group.Append(part);groupText.Append((string)part.Get("value"));
                }
            }
            for(int i=0;i<Fields.Length;i++) {
                if(numbers[i]==null)continue;
                int last=i;BigInteger quantity=values[i];
                while(last+1<Fields.Length && last+1>=7 && Numeric(styles[last+1])){quantity=quantity*1000+values[++last];}
                bool bridge=i==5 && Numeric(styles[i]) && Numeric(styles[6]) && groupText.Length!=0 &&
                    (displays[6]=="always" || !values[6].IsZero || !values[7].IsZero || !values[8].IsZero || !values[9].IsZero);
                if(quantity.IsZero && displays[i]=="auto" && !bridge)continue;
                if(groupText.Length!=0) {
                    if(i==5 && styles[i]=="2-digit" || i==6 && Numeric(styles[i])) {
                        if(parts){var colon=new JsDynamicObject();colon.DefineOwn("type","literal");colon.DefineOwn("value",separator);group.Append(colon);}groupText.Append(separator);
                    }else Flush();
                }
                var value=new LocaleDecimal(quantity,-3*(last-i),negative && !signUsed);
                if(parts)Add((JsArray)numbers[i].Parts(value),Fields[i][..^1]);else groupText.Append(numbers[i].Text(value));signUsed=true;
            }
            Flush();
            if(!parts)return list.Format(texts,false);
            var listParts=(JsArray)list.Format(texts,true);var output=new JsArray(0);int component=0;
            for(uint i=0;i<listParts.Length();i++) {
                var part=(TsObject)listParts.ElementAt(i);
                if(Equals(part.Get("type"),"element")){var selected=components[component++];for(uint j=0;j<selected.Length();j++)output.Append(selected.ElementAt(j));}
                else output.Append(part);
            }
            return output;
        }
        internal object Options()
        {
            var result=new JsDynamicObject();result.DefineOwn("locale",locale);result.DefineOwn("numberingSystem",numberingSystem);result.DefineOwn("style",style);
            for(int i=0;i<Fields.Length;i++){result.DefineOwn(Fields[i],styles[i]);result.DefineOwn(Fields[i]+"Display",displays[i]);}
            if(fractionalDigits>=0)result.DefineOwn("fractionalDigits",(double)fractionalDigits);return result;
        }
    }
    static DurationFormatValue RequireDurationFormat(object receiver)=>receiver as DurationFormatValue ?? throw new InvalidOperationException("DurationFormat receiver required");
    sealed class DurationFormatConstructor:NativeNamespaceFunction
    {
        internal DurationFormatConstructor():base("DurationFormat",static _=>throw new InvalidOperationException("DurationFormat requires new"),0)
        {
            var prototype=new JsClassInstance{ParentPrototype=DefaultClassObjectPrototype};UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor",this);prototype.DefineMethod(PropertyKey(ToStringTagSymbol),"Intl.DurationFormat",false);
            prototype.DefineMethod("format",new CollectionMethod("format",1,static (receiver,args)=>RequireDurationFormat(receiver).Format(FunctionArgument(args,0),false)));
            prototype.DefineMethod("formatToParts",new CollectionMethod("formatToParts",1,static (receiver,args)=>RequireDurationFormat(receiver).Format(FunctionArgument(args,0),true)));
            prototype.DefineMethod("resolvedOptions",new CollectionMethod("resolvedOptions",0,static (receiver,_)=>RequireDurationFormat(receiver).Options()));
            Member("supportedLocalesOf",new JsFunction((FunctionBody)(static args=>CollatorSupportedLocales(FunctionArgument(args,0),FunctionArgument(args,1))),null,"supportedLocalesOf",1));
        }
        public override object Construct(object[] args)=>new DurationFormatValue(FunctionArgument(args,0),FunctionArgument(args,1));
        internal override bool CanInitializeDerived=>true;
        internal override object InitializeDerived(object instance,object[] args,JsFunction newTarget)
        {
            var prototype=ConstructionPrototype(newTarget);var result=(TsObject)Construct(args);result.SetPrototype(prototype);return result;
        }
    }
    static class IntlDurationFormat{internal static readonly JsFunction Constructor=new DurationFormatConstructor();}
}
