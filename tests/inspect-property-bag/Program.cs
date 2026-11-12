using System.Globalization;

static string[] Keys(TsObject value) {
    var keys=(JsArray)Js.ObjectKeys(value);
    return Enumerable.Range(0,(int)keys.Length()).Select(i=>(string)keys.Get(i.ToString(CultureInfo.InvariantCulture))).ToArray();
}
static bool Index(string key, out uint value) =>
    uint.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value != uint.MaxValue &&
    value.ToString(CultureInfo.InvariantCulture) == key;
var actual=new Record();
var expected=new Dictionary<string,object>();
string[] names = Enumerable.Range(0,40).Select(i=>"key"+i).Concat(new[]{"", "__proto__", "0", "00", "2", "10", "4294967294", "4294967295", "-0"}).ToArray();
var random=new Random(4917);
for(int i=0;i<10000;i++) {
    string key=names[random.Next(names.Length)];
    object value=i%3==0 ? Js.Undefined : i%3==1 ? null : (object)(double)i;
    expected[key]=value;
    // This storage test needs a data property, including for the special setter name.
    if(key=="__proto__")actual.DefineOwn(key,value);else actual.Set(key,value);
    if(!actual.HasOwn(key) || !ReferenceEquals(actual.Get(key),value)) throw new Exception("Property write changed its value or presence");
    if(i%31==0) {
        var ordered=expected.Keys.Where(k=>Index(k,out _)).OrderBy(k=>uint.Parse(k,CultureInfo.InvariantCulture))
            .Concat(expected.Keys.Where(k=>!Index(k,out _))).ToArray();
        if(!Keys(actual).SequenceEqual(ordered) || Js.ObjectKeyCount(actual)!=expected.Count) throw new Exception("Property order or count changed");
    }
}
actual.Set("self",actual);expected["self"]=actual;
var clone=(TsObject)actual.CloneForSpread();
if(!Keys(clone).SequenceEqual(Keys(actual))) throw new Exception("Cloning changed property order");
foreach(var entry in expected) if(!ReferenceEquals(clone.Get(entry.Key),entry.Value)) throw new Exception("Clone lost a value");
object old=actual.Get("key39");clone.Set("key39","changed");
if(!ReferenceEquals(actual.Get("key39"),old)) throw new Exception("Clone shared its property storage");
foreach(string key in new[]{expected.Keys.First(),expected.Keys.Skip(4).First()}) {
    object value=actual.Get(key);clone.Set(key,"changed");
    if(!ReferenceEquals(actual.Get(key),value)) throw new Exception("Clone shared inline or tail storage");
}
if(!ReferenceEquals(clone.Get("self"),actual)) throw new Exception("Clone stopped being shallow");
if(actual.HasOwn("missing") || !ReferenceEquals(actual.Get("missing"),Js.Undefined)) throw new Exception("Missing property became present");
object shared=3d;
long Allocate(int count) {
    long before=GC.GetAllocatedBytesForCurrentThread();
    var record=new Record();
    for(int i=0;i<count;i++) record.Set(names[i],shared);
    long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
    GC.KeepAlive(record);
    return bytes;
}
for(int i=0;i<100;i++) Allocate(4);
if(Allocate(1)>160 || Allocate(4)>160) throw new Exception("Small property bags allocated dictionary buffers");
var numeric = new JsClassInstance(); numeric.DefineOwn("value", 0d); Js.SetNumber(numeric, "value", 1d); Js.SetNumber(numeric, "value", 2d);
object oldNumber = numeric.Get("value"); var numericCopy = new JsDynamicObject(); numeric.CopySpreadExtras(numericCopy);
Js.SetNumber(numeric, "value", 3d); numericCopy.Set("value", 4d);
if ((double)oldNumber != 2 || Js.GetNumber(numeric,"value") != 3 || Js.GetNumber(numericCopy,"value") != 4)
    throw new Exception("Numeric property cells shared state with an old value or copied store");
var custom = new CustomSetter(); custom.DefineOwn("value", 1d); Js.SetNumber(custom,"value",5d);
if (custom.Calls != 1 || Js.GetNumber(custom,"value") != 5) throw new Exception("Numeric writes bypassed a native setter override");
var dynamicSetter = new CustomDynamicSetter(); dynamicSetter.DefineOwn("value",1d); Js.SetNumber(dynamicSetter,"value",6d);
if (dynamicSetter.Calls!=1 || Js.GetNumber(dynamicSetter,"value")!=6) throw new Exception("Numeric writes bypassed a native dictionary setter");
var namespaceValue = new JsClassInstance(); namespaceValue.DefineOwn("value",0d); Js.SetNumber(namespaceValue,"value",1d); Js.SetNumber(namespaceValue,"value",2d);
Js.SealModuleNamespace(namespaceValue); bool rejected=false;
try {Js.SetNumber(namespaceValue,"value",3d);} catch {rejected=true;}
if (!rejected || Js.GetNumber(namespaceValue,"value")!=2) throw new Exception("Numeric cell writes bypassed namespace protection");
for (int i=0;i<10000;i++) Js.SetNumber(numeric,"value",i);
long numericBefore = GC.GetAllocatedBytesForCurrentThread();
for (int i=0;i<10000;i++) Js.SetNumber(numeric,"value",i);
if (GC.GetAllocatedBytesForCurrentThread()!=numericBefore || Js.GetNumber(numeric,"value")!=9999)
    throw new Exception("Repeated numeric property writes allocated or lost their value");
var mixedNumeric = new JsClassInstance(); var mixedExpected = new Dictionary<string,object>();
for (int i=0;i<10000;i++) {
    string key=names[random.Next(names.Length)];
    object value=i%4==0 ? Js.Undefined : i%4==1 ? null : i%4==2 ? (object)(double)i : "text";
    if (i%3==0 && key!="__proto__") {Js.SetNumber(mixedNumeric,key,i);value=(double)i;}
    else mixedNumeric.DefineOwn(key,value);
    mixedExpected[key]=value;
    foreach (var entry in mixedExpected)
        if (!Equals(mixedNumeric.Get(entry.Key),entry.Value)) throw new Exception("Numeric and generic writes disagreed");
}
var mixedCopy = new JsDynamicObject(); mixedNumeric.CopySpreadExtras(mixedCopy);
foreach (var entry in mixedExpected) {
    mixedCopy.Set(entry.Key,"changed");
    if (!Equals(mixedNumeric.Get(entry.Key),entry.Value)) throw new Exception("Copied numeric inline cells shared state");
}
Console.WriteLine("PASS numeric property writes, copied stores, native setter overrides, and zero loop allocations");
Console.WriteLine("PASS dynamic property order, overwrites, growth, independent clones, and small-bag allocation");
sealed class Record : TsObject { }

sealed class CustomSetter : JsClassInstance {public int Calls; public override object Set(string key, object value) {Calls++;return base.Set(key,value);}}

sealed class CustomDynamicSetter : JsDynamicObject {public int Calls; public override object Set(string key, object value) {Calls++;return base.Set(key,value);}}
