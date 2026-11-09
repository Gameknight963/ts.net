using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

public static partial class Js
{
    sealed class NativeDateIntervals
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr Open([MarshalAs(UnmanagedType.LPUTF8Str)] string locale, [MarshalAs(UnmanagedType.LPWStr)] string skeleton, int length, [MarshalAs(UnmanagedType.LPWStr)] string zone, int zoneLength, ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close(IntPtr handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int FormatText(Handle formatter, double start, double end, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.U2)] char[] buffer, int capacity, IntPtr position, ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr OpenDate(int timeStyle, int dateStyle, [MarshalAs(UnmanagedType.LPUTF8Str)] string locale, [MarshalAs(UnmanagedType.LPWStr)] string zone, int zoneLength, IntPtr pattern, int patternLength, ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int Pattern(Handle date, byte localized, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.U2)] char[] buffer, int capacity, ref int error);
        sealed record Api(IcuModule Module, Open Open, Close Close, FormatText Text, OpenDate Date, Close CloseDate, Pattern Pattern);
        sealed class Handle(IcuModule module, Close close) : SafeHandle(IntPtr.Zero,true)
        {
            public override bool IsInvalid => handle == IntPtr.Zero;
            internal void Initialize(IntPtr value) => SetHandle(value);
            protected override bool ReleaseHandle() {try {using var thread=module.Enter();close(handle);return true;}catch{return false;}}
        }
        static readonly Lazy<Api> Backend = new(() => PlatformIcu.Load(Bind));
        static Api Bind(IcuModule module)
        {
            if (!module.Function<Open>("udtitvfmt_open",out var open) || !module.Function<Close>("udtitvfmt_close",out var close) ||
                !module.Function<FormatText>("udtitvfmt_format",out var formatText) || !module.Function<OpenDate>("udat_open",out var date) ||
                !module.Function<Close>("udat_close",out var closeDate) || !module.Function<Pattern>("udat_toPattern",out var pattern)) return null;
            return new(module,open,close,formatText,date,closeDate,pattern);
        }
        readonly Api api;
        readonly Handle formatter;
        readonly object gate = new();
        static void Check(int error) {if(error>0)throw new InvalidOperationException("Native date range formatting failed: ICU status "+error);}
        internal NativeDateIntervals(TsObject options, string culture)
        {
            api=Backend.Value ?? throw new NotSupportedException("Date ranges require system ICU");
            string Read(string key) => options.Get(key) as string;
            string locale=culture.Replace('-','_')+"@calendar="+NativeDateFormatting.IcuCalendar(Read("calendar"))+";numbers="+Read("numberingSystem");
            string zone=Read("timeZone");
            if(zone.StartsWith('+') || zone.StartsWith('-'))zone="GMT"+zone;
            else if(System.TimeZoneInfo.TryConvertWindowsIdToIanaId(zone,out var iana))zone=iana;
            using var thread=api.Module.Enter();
            int error=0;
            string skeleton;
            string hour=Read("hourCycle") switch {"h11"=>"K","h12"=>"h","h23"=>"H","h24"=>"k",_=>"j"};
            if(Read("dateStyle")!=null || Read("timeStyle")!=null) {
                int Style(string key)=>Read(key) switch {"full"=>0,"long"=>1,"medium"=>2,"short"=>3,_=>-1};
                using var date=new Handle(api.Module,api.CloseDate);
                date.Initialize(api.Date(Style("timeStyle"),Style("dateStyle"),locale,zone,zone.Length,IntPtr.Zero,0,ref error));Check(error);
                if(date.IsInvalid)throw new InvalidOperationException("Native date formatter was not created");
                var buffer=new char[256];int length=api.Pattern(date,0,buffer,buffer.Length,ref error);
                if(error==15){error=0;buffer=new char[length+1];length=api.Pattern(date,0,buffer,buffer.Length,ref error);}Check(error);
                var fields=new StringBuilder();bool quoted=false;
                for(int index=0;index<length;index++) {
                    char ch=buffer[index];
                    if(ch=='\''){if(index+1<length && buffer[index+1]=='\''){index++;continue;}quoted=!quoted;continue;}
                    if(quoted || !char.IsAsciiLetter(ch))continue;
                    if(ch is 'h' or 'H' or 'K' or 'k')fields.Append(hour=="j"?ch:hour[0]);
                    else if(ch!='a' || hour is not ("H" or "k"))fields.Append(ch);
                }
                skeleton=fields.ToString();
            } else {
                var fields=new StringBuilder();
                void Width(string key,string numeric,string two,string narrow=null,string shortName=null,string longName=null) {
                    string width=Read(key);fields.Append(width switch {"numeric"=>numeric,"2-digit"=>two,"narrow"=>narrow,"short"=>shortName,"long"=>longName,_=>null});
                }
                Width("era",null,null,"GGGGG","G","GGGG");
                string year=Read("calendar") is "chinese" or "dangi" ? "r" : "y";
                Width("year",year,year+year);Width("month","M","MM","MMMMM","MMM","MMMM");
                Width("day","d","dd");Width("weekday",null,null,"EEEEE","EEE","EEEE");
                Width("hour",hour,hour+hour);Width("minute","m","mm");Width("second","s","ss");
                Width("dayPeriod",null,null,"BBBBB","B","BBBB");
                if(options.Get("fractionalSecondDigits") is double digits)fields.Append('S',(int)digits);
                fields.Append(Read("timeZoneName") switch {"short"=>"z","long"=>"zzzz","shortOffset"=>"O","longOffset"=>"OOOO","shortGeneric"=>"v","longGeneric"=>"vvvv",_=>null});
                skeleton=fields.ToString();
            }
            formatter=new Handle(api.Module,api.Close);
            try {formatter.Initialize(api.Open(locale,skeleton,skeleton.Length,zone,zone.Length,ref error));Check(error);if(formatter.IsInvalid)throw new InvalidOperationException("Native date interval formatter was not created");}
            catch {formatter.Dispose();throw;}
        }
        internal string Range(double start,double end)
        {
            lock(gate) {
                using var thread=api.Module.Enter();int error=0;
                var buffer=ArrayPool<char>.Shared.Rent(256);
                try {
                    int count=api.Text(formatter,start,end,buffer,buffer.Length,IntPtr.Zero,ref error);
                    if(error==15){error=0;var larger=ArrayPool<char>.Shared.Rent(checked(count+1));ArrayPool<char>.Shared.Return(buffer);buffer=larger;count=api.Text(formatter,start,end,buffer,buffer.Length,IntPtr.Zero,ref error);}Check(error);
                    if(count<0 || count>buffer.Length)throw new InvalidOperationException("Invalid native date interval length");
                    return new string(buffer,0,count);
                }
                finally {ArrayPool<char>.Shared.Return(buffer);}
            }
        }
    }
}
