var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("DurationFormat");
TsObject Formatter(string locale,string numberingSystem=null) {
 var options=new JsDynamicObject();options.Set("style","digital");if(numberingSystem!=null)options.Set("numberingSystem",numberingSystem);
 return (TsObject)constructor.Construct(new object[]{locale,options});
}
string Text(TsObject format,params (string Field,double Value)[] fields) {
 var value=new JsDynamicObject();foreach(var field in fields)value.Set(field.Field,field.Value);
 var method=(JsFunction)format.Get("format");string text=(string)method.InvokeWithReceiver(format,new object[]{value});
 var parts=(JsArray)((JsFunction)format.Get("formatToParts")).InvokeWithReceiver(format,new object[]{value});var joined=new System.Text.StringBuilder();
 for(uint i=0;i<parts.Length();i++)joined.Append(((TsObject)parts.ElementAt(i)).Get("value"));
 if(text!=joined.ToString())throw new Exception("Duration parts differ from text");return text;
}
var exact=Formatter("en-US");
if(Text(exact,("seconds",1d),("milliseconds",481d))!="0:00:01.481")throw new Exception("Milliseconds lost a nanosecond during formatting");
if(Text(exact,("nanoseconds",9007199254740992d))!="0:00:9007199.254740992")throw new Exception("Large nanoseconds lost precision");
if(Text(exact,("seconds",9007199254740991d),("milliseconds",999d))!="0:00:9007199254740991.999")throw new Exception("Large seconds lost precision");
foreach(string locale in new[]{"ar-EG","fa-IR","bn-BD"})if(Text(Formatter(locale,"latn"),("seconds",1d))!="0:00:01")throw new Exception("Explicit Latin digits were ignored");
foreach(string locale in new[]{"en-US","de-DE","ru-RU","ar-EG"}) {
 var format=Formatter(locale);string[] expected=Enumerable.Range(0,10).Select(i=>Text(format,("hours",(double)i),("nanoseconds",1d))).ToArray();
 Parallel.For(0,8,worker=>{for(int i=0;i<30;i++){int value=(worker+i)%10;if(Text(format,("hours",(double)value),("nanoseconds",1d))!=expected[value])throw new Exception("Concurrent durations mixed values");}});
 GC.Collect();GC.WaitForPendingFinalizers();if(Text(format,("hours",1d),("nanoseconds",1d))!=expected[1])throw new Exception("Duration formatter lost native state");
}
Console.WriteLine("PASS exact subsecond precision, explicit Latin digits, concurrent duration parts and native handle lifetime");
