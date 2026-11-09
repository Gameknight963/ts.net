using System.Numerics;
var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("NumberFormat");
TsObject Formatter(string locale,int significant=0,string currency="USD") {
 var options=new JsDynamicObject();options.Set("style","currency");options.Set("currency",currency);options.Set("currencyDisplay","name");options.Set("minimumFractionDigits",0d);
 if(significant!=0){options.Set("minimumSignificantDigits",(double)significant);options.Set("maximumSignificantDigits",(double)significant);}
 return (TsObject)constructor.Construct(new object[]{locale,options});
}
string Name(TsObject format,object value) {
 var parts=(JsArray)((JsFunction)format.Get("formatToParts")).InvokeWithReceiver(format,new[]{value});var text=new System.Text.StringBuilder();string name="";int count=0;
 for(uint i=0;i<parts.Length();i++){var part=(TsObject)parts.ElementAt(i);text.Append(part.Get("value"));if(Equals(part.Get("type"),"currency")){name=(string)part.Get("value");count++;}}
 if(count!=1)throw new Exception("Currency name formatting exposed an extra currency symbol");
 if(!Equals(((JsFunction)format.Get("format")).Invoke(new[]{value}),text.ToString()))throw new Exception("Currency name parts differ from text");
 return name;
}
var unknown=Formatter("tr-TR",currency:"ZZZ");
if(Name(unknown,0d)!="ZZZ")throw new Exception("Unknown currency name changed");
var high=BigInteger.Pow(10,40);
foreach(var item in new[]{("fr-FR",1,2),("ru-RU",1,1),("ru-RU",2,2),("ru-RU",5,5),("uk-UA",1,1),("pl-PL",1,5),("pl-PL",2,2),("lt-LT",1,1),("lt-LT",2,2),("lv-LV",1,1),("lv-LV",11,0),("ro-RO",1,101)}) {
 var format=Formatter(item.Item1);if(Name(format,high+item.Item2)!=Name(format,(double)item.Item3))throw new Exception("Large exact currency plural mismatch: "+item);
}
foreach(string locale in new[]{"fr-FR","pt-BR"}){var format=Formatter(locale,2);if(Name(format,high)!=Name(format,2d))throw new Exception("Rounded large currency used singular: "+locale);}
foreach(string locale in new[]{"en-GB","de-DE","fr-FR","ru-RU","ja-JP","ar-EG","tr-TR"}) {
 var format=Formatter(locale);string[] expected=Enumerable.Range(0,10).Select(i=>Name(format,(double)i)).ToArray();
 Parallel.For(0,8,worker=>{for(int i=0;i<100;i++){int value=(worker+i)%10;if(Name(format,(double)value)!=expected[value])throw new Exception("Concurrent currency name cache mixed values");}});
 GC.Collect();GC.WaitForPendingFinalizers();if(Name(format,1d)!=expected[1])throw new Exception("Currency name formatter lost its native handle");
}
Console.WriteLine("PASS exact large currency plurals, names without extra symbols, concurrent text/parts and native handle lifetime");
