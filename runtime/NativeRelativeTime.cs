using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static partial class Js
{
    sealed class NativeRelativeTime
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Open([MarshalAs(UnmanagedType.LPUTF8Str)] string locale,Handle number,int width,int context,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close(IntPtr handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Text(Handle formatter,double value,int unit,[Out,MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.U2)] char[] output,int capacity,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr OpenNumber(int style,IntPtr pattern,int patternLength,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,IntPtr parseError,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int NumberText(Handle formatter,double value,[Out,MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.U2)] char[] output,int capacity,Handle fields,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr CloneNumber(Handle formatter,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void NumberAttribute(Handle formatter,int attribute,int value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr OpenFields(ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int NextField(Handle fields,out int start,out int end);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr OpenSystem([MarshalAs(UnmanagedType.LPUTF8Str)] string locale,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr SystemName(Handle system);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int CountLocales();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr LocaleAt(int index);
        sealed record Api(IcuModule Module,Open Open,Close Close,Text Numeric,Text Automatic,OpenNumber Number,Close CloseNumber,NumberText NumberText,CloneNumber Copy,NumberAttribute Set,OpenFields Fields,Close CloseFields,NextField Next,OpenSystem System,SystemName Name,Close CloseSystem,HashSet<string> Locales);
        sealed class Handle(IcuModule module,Close close) : SafeHandle(IntPtr.Zero,true)
        {
            public override bool IsInvalid=>handle==IntPtr.Zero;
            internal void Initialize(IntPtr value)=>SetHandle(value);
            protected override bool ReleaseHandle(){try{using var thread=module.Enter();close(handle);return true;}catch{return false;}}
        }
        static readonly Lazy<Api> Backend=new(()=>PlatformIcu.Load(Bind));
        static Api Bind(IcuModule module)
        {
            if(!module.Function<Open>("ureldatefmt_open",out var open) || !module.Function<Close>("ureldatefmt_close",out var close) ||
                !module.Function<Text>("ureldatefmt_formatNumeric",out var numeric) || !module.Function<Text>("ureldatefmt_format",out var automatic) ||
                !module.Function<OpenNumber>("unum_open",out var number) || !module.Function<Close>("unum_close",out var closeNumber) ||
                !module.Function<NumberText>("unum_formatDoubleForFields",out var numberText) || !module.Function<OpenFields>("ufieldpositer_open",out var fields) ||
                !module.Function<CloneNumber>("unum_clone",out var clone) || !module.Function<NumberAttribute>("unum_setAttribute",out var set) ||
                !module.Function<Close>("ufieldpositer_close",out var closeFields) || !module.Function<NextField>("ufieldpositer_next",out var next) ||
                !module.Function<OpenSystem>("unumsys_open",out var system) || !module.Function<SystemName>("unumsys_getName",out var name) ||
                !module.Function<Close>("unumsys_close",out var closeSystem) || !module.Function<CountLocales>("uloc_countAvailable",out var count) ||
                !module.Function<LocaleAt>("uloc_getAvailable",out var localeAt))return null;
            using var thread=module.Enter();var locales=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int length=count();if(length<0 || length>10000)throw new InvalidOperationException("Invalid native locale count");
            for(int i=0;i<length;i++) {
                string locale=Marshal.PtrToStringUTF8(localeAt(i));
                if(locale!=null)locales.Add(locale.Replace('_','-'));
            }
            return new(module,open,close,numeric,automatic,number,closeNumber,numberText,clone,set,fields,closeFields,next,system,name,closeSystem,locales);
        }
        internal static string Lookup(string locale)
        {
            var api=Backend.Value;if(api==null)return null;
            while(locale.Length!=0) {
                if(api.Locales.Contains(locale))return locale;
                int end=locale.LastIndexOf('-');if(end<0)break;locale=locale[..end];
            }
            return null;
        }
        readonly Api api;
        readonly Handle formatter,number;
        readonly object gate=new();
        internal readonly string NumberingSystem;
        static void Check(int error){if(error>0)throw new InvalidOperationException("Native relative time failed: ICU status "+error);}
        internal NativeRelativeTime(string locale,string style,string requestedSystem)
        {
            api=Backend.Value ?? throw new NotSupportedException("Relative time formatting requires system ICU");
            using var thread=api.Module.Enter();int error=0;
            string name=locale.Replace('-','_');
            string ResolveSystem(string selected,bool optional=false) {
                using var system=new Handle(api.Module,api.CloseSystem);int status=0;
                system.Initialize(api.System(selected,ref status));if(optional && status is 1 or 16)return null;Check(status);
                if(system.IsInvalid)throw new InvalidOperationException("Native numbering system was not created");
                return Marshal.PtrToStringUTF8(api.Name(system));
            }
            NumberingSystem=ResolveSystem(name);
            if(requestedSystem is "arab" or "arabext" || requestedSystem!=null && Array.IndexOf(IntlValueLists.NumberingSystems,requestedSystem)>=0) {
                string selected=name+"@numbers="+requestedSystem;
                if(ResolveSystem(selected,true)==requestedSystem){NumberingSystem=requestedSystem;name=selected;}
            }
            formatter=new Handle(api.Module,api.Close);number=new Handle(api.Module,api.CloseNumber);
            try {
                number.Initialize(api.Number(1,IntPtr.Zero,0,name,IntPtr.Zero,ref error));Check(error);
                if(number.IsInvalid)throw new InvalidOperationException("Native relative time number formatter was not created");
                api.Set(number,11,6); // UNUM_ROUNDING_MODE: half away from zero.
                api.Set(number,22,-2); // Locale-default minimum grouping digits.
                using var adopted=new Handle(api.Module,api.CloseNumber);
                adopted.Initialize(api.Copy(number,ref error));Check(error);
                if(adopted.IsInvalid)throw new InvalidOperationException("Native number formatter was not cloned");
                formatter.Initialize(api.Open(name,adopted,style=="long" ? 0 : style=="short" ? 1 : 2,0x100,ref error));
                adopted.SetHandleAsInvalid(); // ureldatefmt_open adopts the clone.
                Check(error);if(formatter.IsInvalid)throw new InvalidOperationException("Native relative time formatter was not created");
            }catch{formatter.Dispose();number.Dispose();throw;}
        }
        delegate int Write(char[] buffer,ref int error);
        static string Read(Write write)
        {
            char[] buffer=ArrayPool<char>.Shared.Rent(256);
            try {
                int error=0;int length=write(buffer,ref error);
                if(error==15) {
                    if(length<0 || length>10000)throw new InvalidOperationException("Native relative time result is too long");
                    char[] larger=ArrayPool<char>.Shared.Rent(length+1);ArrayPool<char>.Shared.Return(buffer);buffer=larger;error=0;length=write(buffer,ref error);
                }
                Check(error);if(length<0 || length>buffer.Length)throw new InvalidOperationException("Invalid native relative time result length");
                return new string(buffer,0,length);
            }finally{ArrayPool<char>.Shared.Return(buffer);}
        }
        internal object Format(double value,string unit,bool automatic,bool parts)
        {
            automatic &= double.IsInteger(value);
            int field=unit switch{"year"=>0,"quarter"=>1,"month"=>2,"week"=>3,"day"=>4,"hour"=>5,"minute"=>6,_=>7};
            lock(gate) {
                using var thread=api.Module.Enter();
                string Text(bool useAutomatic)=>Read((char[] output,ref int error)=>(useAutomatic ? api.Automatic : api.Numeric)(formatter,value,field,output,output.Length,ref error));
                string text=Text(automatic);if(!parts)return text;
                var result=new JsArray(0);
                void Part(string type,string contents,bool numeric=false) {
                    if(contents.Length==0)return;
                    var part=new JsDynamicObject();part.DefineOwn("type",type);part.DefineOwn("value",contents);if(numeric)part.DefineOwn("unit",unit);result.Append(part);
                }
                if(automatic && text!=Text(false)){Part("literal",text);return result;}
                using var fields=new Handle(api.Module,api.CloseFields);int error=0;fields.Initialize(api.Fields(ref error));Check(error);
                if(fields.IsInvalid)throw new InvalidOperationException("Native number fields were not created");
                string numberText=Read((char[] output,ref int status)=>api.NumberText(number,Math.Abs(value),output,output.Length,fields,ref status));
                int position=text.IndexOf(numberText,StringComparison.Ordinal);
                if(position<0){Part("literal",text);return result;}
                int[] kinds=new int[numberText.Length];
                for(;;) {
                    int kind=api.Next(fields,out int start,out int end);if(kind<0)break;
                    if(start<0 || end<start || end>kinds.Length)throw new InvalidOperationException("Invalid native number field");
                    int label=kind switch{0=>1,1=>2,2=>3,6=>4,_=>0};
                    for(int i=start;i<end;i++)kinds[i]=Math.Max(kinds[i],label);
                }
                Part("literal",text[..position]);
                for(int i=0;i<kinds.Length;) {
                    int end=i+1;while(end<kinds.Length && kinds[end]==kinds[i])end++;
                    Part(kinds[i] switch{1=>"integer",2=>"fraction",3=>"decimal",4=>"group",_=>"literal"},numberText[i..end],true);i=end;
                }
                Part("literal",text[(position+numberText.Length)..]);return result;
            }
        }
    }
}
