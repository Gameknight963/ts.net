var constructor=(JsFunction)((TsObject)Js.BuiltinNamespace("Intl")).Get("PluralRules");
object Call(TsObject rules,string name,params object[] args)=>((JsFunction)rules.Get(name)).InvokeWithReceiver(rules,args);
foreach(var item in new[]{("ar","other"),("pl","many"),("uk","many"),("cs","other"),("sk","other"),("sl","other"),("lt","other"),("lv","zero"),("he","other"),("ro","other")}) {
    var rules=(TsObject)constructor.Construct(new object[]{item.Item1});
    // These decimal integers end with at least two zeros. Use their decimal
    // operands rather than converting the integer to a bounded native integer.
    foreach(double value in new[]{1e21,double.MaxValue,-double.MaxValue})
        if(!Equals(Call(rules,"select",value),item.Item2))throw new Exception("Wrong large-number category for "+item.Item1);
    if(!Equals(Call(rules,"selectRange",1d,1d),Call(rules,"select",1d)))throw new Exception("Equal range did not collapse for "+item.Item1);
    var options=new JsDynamicObject();options.Set("minimumFractionDigits",2d);options.Set("maximumFractionDigits",3d);options.Set("maximumSignificantDigits",2d);options.Set("roundingPriority","morePrecision");
    var configured=(TsObject)constructor.Construct(new object[]{item.Item1,options});
    var resolved=(TsObject)Call(configured,"resolvedOptions");
    foreach(var field in new[]{("minimumFractionDigits",2d),("maximumFractionDigits",3d),("minimumSignificantDigits",1d),("maximumSignificantDigits",2d)})
        if(!Equals(resolved.Get(field.Item1),field.Item2))throw new Exception("Missing precision metadata: "+field.Item1);
    var categories=(JsArray)resolved.Get("pluralCategories");categories.Set("0","changed");
    var fresh=(TsObject)Call(configured,"resolvedOptions");
    if(Equals(((JsArray)fresh.Get("pluralCategories")).ElementAt(0),"changed"))throw new Exception("Plural categories share mutable storage");
}
Console.WriteLine("PASS large plural operands, equal ranges, and precision metadata");
