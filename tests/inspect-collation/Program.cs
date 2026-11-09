using System.Globalization;
using System.Reflection;
bool usesIcu=(bool)typeof(Js).GetNestedType("NativeCollation",BindingFlags.NonPublic)!.GetProperty("Available",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("Collator");
foreach(var item in new[]{("base",CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace),("case",CompareOptions.IgnoreNonSpace),("variant",CompareOptions.None)}) {
 var options=new JsDynamicObject();options.DefineOwn("caseFirst","upper");options.DefineOwn("sensitivity",item.Item1);
 var value=(TsObject)constructor.Construct(new object[]{"en-US",options});
 var compare=(JsFunction)value.Get("compare");
 double Compare(string left,string right)=>(double)compare.InvokeWithReceiver(Js.Undefined,new object[]{left,right});
 var native=CultureInfo.GetCultureInfo("en-US-u-kf-upper").CompareInfo;
 foreach(var pair in new[]{("a","\uff41"),("A","\uff21")}) {
  double expected=usesIcu ? item.Item1=="variant" ? -1 : 0 : Math.Sign(native.Compare(pair.Item1,pair.Item2,item.Item2));
  if(Compare(pair.Item1,pair.Item2)!=expected)throw new Exception("Character width disagrees with collation sensitivity: "+item.Item1);
 }
 if(item.Item1=="case" && Compare("A","a")>=0)throw new Exception("Accent-insensitive comparison lost upper-first ordering");
}
Console.WriteLine("PASS case ordering and character width follow collation sensitivity");
if(usesIcu) {
 foreach(var item in new[]{("en-US-u-kn","numeric",(object)false,"en-US-u-kn"),("en-US-u-kf-upper","caseFirst",(object)"lower","en-US-u-kf-upper"),("en-US-u-co-emoji","collation",(object)"eor","en-US-u-co-eor")}) {
  var options=new JsDynamicObject();options.Set(item.Item2,item.Item3);
  var value=(TsObject)constructor.Construct(new object[]{item.Item1,options});
  var resolved=(TsObject)((JsFunction)value.Get("resolvedOptions")).InvokeWithReceiver(value,Array.Empty<object>());
  if(!Equals(resolved.Get("locale"),item.Item4))throw new Exception("Resolved locale compatibility changed");
 }
 Console.WriteLine("PASS resolved locale compatibility for Unicode keywords and option overrides");
 var heldOptions=new JsDynamicObject();heldOptions.Set("collation","emoji");
 var held=(TsObject)constructor.Construct(new object[]{"en-US",heldOptions});var compare=(JsFunction)held.Get("compare");
 double before=(double)compare.InvokeWithReceiver(Js.Undefined,new object[]{"a","b"});
 foreach(string locale in new[]{"en-US","de-DE"})foreach(string sensitivity in new[]{"base","accent","case","variant"})foreach(string caseFirst in new[]{"false","lower","upper"})foreach(bool numeric in new[]{false,true})foreach(bool punctuation in new[]{false,true}) {
  var options=new JsDynamicObject();options.Set("sensitivity",sensitivity);options.Set("caseFirst",caseFirst);options.Set("numeric",numeric);options.Set("ignorePunctuation",punctuation);
  _=constructor.Construct(new object[]{locale,options});
 }
 GC.Collect();GC.WaitForPendingFinalizers();
 if((double)compare.InvokeWithReceiver(Js.Undefined,new object[]{"a","b"})!=before)throw new Exception("Cache eviction invalidated a live collator");
 Console.WriteLine("PASS collators remain valid after cache eviction and native handle collection");
}
