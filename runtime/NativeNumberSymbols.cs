using System;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static partial class Js
{
    static class NativeNumberSymbols
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Open(int style,IntPtr pattern,int length,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,IntPtr parseError,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close(IntPtr formatter);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Symbol(Handle formatter,int symbol,[Out,MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.U2)] char[] output,int capacity,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr CurrencyName(IntPtr code,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,int style,out byte choice,out int length,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Attribute(Handle formatter,int attribute);
        sealed record Api(IcuModule Module,Open Open,Close Close,Symbol Symbol,CurrencyName Currency,Attribute Attribute);
        sealed class Handle(IcuModule module,Close close):SafeHandle(IntPtr.Zero,true)
        {
            public override bool IsInvalid=>handle==IntPtr.Zero;
            internal void Initialize(IntPtr value)=>SetHandle(value);
            protected override bool ReleaseHandle(){try{using var thread=module.Enter();close(handle);return true;}catch{return false;}}
        }
        static readonly Lazy<Api> Backend=new(()=>PlatformIcu.Load(Bind));
        static Api Bind(IcuModule module)
        {
            if(!module.Function<Open>("unum_open",out var open) || !module.Function<Close>("unum_close",out var close) || !module.Function<Symbol>("unum_getSymbol",out var symbol) || !module.Function<CurrencyName>("ucurr_getName",out var currency) || !module.Function<Attribute>("unum_getAttribute",out var attribute))return null;
            return new(module,open,close,symbol,currency,attribute);
        }
        static void Check(int error){if(error>0)throw new InvalidOperationException("Native number symbols failed: ICU status "+error);}
        internal static bool Needs(CultureInfo culture,string system)=>system is "arab" or "arabext" || culture.NumberFormat.NumberDecimalSeparator=="\u066b" || culture.TwoLetterISOLanguageName is "ar" or "fa" or "bn" or "mr" or "my" or "ne" or "pa";
        internal static bool Patterns(CultureInfo culture,string system)=>Needs(culture,system) && Backend.Value!=null;
        internal static string CurrencySymbol(string locale,string code,bool narrow)
        {
            var api=Backend.Value;if(api==null)return null;
            using var thread=api.Module.Enter();int error=0;
            // ICU may return the input buffer for unknown codes. Own that buffer
            // until its result has been copied, including across a possible GC.
            IntPtr input=Marshal.StringToHGlobalUni(code);
            try {
                IntPtr value=api.Currency(input,locale.Replace('-','_'),narrow ? 2 : 0,out byte choice,out int length,ref error);Check(error);
                if(value==IntPtr.Zero || length<0 || length>1024 || choice!=0)throw new InvalidOperationException("Invalid native currency symbol");
                return value==input ? code : Marshal.PtrToStringUni(value,length);
            }finally{Marshal.FreeHGlobal(input);}
        }
        internal static int[] Groups(string locale,string system,int style)
        {
            if(locale=="ne" || locale.StartsWith("ne-",StringComparison.Ordinal))return style==3 && system!="deva" || style==2 && system=="arab" ? new[]{3} : new[]{3,2};
            var api=Backend.Value;if(api==null)return null;
            using var thread=api.Module.Enter();int error=0;using var formatter=new Handle(api.Module,api.Close);
            formatter.Initialize(api.Open(style,IntPtr.Zero,0,locale.Replace('-','_')+"@numbers="+system,IntPtr.Zero,ref error));Check(error);
            if(formatter.IsInvalid)throw new InvalidOperationException("Native grouping formatter was not created");
            int first=api.Attribute(formatter,10),second=api.Attribute(formatter,15);
            if(first<0 || first>9 || second<0 || second>9)throw new InvalidOperationException("Invalid native grouping sizes");
            return first==0 || second==0 || first==second ? new[]{first} : new[]{first,second};
        }
        sealed record SymbolData(string[] Values,int[] Groups);
        static readonly object SymbolGate=new();
        static readonly Dictionary<(string Locale,string System),SymbolData> SymbolCache=new();
        static SymbolData Data(Api api,string locale,string system)
        {
            lock(SymbolGate) {
                if(SymbolCache.TryGetValue((locale,system),out var cached))return cached;
                using var thread=api.Module.Enter();int error=0;using var formatter=new Handle(api.Module,api.Close);
                formatter.Initialize(api.Open(1,IntPtr.Zero,0,locale.Replace('-','_')+"@numbers="+system,IntPtr.Zero,ref error));Check(error);
                if(formatter.IsInvalid)throw new InvalidOperationException("Native number symbol formatter was not created");
                var output=new char[32];var values=new string[18];
                foreach(int field in new[]{0,1,3,6,7,10,11,14,15,17}) {
                    int status=0,length=api.Symbol(formatter,field,output,output.Length,ref status);
                    if(status==15){if(length<0 || length>1024)throw new InvalidOperationException("Native number symbol is too long");output=new char[length+1];status=0;length=api.Symbol(formatter,field,output,output.Length,ref status);}
                    Check(status);if(length<0 || length>output.Length)throw new InvalidOperationException("Invalid native number symbol length");values[field]=new string(output,0,length);
                }
                var data=new SymbolData(values,Groups(locale,system,1));
                // Retain copied locale data, never mutable CultureInfo objects or native handles.
                if(SymbolCache.Count>=64)SymbolCache.Clear();SymbolCache.Add((locale,system),data);return data;
            }
        }
        internal static CultureInfo Configure(CultureInfo culture,string system,out string exponent)
        {
            exponent="E";if(!Needs(culture,system))return culture;
            var result=(CultureInfo)culture.Clone();var info=result.NumberFormat;
            var api=Backend.Value;
            if(api==null) {
                // Keep a usable decimal path on platforms without system ICU.
                bool arabic=system is "arab" or "arabext";
                info.NumberDecimalSeparator=info.CurrencyDecimalSeparator=info.PercentDecimalSeparator=arabic ? "\u066b" : ".";
                info.NumberGroupSeparator=info.CurrencyGroupSeparator=info.PercentGroupSeparator=arabic ? "\u066c" : ",";
                return result;
            }
            var data=Data(api,culture.Name,system);
            string Read(int field)=>data.Values[field];
            info.NumberGroupSizes=data.Groups ?? info.NumberGroupSizes;
            info.NumberDecimalSeparator=info.PercentDecimalSeparator=Read(0);
            info.NumberGroupSeparator=info.PercentGroupSeparator=Read(1);
            info.CurrencyDecimalSeparator=Read(10);info.CurrencyGroupSeparator=Read(17);
            exponent=Read(11);if(culture.TwoLetterISOLanguageName=="fa" && system=="arab")exponent="E";if(culture.TwoLetterISOLanguageName=="ar" && system=="arab" && exponent=="\u0627\u0633")exponent="\u0623\u0633";
            info.PositiveSign=Read(7);info.NegativeSign=Read(6);info.PercentSymbol=Read(3);
            info.NaNSymbol=Read(15);info.PositiveInfinitySymbol=Read(14);info.NegativeInfinitySymbol=info.NegativeSign+info.PositiveInfinitySymbol;
            if(culture.TwoLetterISOLanguageName=="ar" && info.NaNSymbol is "NaN" or "\u0644\u064a\u0633\u00a0\u0631\u0642\u0645")info.NaNSymbol="\u0644\u064a\u0633\u00a0\u0631\u0642\u0645\u064b\u0627";
            if(culture.TwoLetterISOLanguageName=="fa" && info.NaNSymbol=="NaN")info.NaNSymbol="\u0646\u0627\u0639\u062f\u062f";
            return result;
        }
    }
}
