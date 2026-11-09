using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
var backend=typeof(Js).GetNestedType("NativeExtendedDates",BindingFlags.NonPublic)!;
var fromWall=backend.GetMethod("FromWall",BindingFlags.Static|BindingFlags.NonPublic)!;
var offset=backend.GetMethod("Offset",BindingFlags.Static|BindingFlags.NonPublic)!;
const string script="""
function sunday(y,m,n){const d=new Date(0);d.setUTCFullYear(y,m,1);return 1+(7-d.getUTCDay())%7+7*(n-1);}
const cases=[[-10000,0,1,12,34,56,789],[-400,1,29,12,34,56,789],[-1,11,31,23,59,59,999],[0,1,29,0,0,0,0],[1,0,1,0,0,0,0],[9999,11,31,23,59,59,999],[10000,0,1,0,0,0,0],[20000,1,29,12,34,56,789],[275000,0,1,0,0,0,0],[2011,11,30,12,0,0,0]];
for(const y of [10000,20000]){
 cases.push([y,2,sunday(y,2,2),2,30,0,0],[y,10,sunday(y,10,1),1,30,0,0],[y,9,sunday(y,9,1),2,15,0,0]);
 const d=new Date(0);d.setUTCFullYear(y,2,31);cases.push([y,2,31-d.getUTCDay(),2,30,0,0]);
}
console.log(JSON.stringify(cases.map(([y,m,d,h,min,s,ms])=>{const wall=new Date(0);wall.setUTCFullYear(y,m,d);wall.setUTCHours(h,min,s,ms);const local=new Date(2000,0,1);local.setFullYear(y,m,d);local.setHours(h,min,s,ms);return {wall:wall.getTime(),utc:local.getTime(),offset:local.getTimezoneOffset()};})));
""";
foreach(string zone in new[]{"UTC","America/Chicago","Europe/Berlin","Australia/Lord_Howe","Pacific/Apia","Asia/Kathmandu"}) {
    var start=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
    start.ArgumentList.Add("-e");start.ArgumentList.Add(script);start.Environment["TZ"]=zone;
    using var process=Process.Start(start)!;string output=process.StandardOutput.ReadToEnd(),error=process.StandardError.ReadToEnd();process.WaitForExit();
    if(process.ExitCode!=0)throw new Exception("Node date reference failed: "+error);
    using var json=JsonDocument.Parse(output);
    foreach(var row in json.RootElement.EnumerateArray()) {
        double wall=row.GetProperty("wall").GetDouble(),expected=row.GetProperty("utc").GetDouble();
        double actual=(double)fromWall.Invoke(null,new object[]{wall,zone})!;
        if(actual!=expected)throw new Exception($"Local calendar mismatch in {zone}: wall={wall}, actual={actual}, expected={expected}");
        object offsets=offset.Invoke(null,new object[]{expected,zone})!;
        int milliseconds=(int)offsets.GetType().GetField("Item1")!.GetValue(offsets)!;
        if(Math.Truncate(-milliseconds/60000d)!=row.GetProperty("offset").GetDouble())throw new Exception("Offset mismatch in "+zone);
    }
    Console.WriteLine("PASS extended local dates, skipped/repeated wall times, and offsets: "+zone);
}
var constructor=(JsFunction)Js.BuiltinNamespace("Date");
var ordinary=(TsObject)constructor.Construct(new object[]{1579089600000d});
var extended=(TsObject)constructor.Construct(new object[]{568972684800000d}); // January 20000, away from year boundaries.
var getter=(JsFunction)ordinary.Get("getFullYear");
Parallel.For(0,8,worker=>{
    for(int i=0;i<1000;i++) {
        var date=(worker+i)%2==0 ? ordinary : extended;
        double expected=ReferenceEquals(date,ordinary) ? 2020d : 20000d;
        if(!Equals(getter.InvokeWithReceiver(date,Array.Empty<object>()),expected))throw new Exception("Concurrent date field cache mixed timestamps");
    }
});
TimeZoneInfo.ClearCachedData();
if(!Equals(getter.InvokeWithReceiver(ordinary,Array.Empty<object>()),2020d))throw new Exception("Date field cache survived an incompatible timezone refresh");
Console.WriteLine("PASS date field caches remain independent across threads and timezone refreshes");
var formats=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("DateTimeFormat");
foreach(var item in new[]{("indian","10","11","-79"),("islamic","05","08","-641"),("islamic-rgsa","05","08","-641")}) {
    var options=new JsDynamicObject();options.Set("calendar",item.Item1);options.Set("timeZone","UTC");options.Set("year","numeric");options.Set("month","2-digit");options.Set("day","2-digit");
    var format=(TsObject)formats.Construct(new object[]{"en-US",options});
    try {
        var parts=(JsArray)((JsFunction)format.Get("formatToParts")).InvokeWithReceiver(format,new object[]{-62167219200000d});
        var fields=new Dictionary<string,string>();
        for(uint i=0;i<parts.Length();i++){var part=(TsObject)parts.ElementAt(i);fields[(string)part.Get("type")]=(string)part.Get("value");}
        if(fields["month"]!=item.Item2 || fields["day"]!=item.Item3 || fields["year"]!=item.Item4)throw new Exception("Historical calendar produced incorrect fields");
    }catch(NotSupportedException error) when(error.Message.Contains("invalid month")) {
        // System ICU predates its negative-year month fixes. Both entrypoints
        // must reject the broken native result rather than returning month -7.
        try{((JsFunction)format.Get("format")).Invoke(new object[]{-62167219200000d});throw new Exception("Historical calendar text bypassed the month guard");}
        catch(NotSupportedException textError) when(textError.Message.Contains("invalid month")){}
        try{((JsFunction)format.Get("formatRange")).InvokeWithReceiver(format,new object[]{-62167219200000d,-62167132800000d});throw new Exception("Historical calendar range bypassed the month guard");}
        catch(NotSupportedException rangeError) when(rangeError.Message.Contains("invalid month")){}
    }
}
Console.WriteLine("PASS historical calendar fields or explicit guards for broken system ICU month calculations");
