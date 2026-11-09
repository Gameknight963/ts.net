var intl=(TsObject)Js.BuiltinNamespace("Intl");
JsFunction names=(JsFunction)intl.Get("DisplayNames"),numbers=(JsFunction)intl.Get("NumberFormat");
var options=new JsDynamicObject();options.Set("type","currency");options.Set("fallback","code");
var named=(TsObject)names.Construct(new object[]{"en-US",options});var of=(JsFunction)named.Get("of");
options.Set("fallback","none");var optional=(TsObject)names.Construct(new object[]{"en-US",options});var optionalOf=(JsFunction)optional.Get("of");
bool stop=false;
var collector=new Thread(()=>{while(!Volatile.Read(ref stop)){GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);Thread.Sleep(1);}}){IsBackground=true};collector.Start();
try {
 Parallel.For(0,8,worker=>{
  for(int i=0;i<100;i++) {
   string code="ZZ"+(char)('A'+(i+worker)%26);
   if(!Equals(of.InvokeWithReceiver(named,new object[]{code}),code))throw new Exception("Unknown currency display name read a stale input buffer");
   if(!ReferenceEquals(optionalOf.InvokeWithReceiver(optional,new object[]{code}),Js.Undefined))throw new Exception("Unknown currency fallback:none changed");
   var settings=new JsDynamicObject();settings.Set("style","currency");settings.Set("currency",code);
   var format=(TsObject)numbers.Construct(new object[]{"ar-EG",settings});var text=(JsFunction)format.Get("format");
   string expected="\u200f\u0661\u0662\u066b\u0660\u0660\u00a0"+code;
   if(!Equals(text.Invoke(new object[]{12d}),expected))throw new Exception("Unknown currency symbol read a stale input buffer");
  }
 });
}finally{Volatile.Write(ref stop,true);collector.Join();}
Console.WriteLine("PASS currency code buffers and both display-name fallbacks survive concurrent compacting GC");

var held=(TsObject)numbers.Construct(new object[]{"ar-EG"});var heldFormat=(JsFunction)held.Get("format");
foreach(string locale in new[]{"ar-EG","ar-SA","ar-AE","ar-MA","ar-BH","ar-IQ","ar-JO","ar-KW","ar-LB","ar-QA"})
 foreach(string system in new[]{"latn","arab","arabext","deva","beng","thai","fullwide","mathbold"}) {
  var settings=new JsDynamicObject();settings.Set("numberingSystem",system);numbers.Construct(new object[]{locale,settings});
 }
GC.Collect();GC.WaitForPendingFinalizers();
if(!Equals(heldFormat.Invoke(new object[]{12.5}),"\u0661\u0662\u066b\u0665"))throw new Exception("Existing number formatter lost symbols after locale cache eviction");
Console.WriteLine("PASS existing number formatters retain symbols after locale cache eviction");
