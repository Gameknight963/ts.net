using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

public static partial class Js
{
    sealed class NativeCompactNumbers
    {
        [StructLayout(LayoutKind.Sequential)] struct Position {internal int Field,Begin,End;}
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Open(int style,IntPtr pattern,int length,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,IntPtr parseError,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close(IntPtr formatter);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Attribute(Handle formatter,int attribute,int value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Symbol(Handle formatter,int symbol,[MarshalAs(UnmanagedType.LPWStr)] string value,int length,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Decimal(Handle formatter,[MarshalAs(UnmanagedType.LPUTF8Str)] string number,int length,[Out,MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.U2)] char[] output,int capacity,ref Position position,ref int error);
        sealed record Api(IcuModule Module,Open Open,Close Close,Attribute Set,Symbol Symbol,Decimal Decimal);
        sealed class Handle(IcuModule module,Close close):SafeHandle(IntPtr.Zero,true)
        {
            public override bool IsInvalid=>handle==IntPtr.Zero;
            internal void Initialize(IntPtr value)=>SetHandle(value);
            protected override bool ReleaseHandle(){try{using var thread=module.Enter();close(handle);return true;}catch{return false;}}
        }
        static readonly Lazy<Api> Backend=new(()=>PlatformIcu.Load(Bind));
        static Api Bind(IcuModule module)
        {
            if(!module.Function<Open>("unum_open",out var open) || !module.Function<Close>("unum_close",out var close) ||
                !module.Function<Attribute>("unum_setAttribute",out var set) || !module.Function<Symbol>("unum_setSymbol",out var symbol) ||
                !module.Function<Decimal>("unum_formatDecimal",out var format))return null;
            return new(module,open,close,set,symbol,format);
        }
        readonly Api api;
        readonly Handle formatter;
        readonly object gate=new();
        readonly int[] exponents=new int[19];
        readonly string locale;
        readonly bool longStyle;
        static void Check(int error){if(error>0)throw new InvalidOperationException("Native compact formatting failed: ICU status "+error);}
        internal NativeCompactNumbers(string locale,bool longStyle)
        {
            this.locale=locale;this.longStyle=longStyle;
            api=Backend.Value ?? throw new NotSupportedException("Compact notation for this locale requires system ICU");
            Array.Fill(exponents,-1);formatter=new Handle(api.Module,api.Close);
            try {
                using var thread=api.Module.Enter();int error=0;
                formatter.Initialize(api.Open(longStyle ? 15 : 14,IntPtr.Zero,0,locale.Replace('-','_')+"@numbers=latn",IntPtr.Zero,ref error));Check(error);
                if(formatter.IsInvalid)throw new InvalidOperationException("Native compact formatter was not created");
                api.Set(formatter,1,0); // No grouping: the managed formatter owns numeric presentation.
                api.Set(formatter,16,0);api.Set(formatter,6,100);api.Set(formatter,7,0);api.Set(formatter,3,10000);
                api.Symbol(formatter,0,".",1,ref error);Check(error);
            }catch{formatter.Dispose();throw;}
        }
        string Read(string number,ref Position position)
            => Read(formatter,number,ref position);
        string Read(Handle selected,string number,ref Position position)
        {
            var output=ArrayPool<char>.Shared.Rent(256);
            try {
                int error=0,length=api.Decimal(selected,number,number.Length,output,output.Length,ref position,ref error);
                if(error==15) {
                    if(length<0 || length>20000)throw new InvalidOperationException("Native compact text is too long");
                    var larger=ArrayPool<char>.Shared.Rent(length+1);ArrayPool<char>.Shared.Return(output);output=larger;error=0;
                    length=api.Decimal(selected,number,number.Length,output,output.Length,ref position,ref error);
                }
                Check(error);if(length<0 || length>output.Length || position.Begin<0 || position.End<position.Begin || position.End>length)throw new InvalidOperationException("Invalid native compact field");
                return new string(output,0,length);
            }finally{ArrayPool<char>.Shared.Return(output);}
        }
        internal string CurrencyPattern(string symbol,bool negative,bool accounting)
        {
            using var thread=api.Module.Enter();int error=0;
            using var currency=new Handle(api.Module,api.Close);
            currency.Initialize(api.Open(accounting ? 12 : 2,IntPtr.Zero,0,locale.Replace('-','_')+"@numbers=latn",IntPtr.Zero,ref error));Check(error);
            if(currency.IsInvalid)throw new InvalidOperationException("Native currency formatter was not created");
            api.Set(currency,6,0);api.Set(currency,7,0);api.Symbol(currency,8,symbol,symbol.Length,ref error);Check(error);
            var position=new Position{Field=0};string text=Read(currency,negative ? "-1" : "1",ref position);
            if(position.Begin==position.End)throw new InvalidOperationException("Native currency pattern has no number");
            return text[..position.Begin]+"1"+text[position.End..];
        }
        internal int Exponent(int magnitude)
        {
            if(magnitude<3)return 0;
            // CLDR compact patterns stop well before this; larger numbers reuse
            // the largest pattern, keeping their remaining digits in the mantissa.
            int index=Math.Min(magnitude,exponents.Length-1);
            lock(gate) {
                if(exponents[index]>=0)return exponents[index];
                using var thread=api.Module.Enter();api.Set(formatter,7,0);var position=new Position{Field=0};
                string text=Read("1E"+index.ToString(CultureInfo.InvariantCulture),ref position);
                int digits=0;for(int i=position.Begin;i<position.End;i++)if(char.IsAsciiDigit(text[i]))digits++;
                int exponent=digits==0 ? index : index-digits+1;
                if(!longStyle && index is >=3 and <=5 && (locale=="it" || locale.StartsWith("it-",StringComparison.Ordinal)))exponent=3;
                // Older ICU data stops Japanese notation at trillions.
                if(index>=16 && (locale=="ja" || locale.StartsWith("ja-",StringComparison.Ordinal)))exponent=Math.Max(exponent,16);
                return exponents[index]=exponent;
            }
        }
        string lastInput,lastText,lastLabel;
        int lastExponent;
        NumberFormatInfo lastInfo;
        internal string Apply(string number,int exponent,NumberFormatInfo info,List<string> labels)
        {
            if(exponent==0)return number;
            lock(gate) {
                // Inputs have already been converted and rounded. Reusing this
                // final presentation skips no user code or numeric conversion.
                if(lastInput==number && lastExponent==exponent && ReferenceEquals(lastInfo,info)) {
                    if(lastLabel!=null)labels?.Add(lastLabel);return lastText;
                }
                lastInput=null;lastLabel=null;
                string result=ApplyCore(number,exponent,info);
                lastInput=number;lastExponent=exponent;lastInfo=info;lastText=result;
                if(lastLabel!=null)labels?.Add(lastLabel);return result;
            }
        }
        string ApplyCore(string number,int exponent,NumberFormatInfo info)
        {
            string sign="";
            if(number.StartsWith(info.NegativeSign,StringComparison.Ordinal)){sign=info.NegativeSign;number=number[sign.Length..];}
            else if(number.StartsWith(info.PositiveSign,StringComparison.Ordinal)){sign=info.PositiveSign;number=number[sign.Length..];}
            string raw=number.Replace(info.NumberGroupSeparator,"").Replace(info.NumberDecimalSeparator,".");
            int point=raw.IndexOf('.'),fraction=point<0 ? 0 : raw.Length-point-1;
            {
                using var thread=api.Module.Enter();api.Set(formatter,7,fraction);api.Set(formatter,0x1004,sign==info.PositiveSign ? 1 : 0);var position=new Position{Field=12};
                string text=Read((sign==info.NegativeSign ? "-" : "")+raw+"E"+exponent.ToString(CultureInfo.InvariantCulture),ref position);
                if(position.Begin==position.End) {
                    if(!longStyle && exponent==3 && (locale=="it" || locale.StartsWith("it-",StringComparison.Ordinal))){lastLabel="K";return sign+number+"K";}
                    return sign+number;
                }
                string label=text[position.Begin..position.End],replacement=label;
                // Small data-version corrections; newer system data already
                // supplies these forms and passes through unchanged.
                if(locale=="en-GB" && !longStyle)replacement=label switch{"K"=>"k","M"=>"m","B"=>"bn","T"=>"tn",_=>label};
                if((locale=="it" || locale.StartsWith("it-",StringComparison.Ordinal)) && !longStyle && label=="Mrd")replacement="Mld";
                if(!longStyle && (locale=="fa" || locale.StartsWith("fa-",StringComparison.Ordinal)))replacement=(exponent,label) switch {
                    (6,"\u0645")=>"\u0645\u06cc\u0644\u06cc\u0648\u0646",(9,"\u0645")=>"\u0645\u06cc\u0644\u06cc\u0627\u0631\u062f",(12,"\u062a")=>"\u062a\u0631\u06cc\u0644\u06cc\u0648\u0646",_=>replacement
                };
                if((locale=="ja" || locale.StartsWith("ja-",StringComparison.Ordinal)) && exponent>=16 && label=="\u5146")replacement="\u4eac";
                if(locale is "zh" or "zh-CN" or "zh-SG" or "zh-Hans" or "zh-Hans-CN" or "zh-Hans-SG" && label=="\u5146")replacement="\u4e07\u4ebf";
                bool french=locale=="fr" || locale.StartsWith("fr-",StringComparison.Ordinal),italian=locale=="it" || locale.StartsWith("it-",StringComparison.Ordinal);
                if(longStyle && (french && sign!=info.NegativeSign && label is "millier" or "milliers" || italian && label=="mille") && exponent==3 &&
                    decimal.TryParse(raw,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var mantissa) && mantissa==1) {
                    lastLabel="mille";return sign+"mille";
                }
                if(replacement!=label){text=text[..position.Begin]+replacement+text[position.End..];position.End=position.Begin+replacement.Length;}
                if(!longStyle && (locale=="he" || locale.StartsWith("he-",StringComparison.Ordinal)) && replacement is "K" or "M" or "B" or "T" && !text.AsSpan(position.End).StartsWith("\u200f",StringComparison.Ordinal))text=text[..position.End]+"\u200f"+text[position.End..];
                if(locale=="sw" || locale.StartsWith("sw-",StringComparison.Ordinal)) {
                    if(!longStyle && replacement is "M" or "B" or "T") {
                        int start=0;while(start<position.Begin && !char.IsAsciiDigit(text[start]))start++;
                        if(start<position.Begin){text=text[..start]+replacement+text[start..position.Begin]+text[position.End..];position.Begin=start;position.End=start+replacement.Length;}
                    }
                    int integerEnd=point<0 ? raw.Length : point,nonzero=0;while(nonzero<integerEnd && raw[nonzero]=='0')nonzero++;
                    if(sign.Length!=0 && (!longStyle || exponent>=6 || integerEnd-nonzero>=2)) {
                        int at=text.IndexOf(sign,StringComparison.Ordinal);
                        if(at>0){text=sign+text[..at]+text[(at+sign.Length)..];if(at>=position.End){position.Begin+=sign.Length;position.End+=sign.Length;}}
                    }
                }
                lastLabel=text[position.Begin..position.End];
                int first=-1,last=0;
                for(int i=0;i<text.Length;i++)if((i<position.Begin || i>=position.End) && char.IsAsciiDigit(text[i])){if(first<0)first=i;last=i+1;}
                return first<0 ? text : text[..first]+number+text[last..];
            }
        }
    }
}
