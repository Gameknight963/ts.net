using System.Globalization;
var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("Collator");
foreach(var item in new[]{("base",CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace),("case",CompareOptions.IgnoreNonSpace),("variant",CompareOptions.None)}) {
 var options=new JsDynamicObject();options.DefineOwn("caseFirst","upper");options.DefineOwn("sensitivity",item.Item1);
 var value=(TsObject)constructor.Construct(new object[]{"en-US",options});
 var compare=(JsFunction)value.Get("compare");
 double Compare(string left,string right)=>(double)compare.InvokeWithReceiver(Js.Undefined,new object[]{left,right});
 var native=CultureInfo.GetCultureInfo("en-US-u-kf-upper").CompareInfo;
 foreach(var pair in new[]{("a","\uff41"),("A","\uff21")}) {
  double expected=Math.Sign(native.Compare(pair.Item1,pair.Item2,item.Item2));
  if(Compare(pair.Item1,pair.Item2)!=expected)throw new Exception("Native character width order changed: "+item.Item1);
 }
 if(item.Item1=="case" && Compare("A","a")>=0)throw new Exception("Accent-insensitive comparison lost upper-first ordering");
}
Console.WriteLine("PASS case ordering preserves native character-width distinctions");
