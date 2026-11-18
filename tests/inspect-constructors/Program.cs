using System.Reflection;

try {
_ = typeof(Js).Assembly;
var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
var method = assembly.GetType("Program")!.GetMethods().Single(m =>
    m.ReturnType == typeof(double?) && m.GetParameters().Length == 2 &&
    (args.Length < 3 || m.Name.StartsWith(args[2], StringComparison.Ordinal)) &&
    m.GetParameters().All(p => p.ParameterType == typeof(double?)));
var kernel = method.CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 30; i++) kernel(1000, 0);
long before = GC.GetAllocatedBytesForCurrentThread();
var result = kernel(100000, 0);
long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
long limit = args.Length > 1 ? long.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 18410000;
if (result != 4999950000d || allocated > limit)
    throw new Exception($"Constructor result or allocation budget changed: {result}, {allocated}");
Console.WriteLine($"PASS 100000 typed constructor results and allocation budget: {allocated} bytes");
var native=new NativeConstructor();
if(!ReferenceEquals(Js.ConstructNumberArguments(native,1,2),native.Instance) || native.Calls!=1 ||
    !Equals(native.Arguments[0],1d) || !Equals(native.Arguments[1],2d)) throw new Exception("Native constructor dispatch changed");
Js.ConstructNumberArguments(native,null,2);
if(native.Calls!=2 || !ReferenceEquals(native.Arguments[0],Js.Undefined))throw new Exception("Missing numeric constructor argument changed");
Console.WriteLine("PASS native constructor overrides and undefined arguments");
}
catch(Exception error) {Console.Error.WriteLine(error);Environment.ExitCode=1;}
sealed class NativeConstructor : JsFunction
{
    public readonly JsDynamicObject Instance=new();
    public int Calls;
    public object[] Arguments=Array.Empty<object>();
    public NativeConstructor():base((FunctionBody)(static _=>Js.Undefined),null,"native",0){}
    public override object Construct(object[] arguments){Calls++;Arguments=arguments;return Instance;}
}
