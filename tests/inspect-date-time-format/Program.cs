using System.Globalization;
var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("DateTimeFormat");
var instant=new DateTimeOffset(2024,2,29,13,4,5,678,TimeSpan.Zero);
var timestamp=(double)instant.ToUnixTimeMilliseconds();
TsObject Formatter(string locale,params (string Name,object Value)[] fields) {
 var options=new JsDynamicObject();options.DefineOwn("timeZone","UTC");
 foreach(var field in fields)options.DefineOwn(field.Name,field.Value);
 return (TsObject)constructor.Construct(new object[]{locale,options});
}
string Format(TsObject formatter)=> (string)((JsFunction)formatter.Get("format")).InvokeWithReceiver(Js.Undefined,new object[]{timestamp});
foreach(string locale in new[]{"en-US","en-GB","de-DE","ja-JP","fr-FR","ar-SA","fa-IR","th-TH"}) {
 var culture=CultureInfo.GetCultureInfo(locale);
 var formatter=Formatter(locale);
 string actual=Format(formatter),expected=instant.ToString("d",culture);
 if(actual!=expected)throw new Exception($"Default native date mismatch: {locale}: {actual} != {expected}");
 foreach(string dateStyle in new[]{"short","medium","long","full"}) {
  formatter=Formatter(locale,("dateStyle",dateStyle));
  expected=instant.ToString(dateStyle is "short" or "medium"?"d":"D",culture);
  if(Format(formatter)!=expected)throw new Exception("Date style changed: "+locale+" "+dateStyle);
  formatter=Formatter(locale,("timeStyle",dateStyle));
  expected=instant.ToString(dateStyle=="short"?"t":"T",culture);
  if(dateStyle is "long" or "full")expected+=" "+TimeZoneInfo.Utc.StandardName;
  if(Format(formatter)!=expected)throw new Exception("Time style changed: "+locale+" "+dateStyle);
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
Console.WriteLine("PASS 75 native default/style/time-zone formats and resolved native padding");
