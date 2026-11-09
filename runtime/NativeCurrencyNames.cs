using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;

public static partial class Js
{
    sealed record CurrencyNamePattern(string Prefix,string Suffix,bool NameInPrefix,int NameStart,int NameLength)
    {
        internal static CurrencyNamePattern English(string name)=>new(""," "+name,false,1,name.Length);
        internal string Apply(string number)=>Prefix+number+Suffix;
        internal CurrencyNamePattern WithName(string name)=>NameInPrefix
            ? this with {Prefix=Prefix[..NameStart]+name+Prefix[(NameStart+NameLength)..],NameLength=name.Length}
            : this with {Suffix=Suffix[..NameStart]+name+Suffix[(NameStart+NameLength)..],NameLength=name.Length};
        internal void Parts(bool prefix,Action<string,string> add)
        {
            string text=prefix ? Prefix : Suffix;
            if(NameInPrefix!=prefix){if(text.Length!=0)add("literal",text);return;}
            if(NameStart!=0)add("literal",text[..NameStart]);add("currency",text.Substring(NameStart,NameLength));
            if(NameStart+NameLength<text.Length)add("literal",text[(NameStart+NameLength)..]);
        }
    }
    sealed class NumberFormatAnnotations {internal CurrencyNamePattern Currency;}
    sealed class NativeCurrencyNames
    {
        [StructLayout(LayoutKind.Sequential)] struct Position {internal int Field,Begin,End;}
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Open(int style,IntPtr pattern,int length,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,IntPtr parseError,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close(IntPtr formatter);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Attribute(Handle formatter,int attribute,int value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void TextAttribute(Handle formatter,int attribute,[MarshalAs(UnmanagedType.LPWStr)] string value,int length,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Symbol(Handle formatter,int symbol,[MarshalAs(UnmanagedType.LPWStr)] string value,int length,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Decimal(Handle formatter,[MarshalAs(UnmanagedType.LPUTF8Str)] string number,int length,[Out,MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.U2)] char[] output,int capacity,ref Position position,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int DoubleText(Handle formatter,double number,[Out,MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.U2)] char[] output,int capacity,ref Position position,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr PluralName(IntPtr currency,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,out byte choice,[MarshalAs(UnmanagedType.LPUTF8Str)] string category,out int length,ref int error);
        sealed record Api(IcuModule Module,Open Open,Close Close,Attribute Set,TextAttribute Text,Symbol Symbol,Decimal Decimal,DoubleText Double,PluralName Name);
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
                !module.Function<Attribute>("unum_setAttribute",out var set) || !module.Function<TextAttribute>("unum_setTextAttribute",out var text) ||
                !module.Function<Symbol>("unum_setSymbol",out var symbol) || !module.Function<Decimal>("unum_formatDecimal",out var format) ||
                !module.Function<DoubleText>("unum_formatDouble",out var doubleText) || !module.Function<PluralName>("ucurr_getPluralName",out var name))return null;
            return new(module,open,close,set,text,symbol,format,doubleText,name);
        }
        readonly Api api;
        readonly Handle formatter;
        readonly object gate=new();
        readonly string locale,currency,language;
        string previousRaw;int previousExponent;bool previousFinite;
        CurrencyNamePattern previousPattern;
        static void Check(int error){if(error>0)throw new InvalidOperationException("Native currency names failed: ICU status "+error);}
        internal NativeCurrencyNames(string locale,string currency)
        {
            this.locale=locale;this.currency=currency;language=locale.Split('-')[0];
            api=Backend.Value ?? throw new NotSupportedException("Currency names for this locale require system ICU");formatter=new Handle(api.Module,api.Close);
            try {
                using var thread=api.Module.Enter();int error=0;
                formatter.Initialize(api.Open(11,IntPtr.Zero,0,locale.Replace('-','_')+"@numbers=latn",IntPtr.Zero,ref error));Check(error);
                if(formatter.IsInvalid)throw new InvalidOperationException("Native currency name formatter was not created");
                api.Text(formatter,5,currency,currency.Length,ref error);Check(error);
                api.Set(formatter,1,0);api.Set(formatter,16,0);api.Set(formatter,6,999);api.Set(formatter,3,10000);
                api.Symbol(formatter,0,".",1,ref error);Check(error);api.Symbol(formatter,10,".",1,ref error);Check(error);
            }catch{formatter.Dispose();throw;}
        }
        string Read(string number,bool finite,double special,ref Position position)
        {
            var output=ArrayPool<char>.Shared.Rent(256);
            try {
                var current=position;
                int Write(ref int error)=>finite ? api.Decimal(formatter,number,number.Length,output,output.Length,ref current,ref error) : api.Double(formatter,special,output,output.Length,ref current,ref error);
                int status=0,length=Write(ref status);
                if(status==15) {
                    if(length<0 || length>20000)throw new NotSupportedException("System ICU cannot represent this currency-name value");
                    var larger=ArrayPool<char>.Shared.Rent(length+1);ArrayPool<char>.Shared.Return(output);output=larger;status=0;length=Write(ref status);
                }
                position=current;Check(status);if(length<0 || length>output.Length || position.Begin<0 || position.End<position.Begin || position.End>length)throw new InvalidOperationException("Invalid native currency name field");
                return new string(output,0,length);
            }finally{ArrayPool<char>.Shared.Return(output);}
        }
        static string Expanded(string number,int exponent)
        {
            int point=number.IndexOf('.');if(point<0)point=number.Length;else number=number.Remove(point,1);
            long shifted=(long)point+exponent;
            if(shifted>20000 || shifted< -20000 || number.Length>20000)throw new NotSupportedException("System ICU cannot represent this currency-name value");
            return shifted<=0 ? "0."+new string('0',(int)-shifted)+number : shifted>=number.Length ? number+new string('0',(int)shifted-number.Length) : number[..(int)shifted]+"."+number[(int)shifted..];
        }
        static string Canonical(string number)
        {
            int start=0;while(start+1<number.Length && number[start]=='0' && number[start+1]!='.')start++;
            number=number[start..];if(number.Contains('.'))number=number.TrimEnd('0').TrimEnd('.');return number;
        }
        string Name(string category)
        {
            IntPtr input=Marshal.StringToHGlobalUni(currency);
            try {
                int error=0;IntPtr value=api.Name(input,locale.Replace('-','_'),out byte choice,category,out int length,ref error);Check(error);
                if(value==IntPtr.Zero || length<0 || length>4096 || choice!=0)throw new NotSupportedException("System ICU did not supply this currency name");
                return value==input ? currency : Marshal.PtrToStringUni(value,length);
            }finally{Marshal.FreeHGlobal(input);}
        }
        internal CurrencyNamePattern Pattern(string number,int exponent,NumberFormatInfo info,bool finite)
        {
            if(number.StartsWith(info.NegativeSign,StringComparison.Ordinal))number=number[info.NegativeSign.Length..];
            else if(number.StartsWith(info.PositiveSign,StringComparison.Ordinal))number=number[info.PositiveSign.Length..];
            string raw=finite ? number.Replace(info.NumberGroupSeparator,"").Replace(info.NumberDecimalSeparator,".") : number;
            lock(gate) {
                if(previousPattern!=null && previousRaw==raw && previousExponent==exponent && previousFinite==finite)return previousPattern;
                using var thread=api.Module.Enter();int point=raw.IndexOf('.'),fraction=finite && point>=0 ? raw.Length-point-1 : 0;
                api.Set(formatter,7,finite ? (int)Math.Clamp((long)fraction-exponent,0,999) : 0);
                string input=finite ? raw+"E"+exponent.ToString(CultureInfo.InvariantCulture) : null;double special=number==info.NaNSymbol ? BitConverter.Int64BitsToDouble(0x7ff8000000000000) : double.PositiveInfinity;
                var numeric=new Position{Field=0};string text=Read(input,finite,special,ref numeric);
                var currency=new Position{Field=7};string check=Read(input,finite,special,ref currency);
                if(check!=text || numeric.Begin==numeric.End || currency.Begin==currency.End)throw new NotSupportedException("System ICU did not supply currency-name fields");
                int end=numeric.End;if(end+1<text.Length && text[end]=='.' && char.IsAsciiDigit(text[end+1])){end++;while(end<text.Length && char.IsAsciiDigit(text[end]))end++;}
                string expanded=finite ? Expanded(raw,exponent) : null;
                if(finite && Canonical(text[numeric.Begin..end])!=Canonical(expanded))throw new NotSupportedException("System ICU cannot represent this currency-name value");
                bool prefix=currency.End<=numeric.Begin;if(!prefix && currency.Begin<end)throw new InvalidOperationException("Overlapping native currency name fields");
                var result=new CurrencyNamePattern(text[..numeric.Begin],text[end..],prefix,prefix ? currency.Begin : currency.Begin-end,currency.End-currency.Begin);
                // Older ICU plural operands can wrap large integer parts. Keep
                // the shared exact rules for values beyond their integer range.
                int integerEnd=finite ? expanded.IndexOf('.') : 0;if(integerEnd<0)integerEnd=expanded.Length;
                int firstInteger=0;while(firstInteger+1<integerEnd && expanded[firstInteger]=='0')firstInteger++;
                if(finite && integerEnd-firstInteger>18) {
                    if(SupportsPluralLanguage(language))result=result.WithName(Name(PluralRulesValue.CategoryFor(language,"cardinal",expanded)));
                    else if(language=="pt")result=result.WithName(Name("other"));
                }
                previousRaw=raw;previousExponent=exponent;previousFinite=finite;previousPattern=result;return result;
            }
        }
    }
}
