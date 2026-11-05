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
    expected[key]=value;actual.Set(key,value);
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
Console.WriteLine("PASS dynamic property order, overwrites, growth, independent clones, and small-bag allocation");
sealed class Record : TsObject { }
