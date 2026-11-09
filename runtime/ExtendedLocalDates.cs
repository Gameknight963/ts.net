using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static partial class Js
{
    static bool ExtendedLocalDate(double timestamp)=>timestamp < -62135510400000d || timestamp > 253402214399999d;
    [ThreadStatic] static TimeZoneInfo localZoneForId;
    [ThreadStatic] static string localZoneId;
    static string LocalDateZone()
    {
        var zone=TimeZoneInfo.Local;
        if(ReferenceEquals(zone,localZoneForId))return localZoneId;
        string name=zone.Id;
        localZoneId=TimeZoneInfo.TryConvertWindowsIdToIanaId(name,out var iana) ? iana : name;
        localZoneForId=zone;return localZoneId;
    }
    readonly record struct LocalDateSnapshot(double Timestamp,TimeZoneInfo Zone,UtcDateParts Fields,int Offset);
    [ThreadStatic] static LocalDateSnapshot lastLocalDate;
    readonly record struct NativeLocalSnapshot(double Timestamp,TimeZoneInfo Zone,DateTimeOffset Value);
    [ThreadStatic] static NativeLocalSnapshot lastNativeLocalDate;
    static DateTimeOffset NativeLocalDate(double timestamp)
    {
        var zone=TimeZoneInfo.Local;
        if(ReferenceEquals(lastNativeLocalDate.Zone,zone) && lastNativeLocalDate.Timestamp==timestamp)return lastNativeLocalDate.Value;
        var value=TimeZoneInfo.ConvertTime(NativeDate(timestamp),zone);
        lastNativeLocalDate=new(timestamp,zone,value);return value;
    }
    static double LocalDateField(double timestamp,string name,Func<DateTimeOffset,double> read)=>ExtendedLocalDate(timestamp)
        ? DatePartsField(LocalDateFields(timestamp).Fields,name) : read(NativeLocalDate(timestamp));
    static LocalDateSnapshot LocalDateFields(double timestamp)
    {
        var zone=TimeZoneInfo.Local;
        if(ReferenceEquals(lastLocalDate.Zone,zone) && lastLocalDate.Timestamp==timestamp)return lastLocalDate;
        int offset=NativeExtendedDates.Offset(timestamp,LocalDateZone()).Offset;
        var fields=ExtendedUtcDateParts(timestamp+offset);
        return lastLocalDate=new(timestamp,zone,fields,offset);
    }
    sealed class NativeExtendedDates
    {
        enum Field {Month=2,Day=5,Hour=11,Minute=12,Second=13,Millisecond=14,ZoneOffset=15,DaylightOffset=16,ExtendedYear=19}
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Open([MarshalAs(UnmanagedType.LPWStr)] string zone,int length,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,int type,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close(IntPtr calendar);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void SetTime(Handle calendar,double value,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate double GetTime(Handle calendar,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int GetField(Handle calendar,Field field,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void SetField(Handle calendar,Field field,int value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Clear(Handle calendar);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Attribute(Handle calendar,int attribute,int value);
        sealed record Api(IcuModule Module,Open Open,Close Close,SetTime SetTime,GetTime GetTime,GetField Get,SetField Set,Clear Clear,Attribute Attribute,SetTime GregorianChange);
        sealed class Handle(Api api) : SafeHandle(IntPtr.Zero,true)
        {
            public override bool IsInvalid=>handle==IntPtr.Zero;
            internal void Initialize(IntPtr value)=>SetHandle(value);
            protected override bool ReleaseHandle(){try{using var thread=api.Module.Enter();api.Close(handle);return true;}catch{return false;}}
        }
        static readonly Lazy<Api> Backend=new(()=>PlatformIcu.Load(Bind));
        static readonly Dictionary<string,NativeExtendedDates> Calendars=new(StringComparer.Ordinal);
        static readonly Dictionary<string,TimeZoneInfo> Zones=new(StringComparer.Ordinal);
        static Api Bind(IcuModule module)
        {
            if(!module.Function<Open>("ucal_open",out var open) || !module.Function<Close>("ucal_close",out var close) ||
                !module.Function<SetTime>("ucal_setMillis",out var setTime) || !module.Function<GetTime>("ucal_getMillis",out var getTime) ||
                !module.Function<GetField>("ucal_get",out var get) || !module.Function<SetField>("ucal_set",out var set) ||
                !module.Function<Clear>("ucal_clear",out var clear) || !module.Function<Attribute>("ucal_setAttribute",out var attribute) ||
                !module.Function<SetTime>("ucal_setGregorianChange",out var change))return null;
            return new(module,open,close,setTime,getTime,get,set,clear,attribute,change);
        }
        readonly Api api;
        readonly Handle calendar;
        readonly object gate=new();
        static void Check(int error){if(error>0)throw new InvalidOperationException("Extended local date failed: ICU status "+error);}
        NativeExtendedDates(string zone)
        {
            api=Backend.Value ?? throw new NotSupportedException("Extended local dates require system ICU");calendar=new Handle(api);
            try {
                using var thread=api.Module.Enter();int error=0;
                calendar.Initialize(api.Open(zone,zone.Length,"en_US@calendar=gregorian",1,ref error));Check(error);
                if(calendar.IsInvalid)throw new InvalidOperationException("Native calendar was not created");
                api.GregorianChange(calendar,-1e16,ref error);Check(error);
                api.Attribute(calendar,3,1); // Repeated wall time: earliest occurrence.
                api.Attribute(calendar,4,0); // Skipped wall time: shift forward through the gap.
            }catch{calendar.Dispose();throw;}
        }
        static NativeExtendedDates ForZone(string zone)
        {
            lock(Calendars) {
                if(Calendars.TryGetValue(zone,out var value))return value;
                value=new NativeExtendedDates(zone);if(Calendars.Count>=16)Calendars.Clear();Calendars[zone]=value;return value;
            }
        }
        static TimeZoneInfo ZoneInfo(string zone)
        {
            if(zone==LocalDateZone())return TimeZoneInfo.Local;
            lock(Zones) {
                if(Zones.TryGetValue(zone,out var value))return value;
                value=TimeZoneInfo.FindSystemTimeZoneById(zone);if(Zones.Count>=16)Zones.Clear();Zones[zone]=value;return value;
            }
        }
        // Gregorian dates and weekdays repeat every 400 years. These years
        // are after historical adjustment rules and within DateTime's range.
        static DateTime FutureReference(double timestamp,DateTimeKind kind)
        {
            var parts=ExtendedUtcDateParts(timestamp);
            return new DateTime(9600+parts.Year%400,parts.Month,parts.Day,parts.Hour,parts.Minute,parts.Second,parts.Millisecond,kind);
        }
        internal static (int Offset,int Daylight) Offset(double timestamp,string zone)
        {
            if(zone is "UTC" or "Etc/UTC" or "GMT")return (0,0);
            if(timestamp>=253370764800000d) {
                var info=ZoneInfo(zone);var reference=FutureReference(timestamp,DateTimeKind.Utc);
                return ((int)info.GetUtcOffset(reference).TotalMilliseconds,info.IsDaylightSavingTime(reference) ? 1 : 0);
            }
            var value=ForZone(zone);
            lock(value.gate) {
                using var thread=value.api.Module.Enter();int error=0;
                value.api.SetTime(value.calendar,timestamp,ref error);Check(error);
                int standard=value.api.Get(value.calendar,Field.ZoneOffset,ref error),daylight=value.api.Get(value.calendar,Field.DaylightOffset,ref error);Check(error);
                return (standard+daylight,daylight);
            }
        }
        internal static double FromWall(double timestamp,string zone)
        {
            if(zone is "UTC" or "Etc/UTC" or "GMT")return timestamp;
            if(timestamp>=253370764800000d) {
                var info=ZoneInfo(zone);var reference=FutureReference(timestamp,DateTimeKind.Unspecified);
                TimeSpan offset=info.GetUtcOffset(reference);
                if(info.IsAmbiguousTime(reference)) {
                    var choices=info.GetAmbiguousTimeOffsets(reference);offset=choices[0]>choices[1] ? choices[0] : choices[1];
                }
                return timestamp-offset.TotalMilliseconds;
            }
            var value=ForZone(zone);var parts=ExtendedUtcDateParts(timestamp);
            lock(value.gate) {
                using var thread=value.api.Module.Enter();int error=0;value.api.Clear(value.calendar);
                value.api.Set(value.calendar,Field.ExtendedYear,parts.Year);value.api.Set(value.calendar,Field.Month,parts.Month-1);value.api.Set(value.calendar,Field.Day,parts.Day);
                value.api.Set(value.calendar,Field.Hour,parts.Hour);value.api.Set(value.calendar,Field.Minute,parts.Minute);value.api.Set(value.calendar,Field.Second,parts.Second);value.api.Set(value.calendar,Field.Millisecond,parts.Millisecond);
                double result=value.api.GetTime(value.calendar,ref error);Check(error);return result;
            }
        }
    }
}
