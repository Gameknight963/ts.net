using System.Reflection;
using System.Reflection.Emit;
_ = typeof(Js).Assembly;
foreach (var enumeration in new Func<object,object>[] {Js.ObjectValues, Js.ObjectEntries, Js.ObjectNumberValues}) {
    var changing=new Changing();
    var result=(JsArray)enumeration(changing);
    if(result.Length()!=1 || changing.Reads!=1 || changing.KeyReads!=1) throw new Exception("Enumeration lost getter effects or snapshot semantics");
}
var chars=(JsArray)Js.ObjectValues("A\uD83D\uDE00");
var textEntries=(JsArray)Js.ObjectEntries("A\uD83D\uDE00");
if(chars.Length()!=3 || ((string)chars.ElementAt(1))[0]!='\uD83D' || ((string)chars.ElementAt(2))[0]!='\uDE00') throw new Exception("Values used code points instead of UTF-16 units");
var pair=(JsArray)textEntries.ElementAt(1);
if((string)pair.ElementAt(0)!="1" || ((string)pair.ElementAt(1))[0]!='\uD83D') throw new Exception("String entries changed keys or units");
var assembly=Assembly.LoadFile(Path.GetFullPath(args[0]));
var counts=assembly.GetType("Program")!.GetMethods().Where(m=>m.ReturnType==typeof(double?) && m.GetParameters().Length==1 && m.GetParameters()[0].ParameterType==typeof(object)).ToArray();
if(counts.Length!=2) throw new Exception("Missing values/entries length fixtures");
foreach(var method in counts) {
    var count=method.CreateDelegate<Func<object,double?>>();
    var changing=new Changing();
    if(count(changing)!=1 || changing.Reads!=1) throw new Exception("Length shortcut skipped property reads");
}
// A foreign subclass must keep its virtual property behavior even when its
// generated parent has a fast data-only projection.
var parent=assembly.GetTypes().First(t=>t.Name.StartsWith("O") && !t.IsSealed &&
    t.GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).Count(f=>f.FieldType==typeof(double))==2 &&
    t.GetMethod("OwnValues")!.DeclaringType==t);
var dynamicAssembly=AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("EnumerationGetterProbe"),AssemblyBuilderAccess.Run);
var child=dynamicAssembly.DefineDynamicModule("Probe").DefineType("GetterProbe",TypeAttributes.Public,parent);
var reads=child.DefineField("Reads",typeof(int),FieldAttributes.Public|FieldAttributes.Static);
child.DefineDefaultConstructor(MethodAttributes.Public);
var getter=child.DefineMethod("Get",MethodAttributes.Public|MethodAttributes.Virtual,typeof(object),new[]{typeof(string)});
var il=getter.GetILGenerator();
il.Emit(OpCodes.Ldsfld,reads);il.Emit(OpCodes.Ldc_I4_1);il.Emit(OpCodes.Add);il.Emit(OpCodes.Stsfld,reads);
il.Emit(OpCodes.Ldc_R8,7d);il.Emit(OpCodes.Box,typeof(double));il.Emit(OpCodes.Ret);
child.DefineMethodOverride(getter,parent.GetMethod("Get",new[]{typeof(string)})!);
var type=child.CreateType()!;
foreach(var enumeration in new Func<object,object>[] {Js.ObjectValues,Js.ObjectEntries,Js.ObjectNumberValues}) {
    type.GetField("Reads")!.SetValue(null,0);
    var result=(JsArray)enumeration(Activator.CreateInstance(type)!);
    if(result.Length()!=2 || (int)type.GetField("Reads")!.GetValue(null)!=2) throw new Exception("Data projection bypassed subclass getters");
}
Console.WriteLine("PASS enumeration order, live presence, getter effects, UTF-16 values, and subclass guards");

sealed class Changing : TsObject {
    public int Reads,KeyReads;
    bool second=true,third;
    public override object OwnKeys() { KeyReads++;return new JsArray(new object[]{"a","b"}); }
    public override bool HasOwn(string key) => key=="a" || key=="b" && second || key=="c" && third;
    public override object Get(string key) { Reads++;if(key=="a") { second=false;third=true;return 3d; }return 4d; }
}
