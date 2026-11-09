using System.Diagnostics;
using System.Text.Json;
using System.Globalization;
var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("DateTimeFormat");
var instant=new DateTimeOffset(2024,2,29,13,4,5,678,TimeSpan.Zero);
var timestamp=(double)instant.ToUnixTimeMilliseconds();
TsObject Formatter(string locale,params (string Name,object Value)[] fields) {
 var options=new JsDynamicObject();options.DefineOwn("timeZone","UTC");options.DefineOwn("numberingSystem","latn");
 foreach(var field in fields)options.DefineOwn(field.Name,field.Value);
 return (TsObject)constructor.Construct(new object[]{locale,options});
}
string Format(TsObject formatter)=> (string)((JsFunction)formatter.Get("format")).InvokeWithReceiver(Js.Undefined,new object[]{timestamp});
void CheckNativeCalendar(TsObject formatter,string locale,params (string Name,object Value)[] fields) {
 var options=new Dictionary<string,object>{{"timeZone","UTC"},{"numberingSystem","latn"}};
 foreach(var field in fields)options[field.Name]=field.Value;
 string script="const f=new Intl.DateTimeFormat("+JsonSerializer.Serialize(locale)+","+JsonSerializer.Serialize(options)+");console.log(JSON.stringify(f.formatToParts("+timestamp.ToString(CultureInfo.InvariantCulture)+").filter(p=>p.type!=='literal'&&p.type!=='era')));";
 var start=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};start.ArgumentList.Add("-e");start.ArgumentList.Add(script);
 using var process=Process.Start(start)!;string expected=process.StandardOutput.ReadToEnd(),error=process.StandardError.ReadToEnd();process.WaitForExit();if(process.ExitCode!=0)throw new Exception(error);
 var parts=(JsArray)((JsFunction)formatter.Get("formatToParts")).InvokeWithReceiver(formatter,new object[]{timestamp});
 var actual=new List<Dictionary<string,string>>();var text=new System.Text.StringBuilder();
 for(uint i=0;i<parts.Length();i++){var part=(TsObject)parts.ElementAt(i);string type=(string)part.Get("type"),value=(string)part.Get("value");text.Append(value);if(type is not ("literal" or "era"))actual.Add(new(){{"type",type},{"value",value}});}
 if(text.ToString()!=Format(formatter))throw new Exception("Calendar parts differ from formatted text: "+locale);
 // Newer ICU renders Hebrew style dates with alphabetic day/year numerals,
 // even with latn selected. Check their numeric calendar values independently.
 var reference=JsonSerializer.Deserialize<List<Dictionary<string,string>>>(expected)!;
 if(locale=="he-IL" && fields.Any(field=>field.Name=="calendar" && Equals(field.Value,"hebrew")) && fields.Any(field=>field.Name=="dateStyle")) {
  var hebrew=new HebrewCalendar();foreach(var part in reference)if(part["type"] is "day" or "year")part["value"]=(part["type"]=="day" ? hebrew.GetDayOfMonth(instant.UtcDateTime) : hebrew.GetYear(instant.UtcDateTime)).ToString(CultureInfo.InvariantCulture);
 }
 if(!JsonElement.DeepEquals(JsonSerializer.SerializeToElement(actual),JsonSerializer.SerializeToElement(reference)))throw new Exception("Calendar fields differ from Node: "+locale+" "+JsonSerializer.Serialize(fields.Select(field=>field.Name+"="+field.Value))+" "+JsonSerializer.Serialize(actual)+" != "+expected);
}
foreach(string locale in new[]{"en-US","en-GB","de-DE","ja-JP","fr-FR","ar-SA","fa-IR","th-TH"}) {
 var culture=CultureInfo.GetCultureInfo(locale);
 var formatter=Formatter(locale);
 bool nativeCalendar=culture.DateTimeFormat.Calendar is not GregorianCalendar;
 if(nativeCalendar){CheckNativeCalendar(formatter,locale);}
 string actual=Format(formatter),expected=instant.ToString("d",culture);
 if(!nativeCalendar && actual!=expected)throw new Exception($"Default native date mismatch: {locale}: {actual} != {expected}");
 foreach(string dateStyle in new[]{"short","medium","long","full"}) {
  formatter=Formatter(locale,("dateStyle",dateStyle));
  if(nativeCalendar){CheckNativeCalendar(formatter,locale,("dateStyle",dateStyle));}
  expected=instant.ToString(dateStyle is "short" or "medium"?"d":"D",culture);
  if(!nativeCalendar && Format(formatter)!=expected)throw new Exception("Date style changed: "+locale+" "+dateStyle);
  formatter=Formatter(locale,("timeStyle",dateStyle));
  if(nativeCalendar){CheckNativeCalendar(formatter,locale,("timeStyle",dateStyle));}
  expected=instant.ToString(dateStyle=="short"?"t":"T",culture);
  if(dateStyle is "long" or "full")expected+=" "+TimeZoneInfo.Utc.StandardName;
  if(!nativeCalendar && Format(formatter)!=expected)throw new Exception("Time style changed: "+locale+" "+dateStyle);
 }
}
var german=Formatter("de-DE");
var resolved=(TsObject)((JsFunction)german.Get("resolvedOptions")).InvokeWithReceiver(german,Array.Empty<object>());
if((string)resolved.Get("month")!="2-digit" || (string)resolved.Get("day")!="2-digit")throw new Exception("Resolved native padding is inaccurate");
foreach(string zoneName in new[]{"UTC","America/New_York","Asia/Tokyo"}) {
 var zone=TimeZoneInfo.FindSystemTimeZoneById(zoneName);
 var local=TimeZoneInfo.ConvertTime(instant,zone);
 var formatter=Formatter("en-GB",("timeZone",zoneName),("hour","2-digit"),("minute","2-digit"),("second","2-digit"),("hour12",false));
 if(Format(formatter)!=local.ToString("HH:mm:ss",CultureInfo.GetCultureInfo("en-GB")))throw new Exception("Cached time zone changed: "+zoneName);
}
Console.WriteLine("PASS default/style/time-zone formats, calendar fields against Node, and resolved native padding");
foreach(var selection in new[]{("ja-JP","japanese"),("th-TH","gregory"),("he-IL","hebrew"),("ar-SA","gregory"),("fa-IR","gregory")}) {
 var culture=(CultureInfo)CultureInfo.GetCultureInfo(selection.Item1).Clone();
 culture.DateTimeFormat.Calendar=selection.Item2 switch {
  "japanese"=>new JapaneseCalendar(),"hebrew"=>new HebrewCalendar(),_=>new GregorianCalendar()
 };
 foreach(string style in new[]{"short","long"}) {
  var formatter=Formatter(selection.Item1,("calendar",selection.Item2),("dateStyle",style));
  if(culture.DateTimeFormat.Calendar is not GregorianCalendar || selection.Item1=="ar-SA"){CheckNativeCalendar(formatter,selection.Item1,("calendar",selection.Item2),("dateStyle",style));continue;}
  var expected=instant.ToString(style=="short"?"d":"D",culture);
  if(Format(formatter)!=expected)throw new Exception("Selected calendar mismatch: "+selection);
 }
}
Console.WriteLine("PASS 10 native calendar formats");
var fraction=Formatter("en-US",("hour","2-digit"),("minute","2-digit"),("second","2-digit"),("fractionalSecondDigits",3d));
bool unsupported=false;
try {((JsFunction)fraction.Get("formatRange")).InvokeWithReceiver(fraction,new object[]{timestamp,timestamp+1});}
catch(NotSupportedException){unsupported=true;}
if(!unsupported)throw new Exception("Fractional-second date range was silently collapsed");
foreach(string cycle in new[]{"h11","h24"}) {
 var value=Formatter("en-US",("hour","2-digit"),("minute","2-digit"),("hourCycle",cycle));
 unsupported=false;
 try {((JsFunction)value.Get("formatRange")).InvokeWithReceiver(value,new object[]{0d,3600000d});}
 catch(NotSupportedException){unsupported=true;}
 if(!unsupported)throw new Exception("Unsupported interval hour cycle was silently changed: "+cycle);
}
Console.WriteLine("PASS unsupported fractional-second and hour-cycle ranges are rejected");
