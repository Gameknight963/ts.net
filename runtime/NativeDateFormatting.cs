using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static partial class Js
{
    sealed class NativeDateFormatting
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Open(int timeStyle,int dateStyle,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,[MarshalAs(UnmanagedType.LPWStr)] string zone,int zoneLength,[MarshalAs(UnmanagedType.LPWStr)] string pattern,int patternLength,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close(IntPtr value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Pattern(Handle date,byte localized,[Out,MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.U2)] char[] output,int capacity,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Generator([MarshalAs(UnmanagedType.LPUTF8Str)] string locale,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int BestPattern(Handle generator,[MarshalAs(UnmanagedType.LPWStr)] string skeleton,int length,int options,[Out,MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.U2)] char[] output,int capacity,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FormatText(Handle date,double timestamp,[Out,MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.U2)] char[] output,int capacity,IntPtr fields,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FormatFields(Handle date,double timestamp,[Out,MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.U2)] char[] output,int capacity,Handle fields,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr GetCalendar(Handle date);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr CloneCalendar(IntPtr calendar,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void SetCalendar(Handle date,Handle calendar);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void GregorianChange(Handle calendar,double change,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr OpenFields(ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int NextField(Handle fields,out int start,out int end);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr CalendarValues([MarshalAs(UnmanagedType.LPUTF8Str)] string key,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,byte common,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr NextValue(Handle values,out int length,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void CalendarTime(Handle calendar,double timestamp,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int CalendarField(Handle calendar,int field,ref int error);
        sealed record Api(IcuModule Module,Open Open,Close Close,Pattern Pattern,Generator Generator,Close CloseGenerator,BestPattern Best,FormatText Text,FormatFields Fields,GetCalendar Calendar,CloneCalendar CopyCalendar,SetCalendar SetCalendar,Close CloseCalendar,GregorianChange Change,OpenFields OpenFields,Close CloseFields,NextField Next,CalendarValues Values,NextValue NextValue,Close CloseValues,CalendarTime Time,CalendarField Field);
        sealed class Handle(IcuModule module,Close close) : SafeHandle(IntPtr.Zero,true)
        {
            public override bool IsInvalid=>handle==IntPtr.Zero;
            internal void Initialize(IntPtr value)=>SetHandle(value);
            protected override bool ReleaseHandle(){try{using var thread=module.Enter();close(handle);return true;}catch{return false;}}
        }
        static readonly Lazy<Api> Backend=new(()=>PlatformIcu.Load(Bind));
        static readonly Lazy<string[]> CalendarNames=new(ReadCalendars);
        internal static string[] Calendars=>Backend.Value==null ? null : CalendarNames.Value;
        internal static string IcuCalendar(string value)=>value switch{"gregory"=>"gregorian","ethioaa"=>"ethiopic-amete-alem",_=>value};
        static string[] ReadCalendars()
        {
            var api=Backend.Value;using var thread=api.Module.Enter();int error=0;
            using var values=new Handle(api.Module,api.CloseValues);values.Initialize(api.Values("calendar","en_US",0,ref error));Check(error);
            if(values.IsInvalid)throw new InvalidOperationException("Native calendar inventory was not created");
            var names=new SortedSet<string>(StringComparer.Ordinal){"iso8601"};
            for(;;) {
                IntPtr next=api.NextValue(values,out int length,ref error);Check(error);if(next==IntPtr.Zero)break;
                if(length<1 || length>256)throw new InvalidOperationException("Invalid native calendar identifier");
                string name=Marshal.PtrToStringUTF8(next,length) switch{"gregorian"=>"gregory","ethiopic-amete-alem"=>"ethioaa","islamicc"=>"islamic-civil",var other=>other};names.Add(name);
            }
            return [..names];
        }
        static Api Bind(IcuModule module)
        {
            if(!module.Function<Open>("udat_open",out var open) || !module.Function<Close>("udat_close",out var close) || !module.Function<Pattern>("udat_toPattern",out var pattern) ||
                !module.Function<Generator>("udatpg_open",out var generator) || !module.Function<Close>("udatpg_close",out var closeGenerator) || !module.Function<BestPattern>("udatpg_getBestPatternWithOptions",out var best) ||
                !module.Function<FormatText>("udat_format",out var text) || !module.Function<FormatFields>("udat_formatForFields",out var fields) ||
                !module.Function<GetCalendar>("udat_getCalendar",out var calendar) || !module.Function<CloneCalendar>("ucal_clone",out var copyCalendar) ||
                !module.Function<SetCalendar>("udat_setCalendar",out var setCalendar) || !module.Function<Close>("ucal_close",out var closeCalendar) || !module.Function<GregorianChange>("ucal_setGregorianChange",out var change) ||
                !module.Function<OpenFields>("ufieldpositer_open",out var openFields) || !module.Function<Close>("ufieldpositer_close",out var closeFields) || !module.Function<NextField>("ufieldpositer_next",out var next) ||
                !module.Function<CalendarValues>("ucal_getKeywordValuesForLocale",out var values) || !module.Function<NextValue>("uenum_next",out var nextValue) || !module.Function<Close>("uenum_close",out var closeValues) ||
                !module.Function<CalendarTime>("ucal_setMillis",out var time) || !module.Function<CalendarField>("ucal_get",out var field))return null;
            return new(module,open,close,pattern,generator,closeGenerator,best,text,fields,calendar,copyCalendar,setCalendar,closeCalendar,change,openFields,closeFields,next,values,nextValue,closeValues,time,field);
        }
        readonly Api api;
        readonly Handle formatter;
        readonly bool checkAncientMonth;
        internal readonly string MonthWidth;
        readonly object gate=new();
        delegate int Write(char[] output,ref int error);
        static void Check(int error){if(error>0)throw new InvalidOperationException("Native date formatting failed: ICU status "+error);}
        static string Read(Write write)
        {
            char[] output=ArrayPool<char>.Shared.Rent(256);
            try {
                int error=0,length=write(output,ref error);
                if(error==15) {
                    if(length<0 || length>10000)throw new InvalidOperationException("Native date text is too long");
                    var larger=ArrayPool<char>.Shared.Rent(length+1);ArrayPool<char>.Shared.Return(output);output=larger;error=0;length=write(output,ref error);
                }
                Check(error);if(length<0 || length>output.Length)throw new InvalidOperationException("Invalid native date text length");return new string(output,0,length);
            }finally{ArrayPool<char>.Shared.Return(output);}
        }
        internal NativeDateFormatting(TsObject options,string culture)
        {
            api=Backend.Value ?? throw new NotSupportedException("Extended locale date formatting requires system ICU");
            string Option(string key)=>options.Get(key) as string;
            string calendar=Option("calendar"),locale=culture.Replace('-','_')+"@calendar="+IcuCalendar(calendar)+";numbers="+Option("numberingSystem");
            checkAncientMonth=calendar is "indian" or "islamic" or "islamic-rgsa";
            string zone=Option("timeZone");if(zone.StartsWith('+') || zone.StartsWith('-'))zone="GMT"+zone;
            else if(TimeZoneInfo.TryConvertWindowsIdToIanaId(zone,out var iana))zone=iana;
            string hour=Option("hourCycle") switch{"h11"=>"K","h12"=>"h","h23"=>"H","h24"=>"k",_=>"j"};
            using var thread=api.Module.Enter();int error=0;string pattern;
            bool styled=Option("dateStyle")!=null || Option("timeStyle")!=null;
            string Best(string skeleton) {
                using var generator=new Handle(api.Module,api.CloseGenerator);int status=0;
                generator.Initialize(api.Generator(locale,ref status));Check(status);if(generator.IsInvalid)throw new InvalidOperationException("Native date pattern generator was not created");
                return Read((char[] output,ref int result)=>api.Best(generator,skeleton,skeleton.Length,1<<11,output,output.Length,ref result));
            }
            if(styled) {
                int Style(string key)=>Option(key) switch{"full"=>0,"long"=>1,"medium"=>2,"short"=>3,_=>-1};
                using var date=new Handle(api.Module,api.Close);
                date.Initialize(api.Open(Style("timeStyle"),Style("dateStyle"),locale,zone,zone.Length,null,0,ref error));Check(error);
                if(date.IsInvalid)throw new InvalidOperationException("Native styled date formatter was not created");
                pattern=Read((char[] output,ref int result)=>api.Pattern(date,0,output,output.Length,ref result));
                if(hour!="j") {
                    var skeleton=new StringBuilder();bool quoted=false,changed=false;
                    for(int i=0;i<pattern.Length;i++) {
                        char ch=pattern[i];if(ch=='\''){if(i+1<pattern.Length && pattern[i+1]=='\''){i++;continue;}quoted=!quoted;continue;}
                        if(quoted || !char.IsAsciiLetter(ch))continue;
                        if(ch is 'h' or 'H' or 'K' or 'k'){skeleton.Append(hour);changed|=ch!=hour[0];}
                        else if(ch!='a' || hour is not ("H" or "k"))skeleton.Append(ch);
                    }
                    if(changed)pattern=Best(skeleton.ToString());
                }
            }else{
                var skeleton=new StringBuilder();
                void Width(string key,string numeric,string two,string narrow=null,string shortName=null,string longName=null)=>skeleton.Append(Option(key) switch{"numeric"=>numeric,"2-digit"=>two,"narrow"=>narrow,"short"=>shortName,"long"=>longName,_=>null});
                Width("era",null,null,"GGGGG","G","GGGG");Width("year","y","yy");Width("month","M","MM","MMMMM","MMM","MMMM");Width("day","d","dd");Width("weekday",null,null,"EEEEE","EEE","EEEE");
                Width("hour",hour,hour+hour);Width("minute","m","mm");Width("second","s","ss");
                if(hour is not ("H" or "k"))Width("dayPeriod",null,null,"BBBBB","B","BBBB");
                if(options.Get("fractionalSecondDigits") is double digits)skeleton.Append('S',(int)digits);
                skeleton.Append(Option("timeZoneName") switch{"short"=>"z","long"=>"zzzz","shortOffset"=>"O","longOffset"=>"OOOO","shortGeneric"=>"v","longGeneric"=>"vvvv",_=>null});
                pattern=Best(skeleton.ToString());
                if(calendar=="iso8601" && Option("year")!=null && Option("month") is "numeric" or "2-digit" && Option("day")!=null && Option("weekday")==null && Option("era")==null) {
                    int first=-1,last=0;bool quoted=false;
                    for(int i=0;i<pattern.Length;i++) {
                        char ch=pattern[i];if(ch=='\''){if(i+1<pattern.Length && pattern[i+1]=='\''){i++;continue;}quoted=!quoted;continue;}
                        if(!quoted && ch is 'y' or 'M' or 'd'){if(first<0)first=i;last=i+1;}
                    }
                    if(first>=0)pattern=pattern[..first]+(Option("year")=="2-digit" ? "yy" : "y")+"-MM-dd"+pattern[last..];
                }
            }
            if(hour!="j") {
                var corrected=new StringBuilder(pattern.Length);bool quoted=false;
                for(int i=0;i<pattern.Length;i++) {
                    char ch=pattern[i];if(ch=='\''){if(i+1<pattern.Length && pattern[i+1]=='\''){corrected.Append("''");i++;continue;}quoted=!quoted;}
                    corrected.Append(!quoted && ch is 'h' or 'H' or 'K' or 'k' ? hour[0] : ch);
                }
                pattern=corrected.ToString();
            }
            bool quotedField=false;
            for(int i=0;i<pattern.Length;i++) {
                char ch=pattern[i];if(ch=='\''){if(i+1<pattern.Length && pattern[i+1]=='\''){i++;continue;}quotedField=!quotedField;continue;}
                if(!quotedField && ch is 'M' or 'L') {
                    int width=1;while(i+1<pattern.Length && pattern[i+1]==ch){width++;i++;}
                    MonthWidth=width switch{1=>"numeric",2=>"2-digit",3=>"short",4=>"long",_=>"narrow"};
                }
            }
            formatter=new Handle(api.Module,api.Close);
            try {
                formatter.Initialize(api.Open(-2,-2,locale,zone,zone.Length,pattern,pattern.Length,ref error));Check(error);
                if(formatter.IsInvalid)throw new InvalidOperationException("Native date formatter was not created");
                if(calendar is "gregory" or "iso8601") {
                    using var copy=new Handle(api.Module,api.CloseCalendar);copy.Initialize(api.CopyCalendar(api.Calendar(formatter),ref error));Check(error);
                    if(copy.IsInvalid)throw new InvalidOperationException("Native date calendar was not copied");
                    api.Change(copy,-1e16,ref error);Check(error);api.SetCalendar(formatter,copy);
                }
            }catch{formatter.Dispose();throw;}
        }
        internal string Text(double timestamp)
        {
            lock(gate){using var thread=api.Module.Enter();ValidateCalendar(timestamp);return Read((char[] output,ref int error)=>api.Text(formatter,timestamp,output,output.Length,IntPtr.Zero,ref error));}
        }
        void ValidateCalendar(double timestamp)
        {
            if(!checkAncientMonth || timestamp>=0)return;
            using var copy=new Handle(api.Module,api.CloseCalendar);int error=0;
            copy.Initialize(api.CopyCalendar(api.Calendar(formatter),ref error));GC.KeepAlive(formatter);Check(error);
            if(copy.IsInvalid)throw new InvalidOperationException("Native calendar was not copied");
            api.Time(copy,timestamp,ref error);Check(error);int month=api.Field(copy,2,ref error);Check(error);
            if(month is <0 or >11)throw new NotSupportedException("System ICU produced an invalid month for this historical calendar date");
        }
        internal void CheckCalendar(double timestamp)
        {
            if(!checkAncientMonth || timestamp>=0)return;
            lock(gate){using var thread=api.Module.Enter();ValidateCalendar(timestamp);}
        }
        internal long LocalDay(double timestamp)
        {
            lock(gate) {
                using var thread=api.Module.Enter();using var copy=new Handle(api.Module,api.CloseCalendar);int error=0;
                copy.Initialize(api.CopyCalendar(api.Calendar(formatter),ref error));GC.KeepAlive(formatter);Check(error);
                if(copy.IsInvalid)throw new InvalidOperationException("Native calendar was not copied");
                api.Time(copy,timestamp,ref error);Check(error);
                int standard=api.Field(copy,15,ref error),daylight=api.Field(copy,16,ref error);Check(error);
                long local=checked((long)timestamp+standard+daylight);
                return local>=0 ? local/86400000 : (local-86399999)/86400000;
            }
        }
        internal JsArray Parts(double timestamp)
        {
            lock(gate) {
                using var thread=api.Module.Enter();ValidateCalendar(timestamp);int error=0;using var fields=new Handle(api.Module,api.CloseFields);
                fields.Initialize(api.OpenFields(ref error));Check(error);if(fields.IsInvalid)throw new InvalidOperationException("Native date fields were not created");
                string text=Read((char[] output,ref int status)=>api.Fields(formatter,timestamp,output,output.Length,fields,ref status));
                string[] types=new string[text.Length];
                for(;;) {
                    int field=api.Next(fields,out int start,out int end);if(field<0)break;
                    if(start<0 || end<start || end>text.Length)throw new InvalidOperationException("Invalid native date field");
                    string type=field switch{0=>"era",1 or 18 or 20=>"year",2 or 26=>"month",3=>"day",4 or 5 or 15 or 16=>"hour",6=>"minute",7=>"second",8=>"fractionalSecond",9 or 19 or 25=>"weekday",14 or 35 or 36=>"dayPeriod",17 or 23 or 24 or 29 or 31 or 32 or 33=>"timeZoneName",30=>"yearName",34=>"relatedYear",_=>null};
                    if(type!=null)for(int i=start;i<end;i++)types[i]=type;
                }
                var result=new JsArray(0);
                for(int i=0;i<text.Length;) {
                    int end=i+1;while(end<text.Length && types[end]==types[i])end++;
                    var part=new JsDynamicObject();part.DefineOwn("type",types[i]??"literal");part.DefineOwn("value",text[i..end]);result.Append(part);i=end;
                }
                return result;
            }
        }
    }
}
