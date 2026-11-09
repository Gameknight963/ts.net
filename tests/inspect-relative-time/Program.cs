var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("RelativeTimeFormat");
object Call(TsObject target,string name,params object[] args)=>((JsFunction)target.Get(name)).InvokeWithReceiver(target,args);
var held=(TsObject)constructor.Construct(new object[]{"pl"});
var rounded=(JsArray)Call(held,"formatToParts",1.2345,"day");
bool fraction=false;
for(uint i=0;i<rounded.Length();i++) {
    var part=(TsObject)rounded.ElementAt(i);
    if(Equals(part.Get("type"),"fraction")){fraction=true;if(!Equals(part.Get("value"),"235"))throw new Exception("Relative time must round half away from zero");}
}
if(!fraction)throw new Exception("Fractional relative time lost number parts");
foreach(double value in new[]{double.MaxValue,-double.MaxValue,0d,-0d,1.9999,-0.0001}) {
    var parts=(JsArray)Call(held,"formatToParts",value,"day");string text="";bool numeric=false;
    for(uint i=0;i<parts.Length();i++) {
        var part=(TsObject)parts.ElementAt(i);text+=(string)part.Get("value");numeric|=Equals(part.Get("type"),"integer");
    }
    if(!numeric || !Equals(text,Call(held,"format",value,"day")))throw new Exception("Number fields do not reconstruct relative time");
}
var arabic=(TsObject)constructor.Construct(new object[]{"ar-EG"});
foreach(double value in new[]{1d,2d}) {
    var parts=(JsArray)Call(arabic,"formatToParts",value,"day");
    if(parts.Length()!=1 || !Equals(((TsObject)parts.ElementAt(0)).Get("type"),"literal"))throw new Exception("Arabic written singular/dual acquired numeric parts");
}
string before=(string)Call(held,"format",12345.5,"day");
for(int i=0;i<128;i++)constructor.Construct(new object[]{i%2==0 ? "pl" : "ru"});
GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
if(!Equals(Call(held,"format",12345.5,"day"),before))throw new Exception("Relative time formatter lost its adopted number formatter");
Console.WriteLine("PASS relative time rounding, large number fields, written numbers, and native handle lifetime");
