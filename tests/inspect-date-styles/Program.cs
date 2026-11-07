using System.Globalization;
var constructor = (JsFunction)Js.BuiltinNamespace("Date");
var date = (TsObject)constructor.Construct(new object[] { 1709210096000d });
var utc = DateTimeOffset.FromUnixTimeMilliseconds(1709210096000);
foreach (string locale in new[] { "en-US", "en-GB", "de-DE", "ja-JP" }) {
 var culture = CultureInfo.GetCultureInfo(locale);
 foreach (string zoneName in new[] { "UTC", "America/New_York", "Asia/Tokyo" }) {
  var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneName);
  var local = TimeZoneInfo.ConvertTime(utc, zone);
  foreach (string style in new[] { "short", "medium", "long", "full" }) {
   var options = new JsDynamicObject(); options.DefineOwn("timeZone", zoneName); options.DefineOwn("dateStyle", style);
   var method = (JsFunction)date.Get("toLocaleDateString");
   string actual = (string)method.InvokeWithReceiver(date, new object[] { locale, options });
   string expected = local.ToString(style is "short" or "medium" ? "d" : "D", culture);
   if (actual != expected) throw new Exception($"Date style mismatch: {locale} {style}");
   options = new JsDynamicObject(); options.DefineOwn("timeZone", zoneName); options.DefineOwn("timeStyle", style);
   method = (JsFunction)date.Get("toLocaleTimeString");
   actual = (string)method.InvokeWithReceiver(date, new object[] { locale, options });
   expected = local.ToString(style == "short" ? "t" : "T", culture);
   if (style is "long" or "full") expected += " " + (zone.IsDaylightSavingTime(utc) ? zone.DaylightName : zone.StandardName);
   if (actual != expected) throw new Exception($"Time style mismatch: {locale} {style}");
  }
 }
}
Console.WriteLine("PASS 96 native date/time style and zone combinations");

var fields = new JsDynamicObject();
fields.DefineOwn("timeZone", "UTC"); fields.DefineOwn("year", "numeric"); fields.DefineOwn("month", "2-digit");
var dateMethod = (JsFunction)date.Get("toLocaleDateString");
string numericMonth = (string)dateMethod.InvokeWithReceiver(date, new object[] { "de-DE", fields });
if (numericMonth != "02.2024") throw new Exception("Native German date separator was not retained");
fields = new JsDynamicObject(); fields.DefineOwn("timeZone", "UTC"); fields.DefineOwn("era", "short");
string era = (string)dateMethod.InvokeWithReceiver(date, new object[] { "en-US", fields });
if (era != utc.ToString("gg", CultureInfo.GetCultureInfo("en-US"))) throw new Exception("Native era name was not retained");
Console.WriteLine("PASS native date component separators and era names");
var timeMethod = (JsFunction)date.Get("toLocaleTimeString");
string Time(string locale, params (string Name, object Value)[] entries) {
 var options = new JsDynamicObject(); options.DefineOwn("timeZone", "UTC");
 foreach (var entry in entries) options.DefineOwn(entry.Name, entry.Value);
 return (string)timeMethod.InvokeWithReceiver(date,new object[] {locale,options});
}
if (Time("en-US",("second","2-digit")) != utc.Second.ToString("00")) throw new Exception("Standalone seconds lost explicit padding");
if (Time("de-DE",("hour","numeric")) != utc.Hour.ToString()) throw new Exception("Hour-only formatting added a non-native label");
foreach(string zoneName in new[] {"UTC","America/New_York"}) {
 var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneName);
 var local = TimeZoneInfo.ConvertTime(utc,zone);
 foreach(string width in new[] {"short","long","shortGeneric","longGeneric"}) {
  var options = new JsDynamicObject(); options.DefineOwn("timeZone",zoneName); options.DefineOwn("timeZoneName",width); options.DefineOwn("hour","2-digit"); options.DefineOwn("hour12",false);
  string actual = (string)timeMethod.InvokeWithReceiver(date,new object[]{"en-GB",options});
  string expectedName = width is "shortGeneric" or "longGeneric" ? zone.StandardName : zone.IsDaylightSavingTime(utc) ? zone.DaylightName : zone.StandardName;
  if(actual != local.Hour.ToString("00") + " " + expectedName) throw new Exception("Native time zone name was not retained");
 }
}
Console.WriteLine("PASS native time component widths and zone names");

foreach(string width in new[]{"short","long","narrow"}) {
 string expected = utc.Hour < 12 ? CultureInfo.GetCultureInfo("en-US").DateTimeFormat.AMDesignator : CultureInfo.GetCultureInfo("en-US").DateTimeFormat.PMDesignator;
 if (width == "narrow") expected = StringInfo.GetNextTextElement(expected);
 if(Time("en-US",("dayPeriod",width)) != expected) throw new Exception("Native day period was not used");
}
Console.WriteLine("PASS native AM/PM day periods");

if(Time("en-GB",("dayPeriod","long")) != CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.PMDesignator) throw new Exception("Day-period-only formatting disappeared in a 24-hour locale");

foreach (string locale in new[] {"en-US","en-GB","ko-KR","ja-JP"}) {
 var culture = CultureInfo.GetCultureInfo(locale);
 string expected = utc.Hour < 12 ? culture.DateTimeFormat.AMDesignator : culture.DateTimeFormat.PMDesignator;
 if(Time(locale,("dayPeriod","long")) != expected) throw new Exception("Native day period acquired extra spacing: " + locale);
}
Console.WriteLine("PASS day-period-only formatting across native clock layouts");
