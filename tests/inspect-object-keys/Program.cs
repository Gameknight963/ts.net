using System.Globalization;

static string Keys(object value) {
    var keys = (JsArray)Js.ObjectKeys(value);
    if (Js.ObjectKeyCount(value) != keys.Length()) throw new Exception("Key count disagrees with enumeration");
    var result = new List<string>();
    for (int i = 0; i < keys.Length(); i++) result.Add((string)keys.Get(i.ToString(CultureInfo.InvariantCulture)));
    return string.Join("|", result);
}
foreach (JsArray array in new JsArray[] {new JsArray(0), new NumberArray(0), new TaggedArray(0), new ReversibleArray(0)}) {
    array.Set("4294967294", 7d);
    array.Set("3", Js.Undefined);
    array.Set("extra", 9d);
    if (Keys(array) != "3|4294967294|extra") throw new Exception("Sparse keys scanned holes or lost undefined");
    array.SetLength(4);
    if (Keys(array) != "3|extra") throw new Exception("Truncation retained a deleted index");
}
var reversed = new ReversibleArray(new object[] {1d, JsArray.HoleValue(), 2d, JsArray.HoleValue()});
reversed.Reverse();
if (Keys(reversed) != "1|3") throw new Exception("Reversed holes used physical positions");
var fn = new JsFunction((FunctionBody)(_ => Js.Undefined), null, "example", 3);
fn.Set("extra", 9d);
if (Keys(fn) != "extra") throw new Exception("Function metadata became enumerable");
if (Keys("A??") != "0|1|2") throw new Exception("String indices must use UTF-16 code units");
Console.WriteLine("PASS sparse array keys, truncation, reversal, function metadata, and UTF-16 indices");

if (args.Length != 0) {
    var assembly = System.Reflection.Assembly.LoadFile(System.IO.Path.GetFullPath(args[0]));
    if (assembly.GetTypes().Count(t => t.Name.StartsWith("K")) != 1)
        throw new Exception("Identical property orders must share their generated layout class");
    var method = assembly.GetType("Program")!.GetMethods().Single(m => m.ReturnType == typeof(object) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(bool?));
    var make = method.CreateDelegate<Func<bool?, object>>();
    var first = (TsObject)make(true);var second = (TsObject)make(false);
    if (first.GetType() != second.GetType() || ReferenceEquals(first, second) || Keys(first) != "b|a" || Keys(second) != "b|a")
        throw new Exception("Shared key metadata changed instance identity or order");
    first.Set("z", 5d);
    if (Keys(second) != "b|a") throw new Exception("Instances shared dynamic properties");
    Console.WriteLine("PASS shared property-order classes retain independent object storage");
}

var provider = new CustomKeys();
if (Js.ObjectKeyCount(provider) != 1 || provider.Calls != 1)
    throw new Exception("Custom enumeration must run once when only the key count is requested");
Console.WriteLine("PASS key counts preserve custom enumeration");

sealed class CustomKeys : TsObject {
    public int Calls;
    public override object OwnKeys() { Calls++;return new JsArray(new object[] {"provided"}); }
}
