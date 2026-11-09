using System.Globalization;
using System.Numerics;
var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("NumberFormat");
TsObject Formatter(string locale) {
 var options=new JsDynamicObject();options.Set("useGrouping",false);options.Set("numberingSystem","latn");
 return (TsObject)constructor.Construct(new object[]{locale,options});
}
string Range(TsObject format,object start,object end,bool exact=true) {
 string text=(string)((JsFunction)format.Get("formatRange")).InvokeWithReceiver(format,new[]{start,end});
 var parts=(JsArray)((JsFunction)format.Get("formatRangeToParts")).InvokeWithReceiver(format,new[]{start,end});
 var joined=new System.Text.StringBuilder();var first=new System.Text.StringBuilder();var second=new System.Text.StringBuilder();
 for(uint i=0;i<parts.Length();i++){var part=(TsObject)parts.ElementAt(i);string value=(string)part.Get("value");joined.Append(value);if(Equals(part.Get("source"),"startRange"))first.Append(value);else if(Equals(part.Get("source"),"endRange"))second.Append(value);}
 if(joined.ToString()!=text)throw new Exception("Regional range parts differ from text");
 if(exact){var number=(JsFunction)format.Get("format");if(first.ToString()!=(string)number.Invoke(new[]{start}) || second.ToString()!=(string)number.Invoke(new[]{end}))throw new Exception("Regional range lost an exact endpoint");}
 return text;
}
foreach(string locale in new[]{"en-GB","de-DE","fr-FR","ja-JP","ar-EG","fa-IR","ru-RU"}) {
 var format=Formatter(locale);Range(format,BigInteger.Parse("123456789012345678901"),BigInteger.Parse("123456789012345678902"));
 Range(format,"9007199254740992.125","9007199254740993.125");
 string expected=Range(format,1d,2d);
 Parallel.For(0,8,worker=>{for(int i=0;i<50;i++)if(Range(format,1d,2d)!=expected)throw new Exception("Concurrent range pattern changed");});
}
var held=Formatter("ja-JP");string retained=Range(held,1d,2d);
foreach(var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures).OrderBy(c=>c.Name).Take(80))Range(Formatter(culture.Name),1d,2d);
GC.Collect();GC.WaitForPendingFinalizers();if(Range(held,1d,2d)!=retained)throw new Exception("Range cache eviction changed a retained formatter");
Console.WriteLine("PASS exact integer and decimal range endpoints, source spans, concurrent patterns and bounded cache eviction");
