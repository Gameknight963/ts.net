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
