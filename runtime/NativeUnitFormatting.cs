using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

public static partial class Js
{
    sealed record UnitAffixPattern(string Text,int NumberStart,int NumberEnd,(int Start,int End)[] Units,bool OmitNumber)
    {
        internal string Apply(string number)=>OmitNumber ? Text : Text[..NumberStart]+number+Text[NumberEnd..];
        internal void Parts(bool prefix,Action<string,string> add)
        {
            int start=prefix ? 0 : NumberEnd,end=prefix ? NumberStart : Text.Length;
            if(OmitNumber){if(!prefix)return;start=0;end=Text.Length;}
            foreach(var unit in Units) {
                if(unit.Start<start || unit.End>end)continue;
                if(unit.Start>start)add("literal",Text[start..unit.Start]);
                add("unit",Text[unit.Start..unit.End]);start=unit.End;
            }
            if(start<end)add("literal",Text[start..end]);
        }
    }
    sealed class NativeUnitFormatting
    {
        [StructLayout(LayoutKind.Sequential)] struct Position {internal int Field,Begin,End;}
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Open([MarshalAs(UnmanagedType.LPWStr)] string skeleton,int length,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr OpenResult(ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close(IntPtr handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Decimal(Handle formatter,[MarshalAs(UnmanagedType.LPUTF8Str)] string value,int length,Handle result,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void DoubleText(Handle formatter,double value,Handle result,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Text(Handle result,[Out,MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.U2)] char[] buffer,int capacity,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate byte Next(Handle result,ref Position field,ref int error);
        sealed record Api(IcuModule Module,Open Open,OpenResult Result,Close Close,Close CloseResult,Decimal Decimal,DoubleText Double,Text Text,Next Next);
        sealed class Handle(IcuModule module,Close close):SafeHandle(IntPtr.Zero,true)
        {
            public override bool IsInvalid=>handle==IntPtr.Zero;
            internal void Initialize(IntPtr value)=>SetHandle(value);
            protected override bool ReleaseHandle(){try{using var thread=module.Enter();close(handle);return true;}catch{return false;}}
        }
        static readonly Lazy<Api> Backend=new(()=>PlatformIcu.Load(Bind));
        static Api Bind(IcuModule module)
        {
            if(!module.Function<Open>("unumf_openForSkeletonAndLocale",out var open) || !module.Function<OpenResult>("unumf_openResult",out var result) ||
                !module.Function<Close>("unumf_close",out var close) || !module.Function<Close>("unumf_closeResult",out var closeResult) ||
                !module.Function<Decimal>("unumf_formatDecimal",out var dec) || !module.Function<DoubleText>("unumf_formatDouble",out var dbl) ||
                !module.Function<Text>("unumf_resultToString",out var text) || !module.Function<Next>("unumf_resultNextFieldPosition",out var next))return null;
            return new(module,open,result,close,closeResult,dec,dbl,text,next);
        }
        readonly Api api;
        readonly Handle result;
        readonly string locale,skeleton,language;
        readonly object gate=new();
        readonly Dictionary<int,Handle> formatters=new();
        static void Check(int error){if(error>0)throw new InvalidOperationException("Native unit formatting failed: ICU status "+error);}
        static string Unit(string unit)
        {
            string type=unit switch {
                "acre" or "hectare"=>"area",
                "bit" or "byte" or "gigabit" or "gigabyte" or "kilobit" or "kilobyte" or "megabit" or "megabyte" or "petabyte" or "terabit" or "terabyte"=>"digital",
                "celsius" or "fahrenheit"=>"temperature",
                "day" or "hour" or "microsecond" or "millisecond" or "minute" or "month" or "nanosecond" or "second" or "week" or "year"=>"duration",
                "degree"=>"angle",
                "fluid-ounce" or "gallon" or "liter" or "milliliter"=>"volume",
                "gram" or "kilogram" or "ounce" or "pound" or "stone"=>"mass",
                "percent"=>"concentr",
                _=>"length"
            };
            return type+"-"+unit;
        }
        internal NativeUnitFormatting(string locale,string unit,string display,string numberingSystem)
        {
            language=locale.Split('-')[0];
            this.locale=locale.Replace('-','_')+"@numbers="+numberingSystem;
            int per=unit.IndexOf("-per-",StringComparison.Ordinal);
            skeleton="measure-unit/"+Unit(per<0 ? unit : unit[..per])+(per<0 ? "" : " per-measure-unit/"+Unit(unit[(per+5)..]))+
                " unit-width-"+(display=="long" ? "full-name" : display)+" group-off sign-never";
            api=Backend.Value ?? throw new NotSupportedException("Unit formatting for this locale requires system ICU");
            using var thread=api.Module.Enter();int error=0;result=new Handle(api.Module,api.CloseResult);
            try{result.Initialize(api.Result(ref error));Check(error);if(result.IsInvalid)throw new InvalidOperationException("Native unit result was not created");Formatter(0);}
            catch{result.Dispose();foreach(var formatter in formatters.Values)formatter.Dispose();throw;}
        }
        Handle Formatter(int fraction)
        {
            if(formatters.TryGetValue(fraction,out var found))return found;
            string precision=fraction==0 ? "precision-integer" : "."+new string('0',fraction);
            var formatter=new Handle(api.Module,api.Close);
            try{int error=0;formatter.Initialize(api.Open(skeleton+" "+precision,-1,locale,ref error));Check(error);if(formatter.IsInvalid)throw new InvalidOperationException("Native unit formatter was not created");}
            catch{formatter.Dispose();throw;}
            if(formatters.Count==8){foreach(var old in formatters.Values)old.Dispose();formatters.Clear();}
            formatters.Add(fraction,formatter);return formatter;
        }
        string Read()
        {
            var buffer=ArrayPool<char>.Shared.Rent(256);
            try {
                int error=0,length=api.Text(result,buffer,buffer.Length,ref error);
                if(error==15){if(length<0 || length>20000)throw new NotSupportedException("Native unit result is too long");var larger=ArrayPool<char>.Shared.Rent(length+1);ArrayPool<char>.Shared.Return(buffer);buffer=larger;error=0;length=api.Text(result,buffer,buffer.Length,ref error);}
                Check(error);if(length<0 || length>buffer.Length)throw new InvalidOperationException("Invalid native unit result length");return new string(buffer,0,length);
            }finally{ArrayPool<char>.Shared.Return(buffer);}
        }
        string PluralOperand(string raw,int exponent,int fraction)
        {
            int point=raw.IndexOf('.');if(point<0)point=raw.Length;
            long integerDigits=(long)point+exponent;
            if(integerDigits<=18)return raw+"E"+exponent.ToString(CultureInfo.InvariantCulture);
            if(language=="pt")return "2";
            if(!SupportsPluralLanguage(language))return raw+"E"+exponent.ToString(CultureInfo.InvariantCulture);
            if(integerDigits>20000)throw new NotSupportedException("System ICU cannot represent this unit value");
            string digits=raw.Replace(".","");
            string expanded=integerDigits>=digits.Length ? digits+new string('0',(int)integerDigits-digits.Length) : digits[..(int)integerDigits]+"."+digits[(int)integerDigits..];
            string category=PluralRulesValue.CategoryFor(language,"cardinal",expanded);
            // Older ICU versions narrow plural operands to machine integers.
            // Use an exact, small operand in the same category for the affix;
            // the caller keeps the original rounded number for presentation.
            // Romanian also inserts "de" for large numbers in the few category.
            int[] candidates=language=="ro" ? [101,21,0,1,2,3,5,11,1000000] : [0,1,2,3,5,11,21,101,1000000];
            foreach(int candidate in candidates) {
                string sample=candidate.ToString(CultureInfo.InvariantCulture)+(fraction==0 ? "" : "."+new string('0',fraction));
                if(PluralRulesValue.CategoryFor(language,"cardinal",sample)==category)return sample;
            }
            if(fraction>0)foreach(int candidate in candidates) {
                string sample=candidate.ToString(CultureInfo.InvariantCulture)+".1"+new string('0',fraction-1);
                if(PluralRulesValue.CategoryFor(language,"cardinal",sample)==category)return sample;
            }
            return expanded;
        }
        internal UnitAffixPattern Pattern(string number,int exponent,NumberFormatInfo info,bool finite)
        {
            if(number.StartsWith(info.NegativeSign,StringComparison.Ordinal))number=number[info.NegativeSign.Length..];
            else if(number.StartsWith(info.PositiveSign,StringComparison.Ordinal))number=number[info.PositiveSign.Length..];
            string raw=finite ? number.Replace(info.NumberGroupSeparator,"").Replace(info.NumberDecimalSeparator,".") : number;
            int point=raw.IndexOf('.'),fraction=finite && point>=0 ? raw.Length-point-1 : 0;
            long visible=(long)fraction-exponent;if(visible>999)throw new NotSupportedException("System ICU cannot represent this unit precision");
            lock(gate) {
                using var thread=api.Module.Enter();var formatter=Formatter((int)Math.Max(0,visible));int error=0;
                if(finite){string input=PluralOperand(raw,exponent,(int)Math.Max(0,visible));api.Decimal(formatter,input,input.Length,result,ref error);}
                else api.Double(formatter,number==info.NaNSymbol ? BitConverter.Int64BitsToDouble(0x7ff8000000000000) : double.PositiveInfinity,result,ref error);
                Check(error);string text=Read();int start=text.Length,end=0;var units=new List<(int,int)>();
                foreach(int kind in new[]{0,1,2,8,11}) {
                    var field=new Position{Field=kind};
                    while(api.Next(result,ref field,ref error)!=0) {
                        if(field.Begin<0 || field.End<=field.Begin || field.End>text.Length)throw new InvalidOperationException("Invalid native unit field");
                        if(kind is 8 or 11)units.Add((field.Begin,field.End));else{start=Math.Min(start,field.Begin);end=Math.Max(end,field.End);}
                    }
                    Check(error);
                }
                units.Sort((a,b)=>a.Item1.CompareTo(b.Item1));
                if(units.Count==0)throw new NotSupportedException("System ICU did not supply this unit's fields");
                return new(text,start,end,units.ToArray(),end==0);
            }
        }
    }
}
