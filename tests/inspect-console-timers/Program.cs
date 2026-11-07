using System.Globalization;
using System.Text.RegularExpressions;
var console = (TsObject)Js.BuiltinNamespace("console");
object Call(string name, params object[] args) => ((JsFunction)console.Get(name)).InvokeWithReceiver(console,args);
var output = new StringWriter(CultureInfo.InvariantCulture); var errors = new StringWriter(CultureInfo.InvariantCulture);
var previousOut = Console.Out; var previousError = Console.Error;
try {
 Console.SetOut(output); Console.SetError(errors);
 foreach(string name in new[]{"count","countReset","time","timeLog","timeEnd"}) {
  var method=(JsFunction)console.Get(name);
  if((string)method.Get("name")!=name || Js.Number(method.Get("length"))!=0)throw new Exception("Console method metadata changed");
 }
 if(!Js.IsUndefined(Call("time")))throw new Exception("time returned a value");
 Call("timeLog",Js.Undefined,"payload",7d);
 Call("timeEnd"); Call("timeEnd");
 Call("time","duplicate");Call("time","duplicate");Call("timeEnd","duplicate");
 Call("time","duplicate");Call("timeEnd","duplicate");
 Call("timeLog","missing");Call("countReset","missing");
} finally { Console.SetOut(previousOut);Console.SetError(previousError); }
var lines=output.ToString().Replace("\r\n","\n").Split('\n',StringSplitOptions.RemoveEmptyEntries);
if(lines.Length!=4)throw new Exception("Unexpected timer output: "+output);
for(int index=0;index<lines.Length;index++) {
 string label=index<2?"default":"duplicate";
 var match=Regex.Match(lines[index],"^"+label+@": ([0-9]+\.[0-9]{3})ms"+(index==0?" payload 7":"")+"$");
 if(!match.Success || double.Parse(match.Groups[1].Value,CultureInfo.InvariantCulture)<0)throw new Exception("Invalid timer output: "+lines[index]);
}
string warning=errors.ToString();
if(warning.Replace("\r\n","\n").Split('\n',StringSplitOptions.RemoveEmptyEntries).Length!=4)throw new Exception("Unexpected warning count: "+warning);
foreach(string expected in new[]{"Timer 'default' does not exist","Timer 'duplicate' already exists","Timer 'missing' does not exist","Count for 'missing' does not exist"})
 if(!warning.Contains(expected,StringComparison.Ordinal))throw new Exception("Missing diagnostic: "+expected);
Console.WriteLine("PASS console timers, elapsed output, label reuse, and missing/duplicate diagnostics");

output.GetStringBuilder().Clear(); errors.GetStringBuilder().Clear();
try {
 Console.SetOut(output);Console.SetError(errors);
 Call("count","reset");Call("countReset","reset");Call("countReset","reset");Call("count","reset");
 var label=new JsDynamicObject();int conversions=0;
 label.DefineOwn("toString",new JsFunction((FunctionBody)(_=>{conversions++;return "object-label";}),null,"toString",0));
 Call("count",label);Call("countReset",label);Call("count",label);
 if(conversions!=3)throw new Exception("Console labels were not converted exactly once per call");
} finally { Console.SetOut(previousOut);Console.SetError(previousError); }
if(output.ToString().Replace("\r\n","\n")!="reset: 1\nreset: 1\nobject-label: 1\nobject-label: 1\n" || errors.ToString()!="")throw new Exception("Counter reset or string label normalization changed");
Console.WriteLine("PASS idempotent counter reset and consistent label conversion");
if(args.Length!=0) {
 output.GetStringBuilder().Clear();errors.GetStringBuilder().Clear();
 try {
  Console.SetOut(output);Console.SetError(errors);
  var assembly=System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
  if((int)assembly.EntryPoint!.Invoke(null,null)!=0)throw new Exception("Compiled console timer fixture failed");
 } finally {Console.SetOut(previousOut);Console.SetError(previousError);}
 string text=output.ToString().Replace("\r\n","\n");
 string normalized=Regex.Replace(text,@"(?m)^(default|work|async): [0-9]+\.[0-9]{3}ms", "$1: elapsed");
 if(normalized!="default: elapsed default-data\ndefault: elapsed\nwork: elapsed payload 42\nwork: elapsed\nasync: elapsed awaited\nasync: elapsed\n" || errors.ToString()!="")throw new Exception("Compiled timer calls produced unexpected output: "+text+errors);
 Console.WriteLine("PASS compiled timer calls, detached methods, and awaited arguments");
}
