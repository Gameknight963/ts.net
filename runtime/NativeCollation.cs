using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static partial class Js
{
    sealed class NativeCollation
    {
        enum Attribute {AlternateHandling=1,CaseFirst=2,CaseLevel=3,Normalization=4,Strength=5,Numeric=7}
        enum Value {Primary=0,Secondary=1,Tertiary=2,Off=16,On=17,Shifted=20,NonIgnorable=21,LowerFirst=24,UpperFirst=25}
        const int PunctuationGroup=0x1001;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Open([MarshalAs(UnmanagedType.LPUTF8Str)] string locale,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close(IntPtr handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int CompareText(Handle collator,[MarshalAs(UnmanagedType.LPWStr)] string first,int firstLength,[MarshalAs(UnmanagedType.LPWStr)] string second,int secondLength);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void SetAttribute(Handle collator,Attribute attribute,Value value,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate Value GetAttribute(Handle collator,Attribute attribute,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void SetVariable(Handle collator,int group,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr OpenValues([MarshalAs(UnmanagedType.LPUTF8Str)] string key,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr OpenLocaleValues([MarshalAs(UnmanagedType.LPUTF8Str)] string key,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,byte common,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr NextValue(Handle enumeration,out int length,ref int error);
        sealed record Api(IcuModule Module,Open Open,Close Close,CompareText Compare,SetAttribute Set,GetAttribute Get,SetVariable Variable,OpenValues Values,OpenLocaleValues LocaleValues,NextValue Next,Close CloseValues);
        sealed class Handle(IcuModule module,Close close) : SafeHandle(IntPtr.Zero,true)
        {
            public override bool IsInvalid=>handle==IntPtr.Zero;
            internal void Initialize(IntPtr value)=>SetHandle(value);
            protected override bool ReleaseHandle(){try{using var thread=module.Enter();close(handle);return true;}catch{return false;}}
        }
        static readonly Lazy<Api> Backend=new(()=>PlatformIcu.Load(Bind));
        static readonly Dictionary<string,(string[] Values,string Default)> Names=new(StringComparer.Ordinal);
        static readonly Dictionary<(string Locale,string Collation,string Usage,string Sensitivity,string CaseFirst,bool Numeric,bool Punctuation),NativeCollation> Cache=new();
        internal static bool Available=>Backend.Value!=null;
        static Api Bind(IcuModule module)
        {
            if(!module.Function<Open>("ucol_open",out var open) || !module.Function<Close>("ucol_close",out var close) ||
                !module.Function<CompareText>("ucol_strcoll",out var compare) || !module.Function<SetAttribute>("ucol_setAttribute",out var set) ||
                !module.Function<GetAttribute>("ucol_getAttribute",out var get) || !module.Function<SetVariable>("ucol_setMaxVariable",out var variable) ||
                !module.Function<OpenValues>("ucol_getKeywordValues",out var values) || !module.Function<OpenLocaleValues>("ucol_getKeywordValuesForLocale",out var locales) ||
                !module.Function<NextValue>("uenum_next",out var next) || !module.Function<Close>("uenum_close",out var closeValues))return null;
            return new(module,open,close,compare,set,get,variable,values,locales,next,closeValues);
        }
        static void Check(int error){if(error>0)throw new InvalidOperationException("Native collation failed: ICU status "+error);}
        static string ToIcu(string value)=>value switch{"phonebk"=>"phonebook","trad"=>"traditional","dict"=>"dictionary","gb2312"=>"gb2312han",_=>value};
        static string FromIcu(string value)=>value switch{"phonebook"=>"phonebk","traditional"=>"trad","dictionary"=>"dict","gb2312han"=>"gb2312",_=>value};
        internal static string[] Values(string locale=null)
        {
            lock(Names) {
                string key=locale??"\0";
                if(Names.TryGetValue(key,out var cached))return cached.Values;
                var api=Backend.Value ?? throw new NotSupportedException("Collation inventories require system ICU");
                using var thread=api.Module.Enter();int error=0;
                using var enumeration=new Handle(api.Module,api.CloseValues);
                enumeration.Initialize(locale==null ? api.Values("collation",ref error) : api.LocaleValues("collation",locale.Replace('-','_'),0,ref error));Check(error);
                if(enumeration.IsInvalid)throw new InvalidOperationException("Native collation inventory was not created");
                var names=new SortedSet<string>(StringComparer.Ordinal);
                string defaultName=null;
                for(;;) {
                    IntPtr pointer=api.Next(enumeration,out int length,ref error);Check(error);if(pointer==IntPtr.Zero)break;
                    if(length<0 || length>256)throw new InvalidOperationException("Invalid native collation name length");
                    string value=FromIcu(Marshal.PtrToStringUTF8(pointer,length));
                    defaultName??=value;
                    if(value is not ("standard" or "search" or "default" or "big5han" or "direct" or "gb2312" or "reformed"))names.Add(value);
                }
                // Some system shims omit inherited root collations from the global inventory.
                if(locale==null)foreach(string value in Values(""))names.Add(value);
                var result=new string[names.Count];names.CopyTo(result);
                if(Names.Count>=128)Names.Clear();Names[key]=(result,defaultName??"standard");return result;
            }
        }
        internal static bool IsDefault(string locale,string collation)
        {
            lock(Names){Values(locale);return Names[locale].Default==collation;}
        }
        internal static NativeCollation Get(string locale,string collation,string usage,string sensitivity,string caseFirst,bool numeric,bool punctuation)
        {
            lock(Cache) {
                var key=(locale,collation,usage,sensitivity,caseFirst,numeric,punctuation);
                if(Cache.TryGetValue(key,out var value))return value;
                value=new NativeCollation(locale,collation,usage,sensitivity,caseFirst,numeric,punctuation);
                if(Cache.Count>=64)Cache.Clear();Cache[key]=value;return value;
            }
        }
        readonly Api api;
        readonly Handle handle;
        internal readonly string CaseFirst;
        NativeCollation(string locale,string collation,string usage,string sensitivity,string caseFirst,bool numeric,bool punctuation)
        {
            api=Backend.Value ?? throw new NotSupportedException("Collation requires system ICU");
            string name=locale.Replace('-','_');
            if(usage=="search")name+="@collation=search";
            else if(collation!="default")name+="@collation="+ToIcu(collation);
            handle=new Handle(api.Module,api.Close);
            try {
                using var thread=api.Module.Enter();int error=0;
                handle.Initialize(api.Open(name,ref error));Check(error);if(handle.IsInvalid)throw new InvalidOperationException("Native collator was not created");
                void Set(Attribute attribute,Value value){api.Set(handle,attribute,value,ref error);Check(error);}
                Set(Attribute.Strength,sensitivity is "base" or "case" ? Value.Primary : sensitivity=="accent" ? Value.Secondary : Value.Tertiary);
                Set(Attribute.CaseLevel,sensitivity=="case" ? Value.On : Value.Off);Set(Attribute.Normalization,Value.On);Set(Attribute.Numeric,numeric ? Value.On : Value.Off);
                Set(Attribute.AlternateHandling,punctuation ? Value.Shifted : Value.NonIgnorable);api.Variable(handle,PunctuationGroup,ref error);Check(error);
                if(caseFirst!=null)Set(Attribute.CaseFirst,caseFirst=="upper" ? Value.UpperFirst : caseFirst=="lower" ? Value.LowerFirst : Value.Off);
                Value order=api.Get(handle,Attribute.CaseFirst,ref error);Check(error);CaseFirst=order==Value.UpperFirst ? "upper" : order==Value.LowerFirst ? "lower" : "false";
            }catch{handle.Dispose();throw;}
        }
        internal double Compare(string first,string second)
        {
            using var thread=api.Module.Enter();return Math.Sign(api.Compare(handle,first,first.Length,second,second.Length));
        }
    }
}
