using System.Globalization;
var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("NumberFormat");
int checks=0;
foreach(string name in new[]{"en-US","de-DE","fr-FR","tr-TR"}) {
 var culture=(CultureInfo)CultureInfo.GetCultureInfo(name).Clone();
 string currency=new RegionInfo(name).ISOCurrencySymbol;
 culture.NumberFormat.CurrencyNegativePattern=culture.NumberFormat.CurrencyPositivePattern switch{0=>0,1=>4,2=>14,_=>15};
 var options=new JsDynamicObject();options.DefineOwn("style","currency");options.DefineOwn("currency",currency);options.DefineOwn("currencySign","accounting");
 var formatter=(TsObject)constructor.Construct(new object[]{name,options});
 var format=(JsFunction)formatter.Get("format");
 foreach(double value in new[]{-1234.56,-0d,0d,1234.56}) {
  string actual=(string)format.InvokeWithReceiver(Js.Undefined,new object[]{value});
  string expected=value.ToString("C2",culture);
  if(actual!=expected)throw new Exception($"Native accounting pattern changed: {name}: {actual} != {expected}");
  checks++;
 }
 options.DefineOwn("signDisplay","never");
 formatter=(TsObject)constructor.Construct(new object[]{name,options});
 var resolved=(TsObject)((JsFunction)formatter.Get("resolvedOptions")).InvokeWithReceiver(formatter,Array.Empty<object>());
 if((string)resolved.Get("currencySign")!="accounting")throw new Exception("Requested accounting mode was lost when signs are hidden");
}
var previousCulture=CultureInfo.CurrentCulture;
try {
 for(int pattern=0;pattern<4;pattern++) {
  var culture=(CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
  culture.NumberFormat.CurrencyPositivePattern=pattern;
  culture.NumberFormat.CurrencyNegativePattern=pattern switch{0=>0,1=>4,2=>14,_=>15};
  CultureInfo.CurrentCulture=culture;
  var options=new JsDynamicObject();options.DefineOwn("style","currency");options.DefineOwn("currency","USD");options.DefineOwn("currencySign","accounting");
  var formatter=(TsObject)constructor.Construct(new object[]{Js.Undefined,options});
  var format=(JsFunction)formatter.Get("format");
  foreach(double value in new[]{-1234.56,-0d,0d,1234.56}) {
   if((string)format.InvokeWithReceiver(Js.Undefined,new object[]{value})!=value.ToString("C2",culture))throw new Exception("Accounting layout changed: "+pattern);
   checks++;
  }
 }
} finally {CultureInfo.CurrentCulture=previousCulture;}
Console.WriteLine($"PASS {checks} native accounting patterns and retained currency-sign options");
