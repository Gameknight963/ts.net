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
 var options=new JsDynamicObject();options.DefineOwn("granularity",granularity);
 var selected=(TsObject)constructor.Construct(new object[]{"en-US",options});
 var method=(JsFunction)selected.Get("segment");
 string text=granularity=="word"?"Hello, world!":"One. Two!";
 var segments=(TsObject)method.InvokeWithReceiver(selected,new object[]{text});
 var containing=(JsFunction)segments.Get("containing");
 var expected=granularity=="word"?new[]{"Hello",","," ","world","!"}:new[]{"One. ","Two!"};
 int offset=0;
 for(int index=0;index<expected.Length;index++) {
  var data=(TsObject)containing.InvokeWithReceiver(segments,new object[]{(double)offset});
  if((string)data.Get("segment")!=expected[index] || (double)data.Get("index")!=offset)throw new Exception("Incorrect native segment");
  if(granularity=="word") {
   if(data.Get("isWordLike") is not bool word || word!=(index is 0 or 3))throw new Exception("Incorrect word status");
  }
  else if(!ReferenceEquals(data.Get("isWordLike"),Js.Undefined))throw new Exception("Sentence has word status");
  offset+=expected[index].Length;
 }
 if(offset!=text.Length)throw new Exception("Segmentation does not cover input");
}
Console.WriteLine("PASS native grapheme, word and sentence boundaries");

[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
static WeakReference ReleasedInput(JsFunction constructor)
{
 var options=new JsDynamicObject();options.DefineOwn("granularity","word");
 var formatter=(TsObject)constructor.Construct(new object[]{"en-US",options});
 var segment=(JsFunction)formatter.Get("segment");
 string text=new string('a',100_000)+"!";
 var result=segment.InvokeWithReceiver(formatter,new object[]{text});
 GC.KeepAlive(result);
 return new WeakReference(text);
}
var released=ReleasedInput(constructor);
GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
if(released.IsAlive)throw new Exception("Native segmentation retained its input after the result was released");
Console.WriteLine("PASS native segmentation releases input storage");
