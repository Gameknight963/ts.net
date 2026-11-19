using System.Reflection;
using System.Runtime.CompilerServices;

class Program {
 [MethodImpl(MethodImplOptions.NoInlining)]
 static (object Root, WeakReference Reference) CreateParsedRecord(bool retain) {
  object json=Js.BuiltinNamespace("JSON");
  var rows=(JsArray)Js.CallMethodArgument(Js.Get(json,"parse"),json,"[{\"payload\":null,\"id\":1}]");
  var root=(TsObject)rows.ElementAt(0);
  object payload=new JsDynamicObject();
  var reference=new WeakReference(payload);
  root.Set("payload",payload);
  if(!retain)root.DeleteOwn("payload");
  return(root,reference);
 }
 static void CheckParsedRecord(bool retain) {
  var result=CreateParsedRecord(retain);
  for(int i=0;i<4;i++){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();}
  bool alive=result.Reference.IsAlive;
  GC.KeepAlive(result.Root);
  if(alive!=retain)throw new Exception("Parsed record reference lifetime changed");
 }
 [MethodImpl(MethodImplOptions.NoInlining)]
 static (object Root, WeakReference Reference) Create(Func<object,object> make, bool restore) {
  object payload=new JsDynamicObject();
  var reference=new WeakReference(payload);
  object root=make(payload);
  var field=root.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic).Single(f=>ReferenceEquals(f.GetValue(root),payload));
  Js.DeleteProperty(root,"payload");
  if(restore)field.SetValue(root,payload);
  return (root,reference);
 }
 static void Check(Func<object,object> make,bool restore) {
  var result=Create(make,restore);
  for(int i=0;i<4;i++){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();}
  bool alive=result.Reference.IsAlive;
  GC.KeepAlive(result.Root);
  if(alive!=restore)throw new Exception(restore ? "Lifetime control did not retain the payload" : "Deleted payload is retained by a live class instance");
 }
 static void Main(string[] args) {
  _=typeof(Js).Assembly;
  var assembly=Assembly.LoadFile(Path.GetFullPath(args[0]));
  var original=Console.Out;
  try {Console.SetOut(TextWriter.Null);assembly.EntryPoint!.Invoke(null,null);}
  finally {Console.SetOut(original);}
  var make=assembly.GetType("Program")!.GetMethods().Single(m=>m.Name.StartsWith("make_f")).CreateDelegate<Func<object,object>>();
  Check(make,false);Check(make,true);
  CheckParsedRecord(false);CheckParsedRecord(true);
  Console.WriteLine("PASS deleted parsed reference is collectible with its former owner alive, and retained-reference control");
  var updates=assembly.GetType("Program")!.GetMethods().Single(m=>m.Name.StartsWith("updates_f")).CreateDelegate<Func<double?,double?>>();
  for(int i=0;i<30;i++)updates(1000);
  long before=GC.GetAllocatedBytesForCurrentThread();
  var value=updates(200000);
  long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
  if(value!=199999 || allocated>8192)throw new Exception($"Reference field numeric writes changed: {value}, {allocated} bytes");
  Console.WriteLine($"PASS reference field numeric writes allocate {allocated} bytes for 200000 updates");
  Console.WriteLine("PASS deleted class reference is collectible with its former owner alive, and retained-reference control");
 }
}
