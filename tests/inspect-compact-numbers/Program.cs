using System.Diagnostics;
using System.Text.Json;
var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("NumberFormat");
var values=new[]{0d,-0d,999d,1000d,1200d,999500d,1200000d,-1200d,1e16,1e18};
foreach(string locale in new[]{"de-DE","ja-JP","pl-PL","sw-KE","he-IL"}) {
 var options=new JsDynamicObject();options.Set("notation","compact");options.Set("compactDisplay","long");options.Set("numberingSystem","latn");options.Set("signDisplay","always");
 var format=(TsObject)constructor.Construct(new object[]{locale,options});var text=(JsFunction)format.Get("format");var parts=(JsFunction)format.Get("formatToParts");
 string script="const f=new Intl.NumberFormat("+JsonSerializer.Serialize(locale)+",{notation:'compact',compactDisplay:'long',numberingSystem:'latn',signDisplay:'always'});console.log(JSON.stringify([0,-0,999,1000,1200,999500,1200000,-1200,1e16,1e18].map(n=>f.format(n))));";
 var start=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};start.ArgumentList.Add("-e");start.ArgumentList.Add(script);using var process=Process.Start(start)!;string output=process.StandardOutput.ReadToEnd(),error=process.StandardError.ReadToEnd();process.WaitForExit();if(process.ExitCode!=0)throw new Exception(error);
 var expected=JsonSerializer.Deserialize<string[]>(output)!;
 Parallel.For(0,8,worker=>{
  for(int i=0;i<1000;i++) {
   int index=(i+worker)%values.Length;string actual=(string)text.Invoke(new object[]{values[index]});
   if(actual!=expected[index])throw new Exception("Concurrent compact text mismatch: "+locale+" "+values[index]);
   var list=(JsArray)parts.InvokeWithReceiver(format,new object[]{values[index]});var joined=new System.Text.StringBuilder();for(uint n=0;n<list.Length();n++)joined.Append(((TsObject)list.ElementAt(n)).Get("value"));
   if(joined.ToString()!=actual)throw new Exception("Concurrent compact parts/cache mismatch: "+locale);
  }
 });
 GC.Collect();GC.WaitForPendingFinalizers();
 if((string)text.Invoke(new object[]{1200d})!=expected[4])throw new Exception("Compact formatter lost its native handle");
 Console.WriteLine("PASS shared compact formatter text, parts, cache changes and GC: "+locale);
}
