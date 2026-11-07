using System.Globalization;
var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("Segmenter");
var formatter=(TsObject)constructor.Construct(new object[]{"en-US"});
var segment=(JsFunction)formatter.Get("segment");
foreach(string text in new[]{"\u0915\u094d\u0915","a\u0301","\ud83d\udc69\u200d\ud83d\udcbb","\r\n","\ud800x",""}) {
 var value=(TsObject)segment.InvokeWithReceiver(formatter,new object[]{text});
 var containing=(JsFunction)value.Get("containing");
 int[] starts=StringInfo.ParseCombiningCharacters(text);
 for(int index=0;index<text.Length;index++) {
  int slot=Array.BinarySearch(starts,index);if(slot<0)slot=~slot-1;
  int end=slot+1<starts.Length?starts[slot+1]:text.Length;
  var result=(TsObject)containing.InvokeWithReceiver(value,new object[]{(double)index});
  if((string)result.Get("segment")!=text[starts[slot]..end] || (double)result.Get("index")!=starts[slot])throw new Exception("Segmentation differs from native Unicode data");
 }
}
foreach(string granularity in new[]{"word","sentence"}) {
 var options=new JsDynamicObject();options.DefineOwn("granularity",granularity);bool failed=false;
 try{constructor.Construct(new object[]{"en-US",options});}catch(NotSupportedException){failed=true;}
 if(!failed)throw new Exception("Unsupported segmentation silently accepted");
}
Console.WriteLine("PASS native Unicode boundaries and explicit unsupported granularities");
